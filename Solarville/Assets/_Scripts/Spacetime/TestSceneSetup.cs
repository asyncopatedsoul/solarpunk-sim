using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Solarville.Spacetime
{
    /// <summary>
    /// Helper class to set up a test scene for multiplayer character sync
    /// </summary>
    public class TestSceneSetup : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject mainCameraPrefab;
        [SerializeField] private GameObject gameManagerPrefab;
        [SerializeField] private GameObject networkManagerPrefab;
        [SerializeField] private GameObject playerPrefab;
        
        [Header("UI Elements")]
        [SerializeField] private GameObject connectionUIPrefab;
        
        [Header("Scene Setup")]
        [SerializeField] private bool createFloor = true;
        [SerializeField] private Vector3 floorSize = new Vector3(50, 1, 50);
        [SerializeField] private Material floorMaterial;

        /// <summary>
        /// Sets up a basic multiplayer test scene with all necessary components
        /// </summary>
        public void SetupTestScene()
        {
            // Create the floor
            if (createFloor)
            {
                CreateFloor();
            }
            
            // Create the game manager
            GameObject gameManagerObject = Instantiate(gameManagerPrefab ? gameManagerPrefab : new GameObject("GameManager"));
            GameManager gameManager = gameManagerObject.GetComponent<GameManager>() ?? gameManagerObject.AddComponent<GameManager>();
            
            // Create the network manager
            GameObject networkManagerObject = Instantiate(networkManagerPrefab ? networkManagerPrefab : new GameObject("NetworkManager"));
            NetworkManager networkManager = networkManagerObject.GetComponent<NetworkManager>() ?? networkManagerObject.AddComponent<NetworkManager>();
            
            // Create the player container
            GameObject playerContainer = new GameObject("Players");
            
            // Create the camera
            GameObject cameraObject = Instantiate(mainCameraPrefab ? mainCameraPrefab : new GameObject("Main Camera"));
            Camera camera = cameraObject.GetComponent<Camera>() ?? cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            
            CameraController cameraController = cameraObject.GetComponent<CameraController>() ?? cameraObject.AddComponent<CameraController>();
            
            // Set up the UI
            if (connectionUIPrefab)
            {
                GameObject uiObject = Instantiate(connectionUIPrefab);
                Canvas canvas = uiObject.GetComponent<Canvas>();
                if (canvas)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.worldCamera = camera;
                }
                
                // Link UI elements to the network manager
                TMP_InputField nameInput = uiObject.GetComponentInChildren<TMP_InputField>();
                Button connectButton = uiObject.GetComponentInChildren<Button>();
                TextMeshProUGUI statusText = uiObject.GetComponentInChildren<TextMeshProUGUI>();
#if UNITY_EDITOR
                if (networkManager && nameInput && connectButton && statusText)
                {
                    var serializedObject = new SerializedObject(networkManager);
                    serializedObject.FindProperty("nameInputField").objectReferenceValue = nameInput;
                    serializedObject.FindProperty("connectButton").objectReferenceValue = connectButton;
                    serializedObject.FindProperty("connectionStatus").objectReferenceValue = statusText;
                    serializedObject.FindProperty("gameManager").objectReferenceValue = gameManager;
                    serializedObject.ApplyModifiedProperties();
                }
#endif
            }
#if UNITY_EDITOR
            // Link player prefab to game manager
            if (gameManager && playerPrefab)
            {
                var serializedObject = new SerializedObject(gameManager);
                serializedObject.FindProperty("playerPrefab").objectReferenceValue = playerPrefab;
                serializedObject.FindProperty("playersContainer").objectReferenceValue = playerContainer.transform;
                serializedObject.ApplyModifiedProperties();
            }
#endif
            Debug.Log("Test scene setup complete!");
        }
        
        private void CreateFloor()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.position = new Vector3(0, -0.5f, 0);
            floor.transform.localScale = floorSize;
            
            if (floorMaterial)
            {
                floor.GetComponent<MeshRenderer>().material = floorMaterial;
            }
            else
            {
                // Create a simple material
                Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.color = new Color(0.5f, 0.5f, 0.5f);
                floor.GetComponent<MeshRenderer>().material = material;
            }
        }
    }
}
