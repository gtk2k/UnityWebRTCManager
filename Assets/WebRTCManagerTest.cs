using gtk2k.WebRTCSignaler;
using UnityEngine;

public class WebRTCManagerTest : MonoBehaviour
{
    [SerializeField] private WebRTCManager webRTCManager;

    // Start is called before the first frame update
    void Start()
    {
        webRTCManager.OnVideoTexture.AddListener(OnVideoTexture);
        webRTCManager.OnAudioData.AddListener(OnAudioData);
        webRTCManager.Connect();
    }

    private void OnVideoTexture(string id, Texture tex)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.GetComponent<Renderer>().material.mainTexture = tex;
    }

    private void OnAudioData(string id, float[] data, int channels, int sampleRate)
    {
        // TODO
    }
}
