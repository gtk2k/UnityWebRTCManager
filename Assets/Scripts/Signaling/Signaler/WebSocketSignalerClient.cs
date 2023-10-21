using Newtonsoft.Json;
using System;
using System.Threading;
using Unity.WebRTC;
using WebSocketSharp;
using UnityEngine;

namespace gtk2k.WebRTCSignaler
{
    public class WebSocketSignalerClient : ISignaler
    {
        public event Action<string> OnConnect;
        public event Action<RTCSessionDescription, string> OnDesc;
        public event Action<RTCIceCandidate, string> OnCand;
        public event Action<ushort, string, string> OnDisconnect;
        public event Action<string, string> OnError;

        private string signalingURL;

        private WebSocket ws;
        private SynchronizationContext ctx;

        private JsonSerializerSettings jsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        public SignalerType GetSignalerType()
        {
            return SignalerType.WebSocketClient;
        }

        public WebSocketSignalerClient(string signalingURL)
        {
            Debug.Log($"=== SignalingClient constructor({signalingURL})");

            ctx = SynchronizationContext.Current;
            this.signalingURL  = signalingURL;
        }

        public void Start()
        {
            Debug.Log($"=== SignalingClient Start()");

            ws = new WebSocket(signalingURL);
            ws.OnError += Ws_OnError;
            ws.OnClose += Ws_OnClose;
            ws.OnMessage += Ws_OnMessage;
            ws.OnOpen += Ws_OnOpen;
            ws.Connect();
        }

        public void Stop()
        {
            Debug.Log($"=== SignalingClient Stop()");

            ws?.Close();
            ws = null;
        }

        private void Ws_OnOpen(object sender, EventArgs e)
        {
            Debug.Log($"=== SignalingClient Ws_OnOpen()");

            ctx.Post(_ =>
            {
                OnConnect?.Invoke(null);
            }, null);
        }

        private void Ws_OnMessage(object sender, MessageEventArgs e)
        {
            Debug.Log($"=== SignalingClient Ws_OnMessage()");

            ctx.Post(_ =>
            {
                var msg = JsonConvert.DeserializeObject<SignalingMessage>(e.Data);
                switch (msg.type)
                {
                    case "offer": case "answer": OnDesc?.Invoke(msg.toDesc(), null); break;
                    case "candidate": OnCand?.Invoke(msg.toCand(), null); break;
                }
            }, null);
        }

        private void Ws_OnClose(object sender, CloseEventArgs e)
        {
            Debug.Log($"=== SignalingClient Ws_OnClose()");

            ctx.Post(_ =>
            {
                OnDisconnect?.Invoke(e.Code, e.Reason, null);
            }, null);
        }

        private void Ws_OnError(object sender, ErrorEventArgs e)
        {
            Debug.Log($"=== SignalingClient Ws_OnError()");

            ctx.Post(_ =>
            {
                OnError?.Invoke(e.Message, null);
            }, null);
        }

        public void Send(string id, ref RTCSessionDescription desc)
        {
            Debug.Log($"=== SignalingClient Send(desc) [{id}]");

            Send(SignalingMessage.fromDesc(ref desc));
        }

        public void Send(string id, RTCIceCandidate cand)
        {
            Debug.Log($"=== SignalingClient Send(cand) [{id}]");

            Send(SignalingMessage.fromCand(cand));
        }

        private void Send(SignalingMessage msg)
        {
            Debug.Log($"=== SignalingClient Send(signalingMessage)");

            var data = JsonConvert.SerializeObject(msg, jsonSettings);
            ws.Send(data);
        }
    }
}
