using UnityEngine;
using PPP.OS.Save;

public class SaveDebugController : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private WindowManager windowManager;

    /* =========================
     * DEBUG SAVE
     * ========================= */
    public void DebugSaveOS()
    {
        if (windowManager == null)
        {
            Debug.LogError("[SaveDebug] WindowManager not assigned!");
            return;
        }

        windowManager.SaveOS();
        Debug.Log("[SaveDebug] SaveOS executed.");
    }

    /* =========================
     * DEBUG LOAD
     * ========================= */
    public void DebugLoadOS()
    {
        if (windowManager == null)
        {
            Debug.LogError("[SaveDebug] WindowManager not assigned!");
            return;
        }

        windowManager.LoadOS();
        Debug.Log("[SaveDebug] LoadOS executed.");
    }
}
