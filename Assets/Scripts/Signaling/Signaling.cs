using System;
using Unity.WebRTC;
using UnityEngine;

namespace gtk2k.WebRTCSignaler
{
    public class Signaling
    {
        public event Action<string> OnConnect;
        public event Action<ushort, string, string> OnDisconnect;
        public event Action<RTCSessionDescription, string> OnDesc;
        public event Action<RTCIceCandidate, string> OnCand;
        public event Action<string, string> OnError;

        private string signalingURL;
        private ISignaler signaler;
        private ProtocolType protocolType;
        private string ipAddress;
        private int port;

        public Signaling(ProtocolType protocolType, string ipAddress, int port)
        {
            Debug.Log($"=== Signaling constructor({protocolType}, {ipAddress}, {port})");

            this.protocolType = protocolType;
            this.ipAddress = ipAddress;
            this.port = port;
        }

        public void Connect()
        {
            Debug.Log($"=== Signaling Connect()");

            switch (protocolType)
            {
                case ProtocolType.WebSocket:
                    signalingURL = $"ws://{ipAddress}:{port}";
                    signaler = new WebSocketSignalerClient(signalingURL);
                    signaler.OnConnect += OnConnect;
                    signaler.OnDisconnect += OnDisconnect;
                    signaler.OnDesc += OnDesc;
                    signaler.OnCand += OnCand;
                    signaler.OnError += OnError;
                    break;
                case ProtocolType.UDP:
                    signaler = new UDPSignaler(ipAddress, port);
                    signaler.OnConnect += OnConnect;
                    signaler.OnDisconnect += OnDisconnect;
                    signaler.OnDesc += OnDesc;
                    signaler.OnCand += OnCand;
                    signaler.OnError += OnError;
                    break;
            }
        }

        public void Disconnect()
        {
            Debug.Log($"=== Signaling Disconnect()");

            signaler?.Stop();
            signaler = null;
        }

        private void serverStart()
        {
            Debug.Log($"=== Signaling serverStart()");

            var url = new Uri(signalingURL);
            signaler = new WebSocketSignalerServer(url.Port);
            signaler.OnConnect += onConnect;
            signaler.OnDisconnect += onDisconnect;
            signaler.OnDesc += onDesc;
            signaler.OnCand += onCand;
            signaler.OnError += onError;
            signaler.Start();
        }

        private void signalerStop()
        {
            Debug.Log($"=== Signaling signalerStop()");

            signaler?.Stop();
            signaler = null;
        }

        private void onConnect(string id = null)
        {
            Debug.Log($"=== Signaling onConnect [{id}]");

            OnConnect?.Invoke(id);
        }

        private void onDisconnect(ushort code, string reason, string id = null)
        {
            Debug.Log($"=== Signaling onDisconnect [{id}]");

            if (protocolType == ProtocolType.WebSocket && code == 1006 && signaler.GetSignalerType() == SignalerType.WebSocketClient)
            {
                signalerStop();
                serverStart();
            }
            else
            {
                OnDisconnect?.Invoke(code, reason, id);
            }
        }

        private void onDesc(RTCSessionDescription desc, string id = null)
        {
            Debug.Log($"=== Signaling onDesc [{id}]");

            OnDesc?.Invoke(desc, id);
        }

        private void onCand(RTCIceCandidate cand, string id = null)
        {
            Debug.Log($"=== Signaling onCand [{id}]");

            OnCand?.Invoke(cand, id);
        }

        private void onError(string errorMessage, string id = null)
        {
            Debug.Log($"=== Signaling onError [{id}]");

            OnError?.Invoke(errorMessage, id);
        }

        public void Send(string id, ref RTCSessionDescription desc)
        {
            Debug.Log($"=== Signaling Send(desc) [{id}]");

            signaler.Send(id, ref desc);
        }

        public void Send(string id, RTCIceCandidate cand)
        {
            Debug.Log($"=== Signaling Send(cand) [{id}]");

            signaler.Send(id, cand);
        }
    }
}
