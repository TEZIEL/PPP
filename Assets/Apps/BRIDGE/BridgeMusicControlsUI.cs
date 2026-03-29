using UnityEngine;
using UnityEngine.UI;

public class BridgeMusicControlsUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button pauseToggleButton;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button shuffleToggleButton;
    [SerializeField] private Button loopRoundToggleButton;

    [Header("Visual Toggles")]
    [SerializeField] private UIButtonSpriteToggle pauseToggleVisual;
    [SerializeField] private UIButtonSpriteToggle shuffleToggleVisual;
    [SerializeField] private UIButtonSpriteToggle loopRoundToggleVisual;

    private void OnEnable()
    {
        if (pauseToggleButton != null)
            pauseToggleButton.onClick.AddListener(OnClickPauseToggle);

        if (previousButton != null)
            previousButton.onClick.AddListener(OnClickPrevious);

        if (nextButton != null)
            nextButton.onClick.AddListener(OnClickNext);

        if (shuffleToggleButton != null)
            shuffleToggleButton.onClick.AddListener(OnClickShuffleToggle);

        if (loopRoundToggleButton != null)
            loopRoundToggleButton.onClick.AddListener(OnClickLoopRoundToggle);

        var manager = BGMManager.Instance;
        if (manager != null)
        {
            manager.OnPlayStateChanged += HandlePlayStateChanged;
            manager.OnShuffleChanged += HandleShuffleChanged;
            manager.OnPlaybackModeChanged += HandlePlaybackModeChanged;
            manager.OnTrackChanged += HandleTrackChanged;
        }

        RefreshAllVisuals();
    }

    private void OnDisable()
    {
        if (pauseToggleButton != null)
            pauseToggleButton.onClick.RemoveListener(OnClickPauseToggle);

        if (previousButton != null)
            previousButton.onClick.RemoveListener(OnClickPrevious);

        if (nextButton != null)
            nextButton.onClick.RemoveListener(OnClickNext);

        if (shuffleToggleButton != null)
            shuffleToggleButton.onClick.RemoveListener(OnClickShuffleToggle);

        if (loopRoundToggleButton != null)
            loopRoundToggleButton.onClick.RemoveListener(OnClickLoopRoundToggle);

        var manager = BGMManager.Instance;
        if (manager != null)
        {
            manager.OnPlayStateChanged -= HandlePlayStateChanged;
            manager.OnShuffleChanged -= HandleShuffleChanged;
            manager.OnPlaybackModeChanged -= HandlePlaybackModeChanged;
            manager.OnTrackChanged -= HandleTrackChanged;
        }
    }

    private void Update()
    {
        var manager = BGMManager.Instance;
        if (manager == null)
            return;

        if (previousButton != null)
            previousButton.interactable = manager.CanGoPrevious;

        if (pauseToggleButton != null)
            pauseToggleButton.interactable = manager.CurrentTrack != null;

        if (nextButton != null)
            nextButton.interactable = manager.CanGoNext;
    }

    private void OnClickPauseToggle()
    {
        BGMManager.Instance?.TogglePause();
        RefreshPauseVisual();
    }

    private void OnClickPrevious()
    {
        BGMManager.Instance?.PlayPrevious();
    }

    private void OnClickNext()
    {
        BGMManager.Instance?.PlayNext();
    }

    private void OnClickShuffleToggle()
    {
        BGMManager.Instance?.ToggleShuffle();
        RefreshShuffleVisual();
    }

    private void OnClickLoopRoundToggle()
    {
        BGMManager.Instance?.TogglePlaybackMode();
        RefreshLoopRoundVisual();
    }

    private void HandlePlayStateChanged(bool _)
    {
        RefreshPauseVisual();
    }

    private void HandleShuffleChanged(bool _)
    {
        RefreshShuffleVisual();
    }

    private void HandlePlaybackModeChanged(PlaybackMode _)
    {
        RefreshLoopRoundVisual();
    }

    private void HandleTrackChanged(BGMTrackData _)
    {
        RefreshPauseVisual();
    }

    private void RefreshAllVisuals()
    {
        RefreshPauseVisual();
        RefreshShuffleVisual();
        RefreshLoopRoundVisual();
    }

    private void RefreshPauseVisual()
    {
        var manager = BGMManager.Instance;
        if (pauseToggleVisual == null) return;

        bool isPaused = manager != null && manager.IsPaused;
        bool hasTrack = manager != null && manager.CurrentTrack != null;

        pauseToggleVisual.SetInteractable(hasTrack);
        pauseToggleVisual.SetOn(isPaused);
    }

    private void RefreshShuffleVisual()
    {
        var manager = BGMManager.Instance;
        if (shuffleToggleVisual == null) return;

        bool enabled = manager != null && manager.ShuffleEnabled;
        shuffleToggleVisual.SetInteractable(true);
        shuffleToggleVisual.SetOn(enabled);
    }

    private void RefreshLoopRoundVisual()
    {
        var manager = BGMManager.Instance;
        if (loopRoundToggleVisual == null) return;

        bool isRound = manager != null && manager.PlaybackMode == PlaybackMode.Round;
        loopRoundToggleVisual.SetInteractable(true);
        loopRoundToggleVisual.SetOn(isRound);
    }
}