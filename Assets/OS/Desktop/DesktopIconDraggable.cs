using UnityEngine;
using UnityEngine.EventSystems;

public class DesktopIconDraggable : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private string iconId;
    [SerializeField] private RectTransform rect;

    private RectTransform desktopRect;   // iconsRoot(DesktopIconBG)
    private Vector2 dragOffset;
    private WindowManager windowManager;
    private DesktopGridManager gridManager;

    public bool IsDragging { get; private set; }
    public float LastDragEndTime { get; private set; } = -999f;

    public string GetId() => iconId;
    public RectTransform GetRect() => rect;

    private void Awake()
    {
        if (rect == null) rect = (RectTransform)transform;

        if (string.IsNullOrWhiteSpace(iconId))
        {
            iconId = System.Guid.NewGuid().ToString("N");
            Debug.LogWarning($"[Icon] iconId was empty. Generated GUID: {iconId}", this);
        }
    }

    private void OnDisable()
    {
        IsDragging = false;
    }

    public void Initialize(WindowManager wm, RectTransform desktopRoot, DesktopGridManager grid)
    {
        windowManager = wm;
        desktopRect = desktopRoot;
        gridManager = grid;

        if (rect == null)
            rect = (RectTransform)transform;
    }

    private bool TryPointerLocal(PointerEventData e, out Vector2 local)
    {
        if (desktopRect == null)
        {
            local = default;
            return false;
        }

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            desktopRect, e.position, e.pressEventCamera, out local);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!TryPointerLocal(eventData, out var p)) return;

        IsDragging = true;
        dragOffset = rect.anchoredPosition - p;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!TryPointerLocal(eventData, out var p)) return;

        Vector2 target = p + dragOffset;

        Rect allowed = DesktopBounds.GetAllowedRect(desktopRect);
        rect.anchoredPosition = DesktopBounds.ClampAnchoredPosition(target, rect, allowed);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        IsDragging = false;
        LastDragEndTime = Time.unscaledTime;

        if (desktopRect != null)
        {
            Rect allowed = DesktopBounds.GetAllowedRect(desktopRect);
            rect.anchoredPosition = DesktopBounds.ClampAnchoredPosition(rect.anchoredPosition, rect, allowed);
        }

        // ✅ Grid 모드면 "겹침 방지 포함 스냅"은 GridManager가 책임
        if (gridManager != null && gridManager.LayoutMode == DesktopGridManager.DesktopLayoutMode.Grid)
        {
            gridManager.SnapIconToGridNoOverlap(this);
        }

        windowManager?.RequestAutoSave();
    }
}