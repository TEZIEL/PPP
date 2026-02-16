using UnityEngine;
using PPP.OS.Save;

public class SaveDebugController : MonoBehaviour
{
    [SerializeField] private WindowManager windowManager;

    public void DebugSaveWindows()
    {
        if (windowManager == null)
        {
            Debug.LogError("[SaveDebug] WindowManager not assigned!");
            return;
        }

        windowManager.SaveWindows();
    }

    public void DebugLoadWindows()
    {
        if (windowManager == null)
        {
            Debug.LogError("[SaveDebug] WindowManager not assigned!");
            return;
        }

        windowManager.LoadWindows(); // 아래에서 구현할 함수
    }
}
