using UnityEditor;
using UnityEngine;

namespace Resonance.Helper
{
#if UNITY_EDITOR
    public class WwiseMuteHelper : MonoBehaviour
    {
        #region Singleton
        public static WwiseMuteHelper Instance { get; private set; }
        #endregion

        #region Startup
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        #endregion

        #region Update
        void Update()
        {
            bool muted = EditorUtility.audioMasterMute;
            AkUnitySoundEngine.SetOutputVolume(0, muted ? 0f : 1f);
        }
        #endregion

        #region Cleanup
        void OnDestroy()
        {
            AkUnitySoundEngine.SetOutputVolume(0, 1f);
        }
        #endregion
    }
#endif
}
