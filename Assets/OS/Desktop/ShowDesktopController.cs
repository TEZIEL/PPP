using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShowDesktopController : MonoBehaviour
{
    [SerializeField] private WindowManager windowManager;
    [SerializeField] private Button showDesktopButton;

    private readonly List<string> previouslyVisibleApps = new();
    private bool isDesktopShown;

    private float lastToggleTime;
    private const float cooldown = 0.2f;

    private void Awake()
    {
        if (showDesktopButton != null)
            showDesktopButton.onClick.AddListener(ToggleShowDesktop);
    }

    public void ToggleShowDesktop()
    {
        if (windowManager == null) return;

        if (Time.time - lastToggleTime < cooldown)
            return;

        lastToggleTime = Time.time;

        // ▼ 최소화
        if (!isDesktopShown)
        {
            previouslyVisibleApps.Clear();

            foreach (var pair in windowManager.GetOpenWindows())
            {
                if (pair.Value != null && pair.Value.gameObject.activeSelf)
                {
                    previouslyVisibleApps.Add(pair.Key);
                    windowManager.Minimize(pair.Key);
                }
            }

            isDesktopShown = true;
            return;
        }

        // ▼ 복원
        foreach (string id in previouslyVisibleApps)
        {
            windowManager.Restore(id);
        }

        previouslyVisibleApps.Clear();
        isDesktopShown = false;
    }
}
