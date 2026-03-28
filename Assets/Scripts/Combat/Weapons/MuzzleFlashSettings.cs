using UnityEngine;

namespace Resonance.Combat.Weapons
{
    [CreateAssetMenu(fileName = "MuzzleFlashSettings", menuName = "Resonance/Weapons/Muzzle Flash Settings")]
    public class MuzzleFlashSettings : ScriptableObject
    {
        [Header("Flash Light")]
        public float lightIntensity = 8f;
        public float lightRange = 3f;
        public Color lightColor = new Color(1f, 0.75f, 0.3f);
        public float lightDuration = 0.05f;

        [Header("Core Flash")]
        public float flashScale = 0.3f;
        public float flashDuration = 0.05f;
        public Color flashColor = new Color(1f, 0.85f, 0.4f);

        [Header("Sparks")]
        public int sparkCount = 12;
        public float sparkSpeed = 4f;
        public float sparkLifetime = 0.12f;
        public float sparkSpread = 25f;

        [Header("Smoke")]
        public bool emitSmoke = true;
        public int smokeCount = 3;
        public float smokeLifetime = 0.4f;
        public float smokeStartSize = 0.05f;
        public float smokeEndSize = 0.25f;
    }
}
