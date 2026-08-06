using System.Threading.Tasks;
using Resonance.BuildTools;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace Resonance.GameBootstrap
{
    /// <summary>
    /// Load the next scene if all environment variables are set.
    /// If not set, the script provides a way to manually load the scene,
    /// starting the game as the server.
    /// </summary>
    public class ServerStartSceneNextSceneLoader : MonoBehaviour
    {
        [FormerlySerializedAs("gameSceneName")] [SerializeField]
        private string nextSceneName = "GameBootstrapScene";

        [SerializeField] private string editorOrchestratorUrl = "http://localhost:9000";
        [SerializeField] private ushort editorGameServerPort = 7777;
        [SerializeField] private string editorGameMode = "Arena";
        [SerializeField] private string editorMatchId = "test-match-id";
        [SerializeField] private string editorMatchKey = "test-match-key";

        private void Awake()
        {
            LoadNextScene();
        }

        private void LoadNextScene()
        {
            if (EnvironmentVariablesReceiver.Instance.AllVariablesSet)
            {
                SceneManager.LoadScene(nextSceneName);
            }
            else
            {
                Debug.LogWarning(
                    "[ServerLobbyCodeReader] Missing -lobbyCode or -orchestratorUrl. Use the inspector button to load manually.");
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
                nextSceneName,
                editorGameMode
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