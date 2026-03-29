using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class TcpClientController : MonoBehaviour
{
    [Header("Network Settings")]
    public string host = "127.0.0.1";
    public int port = 5000;
    
    private CvHeuristicController controller;

    public static readonly ConcurrentQueue<byte[]> FrameQueue = new ConcurrentQueue<byte[]>();

    private TcpClient _client;
    private NetworkStream _stream;
    private StreamReader _reader;

    private Thread _thread;
    private volatile bool _running;

    private void Start()
    {
        while (FrameQueue.TryDequeue(out _)) { }

        _running = true;
        _thread = new Thread(NetworkLoop) { IsBackground = true };
        _thread.Start();
    }

    private void OnDestroy() => CleanUp();
    private void OnApplicationQuit() => CleanUp();

    private void CleanUp()
    {
        _running = false;

        try { _reader?.Close(); } catch { }
        try { _stream?.Close(); } catch { }
        try { _client?.Close(); } catch { }

        if (_thread != null && _thread.IsAlive)
        {
            if (!_thread.Join(200))
            {
                try { _thread.Interrupt(); } catch { }
            }
        }

        Debug.Log("[TCP] Connection closed, thread cleaned up.");
    }

    private void NetworkLoop()
    {
        try
        {
            DebugLog("[TCP] Connecting to Python...");
            _client = new TcpClient();

            var result = _client.BeginConnect(host, port, null, null);
            var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(2));

            if (!success)
                throw new Exception("Connection timeout. Is the Python server running?");

            _client.EndConnect(result);
            _stream = _client.GetStream();
            _reader = new StreamReader(_stream, Encoding.UTF8);

            DebugLog("[TCP] Connected.");

            while (_running)
            {
                if (FrameQueue.TryDequeue(out var frameBytes))
                {
                    try
                    {
                        byte[] lenBytes = BitConverter.GetBytes(frameBytes.Length);
                        if (BitConverter.IsLittleEndian) Array.Reverse(lenBytes);

                        _stream.Write(lenBytes, 0, 4);
                        _stream.Write(frameBytes, 0, frameBytes.Length);
                    }
                    catch (Exception e)
                    {
                        DebugLog("[TCP] Error sending frame: " + e.Message);
                        break;
                    }
                }

                if (_stream.DataAvailable)
                {
                    string command = _reader.ReadLine();
                    if (command != null) HandleCommand(command);
                    else { DebugLog("[TCP] Server closed connection."); break; }
                }

                Thread.Sleep(5);
            }
        }
        catch (ThreadAbortException) { }
        catch (Exception e)
        {
            if (_running) DebugLog("[TCP] Network error: " + e.Message);
        }
        finally
        {
            try { _reader?.Dispose(); } catch { }
            try { _stream?.Dispose(); } catch { }
            try { _client?.Close(); } catch { }
        }
    }

    private void HandleCommand(string command)
    {
        string[] parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        string action = parts[0].ToUpperInvariant();

        // NOWE API: "CONTROL <throttle> <steer>"
        // Używamy CultureInfo.InvariantCulture, żeby "1.5" i "1,5" działały tak samo (kropki vs przecinki)
        if (action == "CONTROL" && parts.Length >= 3 && 
            float.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out float throttle) &&
            float.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out float steer))
        {
            UnityMainThreadDispatcher.Enqueue(() =>
            {
                EnsureControllerReference();
                if (controller != null && controller.isActiveAndEnabled)
                    controller.SetControl(throttle, steer);
            });
        }
        // NOWE API: "STOP" (Awaryjne hamowanie)
        else if (action == "STOP")
        {
            UnityMainThreadDispatcher.Enqueue(() =>
            {
                EnsureControllerReference();
                if (controller != null && controller.isActiveAndEnabled)
                    controller.StopAgent();
            });
        }
        // STARE API: Zostawiamy reset w spokoju
        else if (action == "RESET")
        {
            UnityMainThreadDispatcher.Enqueue(() =>
            {
                if (EvaluationEnvManager.Instance != null)
                {
                    Debug.Log("[TCP] Otrzymano komendę RESET z Pythona.");
                    EvaluationEnvManager.Instance.GenerateNewLevel();
                }
            });
        }
        else
        {
            DebugLog("[TCP] Nieznana komenda lub zły format: " + command);
        }
    }

    // Pomocnicza metoda, która upewnia się, że mamy aktualnego agenta, 
    // nawet jeśli stary został zniszczony przy zmianie seeda!
    private void EnsureControllerReference()
    {
        if (controller == null || !controller.gameObject.activeInHierarchy)
        {
            controller = FindFirstObjectByType<CvHeuristicController>(FindObjectsInactive.Exclude);
        }
    }

    private void DebugLog(string msg)
    {
        UnityMainThreadDispatcher.Enqueue(() => Debug.Log(msg));
    }
}