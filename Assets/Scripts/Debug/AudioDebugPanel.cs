using UnityEngine;
using Resonance.Audio;

namespace Resonance.DebugTools
{
    public class AudioDebugPanel : MonoBehaviour
    {
        #region Inspector Fields
        [Header("Test Sound Settings")]
        [SerializeField] private string testEvent = "Play_Test_Sound";
        [SerializeField] private GameObject emitter;
        #endregion
        
        #region Private Fields
        private string _customEvent = "";
        #endregion
        
        #region Unity Lifecycle
        private void Start()
        {
            FindSoundEmitter();
        }
        #endregion
        
        #region Initialization
        private void FindSoundEmitter()
        {
            if (emitter == null)
            {
                emitter = GameObject.FindGameObjectWithTag("Player");
                
                if (emitter != null)
                {
                    Debug.Log($"[AudioDebugPanel] Found emitter: {emitter.name}");
                }
            }
        }
        #endregion
        
        #region Public Methods
        public void DrawPanel()
        {
            // Re-find emitter if it's null (happens after scene changes)
            if (emitter == null)
            {
                FindSoundEmitter();
            }
            
            GUILayout.BeginVertical("box");
            GUILayout.Label("=== AUDIO DEBUG ===");
            GUILayout.Space(10);
            
            DrawTestSoundSection();
            DrawBusMonitorSection();
            
            GUILayout.EndVertical();
        }
        #endregion
        
        #region Test Sound Section
        private void DrawTestSoundSection()
        {
            GUILayout.BeginVertical("box");
            
            // Emitter status
            if (emitter != null)
            {
                GUILayout.Label($"Emitter: {emitter.name}");
            }
            else
            {
                GUI.color = Color.yellow;
                GUILayout.Label("Warning: No emitter assigned!");
                GUI.color = Color.white;
            }
            
            GUILayout.Space(10);
            
            // Default test sound
            GUILayout.Label($"Test Event: {testEvent}");
            if (GUILayout.Button("Trigger Test Sound", GUILayout.Height(40)))
            {
                TriggerTestSound();
            }
            
            GUILayout.Space(15);
            
            // Custom event
            GUILayout.Label("Custom Event Name:");
            _customEvent = GUILayout.TextField(_customEvent, GUILayout.Height(25));
            
            if (GUILayout.Button("Trigger Custom Event", GUILayout.Height(30)))
            {
                TriggerCustomSound(_customEvent);
            }
            
            GUILayout.EndVertical();
        }

        private void DrawBusMonitorSection()
        {
            GUILayout.BeginVertical("box");
            
            GUI.color = Color.cyan;
            GUILayout.Label("Bus Levels:", GUILayout.Height(20));
            GUI.color = Color.white;

            if (AudioBusMonitor.Instance != null)
            {
                float foley = AudioBusMonitor.Instance.GetBusIntensity(BusType.Foley);
                float sfx = AudioBusMonitor.Instance.GetBusIntensity(BusType.SFX);
                float environment = AudioBusMonitor.Instance.GetBusIntensity(BusType.Environment);

                GUILayout.Label($"Foley: {foley:F3}");
                GUILayout.Label($"SFX: {sfx:F3}");
                GUILayout.Label($"Environment: {environment:F3}");
                GUILayout.Label($"Max: {AudioBusMonitor.Instance.GetMaxBusIntensity():F3}");
            }
            else
            {
                GUI.color = Color.yellow;
                GUILayout.Label("AudioBusMonitor not found!");
                GUI.color = Color.white;
            }
            
            GUILayout.EndVertical();
        }
        #endregion
        
        #region Sound Triggering
        private void TriggerTestSound()
        {
            if (emitter == null)
            {
                Debug.LogWarning("[AudioDebugPanel] No emitter assigned!");
                return;
            }
            
            Debug.Log($"[AudioDebugPanel] Triggering '{testEvent}' on {emitter.name}");
            AkUnitySoundEngine.PostEvent(testEvent, emitter);
        }
        
        private void TriggerCustomSound(string eventName)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                Debug.LogWarning("[AudioDebugPanel] Event name is empty!");
                return;
            }
            
            if (emitter == null)
            {
                Debug.LogWarning("[AudioDebugPanel] No emitter assigned!");
                return;
            }
            
            Debug.Log($"[AudioDebugPanel] Triggering '{eventName}' on {emitter.name}");
            AkUnitySoundEngine.PostEvent(eventName, emitter);
        }
        #endregion
    }
}