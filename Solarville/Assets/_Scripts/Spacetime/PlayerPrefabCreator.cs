using UnityEngine;

namespace Solarville.Spacetime
{
    /// <summary>
    /// Utility class that creates a basic player character prefab for testing
    /// </summary>
    public class PlayerPrefabCreator : MonoBehaviour
    {
        [Header("Player Prefab Settings")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private string savePath = "Assets/_Prefabs/PlayerCharacter.prefab";

        // Color for the player model
        private Color playerColor = new Color(0.2f, 0.7f, 0.2f);

        /// <summary>
        /// Creates a simple player character prefab with basic components
        /// </summary>
        public void CreatePlayerPrefab()
        {
            GameObject player = new GameObject("PlayerCharacter");

            // Add a capsule as the player's visual representation
            GameObject playerModel = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerModel.transform.SetParent(player.transform);
            playerModel.transform.localPosition = new Vector3(0, 1, 0);
            playerModel.transform.localScale = new Vector3(1, 1, 1);
            playerModel.name = "PlayerModel";

            // Set a material color
            MeshRenderer renderer = playerModel.GetComponent<MeshRenderer>();
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = playerColor;
            renderer.material = material;

            // Add character controller
            CharacterController characterController = player.AddComponent<CharacterController>();
            characterController.center = new Vector3(0, 1, 0);
            characterController.height = 2.0f;
            characterController.radius = 0.5f;
            characterController.skinWidth = 0.08f;

            // Add player controller script
            PlayerController playerController = player.AddComponent<PlayerController>();

            // Optional - Add reference to the model
            SerializedProperty modelProperty = new SerializedObject(playerController).FindProperty("playerModel");
            if (modelProperty != null)
            {
                modelProperty.objectReferenceValue = playerModel.transform;
            }

            SerializedProperty rendererProperty = new SerializedObject(playerController).FindProperty("playerModelRenderer");
            if (rendererProperty != null)
            {
                rendererProperty.objectReferenceValue = renderer;
            }

            Debug.Log("Player prefab created. You should save it manually in the editor.");
            playerPrefab = player;
        }
    }
}
