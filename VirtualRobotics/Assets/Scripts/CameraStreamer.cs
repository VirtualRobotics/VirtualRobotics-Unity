using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Camera))]
public class CameraStreamer : MonoBehaviour
{
    [Header("Configuration")]
    public RenderTexture targetTexture;
    public bool enableStreaming = false;
    
    [Header("Streaming Settings (Heuristic Only)")]
    public float captureRate = 0.1f;
    
    private Camera _cam;
    private Texture2D _tempTexture;
    private float _timer;

    void Start()
    {
        _cam = GetComponent<Camera>();

        // Safety check: Get texture from camera if not assigned in Inspector
        if (targetTexture == null) targetTexture = _cam.targetTexture;
        
        if (targetTexture != null)
        {
            _cam.targetTexture = targetTexture;
            // Allocate memory for 2D texture only once at start (optimization)
            _tempTexture = new Texture2D(targetTexture.width, targetTexture.height, TextureFormat.RGB24, false);
        }
        else
        {
            Debug.LogError("[CameraStreamer] Missing Target Texture! UI preview will not work.");
            this.enabled = false;
        }
    }

    void LateUpdate()
    {
        if (_cam == null || targetTexture == null) return;

        // 1. ALWAYS: Force camera rendering to UI texture.
        _cam.targetTexture = targetTexture;
        _cam.Render();

        // 2. OPTIONALLY: Send data to Python (only when streaming is enabled)
        if (enableStreaming)
        {
            ProcessStreaming();
        }
    }

    void ProcessStreaming()
    {
        _timer += Time.deltaTime;
        if (_timer < captureRate) return;
        _timer = 0f;

        RenderTexture currentRT = RenderTexture.active;
        
        // Set our texture as the source for reading pixels
        RenderTexture.active = targetTexture;

        _tempTexture.ReadPixels(new Rect(0, 0, targetTexture.width, targetTexture.height), 0, 0);
        _tempTexture.Apply();

        RenderTexture.active = currentRT;

        byte[] jpgBytes = _tempTexture.EncodeToJPG(60); 
        
        if (TcpClientController.FrameQueue != null)
        {
            while (TcpClientController.FrameQueue.Count > 3)
            {
                TcpClientController.FrameQueue.TryDequeue(out _);
            }
            
            TcpClientController.FrameQueue.Enqueue(jpgBytes);
        }
    }
}