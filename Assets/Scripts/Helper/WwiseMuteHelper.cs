using UnityEditor;
using UnityEngine;

namespace Resonance.Helper
{
#if UNITY_EDITOR
    public class WwiseMuteHelper : MonoBehaviour
    {
        AkAudioListener _akAudioListener;

        void Awake()
        {
            _akAudioListener = FindFirstObjectByType<AkAudioListener>();
        }

        void Update()
        {
            bool muted = EditorUtility.audioMasterMute;
            _akAudioListener.enabled = !muted;
            AkUnitySoundEngine.SetOutputVolume(0, muted ? 0f : 1f);
        }

        void OnDestroy()
        {
            AkUnitySoundEngine.SetOutputVolume(0, 1f);
        }
    }
#endif
}
