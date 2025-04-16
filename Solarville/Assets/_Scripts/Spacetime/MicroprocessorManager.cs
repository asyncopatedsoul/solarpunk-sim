using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Solarville.Spacetime
{
    /// <summary>
    /// Manages microprocessor devices in the game world
    /// Handles creation, updating, and destruction of vehicles controlled by microprocessors
    /// </summary>
    public class MicroprocessorManager : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject vehiclePrefab;
        [SerializeField] private Transform vehiclesContainer;
        
        [Header("Editor")]
        [SerializeField] private TextAsset defaultPythonScript;
        [SerializeField] private bool createTestVehicleOnStart = false;

        // State
        private Dictionary<uint, VehicleController> activeVehicles = new Dictionary<uint, VehicleController>();
        private Dictionary<uint, MicroprocessCode> codeEntries = new Dictionary<uint, MicroprocessCode>();
        private bool isInitialized = false;

        // Singleton instance
        public static MicroprocessorManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            // Register for SpacetimeDB events
            if (GameManager.IsConnected())
            {
                InitializeManager();
            }
            else
            {
                GameManager.OnSubscriptionApplied += OnSubscriptionApplied;
            }

            // Create a test vehicle if requested (for development)
            if (createTestVehicleOnStart && vehiclePrefab != null)
            {
                StartCoroutine(CreateTestVehicleDelayed());
            }
        }

        private void OnDestroy()
        {
            // Unregister from events
            GameManager.OnSubscriptionApplied -= OnSubscriptionApplied;
            
            if (isInitialized)
            {
                UnregisterEvents();
            }
        }

        private void OnSubscriptionApplied()
        {
            InitializeManager();
        }

        /// <summary>
        /// Initialize the manager and register for events
        /// </summary>
        private void InitializeManager()
        {
            if (isInitialized)
                return;

            if (!GameManager.IsConnected())
            {
                Debug.LogWarning("Cannot initialize MicroprocessorManager: Not connected to SpacetimeDB");
                return;
            }

            Debug.Log("Initializing MicroprocessorManager...");

            RegisterEvents();
            LoadExistingMicroprocessors();

            isInitialized = true;
        }

        /// <summary>
        /// Register for SpacetimeDB events
        /// </summary>
        private void RegisterEvents()
        {
            if (!GameManager.IsConnected())
                return;

            var conn = GameManager.Conn;
            conn.Db.MicroprocessCode.OnInsert += OnMicroprocessCodeInsert;
            conn.Db.MicroprocessCode.OnUpdate += OnMicroprocessCodeUpdate;
            conn.Db.MicroprocessCode.OnDelete += OnMicroprocessCodeDelete;
            conn.Db.MicroprocessState.OnInsert += OnMicroprocessStateInsert;
            conn.Db.MicroprocessState.OnUpdate += OnMicroprocessStateUpdate;
            conn.Db.MicroprocessState.OnDelete += OnMicroprocessStateDelete;
        }

        /// <summary>
        /// Unregister from SpacetimeDB events
        /// </summary>
        private void UnregisterEvents()
        {
            if (!GameManager.IsConnected())
                return;

            var conn = GameManager.Conn;
            conn.Db.MicroprocessCode.OnInsert -= OnMicroprocessCodeInsert;
            conn.Db.MicroprocessCode.OnUpdate -= OnMicroprocessCodeUpdate;
            conn.Db.MicroprocessCode.OnDelete -= OnMicroprocessCodeDelete;
            conn.Db.MicroprocessState.OnInsert -= OnMicroprocessStateInsert;
            conn.Db.MicroprocessState.OnUpdate -= OnMicroprocessStateUpdate;
            conn.Db.MicroprocessState.OnDelete -= OnMicroprocessStateDelete;
        }

        /// <summary>
        /// Load existing microprocessors from the database
        /// </summary>
        private void LoadExistingMicroprocessors()
        {
            if (!GameManager.IsConnected())
                return;

            var conn = GameManager.Conn;
            
            // Load all code entries
            foreach (var code in conn.Db.MicroprocessCode.Iter())
            {
                codeEntries[code.CodeId] = code;
            }
            
            // Load all states and create vehicles
            foreach (var state in conn.Db.MicroprocessState.Iter())
            {
                if (codeEntries.ContainsKey(state.CodeId))
                {
                    CreateOrUpdateVehicle(state);
                }
            }
        }

        /// <summary>
        /// Create a test vehicle (for development)
        /// </summary>
        private IEnumerator CreateTestVehicleDelayed()
        {
            // Wait for connection to SpacetimeDB
            yield return new WaitUntil(() => GameManager.IsConnected() && isInitialized);
            
            // Get the current player
            var player = GameManager.Conn.Db.Player.Identity().Find(GameManager.LocalIdentity);
            if (player == null)
            {
                Debug.LogError("Cannot create test vehicle: Local player not found");
                yield break;
            }

            // Upload a test script if available
            if (defaultPythonScript != null)
            {
                string scriptName = "TestScript";
                string scriptPath = "/test/script.py";
                string scriptContent = defaultPythonScript.text;
                
                try
                {
                    // Call the reducer to save the script
                    uint codeId = GameManager.Conn.Reducers.SaveMicroprocessCode(scriptName, scriptPath, scriptContent);
                    
                    // Wait a bit for the code to be processed
                    yield return new WaitForSeconds(0.5f);
                    
                    // Start the script
                    GameManager.Conn.Reducers.StartMicroprocess(codeId);
                    
                    Debug.Log($"Created and started test vehicle with code ID: {codeId}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error creating test vehicle: {ex.Message}");
                }
            }
            else
            {
                Debug.LogWarning("No default Python script assigned for test vehicle");
            }
        }

        /// <summary>
        /// Handle insertion of a new microprocessor code entry
        /// </summary>
        private void OnMicroprocessCodeInsert(EventContext context, MicroprocessCode code)
        {
            Debug.Log($"New microprocessor code inserted: {code.Name} (ID: {code.CodeId})");
            codeEntries[code.CodeId] = code;
        }

        /// <summary>
        /// Handle update of an existing microprocessor code entry
        /// </summary>
        private void OnMicroprocessCodeUpdate(EventContext context, MicroprocessCode oldCode, MicroprocessCode newCode)
        {
            Debug.Log($"Microprocessor code updated: {newCode.Name} (ID: {newCode.CodeId})");
            codeEntries[newCode.CodeId] = newCode;
        }

        /// <summary>
        /// Handle deletion of a microprocessor code entry
        /// </summary>
        private void OnMicroprocessCodeDelete(EventContext context, MicroprocessCode code)
        {
            Debug.Log($"Microprocessor code deleted: {code.Name} (ID: {code.CodeId})");
            codeEntries.Remove(code.CodeId);
        }

        /// <summary>
        /// Handle insertion of a new microprocessor state
        /// </summary>
        private void OnMicroprocessStateInsert(EventContext context, MicroprocessState state)
        {
            Debug.Log($"New microprocessor state inserted for code ID: {state.CodeId}");
            CreateOrUpdateVehicle(state);
        }

        /// <summary>
        /// Handle update of an existing microprocessor state
        /// </summary>
        private void OnMicroprocessStateUpdate(EventContext context, MicroprocessState oldState, MicroprocessState newState)
        {
            Debug.Log($"Microprocessor state updated for code ID: {newState.CodeId}");
            CreateOrUpdateVehicle(newState);
        }

        /// <summary>
        /// Handle deletion of a microprocessor state
        /// </summary>
        private void OnMicroprocessStateDelete(EventContext context, MicroprocessState state)
        {
            Debug.Log($"Microprocessor state deleted for code ID: {state.CodeId}");
            DestroyVehicle(state.CodeId);
        }

        /// <summary>
        /// Create a new vehicle or update an existing one based on the microprocessor state
        /// </summary>
        private void CreateOrUpdateVehicle(MicroprocessState state)
        {
            // Check if we already have a vehicle for this code
            if (activeVehicles.TryGetValue(state.CodeId, out var existingVehicle))
            {
                // Update the existing vehicle
                existingVehicle.UpdateFromMicroprocessState(state);
                
                // If the microprocessor is no longer running, destroy the vehicle
                if (!state.IsRunning)
                {
                    DestroyVehicle(state.CodeId);
                }
            }
            else if (state.IsRunning)
            {
                // Create a new vehicle
                if (vehiclePrefab != null && vehiclesContainer != null)
                {
                    // Get code info
                    MicroprocessCode code = null;
                    if (!codeEntries.TryGetValue(state.CodeId, out code))
                    {
                        Debug.LogWarning($"Cannot create vehicle: Code entry not found for code ID: {state.CodeId}");
                        return;
                    }
                    
                    // Get owner info
                    var owner = GameManager.Conn.Db.Player.PlayerId().Find(code.OwnerId);
                    if (owner == null)
                    {
                        Debug.LogWarning($"Cannot create vehicle: Owner not found for player ID: {code.OwnerId}");
                        return;
                    }

                    // Create the vehicle
                    GameObject vehicleObject = Instantiate(vehiclePrefab, vehiclesContainer);
                    
                    // Set initial position and rotation
                    Vector3 spawnPosition = owner.Position + Vector3.up * 0.5f;
                    Quaternion spawnRotation = Quaternion.Euler(owner.Rotation);
                    vehicleObject.transform.position = spawnPosition;
                    vehicleObject.transform.rotation = spawnRotation;
                    
                    // Set name
                    vehicleObject.name = $"Vehicle_{code.Name}_{state.CodeId}";
                    
                    // Get controller component
                    VehicleController controller = vehicleObject.GetComponent<VehicleController>();
                    if (controller != null)
                    {
                        // Initialize controller
                        controller.UpdateFromMicroprocessState(state);
                        
                        // Add to active vehicles
                        activeVehicles[state.CodeId] = controller;
                        
                        Debug.Log($"Created vehicle for code ID: {state.CodeId}, Name: {code.Name}");
                    }
                    else
                    {
                        Debug.LogError($"Vehicle prefab does not have a VehicleController component");
                        Destroy(vehicleObject);
                    }
                }
                else
                {
                    Debug.LogWarning($"Cannot create vehicle: Vehicle prefab or container is null");
                }
            }
        }

        /// <summary>
        /// Destroy a vehicle with the given code ID
        /// </summary>
        private void DestroyVehicle(uint codeId)
        {
            if (activeVehicles.TryGetValue(codeId, out var vehicle))
            {
                Debug.Log($"Destroying vehicle for code ID: {codeId}");
                
                // Remove from dictionary
                activeVehicles.Remove(codeId);
                
                // Destroy GameObject
                if (vehicle != null)
                {
                    Destroy(vehicle.gameObject);
                }
            }
        }

        /// <summary>
        /// Save a new Python script to the database
        /// </summary>
        public uint? SavePythonScript(string name, string path, string content)
        {
            if (!GameManager.IsConnected())
            {
                Debug.LogError("Cannot save Python script: Not connected to SpacetimeDB");
                return null;
            }

            try
            {
                uint codeId = GameManager.Conn.Reducers.SaveMicroprocessCode(name, path, content);
                Debug.Log($"Saved Python script: {name} with ID: {codeId}");
                return codeId;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error saving Python script: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Start executing a Python script
        /// </summary>
        public bool StartPythonScript(uint codeId)
        {
            if (!GameManager.IsConnected())
            {
                Debug.LogError("Cannot start Python script: Not connected to SpacetimeDB");
                return false;
            }

            try
            {
                GameManager.Conn.Reducers.StartMicroprocess(codeId);
                Debug.Log($"Started Python script with ID: {codeId}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error starting Python script: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Stop executing a Python script
        /// </summary>
        public bool StopPythonScript(uint codeId)
        {
            if (!GameManager.IsConnected())
            {
                Debug.LogError("Cannot stop Python script: Not connected to SpacetimeDB");
                return false;
            }

            try
            {
                GameManager.Conn.Reducers.StopMicroprocess(codeId);
                Debug.Log($"Stopped Python script with ID: {codeId}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error stopping Python script: {ex.Message}");
                return false;
            }
        }
    }
}