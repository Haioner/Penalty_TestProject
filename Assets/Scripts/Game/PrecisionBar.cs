using UnityEngine;

public enum PrecisionZone
{
    Perfect, Medium, Miss
}

public class PrecisionBar : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Vector2 minMaxSize = new Vector2(0.3f, 1.5f);
    [SerializeField] private float scaleSpeed = 1f;

    [Header("Precision Thresholds")]
    [SerializeField] private float perfectThreshold = 0.4f;
    [SerializeField] private float mediumThreshold = 0.7f;

    private SpriteRenderer spriteRenderer;
    private bool isScaling = false;
    private float currentScale;
    private bool isGrowing = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentScale = minMaxSize.y;
    }

    public void StartScaling()
    {
        isScaling = true;
        currentScale = minMaxSize.y;
        isGrowing = false;
        gameObject.SetActive(true);
    }

    public void StopScaling()
    {
        isScaling = false;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!isScaling)
            return;

        if (isGrowing)
        {
            currentScale += scaleSpeed * Time.deltaTime;
            if (currentScale >= minMaxSize.y)
            {
                currentScale = minMaxSize.y;
                isGrowing = false;
            }
        }
        else
        {
            currentScale -= scaleSpeed * Time.deltaTime;
            if (currentScale <= minMaxSize.x)
            {
                currentScale = minMaxSize.x;
                isGrowing = true;
            }
        }

        transform.localScale = Vector3.one * currentScale;
        UpdateColor();
    }

    private void UpdateColor()
    {
        float normalizedSize = Mathf.InverseLerp(minMaxSize.x, minMaxSize.y, currentScale);
        
        if (normalizedSize <= perfectThreshold)
        {
            spriteRenderer.color = Color.green;
        }
        else if (normalizedSize <= mediumThreshold)
        {
            spriteRenderer.color = Color.yellow;
        }
        else
        {
            spriteRenderer.color = Color.red;
        }
    }

    public PrecisionZone GetCurrentPrecision()
    {
        float normalizedSize = Mathf.InverseLerp(minMaxSize.x, minMaxSize.y, currentScale);
        
        if (normalizedSize <= perfectThreshold)
            return PrecisionZone.Perfect;
        else if (normalizedSize <= mediumThreshold)
            return PrecisionZone.Medium;
        else
            return PrecisionZone.Miss;
    }
}
