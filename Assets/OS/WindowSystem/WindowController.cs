using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WindowController : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler
{
    [Header("Identity")]
    [SerializeField] private string appId;

    [Header("Window Parts")]
    [SerializeField] private RectTransform windowRoot;
    [SerializeField] private RectTransform titleBar;
    [SerializeField] private SkinApplier skinApplier;

    [Header("Buttons")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button minimizeButton;
    [SerializeField] private Button pinToggleButton;

    [Header("Drag")]
    [SerializeField] private float minVisibleTitleBarHeight = 20f;

    private WindowManager owner;
    private RectTransform canvasRect;
    private bool isPinned;

    public string AppId => appId;
    public bool IsMinimized { get; private set; }
    public RectTransform WindowRoot => windowRoot;

    public void SetMinimized(bool minimized)
    {
        IsMinimized = minimized;
    }


    private void Awake()
    {
        if (windowRoot == null)
        {
            windowRoot = transform as RectTransform;
        }

        HookButtons();
    }

    public void Initialize(WindowManager windowManager, string id, RectTransform rootCanvas)
    {
        owner = windowManager;
        appId = id;
        canvasRect = rootCanvas;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        owner?.Focus(appId);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        owner?.Focus(appId);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isPinned || windowRoot == null || canvasRect == null)
        {
            return;
        }

        windowRoot.anchoredPosition += eventData.delta;
        ClampToCanvas();

        owner?.OnWindowMoved(appId, windowRoot.anchoredPosition);
    }

    public void SetActiveVisual(bool isActive)
    {
        skinApplier?.Apply(isActive);
    }

    public void SetWindowPosition(Vector2 anchoredPosition)
    {
        if (windowRoot == null)
        {
            return;
        }

        windowRoot.anchoredPosition = anchoredPosition;
        ClampToCanvas();
    }

    private void HookButtons()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(() => owner?.Close(appId));
        }

        if (minimizeButton != null)
        {
            minimizeButton.onClick.AddListener(() => owner?.Minimize(appId));
        }

        if (pinToggleButton != null)
        {
            pinToggleButton.onClick.AddListener(TogglePin);
        }
    }

    private void TogglePin()
    {
        isPinned = !isPinned;
    }

    private void ClampToCanvas()
    {
        if (windowRoot == null || canvasRect == null)
        {
            return;
        }

        Vector2 windowSize = windowRoot.rect.size;
        Vector2 canvasSize = canvasRect.rect.size;

        float halfWidth = windowSize.x * 0.5f;
        float halfHeight = windowSize.y * 0.5f;

        float minX = -canvasSize.x * 0.5f + halfWidth;
        float maxX = canvasSize.x * 0.5f - halfWidth;

        float visibleTitle = Mathf.Max(minVisibleTitleBarHeight, titleBar != null ? titleBar.rect.height : minVisibleTitleBarHeight);
        float minY = -canvasSize.y * 0.5f + halfHeight;
        float maxY = canvasSize.y * 0.5f - visibleTitle;

        Vector2 pos = windowRoot.anchoredPosition;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        windowRoot.anchoredPosition = pos;
    }
}
