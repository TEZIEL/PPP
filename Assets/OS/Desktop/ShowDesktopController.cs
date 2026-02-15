using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShowDesktopController : MonoBehaviour
{
    [SerializeField] private WindowManager windowManager;
    [SerializeField] private Button showDesktopButton;

    private readonly List<string> previouslyVisibleApps = new();
    private string previouslyActiveAppId;
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
        if (Time.time - lastToggleTime < cooldown) return;
        lastToggleTime = Time.time;

        // 1) ON: 보이는 창만 최소화 + 활성창 기억
        if (!isDesktopShown)
        {
            previouslyVisibleApps.Clear();
            previouslyActiveAppId = windowManager.ActiveAppId;

            foreach (var pair in windowManager.GetOpenWindows())
            {
                if (pair.Value == null) continue;

                // "보이는 창" 기준: activeSelf로 판단(너 구조가 이 방식)
                if (pair.Value.gameObject.activeSelf)
                {
                    previouslyVisibleApps.Add(pair.Key);
                }
            }

            // 리스트 기반으로 최소화 (순회 중 Dictionary 변경 방지)
            for (int i = 0; i < previouslyVisibleApps.Count; i++)
                windowManager.Minimize(previouslyVisibleApps[i]);

            isDesktopShown = true;
            return;
        }

        // 2) OFF: 그때 보이던 창만 복원(포커스는 마지막에 한 번만)
        for (int i = 0; i < previouslyVisibleApps.Count; i++)
            windowManager.RestoreNoFocus(previouslyVisibleApps[i]);

        // 원래 활성창이 살아있고 복원 대상이면 그걸 포커스
        if (!string.IsNullOrEmpty(previouslyActiveAppId) &&
            previouslyVisibleApps.Contains(previouslyActiveAppId))
        {
            windowManager.Focus(previouslyActiveAppId);
        }
        else if (previouslyVisibleApps.Count > 0)
        {
            // 없으면 그냥 마지막으로 복원된 창 포커스
            windowManager.Focus(previouslyVisibleApps[previouslyVisibleApps.Count - 1]);
        }

        previouslyVisibleApps.Clear();
        previouslyActiveAppId = null;
        isDesktopShown = false;
    }
}
