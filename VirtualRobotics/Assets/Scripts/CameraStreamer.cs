using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Camera))]
public class CameraStreamer : MonoBehaviour
{
    [Header("Konfiguracja")]
    public RenderTexture targetTexture; // Tutaj wrzuć "mazeview"
    public bool enableStreaming = false; // To będzie przełączał GameModeManager
    
    [Header("Ustawienia Streamingu (tylko Heurystyka)")]
    public float captureRate = 0.1f; // Co ile sekund wysyłać klatkę do Pythona
    
    private Camera _cam;
    private Texture2D _tempTexture;
    private float _timer;

    void Start()
    {
        _cam = GetComponent<Camera>();

        // Zabezpieczenie: Pobierz teksturę z kamery, jeśli nie przypisano w inspectorze
        if (targetTexture == null) targetTexture = _cam.targetTexture;
        
        if (targetTexture != null)
        {
            _cam.targetTexture = targetTexture;
            // Alokujemy pamięć na teksturę 2D tylko raz na starcie (optymalizacja)
            _tempTexture = new Texture2D(targetTexture.width, targetTexture.height, TextureFormat.RGB24, false);
        }
        else
        {
            Debug.LogError("[CameraStreamer] Brak Target Texture! Podgląd UI nie zadziała.");
            this.enabled = false;
        }
    }

    void LateUpdate()
    {
        if (_cam == null || targetTexture == null) return;

        // 1. ZAWSZE: Wymuszamy renderowanie kamery na teksturę UI.
        // Dzięki temu w trybie RL obraz będzie widoczny na ekranie.
        _cam.targetTexture = targetTexture;
        _cam.Render();

        // 2. OPCJONALNIE: Wysyłamy dane do Pythona (tylko gdy włączony streaming)
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

        // Zapamiętujemy aktualną aktywną teksturę Unity
        RenderTexture currentRT = RenderTexture.active;
        
        // Ustawiamy naszą teksturę jako źródło do czytania pikseli
        RenderTexture.active = targetTexture;

        // Zczytujemy piksele (to obciąża CPU, dlatego robimy to rzadziej przez captureRate)
        _tempTexture.ReadPixels(new Rect(0, 0, targetTexture.width, targetTexture.height), 0, 0);
        _tempTexture.Apply();

        // Przywracamy poprzednią
        RenderTexture.active = currentRT;

        // Kompresja do JPG
        byte[] jpgBytes = _tempTexture.EncodeToJPG(60); 
        
        // Wysyłka do kolejki (zakładam, że w TcpClientController masz statyczną kolejkę FrameQueue)
        if (TcpClientController.FrameQueue != null)
        {
            // ZABEZPIECZENIE: Czyścimy kolejkę, jeśli Python nie nadąża odbierać
            while (TcpClientController.FrameQueue.Count > 3)
            {
                TcpClientController.FrameQueue.TryDequeue(out _);
            }
            
            TcpClientController.FrameQueue.Enqueue(jpgBytes);
        }
    }
}