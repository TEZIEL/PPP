using UnityEngine;

public class WindowShortcutController : MonoBehaviour
{
    [SerializeField] private WindowManager windowManager;

    [Header("Cooldown")]
    [SerializeField] private float actionCooldown = 0.32f;

    private float lastActionTime;

    private void Update()
    {
        if (windowManager == null) return;

        // 포커스 창 없음 → 무시
        string activeId = windowManager.ActiveAppId;
        if (string.IsNullOrEmpty(activeId)) return;

        // 쿨타임
        if (Time.unscaledTime - lastActionTime < actionCooldown)
            return;

        // --- 1 : 최소화 ---
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            windowManager.Minimize(activeId);
            lastActionTime = Time.unscaledTime;
            return;
        }

        // --- 2 : 핀 토글 ---
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            TogglePin(activeId);
            lastActionTime = Time.unscaledTime;
            return;
        }

        // --- 3 : 닫기 ---
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            windowManager.Close(activeId);
            lastActionTime = Time.unscaledTime;
            return;
        }
    }

    private void TogglePin(string appId)
    {
        var windows = windowManager.GetOpenWindows();

        if (!windows.TryGetValue(appId, out var wc) || wc == null)
            return;

        wc.TogglePinFromShortcut();
    }
}
