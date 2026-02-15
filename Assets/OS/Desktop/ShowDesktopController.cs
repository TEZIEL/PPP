using System.Collections;
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

    private Coroutine focusCo;

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

        // 이전 코루틴이 남아있으면 정리
        if (focusCo != null)
        {
            StopCoroutine(focusCo);
            focusCo = null;
        }

        // --- ON ---
        if (!isDesktopShown)
        {
            previouslyVisibleApps.Clear();
            previouslyActiveAppId = windowManager.ActiveAppId;

            windowManager.BeginBatch();

            foreach (var pair in windowManager.GetOpenWindows())
            {
                var wc = pair.Value;
                if (wc == null) continue;

                // ✅ 현재 화면에 떠 있는 창만 = "최소화 아님"
                if (!wc.IsMinimized)
                    previouslyVisibleApps.Add(pair.Key);
            }

            Debug.Log($"Will minimize count: {previouslyVisibleApps.Count}");

            for (int i = 0; i < previouslyVisibleApps.Count; i++)
                windowManager.MinimizeNoFocus(previouslyVisibleApps[i]);

            windowManager.EndBatch();

            isDesktopShown = true;
            return;
        }


        // --- OFF: 저장했던 애들만 복원 ---
        windowManager.BeginBatch();

        for (int i = 0; i < previouslyVisibleApps.Count; i++)
        {
            string id = previouslyVisibleApps[i];

            // 닫힌 창은 무시
            if (!windowManager.GetOpenWindows().TryGetValue(id, out var wc) || wc == null)
                continue;

            // 지금도 최소화 상태인 애만 복원(꼬임 방지)
            if (wc.IsMinimized)
                windowManager.RestoreNoFocus(id);
        }

        windowManager.EndBatch();

        // ✅ 포커스는 1프레임 뒤에 적용(랜덤 포커스 튐 방지)
        focusCo = StartCoroutine(FocusAfterRestore());
    }

    private IEnumerator FocusAfterRestore()
    {
        yield return null; // ✅ 1프레임 대기

        if (windowManager == null)
        {
            previouslyVisibleApps.Clear();
            isDesktopShown = false;
            focusCo = null;
            yield break;

           

        }

        // 1) 원래 활성창이 아직 열려있고(닫히지 않았고), 복원 대상에 있었으면 그걸 포커스
        if (!string.IsNullOrEmpty(previouslyActiveAppId) &&
            previouslyVisibleApps.Contains(previouslyActiveAppId) &&
            windowManager.GetOpenWindows().ContainsKey(previouslyActiveAppId))
        {
            windowManager.Focus(previouslyActiveAppId);
        }
        else
        {
            // 2) 아니면 복원 대상 중 "아직 존재하는 것" 하나를 포커스
            for (int i = previouslyVisibleApps.Count - 1; i >= 0; i--)
            {
                string id = previouslyVisibleApps[i];
                if (windowManager.GetOpenWindows().TryGetValue(id, out var wc) && wc != null && wc.gameObject.activeSelf)
                {
                    windowManager.Focus(id);
                    break;
                }
            }
        }

        previouslyVisibleApps.Clear();
        isDesktopShown = false;
        focusCo = null;
        
    }
}
