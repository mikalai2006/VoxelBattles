using System;
using System.Runtime.InteropServices;
using NativeWebSocket;
using UnityEngine;

public class NetManager : MonoBehaviour
{
    
    WebSocket websocket;
    
    async void Start()
    {
        Application.runInBackground = true; // Recommended for WebGL

        websocket = new WebSocket("ws://localhost:8000/api/v1/ws/room1");

        websocket.OnOpen += () => Debug.Log("Connection open!");
        websocket.OnError += (e) => Debug.Log("Error! " + e);
        websocket.OnClose += (code) => Debug.Log("Connection closed!");

        websocket.OnMessage += (bytes) =>
        {
            var message = System.Text.Encoding.UTF8.GetString(bytes);
            Debug.Log("Received: " + message);
        };

        InvokeRepeating("SendWebSocketMessage", 0.0f, 0.3f);

        await websocket.Connect();
    }

    async void SendWebSocketMessage()
    {
        if (websocket.State == WebSocketState.Open)
        {
            // await websocket.Send(new byte[] { 10, 20, 30 });
            SocketMessage socketMessage = new SocketMessage
            {
                content = new byte[] { 10, 20, 30 },
                type = "message"
            };
            await websocket.Send(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(socketMessage)));
        }
    }

    private async void OnApplicationQuit()
    {
        await websocket.Close();
    }
}

[Serializable]
// [StructLayout(LayoutKind.Sequential)]
public struct SocketMessage
{
    public string type;
    public object content;
}

