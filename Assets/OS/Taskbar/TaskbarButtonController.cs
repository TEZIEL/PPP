using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TaskbarButtonController : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private WindowManager windowManager;
    [SerializeField] private WindowController targetWindow;
    [SerializeField] private Button button;

    [Header("Label")]
    [SerializeField] private TMP_Text titleText; // ✅ 태스크바 버튼 TMP 연결

    [Header("Visual")]
    [SerializeField] private Image background;
    [SerializeField] private Sprite recessedSprite; // 오목(보이는 상태)
    [SerializeField] private Sprite raisedSprite;   // 볼록(최소화)

    private string appId; // ✅ Initialize로 주입
    private bool _listenerHooked;
    public RectTransform Rect => (RectTransform)transform;



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
        if (button == null) return;

        // ✅ 중복 등록 방지 (이미 붙어있을 수 있으니 한번 제거 후 추가)
        button.onClick.RemoveListener(OnClick);
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

    public void Initialize(string id, string displayName, WindowManager manager, WindowController window)
    {
        appId = id;
        windowManager = manager;
        targetWindow = window;

        if (titleText != null)
            titleText.text = displayName;
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

        // 1) 최소화 상태면: 복원(=보이게 + 포커스 규칙은 Restore/Focus 쪽에 맡김)
        if (targetWindow.IsMinimized)
        {
            windowManager.Restore(id);
            return;
        }

        // 2) 포커스가 아니면: 포커스만 하고 끝 (✅ 1클릭 = 포커스)
        if (windowManager.ActiveAppId != id)
        {
            windowManager.Focus(id);
            return;
        }

        // 3) 이미 포커스면: 최소화 (✅ 2클릭 = 최소화)
        windowManager.Minimize(id);
    }
}
