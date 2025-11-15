using System.IO;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraCapture : MonoBehaviour
{
    public Camera cam;
    public float captureRate = 0.5f;
    public string fileName = "agent_frame.png";

    private float _timer;
    private Texture2D _texture;

    void Start()
    {
        if (cam == null) cam = GetComponent<Camera>();

        if (cam.targetTexture == null)
        {
            Debug.LogError("CameraCapture: Camera target texture is null!");
            enabled = false;
            return;
        }
        _texture = new Texture2D(cam.targetTexture.width, cam.targetTexture.height, TextureFormat.RGB24, false);
    }

    void LateUpdate()
    {
        _timer += Time.deltaTime;
        if (_timer < captureRate) return;

        _timer = 0f;
        RenderTexture renderTexture = cam.targetTexture;
        if (renderTexture is null) return;

        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = renderTexture;
        
        cam.Render();
        
        _texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        _texture.Apply();
        
        RenderTexture.active = currentRT;
        
        byte[] bytes = _texture.EncodeToPNG();
        string path = Path.Combine(Application.persistentDataPath, fileName);
        File.WriteAllBytes(path, bytes);
        
        Debug.Log("[CameraCapture] Zapisano klatkę do: " + path);
    }
}
