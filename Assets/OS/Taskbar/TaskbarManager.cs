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

    public void SetMinimized(string appId, bool isMinimized)
    {
        if (isMinimized)
        {
            minimizedApps.Add(appId);
        }
        else
        {
            minimizedApps.Remove(appId);
        }

        SetState(appId, false, isMinimized);
    }

    public void OnTaskbarButtonClicked(string appId)
    {
        if (windowManager == null)
        {
            return;
        }

        if (windowManager.IsMinimized(appId))
        {
            windowManager.Restore(appId);
        }
        else
        {
            windowManager.Focus(appId);
        }
    }

    private void SetState(string appId, bool isActive, bool isMinimized)
    {
        if (!buttons.TryGetValue(appId, out TaskbarButtonController button) || button == null)
        {
            return;
        }

    }
}
