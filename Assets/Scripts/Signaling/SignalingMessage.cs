using Unity.WebRTC;
using UnityEngine;

namespace gtk2k.WebRTCSignaler
{
    public class SignalingMessage
    {
        public string type;
        public string sdp;
        public string candidate;
        public string sdpMid;
        public int sdpMLineIndex;

        public SignalingMessage(string type, string sdp)
        {
            Debug.Log($"=== SignalingMessage constructor(desc)");

            this.type = type;
            this.sdp = sdp;
        }

        public SignalingMessage(string candidate, string sdpMid, int sdpMLineIndex)
        {
            Debug.Log($"=== SignalingMessage constructor(cand)");

            type = "candidate";
            this.candidate = candidate;
            this.sdpMid = sdpMid;
            this.sdpMLineIndex = sdpMLineIndex;
        }

        public static SignalingMessage fromDesc(ref RTCSessionDescription desc)
        {
            Debug.Log($"=== SignalingMessage fromDesc()");

            return new SignalingMessage(desc.type.ToString().ToLower(), desc.sdp);
        }

        public static SignalingMessage fromCand(RTCIceCandidate cand)
        {
            Debug.Log($"=== SignalingMessage fromCand()");

            return new SignalingMessage(cand.Candidate, cand.SdpMid, cand.SdpMLineIndex.Value);
        }

        public RTCSessionDescription toDesc()
        {
            Debug.Log($"=== SignalingMessage toDesc()");

            return new RTCSessionDescription
            {
                type = type == "offer" ? RTCSdpType.Offer :
                        type == "answer" ? RTCSdpType.Answer :
                        type == "pranswer" ? RTCSdpType.Pranswer :
                        RTCSdpType.Rollback,
                sdp = sdp
            };
        }

        public RTCIceCandidate toCand()
        {
            Debug.Log($"=== SignalingMessage toCand()");

            var candidateInfo = new RTCIceCandidateInit
            {
                candidate = candidate,
                sdpMid = sdpMid,
                sdpMLineIndex = sdpMLineIndex
            };
            var cand = new RTCIceCandidate(candidateInfo);
            return cand;
        }
    }
}