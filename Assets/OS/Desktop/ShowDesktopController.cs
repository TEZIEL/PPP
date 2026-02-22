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
    public bool IsDesktopShown => isDesktopShown;


    private Coroutine focusCo;

    private void Awake()
    {
        if (showDesktopButton != null)
            showDesktopButton.onClick.AddListener(ToggleShowDesktop);
    }

    public void ToggleShowDesktop()
    {
        if (windowManager == null) return;
        windowManager.ToggleShowDesktop();
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
