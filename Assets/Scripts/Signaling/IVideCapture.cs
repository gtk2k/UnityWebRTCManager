using UnityEngine;

namespace gtk2k.WebRTCSignaler
{
    internal interface IVideCapture
    {
        Texture GetTexture();

        void Update();
    }
}
