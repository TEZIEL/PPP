using System.Collections.Generic;
using UnityEngine;

public class WindowManager : MonoBehaviour
{
    [SerializeField] private RectTransform windowsRoot;
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private TaskbarManager taskbarManager;

    private readonly Dictionary<string, WindowController> openWindows = new();

    public void Open(string appId, WindowController windowPrefab)
    {
        Open(appId, windowPrefab, Vector2.zero);
    }

    public void Open(string appId, WindowController windowPrefab, Vector2 defaultPos)
    {
        if (string.IsNullOrWhiteSpace(appId) || windowPrefab == null)
        {
            return;
        }

        if (openWindows.ContainsKey(appId))
        {
            Restore(appId);
            Focus(appId);
            return;
        }

        Transform parent = windowsRoot != null ? windowsRoot : transform;
        WindowController spawned = Instantiate(windowPrefab, parent);
        spawned.Initialize(this, appId, canvasRect);

        Vector2 spawnPos = SaveSystem.TryGetWindowPosition(appId, out Vector2 savedPos) ? savedPos : defaultPos;
        spawned.SetWindowPosition(spawnPos);

        openWindows.Add(appId, spawned);
        taskbarManager?.Add(appId, spawned);

        Focus(appId);
    }

    public void Close(string appId)
    {
        if (!openWindows.TryGetValue(appId, out WindowController window))
        {
            return;
        }

        if (window != null && window.WindowRoot != null)
        {
            SaveSystem.SetWindowPositionHook(appId, window.WindowRoot.anchoredPosition);
        }

        taskbarManager?.Remove(appId);
        openWindows.Remove(appId);

        if (window != null)
        {
            Destroy(window.gameObject);
        }
    }

    public void Focus(string appId)
    {
        if (!openWindows.TryGetValue(appId, out WindowController target) || target == null)
        {
            return;
        }

        if (!target.gameObject.activeSelf)
        {
            target.gameObject.SetActive(true);
            taskbarManager?.SetMinimized(appId, false);
        }

        target.transform.SetAsLastSibling();

        foreach (KeyValuePair<string, WindowController> pair in openWindows)
        {
            if (pair.Value == null)
            {
                continue;
            }

            bool active = pair.Key == appId;
            if (pair.Value.gameObject.activeSelf)
            {
                pair.Value.SetActiveVisual(active);
            }

            taskbarManager?.SetActive(pair.Key, active);
        }
    }

    public void Minimize(string appId)
    {
        if (!openWindows.TryGetValue(appId, out WindowController target) || target == null)
        {
            return;
        }

        if (target.WindowRoot != null)
        {
            SaveSystem.SetWindowPositionHook(appId, target.WindowRoot.anchoredPosition);
        }

        target.gameObject.SetActive(false);
        taskbarManager?.SetMinimized(appId, true);
    }

    public void Restore(string appId)
    {
        if (!openWindows.TryGetValue(appId, out WindowController target) || target == null)
        {
            return;
        }

        target.gameObject.SetActive(true);
        taskbarManager?.SetMinimized(appId, false);
        Focus(appId);
    }

    public bool IsMinimized(string appId)
    {
        return openWindows.TryGetValue(appId, out WindowController target) && target != null && !target.gameObject.activeSelf;
    }

    public void OnWindowMoved(string appId, Vector2 anchoredPosition)
    {
        SaveSystem.SetWindowPositionHook(appId, anchoredPosition);
    }

    public IReadOnlyDictionary<string, WindowController> GetOpenWindows()
    {
        return openWindows;
    }
}
