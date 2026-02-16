using UnityEngine;
using UnityEngine.EventSystems;

public class DesktopIconDraggable : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private string iconId;           // 저장 키 (appId랑 동일하게 써도 됨)
    [SerializeField] private RectTransform rect;      // 없으면 자동 할당

    private RectTransform canvasRect;                 // WindowManager가 주입
    private Vector2 dragOffset;
    private WindowManager windowManager;             // 저장 트리거용
    private RectTransform desktopRect;

    public string GetId() => iconId;
    public RectTransform GetRect() => rect;

    public void Initialize(WindowManager wm, RectTransform desktopRoot)
    {
        windowManager = wm;
        desktopRect = desktopRoot;
        if (rect == null) rect = (RectTransform)transform;
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
        dragOffset = rect.anchoredPosition - p;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!TryPointerLocal(eventData, out var p)) return;

        Vector2 target = p + dragOffset;

        // ✅ iconsRoot 좌표계에서 clamp
        Rect allowed = DesktopBounds.GetAllowedRect(desktopRect);
        rect.anchoredPosition = DesktopBounds.ClampAnchoredPosition(target, rect, allowed);
    }


    public void OnEndDrag(PointerEventData eventData)
    {
        if (canvasRect != null)
        {
            Rect allowed = DesktopBounds.GetAllowedRect(canvasRect);
            rect.anchoredPosition = DesktopBounds.ClampAnchoredPosition(rect.anchoredPosition, rect, allowed);
        }

        windowManager?.RequestAutoSave();
    }

}
