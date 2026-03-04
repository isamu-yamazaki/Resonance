using UnityEngine;

public class LobbySceneNavigator : MonoBehaviour
{
    public string NextScene;

    public void NavigateToNextScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(NextScene);
    }
}
