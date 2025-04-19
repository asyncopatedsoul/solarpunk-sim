using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SpacetimeDB.Types;

namespace Solarville.Spacetime
{
    /// <summary>
    /// UI for editing and managing Python code for microprocessors
    /// </summary>
    public class CodeEditorUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject editorPanel;
        [SerializeField] private TMP_InputField codeInputField;
        [SerializeField] private TMP_InputField nameInputField;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button runButton;
        [SerializeField] private Button stopButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Dropdown scriptsDropdown;
        
        [Header("Example Scripts")]
        [SerializeField] private TextAsset[] exampleScripts;
        [SerializeField] private string[] exampleScriptNames;

        // State
        private Dictionary<uint, MicroprocessCode> availableScripts = new Dictionary<uint, MicroprocessCode>();
        private uint? currentCodeId = null;
        private bool isInitialized = false;

        private void Start()
        {
            // Initialize UI
            if (editorPanel != null)
                editorPanel.SetActive(false);
            
            // Set up button listeners
            if (saveButton != null)
                saveButton.onClick.AddListener(SaveCode);
            
            if (runButton != null)
                runButton.onClick.AddListener(RunCode);
            
            if (stopButton != null)
                stopButton.onClick.AddListener(StopCode);
            
            if (closeButton != null)
                closeButton.onClick.AddListener(CloseEditor);
            
            if (scriptsDropdown != null)
                scriptsDropdown.onValueChanged.AddListener(OnScriptSelected);
            
            // Register for SpacetimeDB events
            if (GameManager.IsConnected())
            {
                InitializeEditor();
            }
            else
            {
                GameManager.OnSubscriptionApplied += OnSubscriptionApplied;
            }
        }

        private void OnDestroy()
        {
            // Unregister from events
            GameManager.OnSubscriptionApplied -= OnSubscriptionApplied;
            
            // Remove button listeners
            if (saveButton != null)
                saveButton.onClick.RemoveListener(SaveCode);
            
            if (runButton != null)
                runButton.onClick.RemoveListener(RunCode);
            
            if (stopButton != null)
                stopButton.onClick.RemoveListener(StopCode);
            
            if (closeButton != null)
                closeButton.onClick.RemoveListener(CloseEditor);
            
            if (scriptsDropdown != null)
                scriptsDropdown.onValueChanged.RemoveListener(OnScriptSelected);
            
            if (isInitialized)
            {
                UnregisterEvents();
            }
        }

        private void OnSubscriptionApplied()
        {
            InitializeEditor();
        }

        /// <summary>
        /// Initialize the editor and register for events
        /// </summary>
        private void InitializeEditor()
        {
            if (isInitialized)
                return;

            if (!GameManager.IsConnected())
            {
                Debug.LogWarning("Cannot initialize CodeEditorUI: Not connected to SpacetimeDB");
                return;
            }

            Debug.Log("Initializing CodeEditorUI...");

            RegisterEvents();
            LoadAvailableScripts();

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
        }

        /// <summary>
        /// Load available scripts from the database
        /// </summary>
        private void LoadAvailableScripts()
        {
            if (!GameManager.IsConnected())
                return;

            var conn = GameManager.Conn;
            
            // Clear existing scripts
            availableScripts.Clear();
            
            // Get current player
            var currentPlayer = conn.Db.Player.Identity.Find(GameManager.LocalIdentity);
            if (currentPlayer == null)
            {
                Debug.LogWarning("Cannot load scripts: Local player not found");
                return;
            }
            
            // Load scripts for current player
            foreach (var code in conn.Db.MicroprocessCode.Iter())
            {
                if (code.OwnerId == currentPlayer.PlayerId)
                {
                    availableScripts[code.CodeId] = code;
                }
            }
            
            UpdateScriptsDropdown();
        }

        /// <summary>
        /// Update the scripts dropdown with available scripts
        /// </summary>
        private void UpdateScriptsDropdown()
        {
            if (scriptsDropdown == null)
                return;
            
            // Clear existing options
            scriptsDropdown.ClearOptions();
            
            // Add "New Script" option
            List<string> options = new List<string>
            {
                "New Script"
            };
            
            // Add example scripts
            if (exampleScriptNames != null && exampleScriptNames.Length > 0)
            {
                foreach (var name in exampleScriptNames)
                {
                    options.Add($"Example: {name}");
                }
            }
            
            // Add available scripts
            foreach (var script in availableScripts.Values)
            {
                options.Add(script.Name);
            }
            
            // Update dropdown
            scriptsDropdown.AddOptions(options);
            
            // Reset to "New Script"
            scriptsDropdown.value = 0;
            OnScriptSelected(0);
        }

        /// <summary>
        /// Handle script selection from dropdown
        /// </summary>
        private void OnScriptSelected(int index)
        {
            // Clear current selection
            currentCodeId = null;
            
            if (index == 0)
            {
                // New script
                nameInputField.text = "MyScript";
                codeInputField.text = GetDefaultScriptTemplate();
                UpdateStatus("Creating new script");
            }
            else if (index >= 1 && index <= exampleScriptNames.Length)
            {
                // Example script
                int exampleIndex = index - 1;
                if (exampleScriptNames != null && exampleIndex < exampleScriptNames.Length &&
                    exampleScripts != null && exampleIndex < exampleScripts.Length)
                {
                    nameInputField.text = exampleScriptNames[exampleIndex];
                    codeInputField.text = exampleScripts[exampleIndex].text;
                    UpdateStatus($"Loaded example: {exampleScriptNames[exampleIndex]}");
                }
            }
            else
            {
                // User script
                int scriptIndex = index - 1 - (exampleScriptNames != null ? exampleScriptNames.Length : 0);
                if (scriptIndex >= 0 && scriptIndex < availableScripts.Count)
                {
                    var script = availableScripts.Values.GetEnumerator();
                    for (int i = 0; i <= scriptIndex; i++)
                    {
                        script.MoveNext();
                    }
                    
                    var code = script.Current;
                    nameInputField.text = code.Name;
                    codeInputField.text = code.CodeContent;
                    currentCodeId = code.CodeId;
                    
                    UpdateStatus($"Loaded script: {code.Name}");
                    
                    // Check if script is running
                    // TODO access MicroprocessorState instance by CodeId
                    //var state = GameManager.Conn.Db.MicroprocessState.CodeId(code.CodeId);
                    //UpdateRunStopButtons(state?.IsRunning ?? false);
                }
            }
        }

        /// <summary>
        /// Update the status message
        /// </summary>
        private void UpdateStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        /// <summary>
        /// Update the run/stop buttons based on script state
        /// </summary>
        private void UpdateRunStopButtons(bool isRunning)
        {
            if (runButton != null)
                runButton.interactable = !isRunning;
            
            if (stopButton != null)
                stopButton.interactable = isRunning;
        }

        /// <summary>
        /// Get a default script template
        /// </summary>
        private string GetDefaultScriptTemplate()
        {
            return @"";
        }

        /// <summary>
        /// Save the current code to the database
        /// </summary>
        private void SaveCode()
        {
            if (!GameManager.IsConnected())
            {
                UpdateStatus("Error: Not connected to server");
                return;
            }
            
            string name = nameInputField.text;
            string content = codeInputField.text;
            
            if (string.IsNullOrEmpty(name))
            {
                UpdateStatus("Error: Script name cannot be empty");
                return;
            }
            
            if (string.IsNullOrEmpty(content))
            {
                UpdateStatus("Error: Script content cannot be empty");
                return;
            }
            
            try
            {
                // Create a file path based on name
                string filePath = $"/scripts/{name.Replace(" ", "_")}.py";
                
                // If we have an existing code ID, update it
                if (currentCodeId.HasValue)
                {
                    GameManager.Conn.Reducers.AddMicroprocessCode(name, filePath, content);
                    UpdateStatus($"Updated script: {name}");
                }
                else
                {
                    // Otherwise create a new one
                    GameManager.Conn.Reducers.AddMicroprocessCode(name, filePath, content);
                    //currentCodeId = codeId;
                    UpdateStatus($"Saved new script: {name}");
                }
                
                // Reload scripts after a short delay
                StartCoroutine(ReloadScriptsDelayed());
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error saving script: {ex.Message}");
            }
        }

        /// <summary>
        /// Run the current code
        /// </summary>
        private void RunCode()
        {
            if (!GameManager.IsConnected())
            {
                UpdateStatus("Error: Not connected to server");
                return;
            }
            
            if (!currentCodeId.HasValue)
            {
                // Save first
                SaveCode();
                
                // Wait for save to complete
                StartCoroutine(RunCodeAfterSave());
                return;
            }
            
            try
            {
                GameManager.Conn.Reducers.StartMicroprocess(currentCodeId.Value);
                UpdateStatus($"Started script with ID: {currentCodeId.Value}");
                UpdateRunStopButtons(true);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error starting script: {ex.Message}");
            }
        }

        /// <summary>
        /// Stop the current code
        /// </summary>
        private void StopCode()
        {
            if (!GameManager.IsConnected())
            {
                UpdateStatus("Error: Not connected to server");
                return;
            }
            
            if (!currentCodeId.HasValue)
            {
                UpdateStatus("Error: No script selected");
                return;
            }
            
            try
            {
                GameManager.Conn.Reducers.StopMicroprocess(currentCodeId.Value);
                UpdateStatus($"Stopped script with ID: {currentCodeId.Value}");
                UpdateRunStopButtons(false);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error stopping script: {ex.Message}");
            }
        }

        /// <summary>
        /// Show the code editor
        /// </summary>
        public void ShowEditor()
        {
            if (editorPanel != null)
            {
                editorPanel.SetActive(true);
                
                // Make sure we have the latest scripts
                LoadAvailableScripts();
            }
        }

        /// <summary>
        /// Close the code editor
        /// </summary>
        public void CloseEditor()
        {
            if (editorPanel != null)
            {
                editorPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Reload scripts after a short delay
        /// </summary>
        private IEnumerator ReloadScriptsDelayed()
        {
            yield return new WaitForSeconds(0.5f);
            LoadAvailableScripts();
        }

        /// <summary>
        /// Run code after save completes
        /// </summary>
        private IEnumerator RunCodeAfterSave()
        {
            yield return new WaitForSeconds(0.5f);
            
            if (currentCodeId.HasValue)
            {
                try
                {
                    GameManager.Conn.Reducers.StartMicroprocess(currentCodeId.Value);
                    UpdateStatus($"Started script with ID: {currentCodeId.Value}");
                    UpdateRunStopButtons(true);
                }
                catch (Exception ex)
                {
                    UpdateStatus($"Error starting script: {ex.Message}");
                }
            }
            else
            {
                UpdateStatus("Error: Failed to save script");
            }
        }

        #region SpacetimeDB Event Handlers

        /// <summary>
        /// Handle insertion of a new microprocessor code entry
        /// </summary>
        private void OnMicroprocessCodeInsert(EventContext context, MicroprocessCode code)
        {
            // Get current player
            var currentPlayer = GameManager.Conn.Db.Player.Identity.Find(GameManager.LocalIdentity);
            if (currentPlayer == null)
                return;
            
            // Only add scripts owned by the current player
            if (code.OwnerId == currentPlayer.PlayerId)
            {
                availableScripts[code.CodeId] = code;
                UpdateScriptsDropdown();
            }
        }

        /// <summary>
        /// Handle update of an existing microprocessor code entry
        /// </summary>
        private void OnMicroprocessCodeUpdate(EventContext context, MicroprocessCode oldCode, MicroprocessCode newCode)
        {
            // Get current player
            var currentPlayer = GameManager.Conn.Db.Player.Identity.Find(GameManager.LocalIdentity);
            if (currentPlayer == null)
                return;
            
            // Only update scripts owned by the current player
            if (newCode.OwnerId == currentPlayer.PlayerId)
            {
                availableScripts[newCode.CodeId] = newCode;
                UpdateScriptsDropdown();
            }
        }

        /// <summary>
        /// Handle deletion of a microprocessor code entry
        /// </summary>
        private void OnMicroprocessCodeDelete(EventContext context, MicroprocessCode code)
        {
            if (availableScripts.ContainsKey(code.CodeId))
            {
                availableScripts.Remove(code.CodeId);
                UpdateScriptsDropdown();
            }
        }

        /// <summary>
        /// Handle insertion of a new microprocessor state
        /// </summary>
        private void OnMicroprocessStateInsert(EventContext context, MicroprocessState state)
        {
            // If this is the current script, update the run/stop buttons
            if (currentCodeId.HasValue && state.CodeId == currentCodeId.Value)
            {
                UpdateRunStopButtons(state.IsRunning);
            }
        }

        /// <summary>
        /// Handle update of an existing microprocessor state
        /// </summary>
        private void OnMicroprocessStateUpdate(EventContext context, MicroprocessState oldState, MicroprocessState newState)
        {
            // If this is the current script, update the run/stop buttons
            if (currentCodeId.HasValue && newState.CodeId == currentCodeId.Value)
            {
                UpdateRunStopButtons(newState.IsRunning);
            }
        }

        #endregion
    }
}