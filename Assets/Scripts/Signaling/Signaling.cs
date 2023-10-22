using System;
using System.Threading;
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

        private SynchronizationContext ctx;
        private string signalingURL;
        private ISignaler signaler;
        private ProtocolType protocolType;
        private string ipAddress;
        private int port;

        public Signaling(ProtocolType protocolType, string ipAddress, int port, SynchronizationContext ctx)
        {
            Debug.Log($"=== Signaling constructor({protocolType}, {ipAddress}, {port})");

            this.ctx = ctx;
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
                    signaler = new WebSocketSignalerClient(signalingURL, ctx);
                    signaler.OnConnect += OnConnect;
                    signaler.OnDisconnect += OnDisconnect;
                    signaler.OnDesc += OnDesc;
                    signaler.OnCand += OnCand;
                    signaler.OnError += OnError;
                    break;
                case ProtocolType.UDP:
                    signaler = new UDPSignaler(ipAddress, port, ctx);
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
            ctx.Post(_ =>
            {

                Debug.Log($"=== Signaling onConnect [{id}]");

                OnConnect?.Invoke(id);
            }, null);
        }

        private void onDisconnect(ushort code, string reason, string id = null)
        {
            ctx.Post(_ =>
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
            }, null);
        }

        private void onDesc(RTCSessionDescription desc, string id = null)
        {
            ctx.Post(_ =>
            {
                Debug.Log($"=== Signaling onDesc [{id}]");

                OnDesc?.Invoke(desc, id);
            }, null);
        }

        private void onCand(RTCIceCandidate cand, string id = null)
        {
            ctx.Post(_ =>
            {
                Debug.Log($"=== Signaling onCand [{id}]");

                OnCand?.Invoke(cand, id);
            }, null);
        }

        private void onError(string errorMessage, string id = null)
        {
            ctx.Post(_ =>
            {
                Debug.Log($"=== Signaling onError [{id}]");

                OnError?.Invoke(errorMessage, id);
            }, null);
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
