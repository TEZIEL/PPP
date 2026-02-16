using UnityEngine;
using UnityEngine.UI;

public class DesktopIconLauncher : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private WindowManager windowManager;

    [Header("App")]
    [SerializeField] private string appId = "VN";
    [SerializeField] private WindowController windowPrefab;
    [SerializeField] private Vector2 defaultPos = new Vector2(200, -120);

    [Header("Optional")]
    [SerializeField] private Button iconButton;
    [SerializeField] private float clickCooldownSeconds = 0.15f;
    [SerializeField] private ShowDesktopController showDesktop; // 인스펙터에 연결

    private float _nextAllowedTime;

    private void Awake()
    {
        if (iconButton != null)
            iconButton.onClick.AddListener(HandleClick);
    }

    // Button OnClick에서 직접 연결해도 됨
    public void HandleClick()
    {
        if (Time.unscaledTime < _nextAllowedTime) return;
        _nextAllowedTime = Time.unscaledTime + clickCooldownSeconds;

        if (windowManager == null || windowPrefab == null || string.IsNullOrEmpty(appId))
            return;

        // ✅ 이미 열려있으면 아무것도 안 함 (포커스/복원 금지)
        if (windowManager.IsOpen(appId))
            return;

        windowManager.Open(appId, windowPrefab, defaultPos);
    }


}
