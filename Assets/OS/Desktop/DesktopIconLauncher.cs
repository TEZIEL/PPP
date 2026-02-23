using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class DesktopIconLauncher : MonoBehaviour, IPointerClickHandler
{
    [Header("Wiring")]
    [SerializeField] private WindowManager windowManager;

    [Header("App")]
    [SerializeField] private AppDefinition appDef;

    [Header("Optional Label")]
    [SerializeField] private TMP_Text iconLabel;

    [Header("Double Click")]
    [SerializeField] private float doubleClickThreshold = 0.28f;
    [SerializeField] private float dragClickIgnoreSeconds = 0.15f;

    [Header("Cooldown")]
    [SerializeField] private float clickCooldownSeconds = 0.15f;

    private float lastClickTime = -999f;
    private float nextAllowedTime = 0f;

    private void Awake()
    {
        // 아이콘 라벨 자동 주입
        if (iconLabel != null && appDef != null)
            iconLabel.text = appDef.DisplayName;
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

        if (windowManager == null || appDef == null)
            return;

        if (windowManager.IsOpen(appDef.AppId))
            return;

        windowManager.Open(appDef);
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