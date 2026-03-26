#if !UNITY_SERVER
using UnityEngine;
using System.Collections.Generic;

namespace Resonance.Audio
{
    // Monitors audio bus intensity from Wwise RTPCs (Foley, SFX, Environment)
    // Provides normalized 0-1 values for reactive objects
    public class AudioBusMonitor : MonoBehaviour
    {
        #region Singleton
        public static AudioBusMonitor Instance { get; private set; }
        #endregion

        #region Inspector Fields
        [Header("RTPC Configuration")]
        [SerializeField] private AK.Wwise.RTPC foleyRTPC;
        [SerializeField] private AK.Wwise.RTPC sfxRTPC;
        [SerializeField] private AK.Wwise.RTPC environmentRTPC;

        [Header("Update Rate")]
        [SerializeField] private float updateInterval = 0.05f; // 20Hz
        #endregion

        #region Private Fields
        private Dictionary<BusType, float> _intensities;
        private Dictionary<BusType, AK.Wwise.RTPC> _rtpcs;
        private BusType[] _busTypes;
        private float _updateTimer;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            InitializeSingleton();
            InitializeBusData();
        }

        private void Update()
        {
            _updateTimer += Time.deltaTime;
            if (_updateTimer < updateInterval) return;
            _updateTimer = 0f;
            UpdateBusIntensities();
        }
        #endregion

        #region Initialization
        private void InitializeSingleton()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void InitializeBusData()
        {
            _busTypes = (BusType[])System.Enum.GetValues(typeof(BusType));

            _intensities = new Dictionary<BusType, float>(_busTypes.Length);
            _rtpcs = new Dictionary<BusType, AK.Wwise.RTPC>(_busTypes.Length);

            _rtpcs[BusType.Foley] = foleyRTPC;
            _rtpcs[BusType.SFX] = sfxRTPC;
            _rtpcs[BusType.Environment] = environmentRTPC;

            foreach (BusType busType in _busTypes)
                _intensities[busType] = 0f;
        }
        #endregion

        #region Update Logic
        private void UpdateBusIntensities()
        {
            foreach (BusType busType in _busTypes)
                _intensities[busType] = QueryRTPCValue(busType);
        }

        private float QueryRTPCValue(BusType busType)
        {
            if (!_rtpcs.ContainsKey(busType) || _rtpcs[busType] == null)
            {
                Debug.LogWarning($"[AudioBusMonitor] RTPC not assigned for {busType}!");
                return 0f;
            }

            // Wwise Meter outputs in dB range: -48 (silence) to 0 (full scale)
            float rtpcValue = _rtpcs[busType].GetGlobalValue();
            return Mathf.Clamp01((rtpcValue + 48f) / 48f);
        }
        #endregion

        #region Public API
        public float GetBusIntensity(BusType busType)
        {
            return _intensities.TryGetValue(busType, out float value) ? value : 0f;
        }

        public float GetMaxBusIntensity()
        {
            float maxIntensity = 0f;

            foreach (float intensity in _intensities.Values)
                maxIntensity = Mathf.Max(maxIntensity, intensity);

            return maxIntensity;
        }

        public BusType GetLoudestBus()
        {
            BusType loudestBus = BusType.Foley;
            float maxIntensity = 0f;

            foreach (var kvp in _intensities)
            {
                if (kvp.Value > maxIntensity)
                {
                    maxIntensity = kvp.Value;
                    loudestBus = kvp.Key;
                }
            }

            return loudestBus;
        }

        public IReadOnlyDictionary<BusType, float> GetAllBusIntensities()
        {
            return _intensities;
        }
        #endregion
    }
}
#endif
