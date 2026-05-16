using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Keeps a child viewport at a fixed aspect ratio inside its parent RectTransform.
/// For the fake OS this means the playable UI stays 16:9 while taller displays such as 16:10
/// receive top/bottom letterboxes instead of stretching the 1920x1080 stage vertically.
/// </summary>
[ExecuteAlways]
public sealed class FixedAspectViewport : MonoBehaviour
{
    [Header("Viewport")]
    [SerializeField] private RectTransform parentRect;
    [SerializeField] private RectTransform gameViewport;
    [SerializeField] private float targetAspect = 16f / 9f;
    [SerializeField] private bool roundToWholePixels = true;

    [Header("Letterboxes")]
    [SerializeField] private RectTransform letterboxTop;
    [SerializeField] private RectTransform letterboxBottom;
    [SerializeField] private Color letterboxColor = Color.black;
    [SerializeField] private bool applyLetterboxColor = true;

    [Header("Diagnostics")]
    [SerializeField] private bool logOnRefresh;

    private Vector2 lastParentSize;
    private Vector2 lastViewportSize;
    private Vector2 lastLetterboxSize;
    private float lastTopLetterboxHeight;
    private float lastBottomLetterboxHeight;

    public RectTransform ParentRect => parentRect;
    public RectTransform GameViewport => gameViewport;
    public RectTransform LetterboxTop => letterboxTop;
    public RectTransform LetterboxBottom => letterboxBottom;
    public float TargetAspect => targetAspect;
    public Vector2 LastParentSize => lastParentSize;
    public Vector2 LastViewportSize => lastViewportSize;
    public Vector2 LastLetterboxSize => lastLetterboxSize;
    public float LastVerticalLetterboxTotal => lastTopLetterboxHeight + lastBottomLetterboxHeight;
    public float LastLetterboxEach => lastLetterboxSize.y;
    public float LastTopLetterboxHeight => lastTopLetterboxHeight;
    public float LastBottomLetterboxHeight => lastBottomLetterboxHeight;

    private void Reset()
    {
        parentRect = transform.parent as RectTransform;
        gameViewport = transform as RectTransform;
    }

    private void Awake()
    {
        ResolveDefaults();
        RefreshNow();
    }

    private void OnEnable()
    {
        ResolveDefaults();
        RefreshNow();
    }

    private void OnValidate()
    {
        targetAspect = Mathf.Max(0.01f, targetAspect);
        ResolveDefaults();
        RefreshNow();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!isActiveAndEnabled)
            return;

        RefreshNow();
    }

    public void RefreshNow()
    {
        ResolveDefaults();

        if (parentRect == null || gameViewport == null || targetAspect <= 0f)
            return;

        Rect parent = parentRect.rect;
        float parentWidth = Mathf.Max(0f, parent.width);
        float parentHeight = Mathf.Max(0f, parent.height);

        if (parentWidth <= 0f || parentHeight <= 0f)
            return;

        float parentAspect = parentWidth / parentHeight;
        float viewportWidth;
        float viewportHeight;

        if (parentAspect < targetAspect)
        {
            // Taller/narrower than 16:9, e.g. 16:10. Use full width and letterbox vertically.
            viewportWidth = parentWidth;
            viewportHeight = parentWidth / targetAspect;
        }
        else
        {
            // 16:9 fills the parent. Wider ratios keep a centered 16:9 viewport for future expansion.
            viewportHeight = parentHeight;
            viewportWidth = parentHeight * targetAspect;
        }

        viewportWidth = Mathf.Min(viewportWidth, parentWidth);
        viewportHeight = Mathf.Min(viewportHeight, parentHeight);

        if (roundToWholePixels)
        {
            viewportWidth = Mathf.Round(viewportWidth);
            viewportHeight = Mathf.Round(viewportHeight);
        }

        float verticalLetterboxTotal = Mathf.Max(0f, parentHeight - viewportHeight);
        float topLetterboxHeight = verticalLetterboxTotal * 0.5f;
        float bottomLetterboxHeight = verticalLetterboxTotal * 0.5f;
        if (roundToWholePixels)
        {
            topLetterboxHeight = Mathf.Ceil(verticalLetterboxTotal * 0.5f);
            bottomLetterboxHeight = Mathf.Floor(verticalLetterboxTotal * 0.5f);
        }

        ApplyCenteredRect(gameViewport, viewportWidth, viewportHeight, Vector2.zero);
        ApplyLetterbox(letterboxTop, parentWidth, topLetterboxHeight, new Vector2(0f, (viewportHeight * 0.5f) + (topLetterboxHeight * 0.5f)));
        ApplyLetterbox(letterboxBottom, parentWidth, bottomLetterboxHeight, new Vector2(0f, (-viewportHeight * 0.5f) - (bottomLetterboxHeight * 0.5f)));

        lastParentSize = new Vector2(parentWidth, parentHeight);
        lastViewportSize = new Vector2(viewportWidth, viewportHeight);
        lastLetterboxSize = new Vector2(parentWidth, verticalLetterboxTotal * 0.5f);
        lastTopLetterboxHeight = topLetterboxHeight;
        lastBottomLetterboxHeight = bottomLetterboxHeight;

        if (logOnRefresh)
            Debug.Log(GetDiagnostics("RefreshNow"));
    }

    public string GetDiagnostics(string label)
    {
        return $"[FixedAspectViewport] {label} parent={lastParentSize}, targetAspect={targetAspect:0.####}, " +
               $"viewport={lastViewportSize}, verticalLetterboxTotal={LastVerticalLetterboxTotal:0.##}, " +
               $"top={lastTopLetterboxHeight:0.##}, bottom={lastBottomLetterboxHeight:0.##}";
    }

    private void ResolveDefaults()
    {
        if (parentRect == null)
            parentRect = transform.parent as RectTransform;

        if (gameViewport == null)
            gameViewport = transform as RectTransform;
    }

    private void ApplyCenteredRect(RectTransform target, float width, float height, Vector2 anchoredPosition)
    {
        if (target == null)
            return;

        target.anchorMin = new Vector2(0.5f, 0.5f);
        target.anchorMax = new Vector2(0.5f, 0.5f);
        target.pivot = new Vector2(0.5f, 0.5f);
        target.anchoredPosition = anchoredPosition;
        target.sizeDelta = new Vector2(width, height);
    }

    private void ApplyLetterbox(RectTransform target, float width, float height, Vector2 anchoredPosition)
    {
        if (target == null)
            return;

        ApplyCenteredRect(target, width, height, anchoredPosition);
        target.gameObject.SetActive(height > 0.01f);

        if (!applyLetterboxColor)
            return;

        Image image = target.GetComponent<Image>();
        if (image != null)
            image.color = letterboxColor;
    }
}
