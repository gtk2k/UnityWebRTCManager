using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Unity.VisualScripting;
using Unity.WebRTC;
using UnityEngine;

namespace gtk2k.WebRTCSignaler
{
    internal class UDPSignaler : ISignaler
    {
        public event Action<string> OnConnect;
        public event Action<ushort, string, string> OnDisconnect;
        public event Action<RTCSessionDescription, string> OnDesc;
        public event Action<RTCIceCandidate, string> OnCand;
        public event Action<string, string> OnError;

        private SynchronizationContext ctx;
        private string ipAddress;
        private int port;
        private UdpClient connectionObserver;
        private UdpClient connectionNotifer;
        private UdpClient signaler;

        private JsonSerializerSettings jsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        public SignalerType GetSignalerType()
        {
            return SignalerType.UDP;
        }

        public UDPSignaler(string ipAddress, int port, SynchronizationContext ctx)
        {
            Debug.Log($"=== UDPSignaling constructor({ipAddress}, {port})");

            this.ipAddress = ipAddress;
            this.port = port;

            if (string.IsNullOrEmpty(ipAddress))
            {
                setupConnectionObserver();
                setupConnectionNotifer();
            }
            setupSignaler();
        }

        public void Start() { }
        public void Stop()
        {
            connectionObserver?.Close();
            connectionObserver = null;
            connectionNotifer?.Close();
            connectionNotifer = null;
            signaler.Close();
            signaler = null;
        }

        private void setupConnectionObserver()
        {
            Debug.Log($"=== UDPSignaling setupConnectionObserver");

            connectionObserver = new UdpClient(port + 1);
            connectionObserver.BeginReceive(OnConnectionNotify, null);
        }

        private void OnConnectionNotify(IAsyncResult ar)
        {
            Debug.Log($"=== UDPSignaling OnConnectionNotify");

            if (connectionObserver == null) return;
            var ep = new IPEndPoint(IPAddress.Any, 0);
            connectionObserver.EndReceive(ar, ref ep);
            var ips = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (var ip in ips)
            {
                if (ep.Address.Equals(ip))
                {
                    return;
                }
            }
            OnConnect?.Invoke(ep.Address.ToString());
            connectionObserver.BeginReceive(OnConnectionNotify, null);
        }

        private void setupConnectionNotifer()
        {
            Debug.Log($"=== UDPSignaling setupConnectionNotifer");

            var connectData = Encoding.ASCII.GetBytes("connect");
            var broadcastEp = new IPEndPoint(IPAddress.Broadcast, port + 1);
            connectionNotifer = new UdpClient();
            connectionNotifer.EnableBroadcast = true;
            connectionNotifer.BeginSend(connectData, connectData.Length, broadcastEp, OnBroadcast, null);
        }

        private void OnBroadcast(IAsyncResult ar)
        {
            Debug.Log($"=== UDPSignaling OnBroadcast");

            connectionNotifer.EndSend(ar);
            connectionNotifer.Close();
            connectionNotifer.Dispose();
            connectionNotifer = null;
        }

        private void setupSignaler()
        {
            Debug.Log($"=== UDPSignaling setupSignaler");

            signaler = new UdpClient(port);
            signaler.BeginReceive(OnSignalingMessage, null);
        }

        private void OnSignalingMessage(IAsyncResult ar)
        {
            Debug.Log($"=== UDPSignaling OnSignalingMessage");

            try
            {
                if (signaler == null) return;
                var ep = new IPEndPoint(0, 0);
                var rawData = signaler.EndReceive(ar, ref ep);
                Debug.Log($"=== rawDeata:{rawData.Length}");
                if (ipAddress != null)
                {
                    if (ep.Address.ToString() != ipAddress)
                    {
                        return;
                    }
                }
                var data = Encoding.UTF8.GetString(rawData);
                var msg = JsonConvert.DeserializeObject<SignalingMessage>(data, jsonSettings);
                switch (msg.type)
                {
                    case "offer": case "answer": OnDesc?.Invoke(msg.toDesc(), ep.Address.ToString()); break;
                    case "candidate": OnCand?.Invoke(msg.toCand(), ep.Address.ToString()); break;
                    case "disconnect": OnDisconnect?.Invoke(0, null, ep.Address.ToString()); break;
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex.Message, null);
            }
        }

        public void Send(string ipAddress, ref RTCSessionDescription desc)
        {
            Debug.Log($"=== UDPSignaling Send(desc)");

            var msg = SignalingMessage.fromDesc(ref desc);
            Send(ipAddress, msg);
        }

        public void Send(string ipAddress, RTCIceCandidate cand)
        {
            Debug.Log($"=== UDPSignaling Send(cand)");

            var msg = SignalingMessage.fromCand(cand);
            Send(ipAddress, msg);
        }

        private void Send(string ipAddress, SignalingMessage msg)
        {
            Debug.Log($"=== UDPSignaling Send(SignalingMessage)");

            var ep = new IPEndPoint(IPAddress.Parse(ipAddress), 8889);
            var data = JsonConvert.SerializeObject(msg, jsonSettings);
            var rawData = Encoding.UTF8.GetBytes(data);
            signaler.BeginSend(rawData, rawData.Length, ep, OnSend, null);
        }

        private void OnSend(IAsyncResult ar)
        {
            Debug.Log($"=== UDPSignaling OnSend");

            signaler.EndSend(ar);
        }
    }
}
