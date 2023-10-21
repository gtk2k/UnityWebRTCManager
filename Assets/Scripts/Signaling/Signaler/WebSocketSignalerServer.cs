using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using Unity.WebRTC;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace gtk2k.WebRTCSignaler
{
    public class WebSocketSignalerServer : ISignaler
    {
        public event Action<string> OnConnect;
        public event Action<ushort, string, string> OnDisconnect;
        public event Action<RTCSessionDescription, string> OnDesc;
        public event Action<RTCIceCandidate, string> OnCand;
        public event Action<string, string> OnError;

        private int port;
        private SynchronizationContext ctx;
        private WebSocketServer server;

        private Dictionary<string, WebSocket> clients;

        public SignalerType GetSignalerType()
        {
            return SignalerType.WebSocketServer;
        }

        private class signalingBehaviour : WebSocketBehavior
        {
            public event Action<string> OnClientConnect;
            public event Action<ushort, string, string> OnClientDisconnect;
            public event Action<string, string> OnClientMessage;
            public event Action<string, string> OnClientError;

            protected override void OnOpen()
            {
                Debug.Log($"=== signalingBehaviour OnOpen [{ID}]");

                OnClientConnect?.Invoke(ID);
            }

            protected override void OnMessage(MessageEventArgs e)
            {
                Debug.Log($"=== signalingBehaviour OnOpen [{ID}]");

                OnClientMessage?.Invoke(e.Data, ID);
            }

            protected override void OnClose(CloseEventArgs e)
            {
                Debug.Log($"=== signalingBehaviour OnClose [{ID}]");

                OnClientDisconnect?.Invoke(e.Code, e.Reason, ID);
            }

            protected override void OnError(ErrorEventArgs e)
            {
                Debug.Log($"=== signalingBehaviour OnError [{ID}]");

                OnClientError.Invoke(e.Message, ID);
            }
        }

        public WebSocketSignalerServer(int port = 8998)
        {
            Debug.Log($"=== SignalingServer constructor({port})");

            ctx = SynchronizationContext.Current;
            this.port = port;
        }

        public void Start()
        {
            Debug.Log($"=== SignalingServer Start()");

            this.clients = new Dictionary<string, WebSocket>();
            server = new WebSocketServer(port);
            server.AddWebSocketService<signalingBehaviour>("/", behaviour =>
            {
                behaviour.OnClientConnect += (id) =>
                {
                    ctx.Post(_ =>
                    {
                        disconnectClient(id);
                        OnConnect.Invoke(id);
                    }, null);
                };
                behaviour.OnClientDisconnect += (code, reason, id) =>
                {
                    ctx.Post(_ =>
                    {
                        disconnectClient(id);
                        OnDisconnect.Invoke(code, reason, id);
                    }, null);
                };
                behaviour.OnClientMessage += (id, data) =>
                {
                    ctx.Post(_ =>
                    {
                        var msg = JsonConvert.DeserializeObject<SignalingMessage>(data);
                        switch (msg.type)
                        {
                            case "offer": case "answer": OnDesc?.Invoke(msg.toDesc(), id); break;
                            case "candidate": OnCand?.Invoke(msg.toCand(), id); break;
                        }
                    }, null);
                };
                behaviour.OnClientError += (id, errorMessage) =>
                {
                    ctx.Post(_ =>
                    {
                        OnError.Invoke(id, errorMessage);
                    }, null);
                };
            });
            server.Start();
        }

        public void Stop()
        {
            Debug.Log($"=== SignalingServer Stop()");

            server?.Stop();
            server = null;
            clients?.Clear();
            clients = null;
        }

        private void disconnectClient(string id)
        {
            Debug.Log($"=== SignalingServer disconnectClient [{id}]");

            if (clients.ContainsKey(id))
            {
                clients[id].Close();
                clients.Remove(id);
            }
        }

        public void Send(string id, ref RTCSessionDescription desc)
        {
            Debug.Log($"=== SignalingServer Send(desc) [{id}]");

            var json = JsonUtility.ToJson(SignalingMessage.fromDesc(ref desc));
            server.WebSocketServices["/"].Sessions[id].WebSocket.Send(json);
        }

        public void Send(string id, RTCIceCandidate cand)
        {
            Debug.Log($"=== SignalingServer Send(cand) [{id}]");

            var json = JsonUtility.ToJson(SignalingMessage.fromCand(cand));
            server.WebSocketServices["/"].Sessions[id].WebSocket.Send(json);
        }
    }
}