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

    [Header("Buttons")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button minimizeButton;
    [SerializeField] private Button pinToggleButton;

    [Header("Skin")]
    [SerializeField] private UISkin skin;
    [SerializeField] private Image titleBarImage;   // 타이틀바 배경 Image
    [SerializeField] private Image frameImage;      // 창 프레임/바탕 Image (선택)


    [Header("Drag")]
    [SerializeField] private float minVisibleTitleBarHeight = 20f;

    // WindowController 내부
    [SerializeField] private CanvasGroup cg;

    public bool IsMinimized { get; private set; }

    public void SetMinimized(bool minimized)
    {
        IsMinimized = minimized;

        if (cg == null) cg = GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = minimized ? 0f : 1f;
            cg.interactable = !minimized;
            cg.blocksRaycasts = !minimized;
        }
    }


    private WindowManager owner;
    private RectTransform canvasRect;
    private bool isPinned;

    public string AppId => appId;
    public RectTransform WindowRoot => windowRoot;


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

        HookButtons(); // ✅ owner가 확실히 세팅된 뒤 한 번 더 보장
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
        if (skin == null) return;

        if (titleBarImage != null)
        {
            titleBarImage.color =
                isActive ? skin.titleActiveColor
                         : skin.titleInactiveColor;
        }

        if (frameImage != null)
        {
            frameImage.color =
                isActive ? skin.frameActiveColor
                         : skin.frameInactiveColor;
        }
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

    private bool buttonsHooked;

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
