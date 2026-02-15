using UnityEngine;
using UnityEngine.UI;

public class TaskbarButtonController : MonoBehaviour
{
    [SerializeField] private string appId;
    [SerializeField] private WindowManager windowManager;
    [SerializeField] private WindowController targetWindow;

    [SerializeField] private Button button;

    private void Awake()
    {
        if (button != null)
            button.onClick.AddListener(OnClick);
    }

    public void SetMinimizedVisual(bool minimized)
    {
        // 지금은 아무 것도 안 해도 됨(컴파일용)
    }
    public void SetActiveVisual(bool active)
    {
        // 지금은 아무 것도 안 해도 됨(컴파일용)
    }


    public void Initialize(string id, WindowManager manager, WindowController window)
    {
        appId = id;
        windowManager = manager;
        targetWindow = window;
    }

    private void OnClick()
    {
        if (targetWindow == null || windowManager == null)
            return;

        if (targetWindow.IsMinimized)
            windowManager.Restore(appId);
        else
            windowManager.Minimize(appId);
    }
}
