using System;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.Windows.WebCam;

namespace gtk2k.WebRTCSignaler
{
    public class Peer
    {
        public event Action<string, RTCIceCandidate> OnIceCandidate;
        public event Action<string, Texture> OnVideoTexture;
        public event Action<string, RTCIceGatheringState> OnGatheringStateChange;
        public event Action<string, RTCPeerConnectionState> OnConnectionStateChange;
        public event Action<string, float[], int, int> OnAudioData;

        public string id;
        private RTCPeerConnection pc;
        
        public Peer(string id, RTCConfiguration? config, Texture sendTexture, AudioSource sendAudio)
        {
            Debug.Log($"=== Peer constructor({id}, {config})");

            this.id = id;
            if(config == null)
            {
                pc = new RTCPeerConnection();
            }
            else
            {
                var val = config.Value;
                pc = new RTCPeerConnection(ref val);
            }
            pc.OnIceGatheringStateChange = state =>
            {
                Debug.Log($"=== Peer pc OnIceGatheringStateChange [{id}] > {state}");

                OnGatheringStateChange?.Invoke(id, state);
            };
            pc.OnConnectionStateChange = state =>
            {
                Debug.Log($"=== Peer pc OnConnectionStateChange [{id}] > {state}");

                OnConnectionStateChange?.Invoke(id, state); 
            };
            pc.OnIceCandidate = cand =>
            {
                Debug.Log($"=== Peer pc OnIceCandidate [{id}]");

                if (string.IsNullOrEmpty(cand.Candidate)) return;
                OnIceCandidate?.Invoke(id, cand);
            };
            pc.OnTrack = evt =>
            {
                Debug.Log($"=== Peer pc OnTrack [{id}]");

                if (evt.Track is VideoStreamTrack videoTrack)
                {
                    Debug.Log($"=== Peer pc OnVideoTrack [{id}]");

                    videoTrack.OnVideoReceived += VideoTrack_OnVideoReceived;
                }
                else if (evt.Track is AudioStreamTrack audioTrack)
                {
                    Debug.Log($"=== Peer pc OnAudioTrack [{id}]");

                    audioTrack.onReceived += AudioTrack_onReceived;
                }
            };

            if (sendTexture == null && sendAudio == null)
            {
                if (sendTexture != null)
                {
                    var videoTrack = new VideoStreamTrack(sendTexture);
                    pc.AddTrack(videoTrack);
                }
                if (sendAudio != null)
                {
                    var audioTrack = new AudioStreamTrack(sendAudio);
                    pc.AddTrack(audioTrack);
                }
            }
            else
            {
                var videoTransceiver = pc.AddTransceiver(TrackKind.Video);
                var audioTransceiver = pc.AddTransceiver(TrackKind.Audio);
                videoTransceiver.Direction = RTCRtpTransceiverDirection.RecvOnly;
                audioTransceiver.Direction = RTCRtpTransceiverDirection.RecvOnly;
            }
        }

        public void Close()
        {
            Debug.Log($"=== Peer Close [{id}]");

            pc?.Close();
            pc = null;
        }

        private void AudioTrack_onReceived(float[] data, int channels, int sampleRate)
        {
            //Debug.Log($"=== Peer AudioTrack_onReceived [{id}]");

            OnAudioData?.Invoke(id, data, channels, sampleRate);
        }

        private void VideoTrack_OnVideoReceived(Texture renderer)
        {
            Debug.Log($"=== Peer VideoTrack_OnVideoReceived [{id}]");

            OnVideoTexture?.Invoke(id, renderer);
        }

        public RTCSessionDescriptionAsyncOperation CreateOffer()
        {
            Debug.Log($"=== Peer CreateOffer [{id}]");

            var op = pc.CreateOffer();
            return op;
        }

        public RTCSessionDescriptionAsyncOperation CreateAnswer()
        {
            Debug.Log($"=== Peer CreateAnswer [{id}]");

            var op = pc.CreateAnswer();
            return op;
        }

        public RTCSetSessionDescriptionAsyncOperation SetLocalDescription(ref RTCSessionDescription desc)
        {
            Debug.Log($"=== Peer SetLocalDescription [{id}]");

            var op = pc.SetLocalDescription(ref desc);
            return op;
        }

        public RTCSetSessionDescriptionAsyncOperation SetRemoteDescription(ref RTCSessionDescription desc)
        {
            Debug.Log($"=== Peer SetRemoteDescription [{id}]");

            var op = pc.SetRemoteDescription(ref desc);
            return op;
        }

        public void AddIceCandidate(RTCIceCandidate cand)
        {
            Debug.Log($"=== Peer AddIceCandidate [{id}]");

            pc.AddIceCandidate(cand);
        }
    }
}
