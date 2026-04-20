using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class TcpClientController : MonoBehaviour
{
    [Header("Network Settings")]
    public string serverIP = "127.0.0.1";
    public int serverPort = 5000;
    public static ConcurrentQueue<byte[]> FrameQueue = new ConcurrentQueue<byte[]>();
    public string LatestJsonResponse { get; private set; } = "";

    private TcpClient _client;
    private NetworkStream _stream;
    private bool _isConnected = false;

    private async void Start()
    {
        await ConnectAsync();
        _ = ProcessQueueLoopAsync(); 
    }

    private async Task ConnectAsync()
    {
        try
        {
            _client = new TcpClient();
            await _client.ConnectAsync(serverIP, serverPort);
            _stream = _client.GetStream();
            _isConnected = true;
            Debug.Log("[TCP] Connected to Python Server.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[TCP] Connection failed: {e.Message}");
        }
    }

    private async Task ProcessQueueLoopAsync()
    {
        while (_isConnected && Application.isPlaying)
        {
            if (FrameQueue.TryDequeue(out byte[] imageBytes))
            {
                string json = await SendFrameAndGetResponseAsync(imageBytes);
                if (!string.IsNullOrEmpty(json))
                {
                    LatestJsonResponse = json;
                }
            }
            else
            {
                // If the queue is empty, wait 10ms before checking again to save CPU
                await Task.Delay(10); 
            }
        }
    }

    private async Task<string> SendFrameAndGetResponseAsync(byte[] imageBytes)
    {
        try
        {
            int networkOrderSize = IPAddress.HostToNetworkOrder(imageBytes.Length);
            byte[] sizeBytes = BitConverter.GetBytes(networkOrderSize);
            
            await _stream.WriteAsync(sizeBytes, 0, sizeBytes.Length);
            await _stream.WriteAsync(imageBytes, 0, imageBytes.Length);

            byte[] buffer = new byte[1024];
            int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
            
            return Encoding.UTF8.GetString(buffer, 0, bytesRead);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[TCP] Network Error: {e.Message}");
            _isConnected = false;
            return null;
        }
    }

    private void OnDestroy()
    {
        _isConnected = false;
        _stream?.Close();
        _client?.Close();
    }
    public void ClearResponse()
    {
        LatestJsonResponse = "";
    }
}