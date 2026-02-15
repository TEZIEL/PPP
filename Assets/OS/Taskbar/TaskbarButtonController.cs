using UnityEngine;
using UnityEngine.UI;

public class TaskbarButtonController : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private WindowManager windowManager;
    [SerializeField] private WindowController targetWindow;
    [SerializeField] private Button button;

    [Header("Debug/Identity (optional)")]
    [SerializeField] private string appId; // 디버그/표시용. source of truth는 targetWindow.AppId 권장

    private bool _listenerHooked;

    private void Awake()
    {
        // 버튼 슬롯을 깜빡해도 자동으로 잡아줌
        if (button == null)
            button = GetComponent<Button>();

        HookListener();
    }

    private void OnEnable()
    {
        // 비활성/활성 토글될 때 중복 등록 방지
        HookListener();
    }

    private void OnDisable()
    {
        UnhookListener();
    }

    private void OnDestroy()
    {
        UnhookListener();
    }

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

    // TaskbarManager에서 버튼 생성 직후 호출
    public void Initialize(string id, WindowManager manager, WindowController window)
    {
        appId = id;               // 디버그/호환용
        windowManager = manager;
        targetWindow = window;
    }

    // (선택) TaskbarManager가 상태 표시용으로 호출하는 훅
    public void SetMinimizedVisual(bool minimized)
    {
        // 지금은 비워둬도 됨.
        // 나중에: Image 색/스프라이트(볼록/오목) 바꾸기
    }

    public void SetActiveVisual(bool active)
    {
        // 지금은 비워둬도 됨.
        // 나중에: Image 색/스프라이트 바꾸기
    }

    private void OnClick()
    {
        if (windowManager == null) return;

        // 창이 이미 닫혀서 참조가 끊긴 경우: 안전하게 무시
        if (targetWindow == null) return;

        // 단일 진실은 targetWindow.AppId (문자열 오타/불일치 방지)
        string id = string.IsNullOrEmpty(targetWindow.AppId) ? appId : targetWindow.AppId;
        if (string.IsNullOrEmpty(id)) return;

        // 토글: 최소화면 복원, 아니면 최소화
        if (targetWindow.IsMinimized)
            windowManager.Restore(id);
        else
            windowManager.Minimize(id);
    }
}
