using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class DesktopIconLauncher : MonoBehaviour, IPointerClickHandler
{
    [Header("Wiring")]
    [SerializeField] private WindowManager windowManager;

    [Header("App")]
    [SerializeField] private string appId = "app.vn";
    [SerializeField] private string displayName = "VN"; // ✅ 추가
    [SerializeField] private WindowController windowPrefab;
    [SerializeField] private Vector2 defaultPos = new Vector2(200, -120);
    [SerializeField] private Vector2 defaultSize = new Vector2(640, 480);
    [SerializeField] private GameObject contentPrefab;

    [Header("Icon Label (TMP)")]
    [SerializeField] private TMP_Text iconLabel; // ✅ 아이콘 밑 글씨

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
        ApplyLabel();
    }

    private void ApplyLabel()
    {
        if (iconLabel != null)
            iconLabel.text = displayName;
    }

    private bool CanExecuteNow()
    {
        if (Time.unscaledTime < nextAllowedTime) return false;
        nextAllowedTime = Time.unscaledTime + clickCooldownSeconds;
        return true;
    }

    private void ExecuteOpen()
    {
        if (!CanExecuteNow()) return;

        if (windowManager == null || windowPrefab == null || string.IsNullOrEmpty(appId))
            return;

        if (windowManager.IsOpen(appId))
            return;

        windowManager.Open(appId, displayName, windowPrefab, contentPrefab, defaultPos, defaultSize);
    }

  

    private bool IsDragRecent()
    {
        var drag = GetComponent<DesktopIconDraggable>();
        if (drag == null) return false;

        if (drag.IsDragging) return true;
        if (Time.unscaledTime - drag.LastDragEndTime < dragClickIgnoreSeconds) return true;

        return false;
    }

    

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        if (IsDragRecent())
        {
            lastClickTime = -999f;
            return;
        }

        float now = Time.unscaledTime;
        if (now - lastClickTime <= doubleClickThreshold)
        {
            lastClickTime = -999f;
            ExecuteOpen();
            return;
        }

        lastClickTime = now;
    }

}
