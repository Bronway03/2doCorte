using System;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.UI;  // Asegúrate de añadir esta línea para interactuar con los UI Elements

public class PlayerHost : MonoBehaviour
{
    public UDPProtocol protocolUDP;

    [SerializeField] private GameObject playerPrefab;
    public Transform multiplayerTransform;
    public Transform spawnPosition;

    private ConcurrentQueue<Vector3> positionQueue = new ConcurrentQueue<Vector3>();
    private ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();

    private bool isPaused = false; // Controla si el juego está pausado

    // Referencia al botón de expulsión en la UI
    [SerializeField] private Button kickButton;

    void Start()
    {
        if (protocolUDP == null)
        {
            Debug.LogError("UDPProtocol reference is missing!");
            return;
        }

        protocolUDP.OnConnected += () => mainThreadActions.Enqueue(PlayerConnection);
        protocolUDP.OnDataReceived += ReceivePosition;
        protocolUDP.OnDataReceived += HandleKickMessage; // Añadir escucha de expulsión

        // Si el botón de expulsión no está asignado, buscamos el componente Button en la escena
        if (kickButton == null)
        {
            kickButton = GameObject.Find("KickButton")?.GetComponent<Button>();  // Suponiendo que el botón se llama "KickButton" en la escena
        }

        if (kickButton != null)
        {
            kickButton.onClick.AddListener(KickPlayer); // Asignamos la acción de expulsar al botón
        }
    }

    public void StartProtocol()
    {
        protocolUDP.StartUDP("127.0.0.1", 5010);
    }

    void PlayerConnection()
    {
        Debug.Log("Instance");
        multiplayerTransform = Instantiate(playerPrefab, spawnPosition).transform;
    }

    void Update()
    {
        // Si el jugador presiona "D", desconectar (puedes cambiar esta tecla por la que prefieras)
        if (Input.GetKeyDown(KeyCode.D))
        {
            if (!protocolUDP.isServer) // Solo permitir la desconexión si el jugador es cliente
            {
                protocolUDP.Disconnect(); // Llamamos a la desconexión
                Debug.Log("Disconnected from server.");
            }
        }

        // Si el jugador presiona "P", alterna entre pausar y reanudar el juego
        if (Input.GetKeyDown(KeyCode.P))
        {
            TogglePause();
        }

        // Si el juego está pausado, no hacer nada en el Update() excepto escuchar eventos
        if (isPaused)
        {
            return; // Si está pausado, no actualizamos el juego
        }

        while (mainThreadActions.TryDequeue(out Action action))
        {
            action?.Invoke();
        }

        if (protocolUDP.isServer)
        {
            while (positionQueue.TryDequeue(out Vector3 newPosition))
            {
                if (multiplayerTransform != null)
                    multiplayerTransform.position = newPosition;
            }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                SendPosition();
            }
        }
    }

    // Función para pausar y reanudar el juego
    void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            Time.timeScale = 0f; // Detener el tiempo
            Debug.Log("Game Paused");
        }
        else
        {
            Time.timeScale = 1f; // Reanudar el tiempo
            Debug.Log("Game Resumed");
        }
    }

    // Función para manejar la expulsión de jugadores
    void HandleKickMessage(string message)
    {
        if (message == "KICK_PLAYER")
        {
            Debug.Log("You have been kicked from the server.");
            protocolUDP.Disconnect(); // Desconectamos al jugador
            isPaused = true;  // Pausamos el juego en caso de expulsión
            Time.timeScale = 0f; // Asegurarnos de que el tiempo se detiene cuando el jugador es expulsado
        }
    }

    // Función para que el servidor expulse al jugador al presionar el botón
    void KickPlayer()
    {
        if (protocolUDP.isServer)
        {
            Debug.Log("Expulsing player...");
            protocolUDP.SendData("KICK_PLAYER");  // Enviar mensaje de expulsión al cliente
        }
        else
        {
            Debug.Log("Only the server can kick players.");
        }
    }

    public void SendPosition()
    {
        if (multiplayerTransform == null)
        {
            Debug.LogError("Player not instantiated!");
            return;
        }

        Vector3 position = multiplayerTransform.position;
        string positionData = $"{position.x};{position.y};{position.z}";
        Debug.Log($"Sending position: {positionData}");
        protocolUDP.SendData(positionData);
    }

    public void ReceivePosition(string positionData)
    {
        if (multiplayerTransform == null)
        {
            Debug.LogError("Player not instantiated!");
            return;
        }

        try
        {
            string[] values = positionData.Split(';');
            if (values.Length != 3)
            {
                Debug.LogError("Invalid position data received.");
                return;
            }

            float x = float.Parse(values[0]);
            float y = float.Parse(values[1]);
            float z = float.Parse(values[2]);

            positionQueue.Enqueue(new Vector3(x, y, z));
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error parsing position data: {ex.Message}");
        }
    }
}
