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
        if (targetWindow == null) return;

        string id = string.IsNullOrEmpty(targetWindow.AppId) ? appId : targetWindow.AppId;
        if (string.IsNullOrEmpty(id)) return;

        // 1) 최소화 상태면: 복원 + 포커스
        if (targetWindow.IsMinimized)
        {
            windowManager.Restore(id);     // 내부에서 Focus까지 하면 OK
                                           // 만약 Restore가 Focus를 안 부르는 구조로 바꿨다면 여기서 windowManager.Focus(id); 호출
            return;
        }

        // 2) 현재 포커스 창이면: 최소화 (윈도우식)
        if (windowManager.ActiveAppId == id)
        {
            windowManager.Minimize(id);
            return;
        }

        // 3) 포커스가 아니면: 포커스만
        windowManager.Focus(id);
    }

}
