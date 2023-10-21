using gtk2k.WebRTCSignaler;
using UnityEngine;

namespace Assets.Scripts
{
    internal class CameraCapture : IVideCapture
    {
        private Camera cam;
        public RenderTexture tex;

        public Texture GetTexture()
        {
            return tex;
        }

        public CameraCapture(Camera cam, int width, int height) { 
            this.cam = cam;
            tex = new RenderTexture(width, height, 24, RenderTextureFormat.BGRA32, 0);
            
        }

        public void Update()
        {
            if (cam == null) return;
            var prev = cam.targetTexture;
            cam.targetTexture = tex;
            cam.Render();
            cam.targetTexture = prev;
        }
    }
}
