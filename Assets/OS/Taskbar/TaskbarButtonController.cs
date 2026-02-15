using UnityEngine;
using UnityEngine.UI;

public class TaskbarButtonController : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image stateImage;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color activeColor = new Color(0.75f, 0.9f, 1f, 1f);
    [SerializeField] private Color minimizedColor = new Color(0.55f, 0.55f, 0.55f, 1f);

    private string appId;
    private TaskbarManager owner;

    public void Initialize(TaskbarManager taskbarManager, string id)
    {
        owner = taskbarManager;
        appId = id;

        if (button != null)
        {
            button.onClick.AddListener(OnClick);
        }

        SetState(false, false);
    }

    public void SetState(bool isActive, bool isMinimized)
    {
        if (stateImage == null)
        {
            return;
        }

        stateImage.color = isMinimized ? minimizedColor : (isActive ? activeColor : normalColor);
    }

    private void OnClick()
    {
        owner?.OnTaskbarButtonClicked(appId);
    }
}
