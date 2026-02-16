using UnityEngine;
using UnityEngine.EventSystems;

public class DesktopContextMenuController : MonoBehaviour, IPointerClickHandler
{
    [Header("Wiring (Inspector)")]
    [SerializeField] private RectTransform canvasRect;          // OS Canvas
    [SerializeField] private RectTransform menuPanelRect;       // MenuPanel RectTransform
    [SerializeField] private GameObject menuRoot;               // DesktopContextMenuRoot (전체)
    [SerializeField] private GameObject blocker;                // Blocker (바깥 클릭 닫기)
    [SerializeField] private DesktopContextMenuView view;       // 버튼 이벤트 뷰

    [Header("Targets")]
    [SerializeField] private DesktopGridManager desktopGridManager;
    [SerializeField] private WindowManager windowManager;

    [Header("Behavior")]
    [SerializeField] private float edgePadding = 8f;

    private void Awake()
    {
        if (menuRoot != null) menuRoot.SetActive(false);

        // Blocker 클릭하면 닫기
        if (blocker != null)
        {
            var trigger = blocker.GetComponent<EventTrigger>();
            if (trigger == null) trigger = blocker.AddComponent<EventTrigger>();

            trigger.triggers ??= new System.Collections.Generic.List<EventTrigger.Entry>();

            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            entry.callback.AddListener(_ => Hide());
            trigger.triggers.Add(entry);
        }

        if (view != null)
        {
            view.OnAlignIcons += HandleAlignIcons;
            view.OnToggleLayout += HandleToggleLayout;
            view.OnResetWindows += HandleResetWindows;
            view.OnRefresh += HandleRefresh;
            view.OnCreateFile += HandleCreateFile;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // ✅ 우클릭만
        if (eventData.button != PointerEventData.InputButton.Right) return;

        // ✅ 메뉴 토글(윈도우 감성: 같은 버튼으로 닫히기도)
        if (menuRoot != null && menuRoot.activeSelf)
        {
            Hide();
            return;
        }

        ShowAt(eventData.position, eventData.pressEventCamera);
    }

    private void ShowAt(Vector2 screenPos, Camera uiCam)
    {
        if (menuRoot == null || menuPanelRect == null || canvasRect == null) return;

        menuRoot.SetActive(true);
        menuRoot.transform.SetAsLastSibling();


        // ✅ pivot = 좌상단 (커서가 좌상단 꼭지점)
        menuPanelRect.pivot = new Vector2(0f, 1f);

        // (선택) anchor도 중앙 고정 유지해도 되지만, 깔끔하게 고정하고 싶으면:
        // menuPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
        // menuPanelRect.anchorMax = new Vector2(0.5f, 0.5f);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPos, uiCam, out var localPoint);

        menuPanelRect.anchoredPosition = localPoint;

        ClampMenuIntoCanvas();
    }

    private void ClampMenuIntoCanvas()
    {
        if (menuPanelRect == null || canvasRect == null) return;

        Rect c = canvasRect.rect;
        Vector2 size = menuPanelRect.rect.size;
        Vector2 p = menuPanelRect.anchoredPosition;
        Vector2 pivot = menuPanelRect.pivot;

        // 메뉴의 실제 경계(로컬)
        float left = p.x - size.x * pivot.x;
        float right = left + size.x;
        float top = p.y + size.y * (1f - pivot.y);
        float bottom = top - size.y;

        float dx = 0f;
        if (left < c.xMin + edgePadding) dx = (c.xMin + edgePadding) - left;
        else if (right > c.xMax - edgePadding) dx = (c.xMax - edgePadding) - right;

        float dy = 0f;
        if (bottom < c.yMin + edgePadding) dy = (c.yMin + edgePadding) - bottom;
        else if (top > c.yMax - edgePadding) dy = (c.yMax - edgePadding) - top;

        menuPanelRect.anchoredPosition = new Vector2(p.x + dx, p.y + dy);
    }

    public void Hide()
    {
        if (menuRoot != null) menuRoot.SetActive(false);
    }

    // -------- 버튼 핸들러 --------

    private void HandleAlignIcons()
    {
        if (desktopGridManager != null)
            desktopGridManager.AlignIconsToGrid();

        Hide();
    }

    private void HandleToggleLayout()
    {
        if (desktopGridManager != null)
        {
            desktopGridManager.ToggleLayoutMode();

            // ❌ 여기서 Align/Snap 절대 하지 않기
            // desktopGridManager.SnapAllIfGridMode();

            windowManager?.SaveOS();
        }

        Hide();
    }

    private void HandleResetWindows()
    {
        windowManager?.ResetWindowsToDefaults();
        Hide();
    }


    private void HandleRefresh()
    {
        Debug.Log("[Desktop] Refresh (dummy)");
        Hide();
    }

    private void HandleCreateFile()
    {
        Debug.Log("[Desktop] Create file (dummy)");
        Hide();
    }
}
