using TMPro;
using UnityEngine;

public class DamageNumber : MonoBehaviour
{
    [SerializeField] private TextMeshPro text;
    [SerializeField] private float lifetime = 1f;
    [SerializeField] private float floatSpeed = 1.5f;
    [SerializeField] private float fadeSpeed = 2f;
    [SerializeField] private float batchWindow = 0.07f;

    private float elapsed;
    private float batchElapsed;
    private Color startColor;
    private float totalDamage;
    private bool batching;

    public bool IsBatchExpired => batching && batchElapsed >= batchWindow;

    public void Initialize(float damage)
    {
        totalDamage = damage;
        batching = false;
        elapsed = 0f;
        UpdateText();
        startColor = text.color;
    }

    public void InitializeBatched(float damage)
    {
        totalDamage = damage;
        batching = true;
        batchElapsed = 0f;
        elapsed = 0f;
        UpdateText();
        startColor = text.color;
    }

    public void AddDamage(float damage)
    {
        totalDamage += damage;
        batchElapsed = 0f;
        UpdateText();
    }

    private void UpdateText()
    {
        text.text = Mathf.RoundToInt(totalDamage).ToString();
    }

    private void Update()
    {
        if (batching)
        {
            batchElapsed += Time.deltaTime;

            if (batchElapsed < batchWindow)
            {
                return;
            }

            batching = false;
        }

        elapsed += Time.deltaTime;

        transform.forward = Camera.main.transform.forward;
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;

        float alpha = Mathf.Lerp(1f, 0f, elapsed / lifetime);
        text.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

        if (elapsed >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}