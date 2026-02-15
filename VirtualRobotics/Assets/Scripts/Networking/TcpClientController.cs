using System;
using System.Collections.Concurrent;
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
    
    [Header("References")]
    public HeuristicMovement agent;
    
    public static readonly ConcurrentQueue<byte[]> FrameQueue = new ConcurrentQueue<byte[]>();
    
    private TcpClient _client;
    private NetworkStream _stream;
    private StreamReader _reader;

    private Thread _thread;
    private volatile bool _running = false;

    void Start()
    {
        // 1. Clear queue from potential old data
        while (FrameQueue.TryDequeue(out _)) { }

        _running = true;
        _thread = new Thread(NetworkLoop);
        _thread.IsBackground = true;
        _thread.Start();
    }

    private void OnDestroy()
    {
        CleanUp();
    }

    private void OnApplicationQuit()
    {
        CleanUp();
    }

    private void CleanUp()
    {
        _running = false;

        try { _reader?.Close(); } catch { }
        try { _stream?.Close(); } catch { }
        try { _client?.Close(); } catch { }

        if (_thread != null && _thread.IsAlive)
        {
            if (!_thread.Join(100))
            {
                _thread.Abort();
            }
        }
        
        Debug.Log("[TCP] Connection closed, thread cleaned up.");
    }

    private void NetworkLoop()
    {
        try
        {
            DebugLog("[UNITY] Attempting to connect to Python...");
            _client = new TcpClient();
            
            var result = _client.BeginConnect(host, port, null, null);
            var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(2));

            if (!success)
            {
                throw new Exception("Connection timeout. Is the Python server running?");
            }

            _client.EndConnect(result);
            _stream = _client.GetStream();
            _reader = new StreamReader(_stream, Encoding.UTF8);

            DebugLog("[UNITY] Connected to server!");

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
                        DebugLog("[UNITY] Error sending image: " + e.Message);
                        break;
                    }
                }

                if (_stream.DataAvailable)
                {
                    string command = _reader.ReadLine();
                    if (command != null)
                    {
                        HandleCommand(command);
                    }
                    else
                    {
                        DebugLog("[UNITY] Server closed the connection.");
                        break;
                    }
                }

                Thread.Sleep(5);
            }
        }
        catch (ThreadAbortException) 
        { 
            // Ignore error when closing the game
        }
        catch (Exception e)
        {
            if (_running) 
            {
                DebugLog("[UNITY] Network error: " + e.Message);
            }
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
                if (agent != null && agent.isActiveAndEnabled)
                    agent.RotateDegrees(deg);
            });
        }
        else if (action == "MOVE" && parts.Length >= 2 && float.TryParse(parts[1], out float dist))
        {
            UnityMainThreadDispatcher.Enqueue(() =>
            {
                if (agent != null && agent.isActiveAndEnabled)
                    agent.MoveForward(dist);
            });
        }
        else if (action == "RESET")
        {
            UnityMainThreadDispatcher.Enqueue(() => {
                if (MazeManager.Instance != null)
                    MazeManager.Instance.GenerateNewLevel();
            });
        }
        else
        {
            DebugLog("[UNITY] Unknown command: " + command);
        }
    }
    
    private void DebugLog(string msg)
    {
        UnityMainThreadDispatcher.Enqueue(() => Debug.Log(msg));
    }
}