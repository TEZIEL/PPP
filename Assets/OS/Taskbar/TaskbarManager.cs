using System.Collections.Generic;
using UnityEngine;

public class TaskbarManager : MonoBehaviour
{
    [SerializeField] private WindowManager windowManager;
    [SerializeField] private TaskbarButtonController buttonPrefab;
    [SerializeField] private RectTransform buttonRoot;

    private readonly Dictionary<string, TaskbarButtonController> buttons = new();
    private readonly HashSet<string> minimizedApps = new();

    public void Add(string appId, string displayName, WindowController window)
    {
        if (buttons.ContainsKey(appId) || buttonPrefab == null)
            return;

        minimizedApps.Remove(appId);

        Transform root = buttonRoot != null ? buttonRoot : transform;
        var button = Instantiate(buttonPrefab, root);

        button.Initialize(appId, displayName, windowManager, window);

        buttons.Add(appId, button);
        SetState(appId, false, false);
    }

    public void Remove(string appId)
    {
        minimizedApps.Remove(appId);

        if (!buttons.TryGetValue(appId, out var button) || button == null)
            return;

        buttons.Remove(appId);

        // ✅ 레이아웃 리플로우 애니 후 제거
        button.PlayCloseReflow(() =>
        {
            if (button != null)
                Destroy(button.gameObject);
        });
    }

    public void SetActive(string appId, bool isActive)
    {
        SetState(appId, isActive, minimizedApps.Contains(appId));
    }

    public void SetMinimized(string appId, bool minimized)
    {
        if (minimized) minimizedApps.Add(appId);
        else minimizedApps.Remove(appId);

        if (!buttons.TryGetValue(appId, out var btn) || btn == null) return;

        btn.SetMinimizedVisual(minimized);

        // (선택) minimized면 active 비주얼 끄고 싶으면 여기서 처리 가능
        // btn.SetActiveVisual(false);
    }

    public void OnTaskbarButtonClicked(string appId)
    {
        windowManager?.OnTaskbarButtonPressed(appId);
    }
    

    private void SetState(string appId, bool isActive, bool isMinimized)
    {
        if (!buttons.TryGetValue(appId, out var button) || button == null) return;

        // 너의 TaskbarButtonController는 active 비주얼은 비워놨으니,
        // 지금은 minimized만 반영해도 OK.
        button.SetMinimizedVisual(isMinimized);
        // button.SetActiveVisual(isActive); // 나중에 쓰고 싶을 때
    }

    public RectTransform GetButtonRect(string appId)
    {
        if (buttons.TryGetValue(appId, out var b) && b != null) return b.Rect;
        return null;
    }
}
