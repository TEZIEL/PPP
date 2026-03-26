using UnityEngine;

public class WindowShortcutController : MonoBehaviour
{
    [SerializeField] private WindowManager windowManager;

    [Header("Cooldown")]
    [SerializeField] private float actionCooldown = 0.32f;

    private float lastActionTime;

    // ✅ 외부에서 잠깐 입력 막기(선택)
    private float shortcutLockUntil;
    public void LockForSeconds(float seconds)
    {
        shortcutLockUntil = Mathf.Max(shortcutLockUntil, Time.unscaledTime + seconds);
    }

    // ✅ 같은 프레임 중복 방지
    private int _lastProcessedFrame = -1;
    private string _lastProcessedAppId = null;

    private void Update()
    {
        if (Time.unscaledTime < shortcutLockUntil) return;
        if (windowManager == null) return;

        // 

        string activeId = windowManager.ActiveAppId;
        if (string.IsNullOrEmpty(activeId)) return;

        // ✅ 같은 appId를 같은 프레임에 또 처리하지 않기
        if (_lastProcessedFrame == Time.frameCount && _lastProcessedAppId == activeId) return;

        // 쿨타임
        if (Time.unscaledTime - lastActionTime < actionCooldown) return;

        // ✅ 활성 창이 애니 중이면 쇼트컷 무시
        var windows = windowManager.GetOpenWindows();
        if (windows != null && windows.TryGetValue(activeId, out var w) && w != null)
        {
            if (w.IsAnimating) return;
        }

        // --- 1 : 최소화 ---
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            MarkProcessed(activeId);
            windowManager.Minimize(activeId);
            return;
        }

        // --- 2 : 핀 토글 ---
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            MarkProcessed(activeId);
            TogglePin(activeId);
            return;
        }

        // --- 3 : 닫기 ---
        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            // UI 버튼 클릭 프레임과 단축키 3 동시입력 충돌 방지
            if (Input.GetMouseButtonDown(0))
                return;

            MarkProcessed(activeId);
            windowManager.RequestClose(activeId);
            return;
        }
    }

    private void MarkProcessed(string appId)
    {
        _lastProcessedFrame = Time.frameCount;
        _lastProcessedAppId = appId;
        lastActionTime = Time.unscaledTime;
    }

    private void TogglePin(string appId)
    {
        var windows = windowManager.GetOpenWindows();
        if (windows == null) return;
        if (!windows.TryGetValue(appId, out var wc) || wc == null) return;
        wc.TogglePinFromShortcut();
    }
}
