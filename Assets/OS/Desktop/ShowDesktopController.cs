using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShowDesktopController : MonoBehaviour
{
    [SerializeField] private WindowManager windowManager;
    [SerializeField] private Button showDesktopButton;

    private readonly List<string> previouslyVisibleApps = new();
    private bool isDesktopShown;

    private void Awake()
    {
        if (showDesktopButton != null)
        {
            showDesktopButton.onClick.AddListener(ToggleShowDesktop);
        }
    }

    public void ToggleShowDesktop()
    {
        if (windowManager == null)
        {
            return;
        }

        if (!isDesktopShown)
        {
            previouslyVisibleApps.Clear();

            foreach (KeyValuePair<string, WindowController> pair in windowManager.GetOpenWindows())
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

        for (int i = 0; i < previouslyVisibleApps.Count; i++)
        {
            windowManager.Restore(previouslyVisibleApps[i]);
        }

        previouslyVisibleApps.Clear();
        isDesktopShown = false;
    }
}
