using System;
using System.Net.Sockets;
using System.Net;
using UnityEngine;

public class UDPProtocol : MonoBehaviour, IProtocolUDP
{
    private UdpClient udp;
    private IPEndPoint remoteEndPoint;

    public bool isServerRunning = false;
    public bool isServer = false;
    public bool isConnected = false;

    bool IProtocolUDP.isServer { get => isServer; set => isServer = value; }
    
    public event Action OnConnected;
    public event Action<string> OnDataReceived;


    public void StartUDP(string ipAddress, int port)
    {
        if (isServer)
        {
            udp = new UdpClient(port);
            remoteEndPoint = new IPEndPoint(IPAddress.Any, port);
        }
        else
        {
            udp = new UdpClient();
            remoteEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
        }

        udp.BeginReceive(ReceiveData, null); 
        isServerRunning = true;

        if (!isServer)
        {
            SendData("HELLO");
        }
    }
    public void ReceiveData(IAsyncResult result)
    {
        byte[] receivedBytes = udp.EndReceive(result, ref remoteEndPoint);
        string receivedMessage = System.Text.Encoding.UTF8.GetString(receivedBytes);
        if (isServer)
        {
            if (receivedMessage == "HELLO" && !isConnected)
            {
                isConnected = true;
                SendData("WELCOME");
                Debug.Log("Client connected!");
                OnConnected?.Invoke();
            }
            else
            {
                OnDataReceived?.Invoke(receivedMessage);
            }
        }
        else
        {
            if (receivedMessage == "WELCOME" && !isConnected)
            {
                isConnected = true;
                Debug.Log("Connected to the server!");
                OnConnected?.Invoke();
            }
            else
            {
                OnDataReceived?.Invoke(receivedMessage);
            }
        }
        Debug.Log("Received from client: " + receivedMessage);
        udp.BeginReceive(ReceiveData, null);
    }
    public void Disconnect()
{
    if (udp != null)
    {
        udp.Close(); // Cierra la conexión UDP
        udp = null;  // Libera la instancia de UdpClient
        isConnected = false; // Marca como desconectado
        Debug.Log("Disconnected from server.");
    }
}
public void KickPlayer()
{
    if (!isServer) return; // Solo puede ser ejecutado por el servidor

    // Enviamos un mensaje especial para expulsar al jugador
    SendData("KICK_PLAYER");
    Debug.Log("Player has been kicked.");
    
    // Aquí puedes agregar lógica adicional si necesitas que el servidor cierre la conexión o haga algo más
    Disconnect();
}



    public void SendData(string message)
    {
        byte[] sendBytes = System.Text.Encoding.UTF8.GetBytes(message);
        udp.Send(sendBytes, sendBytes.Length, remoteEndPoint);
        Debug.Log("Sent to client: " + message);
    }

}
