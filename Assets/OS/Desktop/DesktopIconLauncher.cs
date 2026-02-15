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

        if (showDesktop != null && showDesktop.IsDesktopShown)
            return; // ✅ 전체최소화 중이면 아이콘 클릭 무시

        if (windowManager == null || windowPrefab == null || string.IsNullOrEmpty(appId))
            return;

        windowManager.Open(appId, windowPrefab, defaultPos);
    }

}
