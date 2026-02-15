using UnityEngine;
using UnityEngine.UI;

public class TaskbarButtonController : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private WindowManager windowManager;
    [SerializeField] private WindowController targetWindow;
    [SerializeField] private Button button;

    [Header("Visual")]
    [SerializeField] private Image background;
    [SerializeField] private Sprite recessedSprite; // 오목(보이는 상태)
    [SerializeField] private Sprite raisedSprite;   // 볼록(최소화)

    [Header("Debug/Identity (optional)")]
    [SerializeField] private string appId;

    private bool _listenerHooked;

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (background == null) background = GetComponent<Image>();
        HookListener();
    }

    private void OnEnable() => HookListener();
    private void OnDisable() => UnhookListener();
    private void OnDestroy() => UnhookListener();

    private void HookListener()
    {
        if (_listenerHooked) return;
        if (button == null) return;

        button.onClick.AddListener(OnClick);
        _listenerHooked = true;
    }

    private void UnhookListener()
    {
        if (!_listenerHooked) return;
        if (button == null) return;

        button.onClick.RemoveListener(OnClick);
        _listenerHooked = false;
    }

    public void Initialize(string id, WindowManager manager, WindowController window)
    {
        appId = id;
        windowManager = manager;
        targetWindow = window;
    }

    public void SetMinimizedVisual(bool minimized)
    {
        if (background == null) return;
        if (recessedSprite == null || raisedSprite == null) return;

        background.sprite = minimized ? raisedSprite : recessedSprite;
    }

    public void SetActiveVisual(bool active)
    {
        // 너 요구사항에선 포커스/백그라운드 동일 처리라 비워둬도 됨
    }

    private void OnClick()
    {
        if (windowManager == null) return;
        if (targetWindow == null) return;

        string id = !string.IsNullOrEmpty(targetWindow.AppId) ? targetWindow.AppId : appId;
        if (string.IsNullOrEmpty(id)) return;

        // 1) 최소화면 복원
        if (targetWindow.IsMinimized)
        {
            windowManager.Restore(id); // Restore 내부에서 Focus까지 하면 베스트
            return;
        }

        // 2) 보이는 상태: 포커스면 최소화, 포커스 아니면 포커스
        if (windowManager.ActiveAppId == id)
            windowManager.Minimize(id);
        else
            windowManager.Focus(id);
    }
}
