using System;
using System.Collections.Generic;
using UnityEngine;

public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance { get; private set; }

    [Header("Audio")]
    [SerializeField] private AudioSource musicSource;

    [Header("Library")]
    [SerializeField] private List<BGMTrackData> tracks = new();

    public IReadOnlyList<BGMTrackData> Tracks => tracks;
    public BGMTrackData CurrentTrack => currentIndex >= 0 && currentIndex < playOrder.Count
        ? playOrder[currentIndex]
        : null;

    public bool IsPlaying => musicSource != null && musicSource.isPlaying;
    public bool ShuffleEnabled => shuffleEnabled;
    public bool LoopPlaylistEnabled => loopPlaylistEnabled;

    public event Action<BGMTrackData> OnTrackChanged;
    public event Action<bool> OnPlayStateChanged;
    public event Action OnLibraryChanged;

    private readonly List<BGMTrackData> playOrder = new();
    private readonly HashSet<string> blockedTrackIds = new();

    private int currentIndex = -1;
    private bool shuffleEnabled;
    private bool loopPlaylistEnabled = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        RebuildPlayOrder();
    }

    private void Update()
    {
        if (musicSource == null) return;
        if (!musicSource.isPlaying && musicSource.clip != null)
        {
            if (musicSource.time <= 0.01f || musicSource.time >= musicSource.clip.length - 0.05f)
            {
                PlayNext(autoTriggered: true);
            }
        }
    }

    public void PlayTrack(BGMTrackData track)
    {
        if (track == null || musicSource == null) return;

        int index = playOrder.IndexOf(track);
        if (index < 0) return;

        currentIndex = index;
        musicSource.clip = track.clip;
        musicSource.Play();

        OnTrackChanged?.Invoke(track);
        OnPlayStateChanged?.Invoke(true);
    }

    public void Play()
    {
        if (musicSource == null) return;

        if (musicSource.clip == null)
        {
            if (playOrder.Count == 0) return;
            currentIndex = Mathf.Clamp(currentIndex, 0, playOrder.Count - 1);
            if (currentIndex < 0) currentIndex = 0;
            PlayTrack(playOrder[currentIndex]);
            return;
        }

        musicSource.Play();
        OnPlayStateChanged?.Invoke(true);
    }

    public void Pause()
    {
        if (musicSource == null) return;
        musicSource.Pause();
        OnPlayStateChanged?.Invoke(false);
    }

    public void Stop()
    {
        if (musicSource == null) return;
        musicSource.Stop();
        OnPlayStateChanged?.Invoke(false);
    }

    public void PlayNext(bool autoTriggered = false)
    {
        if (playOrder.Count == 0) return;

        if (shuffleEnabled && playOrder.Count > 1)
        {
            int next;
            do
            {
                next = UnityEngine.Random.Range(0, playOrder.Count);
            } while (next == currentIndex);

            currentIndex = next;
            PlayTrack(playOrder[currentIndex]);
            return;
        }

        int nextIndex = currentIndex + 1;
        if (nextIndex >= playOrder.Count)
        {
            if (!loopPlaylistEnabled && autoTriggered)
            {
                Stop();
                return;
            }

            nextIndex = 0;
        }

        currentIndex = nextIndex;
        PlayTrack(playOrder[currentIndex]);
    }

    public void PlayPrevious()
    {
        if (playOrder.Count == 0) return;

        int prevIndex = currentIndex - 1;
        if (prevIndex < 0)
            prevIndex = playOrder.Count - 1;

        currentIndex = prevIndex;
        PlayTrack(playOrder[currentIndex]);
    }

    public void SetShuffle(bool enabled) => shuffleEnabled = enabled;
    public void SetLoopPlaylist(bool enabled) => loopPlaylistEnabled = enabled;

    public void SetVolume(float value)
    {
        if (musicSource == null) return;
        musicSource.volume = Mathf.Clamp01(value);
    }

    public void BlockTrack(string trackId)
    {
        if (string.IsNullOrWhiteSpace(trackId)) return;
        blockedTrackIds.Add(trackId);
        RebuildPlayOrder();
    }

    public void UnblockAllTracks()
    {
        blockedTrackIds.Clear();
        RebuildPlayOrder();
    }

    private void RebuildPlayOrder()
    {
        BGMTrackData current = CurrentTrack;

        playOrder.Clear();
        foreach (var track in tracks)
        {
            if (track == null || track.clip == null) continue;
            if (blockedTrackIds.Contains(track.trackId)) continue;
            playOrder.Add(track);
        }

        if (playOrder.Count == 0)
        {
            currentIndex = -1;
            if (musicSource != null) musicSource.clip = null;
            OnLibraryChanged?.Invoke();
            return;
        }

        currentIndex = current != null ? playOrder.IndexOf(current) : 0;
        if (currentIndex < 0) currentIndex = 0;

        OnLibraryChanged?.Invoke();
    }
}