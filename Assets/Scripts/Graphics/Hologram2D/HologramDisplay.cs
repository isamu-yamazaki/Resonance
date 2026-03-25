using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class HologramDisplay : MonoBehaviour
{
    [Header("Texture")]
    [SerializeField] private Texture2D image;

    [Header("Animation")]
    [SerializeField] private float flickerSpeed = 3f;
    [SerializeField] private float glitchInterval = 4f;
    [SerializeField] private float glitchDuration = 0.15f;

    [Header("Appearance")]
    [SerializeField] private Color hologramColor = new Color(0f, 0.9f, 1f, 1f);
    [SerializeField] private float baseOpacity = 0.75f;

    private Material material;
    private float internalTime;
    private float glitchTimer;
    private bool isGlitching;
    private float glitchEndTime;

    private static readonly int NoiseTimeID = Shader.PropertyToID("_NoiseTime");
    private static readonly int GlitchStrengthID = Shader.PropertyToID("_GlitchStrength");
    private static readonly int HoloColorID = Shader.PropertyToID("_HoloColor");
    private static readonly int OpacityID = Shader.PropertyToID("_Opacity");
    private static readonly int MainTexID = Shader.PropertyToID("_MainTex");

    private void Awake()
    {
        material = GetComponent<MeshRenderer>().material;

        if (image != null)
            material.SetTexture(MainTexID, image);

        material.SetColor(HoloColorID, hologramColor);
        material.SetFloat(OpacityID, baseOpacity);
    }

    private void Update()
    {
        internalTime += Time.deltaTime * flickerSpeed;
        material.SetFloat(NoiseTimeID, internalTime);

        TickGlitch();
    }

    private void TickGlitch()
    {
        if (isGlitching)
        {
            if (Time.time >= glitchEndTime)
            {
                isGlitching = false;
                material.SetFloat(GlitchStrengthID, 0.08f);
                glitchTimer = glitchInterval + Random.Range(-1f, 1f);
            }
        }
        else
        {
            glitchTimer -= Time.deltaTime;
            if (glitchTimer <= 0f)
            {
                isGlitching = true;
                glitchEndTime = Time.time + glitchDuration;
                material.SetFloat(GlitchStrengthID, Random.Range(0.15f, 0.4f));
            }
        }
    }

    private void OnValidate()
    {
        if (material == null) return;
        material.SetColor(HoloColorID, hologramColor);
        material.SetFloat(OpacityID, baseOpacity);
        if (image != null) material.SetTexture(MainTexID, image);
    }
}
