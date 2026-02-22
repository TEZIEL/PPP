using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class DesktopIconLauncher : MonoBehaviour, IPointerClickHandler
{
    [Header("Wiring")]
    [SerializeField] private WindowManager windowManager;

    [Header("App Definition")]
    [SerializeField] private AppDefinition appDef;

    [Header("Optional UI")]
    [SerializeField] private TMP_Text iconLabel;     // ✅ 바로가기 이름
    [SerializeField] private Image iconImage;        // ✅ 바로가기 아이콘 이미지(선택)

    [Header("Double Click")]
    [SerializeField] private float doubleClickThreshold = 0.28f;
    [SerializeField] private float dragClickIgnoreSeconds = 0.15f;

    [Header("Cooldown")]
    [SerializeField] private float clickCooldownSeconds = 0.15f;

    private float lastClickTime = -999f;
    private float nextAllowedTime = 0f;

    private void Awake()
    {
        ApplyVisualFromDef();
    }

    private void OnValidate()
    {
        // 인스펙터 값 바꿔도 바로 보이게
        ApplyVisualFromDef();
    }

    private void ApplyVisualFromDef()
    {
        if (appDef == null) return;

        if (iconLabel != null) iconLabel.text = appDef.DisplayName;
        if (iconImage != null) iconImage.sprite = appDef.IconSprite;
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
        if (windowManager == null || appDef == null) return;

        // 이미 열려있으면 아무것도 안 함(네 정책 유지)
        if (windowManager.IsOpen(appDef.AppId)) return;

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