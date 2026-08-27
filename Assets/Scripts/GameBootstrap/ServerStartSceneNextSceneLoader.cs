using Resonance.BuildTools;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Resonance.GameBootstrap
{
    /// <summary>
    /// Load the next scene if all environment variables are set.
    /// If not set, the script provides a way to manually load the scene,
    /// starting the game as the server.
    /// </summary>
    public class ServerStartSceneNextSceneLoader : MonoBehaviour
    {
        [SerializeField] private string bootstrapSceneName = "GameBootstrapScene";

        [SerializeField] private string editorOrchestratorUrl = "http://localhost:9000";
        [SerializeField] private ushort editorGameServerPort = 7777;
        [SerializeField] private string editorGameMode = "Arena";
        [SerializeField] private string editorMatchId = "test-match-id";
        [SerializeField] private string editorMatchKey = "test-match-key";
        [SerializeField] private string editorNextSceneName = "NightCity";
        [SerializeField] private string intendedServerVersion = "dev";

        private void Awake()
        {
            LoadNextScene();
        }

        private void LoadNextScene()
        {
            if (EnvironmentVariablesReceiver.Instance.AllVariablesSet)
            {
                SceneManager.LoadScene(bootstrapSceneName);
            }
            else
            {
                Debug.LogWarning(
                    "[ServerLobbyCodeReader] Not all environment variables are set. Use the editor to set them manually.");
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Set editor variables")]
        private void SetEditorVariables()
        {
            EnvironmentVariablesReceiver.Instance.SetVariables(
                editorGameServerPort,
                editorMatchId,
                editorMatchKey,
                editorOrchestratorUrl,
                editorNextSceneName,
                editorGameMode,
                intendedServerVersion
            );
        }

        [ContextMenu("Load the next scene manually")]
        private void LoadManually()
        {
            LoadNextScene();
        }
#endif
    }
}