using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class SafeAreaFitter : MonoBehaviour
{
    [SerializeField] private float leftPadding = 22f;
    [SerializeField] private float rightPadding = 22f;
    [SerializeField] private float topPadding = 12f;
    [SerializeField] private float bottomPadding = 12f;

    private RectTransform rect = null!;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        Apply();
    }

    private void Update()
    {
        Vector2Int currentSize = new Vector2Int(Screen.width, Screen.height);
        if (currentSize != lastScreenSize || Screen.safeArea != lastSafeArea)
        {
            Apply();
        }
    }

    public void SetPadding(float left, float right, float top, float bottom)
    {
        leftPadding = left;
        rightPadding = right;
        topPadding = top;
        bottomPadding = bottom;
        Apply();
    }

    private void Apply()
    {
        if (rect == null)
        {
            rect = GetComponent<RectTransform>();
        }

        int width = Mathf.Max(Screen.width, 1);
        int height = Mathf.Max(Screen.height, 1);
        Rect safeArea = Screen.safeArea;
        if (safeArea.width <= 0f || safeArea.height <= 0f)
        {
            safeArea = new Rect(0f, 0f, width, height);
        }

        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;
        anchorMin.x = Mathf.Clamp01(anchorMin.x / width);
        anchorMin.y = Mathf.Clamp01(anchorMin.y / height);
        anchorMax.x = Mathf.Clamp01(anchorMax.x / width);
        anchorMax.y = Mathf.Clamp01(anchorMax.y / height);

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = new Vector2(leftPadding, bottomPadding);
        rect.offsetMax = new Vector2(-rightPadding, -topPadding);

        lastSafeArea = safeArea;
        lastScreenSize = new Vector2Int(width, height);
    }
}

public sealed class UIButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    private Image image = null!;
    private Color restColor;
    private Color targetColor;

    private void Awake()
    {
        image = GetComponent<Image>();
        SyncRestColor();
    }

    private void Update()
    {
        if (image != null)
        {
            image.color = Color.Lerp(image.color, targetColor, Time.unscaledDeltaTime * 18f);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Button button = GetComponent<Button>();
        if (button != null && !button.interactable)
        {
            return;
        }

        if (image != null)
        {
            restColor = image.color;
            targetColor = Color.Lerp(restColor, Color.black, 0.10f);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        targetColor = restColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetColor = restColor;
    }

    public void SyncRestColor()
    {
        restColor = image != null ? image.color : Color.white;
        targetColor = restColor;
    }
}

public sealed class UIAppearAnimator : MonoBehaviour
{
    private CanvasGroup group = null!;

    private void Awake()
    {
        group = gameObject.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = gameObject.AddComponent<CanvasGroup>();
        }

        group.alpha = 1f;
    }

    private void Update()
    {
        if (group != null)
        {
            group.alpha = 1f;
        }

        Destroy(this);
    }
}

public sealed class UIFlashTint : MonoBehaviour
{
    private Image image = null!;
    private Color baseColor;
    private Color flashColor;
    private float age;
    private float duration = 0.55f;

    public void Configure(Color color, float seconds)
    {
        image = GetComponent<Image>();
        if (image == null)
        {
            Destroy(this);
            return;
        }

        baseColor = image.color;
        flashColor = color;
        duration = Mathf.Max(0.05f, seconds);
        image.color = Color.Lerp(baseColor, flashColor, 0.36f);
    }

    private void Awake()
    {
        if (image == null)
        {
            image = GetComponent<Image>();
            baseColor = image != null ? image.color : Color.white;
            flashColor = baseColor;
        }
    }

    private void Update()
    {
        if (image == null)
        {
            Destroy(this);
            return;
        }

        age += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(age / duration);
        float eased = 1f - Mathf.Pow(1f - t, 2f);
        image.color = Color.Lerp(Color.Lerp(baseColor, flashColor, 0.36f), baseColor, eased);
        if (t >= 1f)
        {
            image.color = baseColor;
            Destroy(this);
        }
    }
}
