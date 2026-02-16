using UnityEngine;

public class DesktopResizeWatcher : MonoBehaviour
{
    [SerializeField] private WindowManager windowManager;
    private Vector2 _lastSize;

    private void Start()
    {
        var rt = (RectTransform)transform;
        _lastSize = rt.rect.size;
    }

    private void Update()
    {
        var rt = (RectTransform)transform;
        var size = rt.rect.size;

        if (size != _lastSize)
        {
            _lastSize = size;
            windowManager?.OnDesktopResized(); // 여기서 PostApplyLayoutSanity 호출
        }
    }
}

