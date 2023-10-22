using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.Events;

namespace gtk2k.WebRTCSignaler
{
    public class WebRTCManager : MonoBehaviour
    {
        [SerializeField] private ProtocolType signalingProtocolType;
        [SerializeField] private string signalingIPAddress;
        [SerializeField] private int signalingPort;
        [SerializeField] private bool isSendVideoStreaming;
        [SerializeField] private bool isSendAudioStreaming;
        [SerializeField] private Camera streamingCamera;
        [SerializeField] private int streamingWidth;
        [SerializeField] private int streamingHeight;
        [SerializeField] private AudioSource audioSource;

        public UnityEvent<string, Texture> OnVideoTexture;
        public UnityEvent<string, float[], int, int> OnAudioData;

        private IVideCapture videoCapture;
        private Signaling signaling;
        private Dictionary<string, Peer> peers;

        private RTCConfiguration config = new RTCConfiguration
        {
            iceServers = new[] { new RTCIceServer { urls = new[] { "stun:stun.l.google.com" } } }
        };

        void Start()
        {
            Debug.Log($"=== WebRTCManager Start()");

            StartCoroutine(WebRTC.Update());

            if (isSendVideoStreaming)
            {
                if(streamingCamera == null)
                {
                    videoCapture = new ScreenCapture(streamingWidth, streamingHeight);
                }
                else
                {
                    videoCapture = new CameraCapture(streamingCamera, streamingWidth, streamingHeight);
                }
            }
            else
            {
                videoCapture = null;
            }

            if (!isSendAudioStreaming)
            {
                audioSource = null;
            }

            peers = new Dictionary<string, Peer>();
        }

        private void Update()
        {
            videoCapture?.Update();
        }

        public void Connect()
        {
            Debug.Log($"=== WebRTCManager Connect()");

            signaling = new Signaling(signalingProtocolType, signalingIPAddress, signalingPort);
            signaling.OnConnect += Signaling_OnConnect;
            signaling.OnDesc += Signaling_OnDesc;
            signaling.OnCand += Signaling_OnCand;
            signaling.OnDisconnect += Signaling_OnDisconnect;
            signaling.OnError += Signaling_OnError;
            signaling.Connect();
        }

        public void Close()
        {
            Debug.Log($"=== WebRTCManager Close()");

            signaling.Disconnect();
            signaling = null;
        }

        private void Signaling_OnConnect(string id)
        {
            Debug.Log($"=== WebRTCManager Signaling_OnConnect [{id}]");
        }

        private void Signaling_OnDisconnect(ushort code, string reason, string id)
        {
            Debug.Log($"=== WebRTCManager Signaling_OnDisconnect [{id}] > code:{code}, reason:{reason}");

            if (peers.ContainsKey(id))
            {
                peers[id].Close();
                peers.Remove(id);
            }
        }

        private void Signaling_OnDesc(RTCSessionDescription desc, string id)
        {
            Debug.Log($"=== WebRTCManager Signaling_OnDesc [{id}]");

            Peer peer;
            if (!peers.ContainsKey(id))
            {
                peer = CreatePeer(id);
            }
            else
            {
                peer = peers[id];
            }
            StartCoroutine(SetDesc(peer, Side.Remote, desc));
        }

        private Peer CreatePeer(string id)
        {
            Debug.Log($"=== WebRTCManager CreatePeer [{id}]");

            var peer = new Peer(id, config, videoCapture.GetTexture(), audioSource);
            peers.Add(id, peer);
            peer.OnIceCandidate += Peer_OnIceCandidate;
            peer.OnVideoTexture += Peer_OnVideoTexture;
            peer.OnAudioData += Peer_OnAudioData;
            return peer;
        }

        private void Peer_OnAudioData(string id, float[] data, int channels, int sampleRate)
        {
            //Debug.Log($"=== WebRTCManager Peer_OnAudioData [{id}]");

            OnAudioData?.Invoke(id, data, channels, sampleRate);
        }

        private void Peer_OnVideoTexture(string id, Texture texture)
        {
            Debug.Log($"=== WebRTCManager Peer_OnVideoTexture [{id}]");

            OnVideoTexture?.Invoke(id, texture);
        }

        private void Peer_OnIceCandidate(string id, RTCIceCandidate cand)
        {
            Debug.Log($"=== WebRTCManager Peer_OnIceCandidate [{id}]");

            signaling.Send(id, cand);
        }

        private void Signaling_OnCand(RTCIceCandidate cand, string id)
        {
            Debug.Log($"=== WebRTCManager Signaling_OnCand [{id}]");

            peers[id].AddIceCandidate(cand);
        }

        private void Signaling_OnError(string errorMessage, string id)
        {
            Debug.LogError($"=== WebRTCManager Signaling_OnError [{id}] > {errorMessage}");
        }

        private IEnumerator CreateDesc(Peer peer, RTCSdpType type)
        {
            Debug.Log($"=== WebRTCManager CreateDesc()");

            var op = type == RTCSdpType.Offer ? peer.CreateOffer() : peer.CreateAnswer();
            yield return op;
            if (op.IsError)
            {
                Debug.LogError($"=== Create {type} Error > {op.Error.message}");
                yield break;
            }
            yield return StartCoroutine(SetDesc(peer, Side.Local, op.Desc));
        }

        private IEnumerator SetDesc(Peer peer, Side side, RTCSessionDescription desc)
        {
            Debug.Log($"=== WebRTCManager SetDesc()");

            var op = side == Side.Local ? peer.SetLocalDescription(ref desc) : peer.SetRemoteDescription(ref desc);
            yield return op;
            if (op.IsError)
            {
                Debug.LogError($"=== Set {desc.type} Error > {op.Error.message}");
                yield break;
            }
            if (side == Side.Local)
            {
                signaling.Send(peer.id, ref desc);
            }
            else if (desc.type == RTCSdpType.Offer)
            {
                yield return StartCoroutine(CreateDesc(peer, RTCSdpType.Answer));
            }
        }
    }
}