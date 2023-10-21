using System;
using Unity.WebRTC;

namespace gtk2k.WebRTCSignaler
{
    public interface ISignaler
    {
        event Action<string> OnConnect;
        event Action<ushort, string, string> OnDisconnect;
        event Action<RTCSessionDescription, string> OnDesc;
        event Action<RTCIceCandidate, string> OnCand;
        event Action<string, string> OnError;

        void Start();
        void Stop();

        SignalerType GetSignalerType();

        void Send(string id, ref RTCSessionDescription desc);
        void Send(string id, RTCIceCandidate cand);
    }
}