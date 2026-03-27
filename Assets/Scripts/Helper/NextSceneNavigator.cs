using UnityEngine;

namespace Resonance.Helper
{
    public class NextSceneNavigator : MonoBehaviour
    {
        public string NextScene;

        public void NavigateToNextScene()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(NextScene);
        }
    }

}
