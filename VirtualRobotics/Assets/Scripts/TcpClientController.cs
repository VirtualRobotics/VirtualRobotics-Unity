using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

public class TcpClientController : MonoBehaviour
{
    public string host = "127.0.0.1";
    public int port = 5000;
    
    public HeuristicMovement agent;
    public static readonly ConcurrentQueue<byte[]> FrameQueue = new ConcurrentQueue<byte[]>();
    
    private TcpClient _client;
    private NetworkStream _stream;
    private StreamReader _reader;

    private Thread _thread;
    private bool _running;

    void Start()
    {
        _running = true;
        _thread = new Thread(NetworkLoop);
        _thread.IsBackground = true;
        _thread.Start();
    }

    private void NetworkLoop()
    {
        try
        {
            DebugLog("[UNITY] Próba połączenia...");
            _client = new TcpClient();
            _client.Connect(host, port);
            _stream = _client.GetStream();
            _reader = new StreamReader(_stream, Encoding.UTF8);

            DebugLog("[UNITY] Połączono z serwerem");

            while (_running)
            {
                if (!FrameQueue.TryDequeue(out var frameBytes))
                {
                    Thread.Sleep(5);
                    continue;
                }
                
                int length = frameBytes.Length;
                byte[] lengthBytes = BitConverter.GetBytes(length);
                
                if (BitConverter.IsLittleEndian) Array.Reverse(lengthBytes);
                
                _stream.Write(lengthBytes, 0, 4);
                _stream.Write(frameBytes, 0, frameBytes.Length);
                _stream.Flush();
                
                string line = _reader.ReadLine();
                if (line == null)
                {
                    DebugLog("[UNITY] Serwer zakończył połączenie");
                    break;
                }

                DebugLog("[UNITY] Odebrano z Pythona: " + line);
                HandleCommand(line);
            }
        }
        catch (Exception e)
        {
            DebugLog("[UNITY] Błąd: " + e.Message);
        }
        finally
        {
            _reader?.Dispose();
            _stream?.Dispose();
            _client?.Close();
        }
    }

    private void HandleCommand(string command)
    {
        string[] parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;
        
        string action = parts[0].ToUpperInvariant();

        if (action == "ROTATE" && parts.Length >= 2 && float.TryParse(parts[1], out float deg))
        {
            UnityMainThreadDispatcher.Enqueue(() =>
            {
                if (agent != null)
                    agent.RotateDegrees(deg);
            });
        }
        else if (action == "MOVE" && parts.Length >= 2 && float.TryParse(parts[1], out float dist))
        {
            UnityMainThreadDispatcher.Enqueue(() =>
            {
                if (agent != null)
                    agent.MoveForward(dist);
            });
        }
        else if (action == "RESET")
        {
            UnityMainThreadDispatcher.Enqueue(() => {
                MazeManager.Instance.GenerateNewLevel();
            });
        }
        else
        {
            DebugLog("[UNITY] Nieznana komenda: " + command);
        }
    }
    
    private void DebugLog(string msg)
    {
        UnityMainThreadDispatcher.Enqueue(() => Debug.Log(msg));
    }

    void OnApplicationQuit()
    {
        _running = false;
        _thread?.Join(500);
    }
}
