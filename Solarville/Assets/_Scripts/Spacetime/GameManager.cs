using System;
using System.Collections.Generic;
using SpacetimeDB;
using SpacetimeDB.Types;
using UnityEngine;

namespace Solarville.Spacetime
{
    public class GameManager : MonoBehaviour
    {
        [Header("Spacetime DB Configuration")]
        [SerializeField] private string serverUrl = "http://127.0.0.1:3000";
        [SerializeField] private string moduleName = "solarville_server";

        [Header("Player Settings")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Transform playersContainer;

        // Events
        public static event Action OnConnected;
        public static event Action OnSubscriptionApplied;

        // Static properties
        public static GameManager Instance { get; private set; }
        public static Identity LocalIdentity { get; private set; }
        public static DbConnection Conn { get; private set; }

        // Player tracking
        public static Dictionary<uint, PlayerController> Players = new Dictionary<uint, PlayerController>();
        private PlayerController _localPlayerController;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            Application.targetFrameRate = 60;
            ConnectToSpacetimeDB();
        }

        private void ConnectToSpacetimeDB()
        {
            Debug.Log("Connecting to SpacetimeDB...");

            // Initialize connection to SpacetimeDB
            var builder = DbConnection.Builder()
                .OnConnect(HandleConnect)
                .OnConnectError(HandleConnectError)
                .OnDisconnect(HandleDisconnect)
                .WithUri(serverUrl)
                .WithModuleName(moduleName);

            // Use auth token if available
            if (AuthToken.Token != "")
            {
                builder = builder.WithToken(AuthToken.Token);
            }

            // Connect to the server
            Conn = builder.Build();
        }

        private void HandleConnect(DbConnection conn, Identity identity, string token)
        {
            Debug.Log("Connected to SpacetimeDB server.");
            
            // Save the token and identity
            AuthToken.SaveToken(token);
            LocalIdentity = identity;

            // Register callbacks for table updates
            conn.Db.Player.OnInsert += PlayerOnInsert;
            conn.Db.Player.OnUpdate += PlayerOnUpdate;
            conn.Db.Player.OnDelete += PlayerOnDelete;

            // Fire the connected event
            OnConnected?.Invoke();

            // Request all tables
            Conn.SubscriptionBuilder()
                .OnApplied(HandleSubscriptionApplied)
                .SubscribeToAllTables();
        }

        private void HandleConnectError(Exception ex)
        {
            Debug.LogError($"Connection error: {ex}");
        }

        private void HandleDisconnect(DbConnection conn, Exception ex)
        {
            Debug.Log("Disconnected from SpacetimeDB server.");
            if (ex != null)
            {
                Debug.LogException(ex);
            }
        }

        private void HandleSubscriptionApplied(SubscriptionEventContext ctx)
        {
            Debug.Log("Subscription applied successfully!");
            OnSubscriptionApplied?.Invoke();
        }

        // Player management methods
        private static void PlayerOnInsert(EventContext context, Player insertedPlayer)
        {
            Debug.Log($"Player inserted: {insertedPlayer.Name}");
            GetOrCreatePlayer(insertedPlayer);
        }

        private static void PlayerOnUpdate(EventContext context, Player oldPlayer, Player newPlayer)
        {
            Debug.Log($"Player updated: {newPlayer.Name}");
            
            if (Players.TryGetValue(newPlayer.PlayerId, out var playerController))
            {
                playerController.UpdateFromDatabase(newPlayer);
            }
            else
            {
                GetOrCreatePlayer(newPlayer);
            }
        }

        private static void PlayerOnDelete(EventContext context, Player deletedPlayer)
        {
            Debug.Log($"Player deleted: {deletedPlayer.Name}");
            
            if (Players.TryGetValue(deletedPlayer.PlayerId, out var playerController))
            {
                Players.Remove(deletedPlayer.PlayerId);
                Destroy(playerController.gameObject);
            }
        }

        private static PlayerController GetOrCreatePlayer(Player player)
        {
            if (!Players.TryGetValue(player.PlayerId, out var playerController))
            {
                var playerObject = Instantiate(Instance.playerPrefab, Instance.playersContainer);
                playerController = playerObject.GetComponent<PlayerController>();
                playerController.Initialize(player);
                Players.Add(player.PlayerId, playerController);

                // Set as local player if the identity matches
                if (player.Identity == LocalIdentity)
                {
                    Instance._localPlayerController = playerController;
                }
            }

            return playerController;
        }

        // Register a new player or update the local player's info
        public void RegisterLocalPlayer(string playerName)
        {
            if (!IsConnected())
            {
                Debug.LogError("Cannot register player: Not connected to SpacetimeDB");
                return;
            }

            Vector3 startPosition = new Vector3(0, 1, 0); // Default starting position
            Vector3 startRotation = Vector3.zero;

            // Convert Unity Vector3 to DbVector3
            var dbPosition = new DbVector3 { 
                X = startPosition.x, 
                Y = startPosition.y, 
                Z = startPosition.z 
            };
            
            var dbRotation = new DbVector3 { 
                X = startRotation.x, 
                Y = startRotation.y, 
                Z = startRotation.z 
            };

            // Call the reducer to register the player
            Conn.Reducers.RegisterPlayer(playerName, dbPosition, dbRotation);
        }

        // Utility methods
        public static bool IsConnected()
        {
            return Conn != null && Conn.IsActive;
        }

        public void Disconnect()
        {
            Conn?.Disconnect();
            Conn = null;
        }

        private void OnDestroy()
        {
            Disconnect();
        }
    }
}
