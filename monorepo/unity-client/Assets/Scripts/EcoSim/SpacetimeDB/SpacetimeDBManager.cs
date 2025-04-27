using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SpacetimeDB.Types;
using SpacetimeDB;

/// <summary>
/// Manager class for handling SpacetimeDB integration in Unity
/// </summary>
public class SpacetimeDBManager : MonoBehaviour
{
    [Header("SpacetimeDB Configuration")]
    [SerializeField] private string serverAddress = "localhost:3000";
    [SerializeField] private string moduleName = "robotics-simulation-db";
    [SerializeField] private bool connectOnStart = true;
    [SerializeField] private float reconnectDelay = 5f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    // SpacetimeDB client
    private Client client;
    private Identity identity;
    private bool isConnected = false;
    private bool isConnecting = false;

    // Events
    public event Action OnConnected;
    public event Action OnDisconnected;
    public event Action<string> OnError;

    // Table references
    private Table<MicroprocessCode> microprocessCodeTable;
    private Table<MicroprocessState> microprocessStateTable;
    private Table<Robot> robotTable;
    private Table<GameObject> gameObjectTable;
    private Table<Player> playerTable;
    private Table<Scenario> scenarioTable;

    // Singleton instance
    private static SpacetimeDBManager instance;
    public static SpacetimeDBManager Instance => instance;

    private void Awake()
    {
        // Singleton pattern
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (connectOnStart)
        {
            Connect();
        }
    }

    private void OnDestroy()
    {
        if (isConnected)
        {
            Disconnect();
        }
    }

    /// <summary>
    /// Connect to the SpacetimeDB server
    /// </summary>
    public void Connect()
    {
        if (isConnected || isConnecting)
        {
            return;
        }

        isConnecting = true;
        
        try
        {
            LogDebug("Connecting to SpacetimeDB...");

            // Create a new client
            client = new Client();

            // Set up event handlers
            client.OnConnect += HandleConnect;
            client.OnDisconnect += HandleDisconnect;
            client.OnError += HandleError;

            // Generate or load identity
            identity = LoadOrCreateIdentity();

            // Connect to the server
            client.Connect(new ClientConfig
            {
                Address = serverAddress,
                ModuleName = moduleName,
                Identity = identity
            });
        }
        catch (Exception ex)
        {
            LogError($"Error connecting to SpacetimeDB: {ex.Message}");
            isConnecting = false;
            
            // Try to reconnect after delay
            StartCoroutine(ReconnectAfterDelay());
        }
    }

    /// <summary>
    /// Disconnect from the SpacetimeDB server
    /// </summary>
    public void Disconnect()
    {
        if (!isConnected)
        {
            return;
        }

        try
        {
            LogDebug("Disconnecting from SpacetimeDB...");
            client.Disconnect();
        }
        catch (Exception ex)
        {
            LogError($"Error disconnecting from SpacetimeDB: {ex.Message}");
        }
    }

    /// <summary>
    /// Handle connection established
    /// </summary>
    private void HandleConnect()
    {
        isConnected = true;
        isConnecting = false;
        
        LogDebug("Connected to SpacetimeDB");

        // Set up table references
        SetupTables();

        // Register the client
        RegisterClient();

        // Notify listeners
        OnConnected?.Invoke();
    }

    /// <summary>
    /// Handle disconnection
    /// </summary>
    private void HandleDisconnect()
    {
        isConnected = false;
        
        LogDebug("Disconnected from SpacetimeDB");

        // Notify listeners
        OnDisconnected?.Invoke();

        // Try to reconnect
        StartCoroutine(ReconnectAfterDelay());
    }

    /// <summary>
    /// Handle errors
    /// </summary>
    private void HandleError(string errorMessage)
    {
        LogError($"SpacetimeDB error: {errorMessage}");

        // Notify listeners
        OnError?.Invoke(errorMessage);
    }

    /// <summary>
    /// Try to reconnect after a delay
    /// </summary>
    private IEnumerator ReconnectAfterDelay()
    {
        yield return new WaitForSeconds(reconnectDelay);
        
        if (!isConnected && !isConnecting)
        {
            Connect();
        }
    }

    /// <summary>
    /// Load an existing identity or create a new one
    /// </summary>
    private Identity LoadOrCreateIdentity()
    {
        // Check if we have a saved identity
        string savedIdentityJson = PlayerPrefs.GetString("SpacetimeIdentity", "");
        
        if (!string.IsNullOrEmpty(savedIdentityJson))
        {
            try
            {
                // Load the saved identity
                LogDebug("Loading saved identity");
                return JsonUtility.FromJson<Identity>(savedIdentityJson);
            }
            catch (Exception ex)
            {
                LogError($"Error loading identity: {ex.Message}");
            }
        }
        
        // Generate a new identity
        LogDebug("Generating new identity");
        Identity newIdentity = Client.GenerateIdentity();
        
        // Save the new identity
        try
        {
            string identityJson = JsonUtility.ToJson(newIdentity);
            PlayerPrefs.SetString("SpacetimeIdentity", identityJson);
            PlayerPrefs.Save();
        }
        catch (Exception ex)
        {
            LogError($"Error saving identity: {ex.Message}");
        }
        
        return newIdentity;
    }

    /// <summary>
    /// Set up table references
    /// </summary>
    private void SetupTables()
    {
        microprocessCodeTable = client.GetTable<MicroprocessCode>();
        microprocessStateTable = client.GetTable<MicroprocessState>();
        robotTable = client.GetTable<Robot>();
        gameObjectTable = client.GetTable<GameObject>();
        playerTable = client.GetTable<Player>();
        scenarioTable = client.GetTable<Scenario>();

        // Subscribe to table events
        microprocessCodeTable.OnInsert += HandleMicroprocessCodeInsert;
        microprocessCodeTable.OnUpdate += HandleMicroprocessCodeUpdate;
        microprocessCodeTable.OnDelete += HandleMicroprocessCodeDelete;

        microprocessStateTable.OnInsert += HandleMicroprocessStateInsert;
        microprocessStateTable.OnUpdate += HandleMicroprocessStateUpdate;

        robotTable.OnInsert += HandleRobotInsert;
        robotTable.OnUpdate += HandleRobotUpdate;
        robotTable.OnDelete += HandleRobotDelete;

        gameObjectTable.OnInsert += HandleGameObjectInsert;
        gameObjectTable.OnUpdate += HandleGameObjectUpdate;
        gameObjectTable.OnDelete += HandleGameObjectDelete;
    }

    /// <summary>
    /// Register the client with SpacetimeDB
    /// </summary>
    private void RegisterClient()
    {
        if (!isConnected)
        {
            return;
        }

        try
        {
            // Call the register_player reducer
            client.CallReducer("register_player", new RegisterPlayerArgs
            {
                name = $"Unity Client ({SystemInfo.deviceName})"
            });
            
            LogDebug("Registered client with SpacetimeDB");
        }
        catch (Exception ex)
        {
            LogError($"Error registering client: {ex.Message}");
        }
    }

    /// <summary>
    /// Start running a microprocess
    /// </summary>
    public void StartMicroprocess(ulong codeId)
    {
        if (!isConnected)
        {
            LogError("Cannot start microprocess: Not connected to SpacetimeDB");
            return;
        }

        try
        {
            // Call the start_microprocess reducer
            client.CallReducer("start_microprocess", new StartMicroprocessArgs
            {
                codeId = codeId
            });
            
            LogDebug($"Started microprocess {codeId}");
        }
        catch (Exception ex)
        {
            LogError($"Error starting microprocess {codeId}: {ex.Message}");
        }
    }

    /// <summary>
    /// Stop running a microprocess
    /// </summary>
    public void StopMicroprocess(ulong codeId)
    {
        if (!isConnected)
        {
            LogError("Cannot stop microprocess: Not connected to SpacetimeDB");
            return;
        }

        try
        {
            // Call the stop_microprocess reducer
            client.CallReducer("stop_microprocess", new StopMicroprocessArgs
            {
                codeId = codeId
            });
            
            LogDebug($"Stopped microprocess {codeId}");
        }
        catch (Exception ex)
        {
            LogError($"Error stopping microprocess {codeId}: {ex.Message}");
        }
    }

    /// <summary>
    /// Create a new microprocess code
    /// </summary>
    public void CreateMicroprocessCode(string name, string codeContent, string filePath)
    {
        if (!isConnected)
        {
            LogError("Cannot create microprocess code: Not connected to SpacetimeDB");
            return;
        }

        try
        {
            // Call the add_microprocess_code reducer
            client.CallReducer("add_microprocess_code", new AddMicroprocessCodeArgs
            {
                name = name,
                codeContent = codeContent,
                filePath = filePath
            });
            
            LogDebug($"Created microprocess code: {name}");
        }
        catch (Exception ex)
        {
            LogError($"Error creating microprocess code: {ex.Message}");
        }
    }

    /// <summary>
    /// Update an existing microprocess code
    /// </summary>
    public void UpdateMicroprocessCode(string name, string filePath, string codeContent)
    {
        if (!isConnected)
        {
            LogError("Cannot update microprocess code: Not connected to SpacetimeDB");
            return;
        }

        try
        {
            // Call the update_microprocess_code reducer
            client.CallReducer("update_microprocess_code", new UpdateMicroprocessCodeArgs
            {
                name = name,
                filePath = filePath,
                codeContent = codeContent
            });
            
            LogDebug($"Updated microprocess code: {name}");
        }
        catch (Exception ex)
        {
            LogError($"Error updating microprocess code: {ex.Message}");
        }
    }

    /// <summary>
    /// Spawn a game object in the environment
    /// </summary>
    public void SpawnGameObject(string name, Vector3 position, Vector3 rotation, Vector3 scale, string prefabName, bool isStatic)
    {
        if (!isConnected)
        {
            LogError("Cannot spawn game object: Not connected to SpacetimeDB");
            return;
        }

        try
        {
            // Convert Unity Vector3 to SpacetimeDB Vector3
            var dbPosition = new SpacetimeDB.Types.Vector3
            {
                x = position.x,
                y = position.y,
                z = position.z
            };
            
            var dbRotation = new SpacetimeDB.Types.Vector3
            {
                x = rotation.x,
                y = rotation.y,
                z = rotation.z
            };
            
            var dbScale = new SpacetimeDB.Types.Vector3
            {
                x = scale.x,
                y = scale.y,
                z = scale.z
            };

            // Call the spawn_game_object reducer
            client.CallReducer("spawn_game_object", new SpawnGameObjectArgs
            {
                name = name,
                position = dbPosition,
                rotation = dbRotation,
                scale = dbScale,
                prefabName = prefabName,
                isStatic = isStatic
            });
            
            LogDebug($"Spawned game object: {name}");
        }
        catch (Exception ex)
        {
            LogError($"Error spawning game object: {ex.Message}");
        }
    }

    /// <summary>
    /// Update a robot's position
    /// </summary>
    public void UpdateRobotPosition(ulong robotId, Vector3 position, Vector3 rotation)
    {
        if (!isConnected)
        {
            LogError("Cannot update robot position: Not connected to SpacetimeDB");
            return;
        }

        try
        {
            // Convert Unity Vector3 to SpacetimeDB Vector3
            var dbPosition = new SpacetimeDB.Types.Vector3
            {
                x = position.x,
                y = position.y,
                z = position.z
            };
            
            var dbRotation = new SpacetimeDB.Types.Vector3
            {
                x = rotation.x,
                y = rotation.y,
                z = rotation.z
            };

            // Call the update_robot_position reducer
            client.CallReducer("update_robot_position", new UpdateRobotPositionArgs
            {
                robotId = robotId,
                position = dbPosition,
                rotation = dbRotation
            });
        }
        catch (Exception ex)
        {
            LogError($"Error updating robot position: {ex.Message}");
        }
    }

    // Table event handlers
    
    private void HandleMicroprocessCodeInsert(MicroprocessCode code)
    {
        LogDebug($"Microprocess code inserted: {code.name} (ID: {code.codeId})");
        // Notify other components about the new code
    }

    private void HandleMicroprocessCodeUpdate(MicroprocessCode oldCode, MicroprocessCode newCode)
    {
        LogDebug($"Microprocess code updated: {newCode.name} (ID: {newCode.codeId})");
        // Notify other components about the updated code
    }

    private void HandleMicroprocessCodeDelete(MicroprocessCode code)
    {
        LogDebug($"Microprocess code deleted: {code.name} (ID: {code.codeId})");
        // Notify other components about the deleted code
    }

    private void HandleMicroprocessStateInsert(MicroprocessState state)
    {
        LogDebug($"Microprocess state inserted: {state.codeId} (Left: {state.leftMotorSpeed}, Right: {state.rightMotorSpeed})");
        // Notify other components about the new state
    }

    private void HandleMicroprocessStateUpdate(MicroprocessState oldState, MicroprocessState newState)
    {
        LogDebug($"Microprocess state updated: {newState.codeId} (Left: {newState.leftMotorSpeed}, Right: {newState.rightMotorSpeed})");
        // Notify other components about the updated state
    }

    private void HandleRobotInsert(Robot robot)
    {
        LogDebug($"Robot inserted: {robot.name} (ID: {robot.robotId})");
        // Instantiate the robot in the scene
    }

    private void HandleRobotUpdate(Robot oldRobot, Robot newRobot)
    {
        LogDebug($"Robot updated: {newRobot.name} (ID: {newRobot.robotId})");
        // Update the robot in the scene
    }

    private void HandleRobotDelete(Robot robot)
    {
        LogDebug($"Robot deleted: {robot.name} (ID: {robot.robotId})");
        // Remove the robot from the scene
    }

    private void HandleGameObjectInsert(SpacetimeDB.Types.GameObject gameObject)
    {
        LogDebug($"Game object inserted: {gameObject.name} (ID: {gameObject.objectId})");
        // Instantiate the game object in the scene
    }

    private void HandleGameObjectUpdate(SpacetimeDB.Types.GameObject oldGameObject, SpacetimeDB.Types.GameObject newGameObject)
    {
        LogDebug($"Game object updated: {newGameObject.name} (ID: {newGameObject.objectId})");
        // Update the game object in the scene
    }

    private void HandleGameObjectDelete(SpacetimeDB.Types.GameObject gameObject)
    {
        LogDebug($"Game object deleted: {gameObject.name} (ID: {gameObject.objectId})");
        // Remove the game object from the scene
    }

    // Helper methods
    
    private void LogDebug(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[SpacetimeDB] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[SpacetimeDB] {message}");
    }
}

// SpacetimeDB reducer argument classes

[Serializable]
public class RegisterPlayerArgs
{
    public string name;
}

[Serializable]
public class StartMicroprocessArgs
{
    public ulong codeId;
}

[Serializable]
public class StopMicroprocessArgs
{
    public ulong codeId;
}

[Serializable]
public class AddMicroprocessCodeArgs
{
    public string name;
    public string codeContent;
    public string filePath;
}

[Serializable]
public class UpdateMicroprocessCodeArgs
{
    public string name;
    public string filePath;
    public string codeContent;
}

[Serializable]
public class SpawnGameObjectArgs
{
    public string name;
    public SpacetimeDB.Types.Vector3 position;
    public SpacetimeDB.Types.Vector3 rotation;
    public SpacetimeDB.Types.Vector3 scale;
    public string prefabName;
    public bool isStatic;
}

[Serializable]
public class UpdateRobotPositionArgs
{
    public ulong robotId;
    public SpacetimeDB.Types.Vector3 position;
    public SpacetimeDB.Types.Vector3 rotation;
}
