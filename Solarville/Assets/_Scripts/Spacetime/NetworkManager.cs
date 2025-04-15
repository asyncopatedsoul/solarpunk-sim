using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Solarville.Spacetime
{
    /// <summary>
    /// Manages the network connection UI and connections
    /// </summary>
    public class NetworkManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject connectionPanel;
        [SerializeField] private TMP_InputField nameInputField;
        [SerializeField] private Button connectButton;
        [SerializeField] private TextMeshProUGUI connectionStatus;
        
        [Header("References")]
        [SerializeField] private GameManager gameManager;

        private void Start()
        {
            // Initialize UI
            if (connectionPanel != null)
                connectionPanel.SetActive(true);
            
            if (connectionStatus != null)
                connectionStatus.text = "Not Connected";
            
            if (connectButton != null)
                connectButton.onClick.AddListener(OnConnectButtonClicked);

            // Register for connection events
            GameManager.OnConnected += HandleConnected;
            GameManager.OnSubscriptionApplied += HandleSubscriptionApplied;
        }

        private void OnDestroy()
        {
            // Unregister from events
            GameManager.OnConnected -= HandleConnected;
            GameManager.OnSubscriptionApplied -= HandleSubscriptionApplied;
            
            if (connectButton != null)
                connectButton.onClick.RemoveListener(OnConnectButtonClicked);
        }

        private void HandleConnected()
        {
            if (connectionStatus != null)
                connectionStatus.text = "Connected! Synchronizing data...";
        }

        private void HandleSubscriptionApplied()
        {
            if (connectionStatus != null)
                connectionStatus.text = "Synchronized! Joining game...";
            
            // Get player name from input field or generate a random one
            string playerName = string.IsNullOrEmpty(nameInputField.text) 
                ? $"Player_{UnityEngine.Random.Range(1000, 9999)}" 
                : nameInputField.text;
            
            // Register the player with the server
            if (gameManager != null)
                gameManager.RegisterLocalPlayer(playerName);
            
            // Hide the connection panel
            if (connectionPanel != null)
                connectionPanel.SetActive(false);
        }

        private void OnConnectButtonClicked()
        {
            // This UI button assumes the GameManager is already trying to connect
            // We just update the status text to show we're attempting to connect
            if (connectionStatus != null)
                connectionStatus.text = "Connecting...";
            
            // Disable the connect button to prevent multiple clicks
            if (connectButton != null)
                connectButton.interactable = false;

            //gameManager.ConnectToSpacetimeDB();
        }
    }
}
