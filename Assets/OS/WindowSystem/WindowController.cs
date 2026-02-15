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
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Buttons")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button minimizeButton;
    [SerializeField] private Button pinToggleButton;

    [Header("Skin")]
    [SerializeField] private UISkin skin;
    [SerializeField] private Image titleBarImage;
    [SerializeField] private Image frameImage;

    [Header("Drag")]
    [SerializeField] private float minVisibleTitleBarHeight = 20f;

    private WindowManager owner;
    private RectTransform canvasRect;
    private bool isPinned;
    private bool buttonsHooked;

    public string AppId => appId;
    public RectTransform WindowRoot => windowRoot;

    public bool IsMinimized { get; private set; }

    private void Awake()
    {
        if (canvasGroup == null)
        {
            var go = (windowRoot != null ? windowRoot.gameObject : gameObject);
            canvasGroup = go.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = go.AddComponent<CanvasGroup>();
        }
    }

    public void Initialize(WindowManager wm, string newAppId, RectTransform newCanvasRect)
    {
        owner = wm;
        appId = newAppId;
        canvasRect = newCanvasRect;

        HookButtons(); // 프리팹에서 Awake 전에 세팅될 수도 있어서 안전하게 한 번 더
    }

    public void SetMinimized(bool minimized)
    {
        IsMinimized = minimized;

        canvasGroup.alpha = minimized ? 0f : 1f;
        canvasGroup.interactable = !minimized;
        canvasGroup.blocksRaycasts = !minimized;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsMinimized)
            owner?.Focus(appId);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!IsMinimized)
            owner?.Focus(appId);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (IsMinimized) return;
        if (isPinned || windowRoot == null || canvasRect == null) return;

        windowRoot.anchoredPosition += eventData.delta;
        ClampToCanvas();

        owner?.OnWindowMoved(appId, windowRoot.anchoredPosition);
    }

    public void SetActiveVisual(bool isActive)
    {
        if (skin == null) return;

        if (titleBarImage != null)
            titleBarImage.color = isActive ? skin.titleActiveColor : skin.titleInactiveColor;

        if (frameImage != null)
            frameImage.color = isActive ? skin.frameActiveColor : skin.frameInactiveColor;
    }

    public void SetWindowPosition(Vector2 anchoredPosition)
    {
        if (windowRoot == null) return;

        windowRoot.anchoredPosition = anchoredPosition;
        ClampToCanvas();
    }

    private void HookButtons()
    {
        if (buttonsHooked) return;
        buttonsHooked = true;

        if (closeButton != null)
            closeButton.onClick.AddListener(() => owner?.Close(appId));

        if (minimizeButton != null)
            minimizeButton.onClick.AddListener(() => owner?.Minimize(appId));

        if (pinToggleButton != null)
            pinToggleButton.onClick.AddListener(TogglePin);
    }

    private void TogglePin()
    {
        isPinned = !isPinned;
    }

    private void ClampToCanvas()
    {
        if (windowRoot == null || canvasRect == null) return;

        Vector2 windowSize = windowRoot.rect.size;
        Vector2 canvasSize = canvasRect.rect.size;

        float halfWidth = windowSize.x * 0.5f;
        float halfHeight = windowSize.y * 0.5f;

        float minX = -canvasSize.x * 0.5f + halfWidth;
        float maxX = canvasSize.x * 0.5f - halfWidth;

        float visibleTitle = Mathf.Max(
            minVisibleTitleBarHeight,
            titleBar != null ? titleBar.rect.height : minVisibleTitleBarHeight
        );

        float minY = -canvasSize.y * 0.5f + halfHeight;
        float maxY = canvasSize.y * 0.5f - visibleTitle;

        Vector2 pos = windowRoot.anchoredPosition;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        windowRoot.anchoredPosition = pos;
    }
}
