using System;
using System.Collections.Generic;
using UnityEngine;
using PPP.OS.Save;

public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance { get; private set; }

    [SerializeField] private AudioSource musicSource;
    [SerializeField] private List<BGMTrackData> tracks = new();

    public event Action<BGMTrackData> OnTrackChanged;
    public event Action<bool> OnPlayStateChanged;
    public event Action<bool> OnShuffleChanged;
    public event Action<PlaybackMode> OnPlaybackModeChanged;
    public event Action OnLibraryChanged;

    public IReadOnlyList<BGMTrackData> Tracks => tracks;
    public BGMTrackData CurrentTrack => currentTrack;
    public bool ShuffleEnabled => shuffleEnabled;
    public PlaybackMode PlaybackMode => playbackMode;
    public bool IsPlaying => musicSource != null && musicSource.isPlaying;
    public bool IsPaused => isPaused;
    public bool CanGoPrevious => historyIndex > 0;
    public bool CanGoNext => currentTrack != null;

    private readonly List<BGMTrackData> playHistory = new();
    private readonly List<BGMTrackData> shuffleBag = new();
    public bool HasCurrentTrack => currentTrack != null;

    private BGMTrackData currentTrack;
    private int historyIndex = -1;
    private bool shuffleEnabled;
    private bool isPaused;
    private PlaybackMode playbackMode = PlaybackMode.LoopOne;
    // 제외된 곡 관리
    private readonly HashSet<string> disabledTrackIds = new();
    public bool HasDisabledTracks => disabledTrackIds.Count > 0;
    private static BGMOsStateData pendingOsState;


    public float CurrentTime => musicSource != null ? musicSource.time : 0f;

    public float Duration
    {
        get
        {
            if (musicSource == null || musicSource.clip == null)
                return 0f;
            return musicSource.clip.length;
        }
    }

    public float Progress01
    {
        get
        {
            float duration = Duration;
            if (duration <= 0.0001f)
                return 0f;

            return Mathf.Clamp01(CurrentTime / duration);
        }
    }

    public bool IsTrackDisabled(BGMTrackData track)
    {
        if (track == null) return false;
        return disabledTrackIds.Contains(track.trackId);
    }

    public void DisableTrack(BGMTrackData track)
    {
        if (track == null) return;

        disabledTrackIds.Add(track.trackId);
        OnLibraryChanged?.Invoke();
    }

    public void RestoreAllTracks()
    {
        if (disabledTrackIds.Count == 0)
            return;

        disabledTrackIds.Clear();
        OnLibraryChanged?.Invoke();
    }

    public void PlayTrackFromButton(BGMTrackData track)
    {
        if (track == null || track.clip == null)
            return;

        if (IsTrackDisabled(track))
            return;

        if (shuffleEnabled)
        {
            shuffleEnabled = false;
            OnShuffleChanged?.Invoke(false);
        }

        PlayTrack(track, true);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (transform.parent != null)
            transform.SetParent(null, true);

        DontDestroyOnLoad(gameObject);
        RebuildShuffleBag();

        if (pendingOsState != null)
        {
            ApplyOsState(pendingOsState);
            pendingOsState = null;
        }
    }

    public BGMOsStateData CaptureOsState()
    {
        return new BGMOsStateData
        {
            disabledTrackIds = new List<string>(disabledTrackIds),
            shuffleEnabled = shuffleEnabled,
            playbackMode = playbackMode.ToString()
        };
    }

    public void ApplyOsState(BGMOsStateData data)
    {
        disabledTrackIds.Clear();

        if (data != null)
        {
            if (data.disabledTrackIds != null)
            {
                foreach (var id in data.disabledTrackIds)
                {
                    if (!string.IsNullOrWhiteSpace(id))
                        disabledTrackIds.Add(id);
                }
            }

            shuffleEnabled = data.shuffleEnabled;

            if (!string.IsNullOrWhiteSpace(data.playbackMode)
                && Enum.TryParse(data.playbackMode, true, out PlaybackMode parsedMode))
            {
                playbackMode = parsedMode;
            }
            else
            {
                playbackMode = PlaybackMode.LoopOne;
            }
        }
        else
        {
            shuffleEnabled = false;
            playbackMode = PlaybackMode.LoopOne;
        }

        RebuildShuffleBag();

        OnLibraryChanged?.Invoke();
        OnShuffleChanged?.Invoke(shuffleEnabled);
        OnPlaybackModeChanged?.Invoke(playbackMode);
    }

    public static void CachePendingOsState(BGMOsStateData data)
    {
        if (data == null)
        {
            pendingOsState = null;
            return;
        }

        pendingOsState = new BGMOsStateData
        {
            disabledTrackIds = data.disabledTrackIds != null ? new List<string>(data.disabledTrackIds) : new List<string>(),
            shuffleEnabled = data.shuffleEnabled,
            playbackMode = data.playbackMode
        };
    }

    private void Update()
    {
        if (musicSource == null || currentTrack == null || musicSource.clip == null)
            return;

        if (!musicSource.isPlaying && !isPaused)
        {
            if (playbackMode == PlaybackMode.LoopOne)
            {
                ReplayCurrent();
            }
            else
            {
                PlayNextInternal(false);
            }
        }
    }

    public void TogglePause()
    {
        if (musicSource == null || currentTrack == null)
            return;

        if (musicSource.isPlaying)
        {
            musicSource.Pause();
            isPaused = true;
            OnPlayStateChanged?.Invoke(false);
        }
        else if (isPaused)
        {
            musicSource.UnPause();
            isPaused = false;
            OnPlayStateChanged?.Invoke(true);
        }
    }

    public void PlayTrackByDoubleClick(BGMTrackData track)
    {
        if (track == null || track.clip == null || musicSource == null)
            return;

        if (IsTrackDisabled(track))   // 🔥 추가
            return;

        if (shuffleEnabled)
        {
            shuffleEnabled = false;
            OnShuffleChanged?.Invoke(false);
        }

        PlayTrack(track, true);
    }

    public void PlayNext()
    {
        if (currentTrack == null)
            return;

        PlayNextInternal(true);
    }

    public void PlayPrevious()
    {
        if (musicSource == null)
            return;

        for (int i = historyIndex - 1; i >= 0; i--)
        {
            var track = playHistory[i];

            if (!IsTrackDisabled(track))
            {
                historyIndex = i;
                PlayTrackWithoutHistoryAppend(track);
                return;
            }
        }
    }

    public void ToggleShuffle()
    {
        shuffleEnabled = !shuffleEnabled;
        RebuildShuffleBag();
        OnShuffleChanged?.Invoke(shuffleEnabled);
    }

    public void TogglePlaybackMode()
    {
        playbackMode = playbackMode == PlaybackMode.LoopOne
            ? PlaybackMode.Round
            : PlaybackMode.LoopOne;

        OnPlaybackModeChanged?.Invoke(playbackMode);
    }

    private void PlayNextInternal(bool userTriggered)
    {
        if (tracks.Count == 0 || musicSource == null)
            return;

        BGMTrackData nextTrack = null;

        if (shuffleEnabled)
        {
            if (shuffleBag.Count == 0)
                RebuildShuffleBag();

            if (currentTrack != null)
                shuffleBag.Remove(currentTrack);

            if (shuffleBag.Count == 0)
                RebuildShuffleBag();

            if (currentTrack != null)
                shuffleBag.Remove(currentTrack);

            if (shuffleBag.Count > 0)
            {
                int index = UnityEngine.Random.Range(0, shuffleBag.Count);
                nextTrack = shuffleBag[index];
                shuffleBag.RemoveAt(index);
            }
        }
        else
        {
            if (currentTrack == null)
                return;

            int startIndex = tracks.IndexOf(currentTrack);

            for (int i = 1; i <= tracks.Count; i++)
            {
                int nextIndex = (startIndex + i) % tracks.Count;
                var candidate = tracks[nextIndex];

                if (candidate != null && candidate.clip != null && !IsTrackDisabled(candidate))
                {
                    nextTrack = candidate;
                    break;
                }
            }
        }

        if (nextTrack == null)
        {
            // 재생 가능한 곡이 없음 → 현재곡 유지
            ReplayCurrent();
            return;
        }

        PlayTrack(nextTrack, true);
    }

    private void PlayTrack(BGMTrackData track, bool appendHistory)
    {
        currentTrack = track;
        musicSource.clip = track.clip;
        musicSource.Play();
        isPaused = false;

        if (appendHistory)
            AppendHistory(track);

        OnTrackChanged?.Invoke(track);
        OnPlayStateChanged?.Invoke(true);
    }

    private void PlayTrackWithoutHistoryAppend(BGMTrackData track)
    {
        currentTrack = track;
        musicSource.clip = track.clip;
        musicSource.Play();
        isPaused = false;

        OnTrackChanged?.Invoke(track);
        OnPlayStateChanged?.Invoke(true);
    }



    private void ReplayCurrent()
    {
        if (currentTrack == null || currentTrack.clip == null || musicSource == null)
            return;

        musicSource.clip = currentTrack.clip;
        musicSource.Play();
        isPaused = false;
        OnPlayStateChanged?.Invoke(true);
    }

    private void AppendHistory(BGMTrackData track)
    {
        if (historyIndex < playHistory.Count - 1)
            playHistory.RemoveRange(historyIndex + 1, playHistory.Count - historyIndex - 1);

        playHistory.Add(track);
        historyIndex = playHistory.Count - 1;
    }

    private void RebuildShuffleBag()
    {
        shuffleBag.Clear();
        foreach (var track in tracks)
        {
            if (track != null && track.clip != null && !IsTrackDisabled(track))
                shuffleBag.Add(track);
        }
    }
}