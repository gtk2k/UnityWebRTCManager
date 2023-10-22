using UnityEngine;

namespace gtk2k.WebRTCSignaler
{
    internal class ScreenCapture : IVideCapture
    {
        public RenderTexture tex;

        public Texture GetTexture()
        {
            return tex;
        }

        public ScreenCapture(int width, int height)
        {
            Debug.Log($"=== ScreenCapture constructor > width: {width}, height: {height}");
            tex = new RenderTexture(width, height, 0, RenderTextureFormat.BGRA32, 0);
        }

        public void Update()
        {
            UnityEngine.ScreenCapture.CaptureScreenshotIntoRenderTexture(tex);
        }
    }
}
