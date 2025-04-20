using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private ApiClient api;
    [SerializeField] private List<PlayerController> players;
    public string gameId;

    private void Start()
    {
        api.OnDataReceived += OnDataReceived;

        // Opcional: Llamar datos de los 3 jugadores al inicio
        for (int i = 0; i < players.Count; i++)
        {
            GetPlayerData(i);
        }
    }

    public void GetPlayerData(int playerId)
    {
        if (playerId < 0 || playerId >= players.Count)
        {
            Debug.LogWarning($"GetPlayerData: playerId {playerId} fuera de rango.");
            return;
        }

        StartCoroutine(api.GetPlayerData(gameId, playerId.ToString()));
    }

    public void OnDataReceived(int playerId, ServerData data)
    {
        if (playerId < 0 || playerId >= players.Count)
        {
            Debug.LogWarning($"OnDataReceived: playerId {playerId} fuera de rango.");
            return;
        }

        Vector3 position = new Vector3(data.posX, data.posY, data.posZ);
        players[playerId].MovePlayer(position);
    }

    public void SendPlayerPosition(int playerId)
    {
        if (playerId < 0 || playerId >= players.Count)
        {
            Debug.LogWarning($"SendPlayerPosition: playerId {playerId} fuera de rango.");
            return;
        }

        Vector3 position = players[playerId].GetPosition();
        ServerData data = new ServerData
        {
            posX = position.x,
            posY = position.y,
            posZ = position.z
        };
        StartCoroutine(api.PostPlayerData(gameId, playerId.ToString(), data));
    }

    // Método extra: enviar datos de todos los jugadores
    public void SendAllPlayers()
    {
        for (int i = 0; i < players.Count; i++)
        {
            SendPlayerPosition(i);
        }
    }
}
