using UnityEngine;
using PPP.BLUE.VN;

public class WindowShortcutController : MonoBehaviour
{
    [SerializeField] private WindowManager windowManager;

    [Header("Cooldown")]
    [SerializeField] private float actionCooldown = 0.32f;

    [Header("VN Close Block")]
    [SerializeField] private string vnAppId = "app.vn"; // VN AppId

    private float lastActionTime;
    
    // ✅ 외부에서 잠깐 입력 막기(선택)
    private float shortcutLockUntil;
    public void LockForSeconds(float seconds)
    {
        shortcutLockUntil = Mathf.Max(shortcutLockUntil, Time.unscaledTime + seconds);
    }

    private void Update()
    {
        if (Time.unscaledTime < shortcutLockUntil) return;
        if (windowManager == null) return;

        // 포커스 창 없음 → 무시
        string activeId = windowManager.ActiveAppId;
        if (string.IsNullOrEmpty(activeId)) return;

        // 쿨타임
        if (Time.unscaledTime - lastActionTime < actionCooldown)
            return;

        // --- 1 : 최소화 --- (항상 허용)
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            windowManager.Minimize(activeId);
            lastActionTime = Time.unscaledTime;
            return;
        }

        // --- 2 : 핀 토글 --- (항상 허용)
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            TogglePin(activeId);
            lastActionTime = Time.unscaledTime;
            return;
        }

        // --- 3 : 닫기 --- (VN + Drink모드일 때만 차단)
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            if (ShouldBlockClose(activeId))
            {
                Debug.Log("[Shortcut] Close blocked (VN drink mode).");
                lastActionTime = Time.unscaledTime; // 연타 방지용(선택)
                return;
            }

            windowManager.Close(activeId);
            lastActionTime = Time.unscaledTime;
            return;
        }
    }

    private bool ShouldBlockClose(string activeId)
    {
        if (activeId != vnAppId) return false;

        var windows = windowManager.GetOpenWindows();
        if (!windows.TryGetValue(activeId, out var wc) || wc == null) return false;

        var policy = wc.GetComponentInChildren<VNPolicyController>(true);
        if (policy == null) return false;

        var drinkPanel = wc.GetComponentInChildren<DrinkTestPanel>(true);

        // 🔥 DrinkMode OR PendingOpen이면 Close 차단
        return policy.IsInDrinkMode ||
               (drinkPanel != null && drinkPanel.IsPendingOpen);
    }

    private void TogglePin(string appId)
    {
        var windows = windowManager.GetOpenWindows();
        if (!windows.TryGetValue(appId, out var wc) || wc == null) return;

        wc.TogglePinFromShortcut();
    }
}