using TMPro;
using UnityEngine;

public class BridgeMusicProgressUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform playLine;
    [SerializeField] private RectTransform playCursor;
    [SerializeField] private TMP_Text timeText;

    [Header("Cursor Range")]
    [SerializeField] private float leftPadding = 0f;
    [SerializeField] private float rightPadding = 0f;

    private void Update()
    {
        var manager = BGMManager.Instance;
        if (manager == null)
        {
            SetCursorProgress(0f);
            SetTimeText(0f, 0f);
            return;
        }

        float duration = manager.Duration;
        float current = manager.CurrentTime;
        float progress = manager.Progress01;

        SetCursorProgress(progress);
        SetTimeText(current, duration);
    }

    private void SetCursorProgress(float progress01)
    {
        if (playLine == null || playCursor == null)
            return;

        float usableWidth = playLine.rect.width - leftPadding - rightPadding;
        float x = leftPadding + (usableWidth * Mathf.Clamp01(progress01));

        Vector2 anchored = playCursor.anchoredPosition;
        anchored.x = x;
        playCursor.anchoredPosition = anchored;
    }

    private void SetTimeText(float current, float total)
    {
        if (timeText == null)
            return;

        timeText.text = $"{FormatTime(current)} / {FormatTime(total)}";
    }

    private string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
        int minutes = totalSeconds / 60;
        int remainSeconds = totalSeconds % 60;
        return $"{minutes:00}:{remainSeconds:00}";
    }
}