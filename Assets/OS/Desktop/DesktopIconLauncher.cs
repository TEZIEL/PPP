using System.Collections.Generic;
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

    [Header("Selection Visual (Optional)")]
    [SerializeField] private GameObject selectedVisual;

    [Header("Double Click")]
    [SerializeField] private float doubleClickThreshold = 0.28f;
    [SerializeField] private float dragClickIgnoreSeconds = 0.15f;

    [Header("Cooldown")]
    [SerializeField] private float clickCooldownSeconds = 0.15f;

    private static DesktopIconLauncher activeSelection;

    private float lastClickTime = -999f;
    private float nextAllowedTime = 0f;

    private void Awake()
    {
        // 아이콘 라벨 자동 주입
        if (iconLabel != null && appDef != null)
            iconLabel.text = appDef.DisplayName;

        if (selectedVisual != null)
            selectedVisual.SetActive(false);
    }

    private void OnDisable()
    {
        if (activeSelection == this)
            activeSelection = null;

        if (selectedVisual != null)
            selectedVisual.SetActive(false);
    }

    private void Update()
    {
        if (activeSelection == null) return;
        if (!Input.GetMouseButtonDown(0)) return;

        if (IsPointerOverAnyDesktopIcon()) return;

        SetActiveSelection(null);
    }

    private static bool IsPointerOverAnyDesktopIcon()
    {
        var es = EventSystem.current;
        if (es == null) return false;

        var pointerData = new PointerEventData(es) { position = Input.mousePosition };
        var results = new List<RaycastResult>(16);
        es.RaycastAll(pointerData, results);

        for (int i = 0; i < results.Count; i++)
        {
            var go = results[i].gameObject;
            if (go == null) continue;

            if (go.GetComponentInParent<DesktopIconLauncher>() != null)
                return true;
        }

        return false;
    }

    private static void SetActiveSelection(DesktopIconLauncher target)
    {
        if (activeSelection == target)
        {
            activeSelection?.SetSelectionVisual(true);
            return;
        }

        if (activeSelection != null)
            activeSelection.SetSelectionVisual(false);

        activeSelection = target;

        if (activeSelection != null)
            activeSelection.SetSelectionVisual(true);
    }

    private void SetSelectionVisual(bool selected)
    {
        if (selectedVisual == null) return;
        selectedVisual.SetActive(selected);
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

        SetActiveSelection(this);

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
