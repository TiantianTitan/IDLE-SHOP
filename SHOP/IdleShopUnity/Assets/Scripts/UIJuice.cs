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
    private RectTransform rect = null!;
    private Vector3 baseScale;
    private Vector3 targetScale;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        baseScale = rect.localScale;
        targetScale = baseScale;
    }

    private void Update()
    {
        rect.localScale = Vector3.Lerp(rect.localScale, targetScale, Time.unscaledDeltaTime * 18f);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Button button = GetComponent<Button>();
        if (button != null && !button.interactable)
        {
            return;
        }

        targetScale = baseScale * 0.96f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        targetScale = baseScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = baseScale;
    }
}

public sealed class UIAppearAnimator : MonoBehaviour
{
    private CanvasGroup group = null!;
    private RectTransform rect = null!;
    private float age;
    private Vector3 targetScale;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        group = gameObject.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = gameObject.AddComponent<CanvasGroup>();
        }

        targetScale = rect.localScale;
        rect.localScale = targetScale * 0.985f;
        group.alpha = 0f;
    }

    private void Update()
    {
        age += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(age * 7.5f);
        float eased = 1f - Mathf.Pow(1f - t, 3f);
        group.alpha = eased;
        rect.localScale = Vector3.LerpUnclamped(targetScale * 0.985f, targetScale, eased);

        if (t >= 1f)
        {
            Destroy(this);
        }
    }
}
