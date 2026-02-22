using System.Collections.Generic;
using UnityEngine;

public class TaskbarManager : MonoBehaviour
{
    [SerializeField] private WindowManager windowManager;
    [SerializeField] private TaskbarButtonController buttonPrefab;
    [SerializeField] private RectTransform buttonRoot;

    private readonly Dictionary<string, TaskbarButtonController> buttons = new();
    private readonly HashSet<string> minimizedApps = new();

    public void Add(string appId, WindowController window)
    {
        if (buttons.ContainsKey(appId) || buttonPrefab == null)
        {
            return;
        }

        Transform root = buttonRoot != null ? buttonRoot : transform;
        TaskbarButtonController button = Instantiate(buttonPrefab, root);
        button.Initialize(appId, windowManager, window);
        buttons.Add(appId, button);

        SetState(appId, false, false);
    }

    public void Remove(string appId)
    {
        minimizedApps.Remove(appId);

        if (!buttons.TryGetValue(appId, out TaskbarButtonController button))
        {
            return;
        }

        buttons.Remove(appId);

        if (button != null)
        {
            Destroy(button.gameObject);
        }
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
    }

    public void OnTaskbarButtonClicked(string appId)
    {
        windowManager?.OnTaskbarButtonPressed(appId);
    }

    private void SetState(string appId, bool isActive, bool isMinimized)
    {
        if (!buttons.TryGetValue(appId, out TaskbarButtonController button) || button == null)
        {
            return;
        }

    }

    public RectTransform GetButtonRect(string appId)
    {
        if (buttons.TryGetValue(appId, out var b) && b != null) return b.Rect;
        return null;
    }
}
