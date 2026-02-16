using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DesktopIconLauncher : MonoBehaviour, IPointerClickHandler
{
    [Header("Wiring")]
    [SerializeField] private WindowManager windowManager;


    [Header("App")]
    [SerializeField] private string appId = "VN";
    [SerializeField] private WindowController windowPrefab;
    [SerializeField] private Vector2 defaultPos = new Vector2(200, -120);
    [SerializeField] private float doubleClickThreshold = 0.28f;

    private float lastClickTime = -999f;

    [Header("Optional")]
    [SerializeField] private Button iconButton;
    [SerializeField] private float clickCooldownSeconds = 0.15f;
    [SerializeField] private ShowDesktopController showDesktop; // 인스펙터에 연결

    private float _nextAllowedTime;  // Button OnClick에서 직접 연결해도 됨
    public void HandleClick()
    {
        if (Time.unscaledTime < _nextAllowedTime) return;
        _nextAllowedTime = Time.unscaledTime + clickCooldownSeconds;

        if (windowManager == null || windowPrefab == null || string.IsNullOrEmpty(appId))
            return;

        // ✅ 이미 열려있으면 아무것도 안 함 (포커스/복원 금지)
        if (windowManager.IsOpen(appId))
            return;

        windowManager.Open(appId, windowPrefab, defaultPos);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 왼쪽 클릭만
        if (eventData.button != PointerEventData.InputButton.Left) return;

        // 드래그 중엔 실행 금지(드래그 후 클릭 오동작 방지)
        var drag = GetComponent<DesktopIconDraggable>();
        if (drag != null)
        {
            if (drag.IsDragging) return;
            if (Time.unscaledTime - drag.LastDragEndTime < 0.15f) return; // ✅ 드래그 직후 클릭 무시
        }

        // 더블클릭 판정
        float now = Time.unscaledTime;
        if (now - lastClickTime <= doubleClickThreshold)
        {
            lastClickTime = -999f;
            HandleClick(); // 기존 실행 함수 호출
            return;
        }

        lastClickTime = now;
    }


}
