using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WindowController : MonoBehaviour,
    IPointerDownHandler, IBeginDragHandler, IDragHandler
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

    private WindowManager owner;
    private RectTransform canvasRect;
    private bool isPinned;

    public string AppId => appId;
    public bool IsMinimized { get; private set; }
    public RectTransform WindowRoot => windowRoot;

    public void SetWindowPosition(Vector2 anchoredPosition)
    {
        if (windowRoot == null)
            return;

        windowRoot.anchoredPosition = anchoredPosition;
    }


    private void Awake()
    {
        if (windowRoot == null)
            windowRoot = transform as RectTransform;

        if (canvasGroup == null)
        {
            canvasGroup = windowRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = windowRoot.gameObject.AddComponent<CanvasGroup>();
        }

        HookButtons();
    }

    public void Initialize(WindowManager wm, string id, RectTransform rootCanvas)
    {
        owner = wm;
        appId = id;
        canvasRect = rootCanvas;
    }

    public void SetMinimized(bool minimized)
    {
        IsMinimized = minimized;

        canvasGroup.alpha = minimized ? 0f : 1f;
        canvasGroup.interactable = !minimized;
        canvasGroup.blocksRaycasts = !minimized;
    }

    public void OnPointerDown(PointerEventData e)
    {
        owner?.Focus(appId);
    }

    public void OnBeginDrag(PointerEventData e)
    {
        owner?.Focus(appId);
    }

    public void OnDrag(PointerEventData e)
    {
        if (isPinned) return;

        windowRoot.anchoredPosition += e.delta;
    }

    public void SetActiveVisual(bool active)
    {
        if (skin == null) return;

        if (titleBarImage != null)
            titleBarImage.color =
                active ? skin.titleActiveColor : skin.titleInactiveColor;

        if (frameImage != null)
            frameImage.color =
                active ? skin.frameActiveColor : skin.frameInactiveColor;
    }

    private void HookButtons()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(() => owner?.Close(appId));

        if (minimizeButton != null)
            minimizeButton.onClick.AddListener(() => owner?.Minimize(appId));

        if (pinToggleButton != null)
            pinToggleButton.onClick.AddListener(() => isPinned = !isPinned);
    }
}
