using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DesktopIconLauncher : MonoBehaviour, IPointerClickHandler
{
    [Header("Wiring")]
    [SerializeField] private WindowManager windowManager;

    [Header("App")]
    [SerializeField] private string appId = "VN";
    [SerializeField] private WindowController windowPrefab;
    [SerializeField] private Vector2 defaultPos = new Vector2(200, -120);
    [SerializeField] private Vector2 defaultSize = new Vector2(640, 480);

    [Header("Double Click")]
    [SerializeField] private float doubleClickThreshold = 0.28f;
    [SerializeField] private float dragClickIgnoreSeconds = 0.15f;

    [Header("Cooldown")]
    [SerializeField] private float clickCooldownSeconds = 0.15f;

    [Header("Optional (UI Button)")]
    [SerializeField] private Button iconButton; // 있으면 더블클릭 판정용으로만 사용 권장(실행은 안 함)

    private float lastClickTime = -999f;
    private float nextAllowedTime = 0f;

    private void Awake()
    {
        // Button을 써도 "한 번 클릭 실행"이 되면 윈도우 느낌이 깨지니,
        // 기본은 리스너를 안 붙이는 걸 추천.
        // 그래도 필요하면 아래처럼 "선택 처리" 같은 용도로만 쓰고,
        // 실행은 OnPointerClick(더블클릭)에서만 하자.

        // if (iconButton != null)
        //     iconButton.onClick.AddListener(() => { /* selection only */ });
    }

    private bool CanExecuteNow()
    {
        if (Time.unscaledTime < nextAllowedTime) return false;
        nextAllowedTime = Time.unscaledTime + clickCooldownSeconds;
        return true;
    }

    private bool IsDragRecent()
    {
        var drag = GetComponent<DesktopIconDraggable>();
        if (drag == null) return false;

        if (drag.IsDragging) return true;
        if (Time.unscaledTime - drag.LastDragEndTime < dragClickIgnoreSeconds) return true;

        return false;
    }

    private void ExecuteOpen()
    {
        if (!CanExecuteNow()) return;

        if (windowManager == null || windowPrefab == null || string.IsNullOrEmpty(appId))
            return;

        // 이미 열려있으면 아무것도 안 함(포커스/복원 금지 정책 유지)
        if (windowManager.IsOpen(appId))
            return;

        windowManager.Open(appId, windowPrefab, defaultPos, defaultSize);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 왼쪽 클릭만
        if (eventData.button != PointerEventData.InputButton.Left) return;

        // 드래그 중/직후 클릭 무시 + 더블클릭 상태 초기화
        if (IsDragRecent())
        {
            lastClickTime = -999f;
            return;
        }

        // 더블클릭 판정
        float now = Time.unscaledTime;
        if (now - lastClickTime <= doubleClickThreshold)
        {
            lastClickTime = -999f;
            ExecuteOpen();
            return;
        }

        // 단일 클릭은 "선택만" (아무 것도 실행하지 않음)
        lastClickTime = now;
    }
}
