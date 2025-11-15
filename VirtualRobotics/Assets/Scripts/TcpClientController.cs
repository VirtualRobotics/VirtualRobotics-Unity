using System;
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
    
    public AgentController agent;
    
    private TcpClient _client;
    private NetworkStream _stream;
    private StreamReader _reader;
    private StreamWriter _writer;

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
            _writer = new StreamWriter(_stream, Encoding.UTF8) { AutoFlush = true };

            DebugLog("[UNITY] Połączono z serwerem");

            while (_running)
            {
                _writer.WriteLine("HELLO_FROM_UNITY");

                string line = _reader.ReadLine();
                if (line == null)
                {
                    DebugLog("[UNITY] Serwer zakończył połączenie");
                    break;
                }

                DebugLog("[UNITY] Odebrano z Pythona: " + line);
                
                HandleCommand(line);
                
                Thread.Sleep(1000);
            }
        }
        catch (Exception e)
        {
            DebugLog("[UNITY] Błąd: " + e.Message);
        }
        finally
        {
            _reader?.Dispose();
            _writer?.Dispose();
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
