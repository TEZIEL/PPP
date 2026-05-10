using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public enum AmbientType
{
    None,

    Wave,          // 파도
    Firework,          // 폭풍우
    Lightrain,      // 잔잔한 빗소리
    Heavyrain,   // 아침 새
    Morning,  // 밤 귀뚜라미

    Night,
    Campfire,
    Cafe,     // 번화가
    Subway,
    Grocery        // 모닥불
}


public class AmbientManager : MonoBehaviour
{
    public static AmbientManager Instance;

    [Header("Audio")]
    [SerializeField] private AudioSource sourceA;
    [SerializeField] private AudioSource sourceB;
    private AudioSource currentSource;
    private AudioSource nextSource;
    private bool isTransitioning;
    private bool isPlaying;

    [Header("Ambient Clips")]
    [SerializeField] private AudioClip wave;
    [SerializeField] private AudioClip firework;
    [SerializeField] private AudioClip lightrain;
    [SerializeField] private AudioClip heavyrain;
    [SerializeField] private AudioClip morning;

    [SerializeField] private AudioClip night;
    [SerializeField] private AudioClip campfire;
    [SerializeField] private AudioClip cafe;
    [SerializeField] private AudioClip subway;
    [SerializeField] private AudioClip grocery;

    private Dictionary<AmbientType, AudioClip> map;

    private AmbientType current;

    private void Awake()
    {
        Instance = this;

        currentSource = sourceA;
        nextSource = sourceB;
        TraceAmbientSource("Awake before EnsureSavedAudioSettingsApplied", currentSource);
        OptionManager.EnsureSavedAudioSettingsApplied(currentSource != null ? currentSource.outputAudioMixerGroup?.audioMixer : null);
        TraceAmbientSource("Awake after EnsureSavedAudioSettingsApplied", currentSource);


        OptionManager.TraceAudioOptions("AmbientManager.Awake assigning currentSource.volume=1 nextSource.volume=0");
        currentSource.volume = 1f; // 🔥 반드시 추가
        nextSource.volume = 0f;


        map = new Dictionary<AmbientType, AudioClip>()
        {
            { AmbientType.Wave, wave },
            { AmbientType.Firework, firework },
            { AmbientType.Lightrain, lightrain },
            { AmbientType.Heavyrain, heavyrain },
            { AmbientType.Morning, morning },
            { AmbientType.Night, night },
            { AmbientType.Campfire, campfire },
            { AmbientType.Cafe, cafe },
            { AmbientType.Subway, subway },
            { AmbientType.Grocery, grocery }
        };
    }

    public bool IsPlaying()
    {
        return isPlaying;
    }
    // PLAY
    // =========================
    public void Play(AmbientType type)
    {
        TraceAmbientSource($"Play {type} before EnsureSavedAudioSettingsApplied", currentSource);
        OptionManager.EnsureSavedAudioSettingsApplied(currentSource != null ? currentSource.outputAudioMixerGroup?.audioMixer : null);
        TraceAmbientSource($"Play {type} after EnsureSavedAudioSettingsApplied", currentSource);

        if (type == AmbientType.None)
        {
            Stop();
            return;
        }

        // 🔥 추가 (핵심 안정화)
        if (isTransitioning)
        {
            Stop(); // 상태 완전히 초기화
        }

        if (current == type && isPlaying)
            return;

        if (map.TryGetValue(type, out var clip))
        {
            StopAllCoroutines();
            StartCoroutine(CrossFade(clip));

            current = type;
            isPlaying = true;
        }
    }

    private IEnumerator CrossFade(AudioClip newClip)
    {
        isTransitioning = true; // 🔥 시작

        nextSource.clip = newClip;
        nextSource.loop = true;
        nextSource.volume = 0f;
        TraceAmbientSource($"CrossFade before nextSource.Play clip={(newClip != null ? newClip.name : "NULL")}", nextSource);
        nextSource.Play();

        float duration = 0.7f;
        float time = 0f;

        float startVolume = 1f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            currentSource.volume = Mathf.Lerp(startVolume, 0f, t);
            nextSource.volume = Mathf.Lerp(0f, startVolume, t);

            yield return null;
        }

        currentSource.Stop();

        var temp = currentSource;
        currentSource = nextSource;
        nextSource = temp;

        currentSource.volume = 1f;
        TraceAmbientSource("CrossFade complete currentSource.volume=1", currentSource);

        isTransitioning = false; // 🔥 끝
    }
    // =========================
    // STOP
    // =========================
    public void Stop()
    {
        StopAllCoroutines();

        currentSource.Stop();
        nextSource.Stop();

        OptionManager.TraceAudioOptions("AmbientManager.Stop assigning currentSource.volume=1 nextSource.volume=0");
        currentSource.volume = 1f;
        nextSource.volume = 0f;

        current = AmbientType.None;
        isPlaying = false; // 🔥 핵심
        isTransitioning = false;
    }




    private IEnumerator FadeOut()
    {
        float duration = 0.5f;
        float time = 0f;
        float startVolume = currentSource.volume;

        while (time < duration)
        {
            time += Time.deltaTime;
            currentSource.volume = Mathf.Lerp(startVolume, 0f, time / duration);
            yield return null;
        }

        currentSource.Stop();

        current = AmbientType.None;
    }

    private void TraceAmbientSource(string context, AudioSource source)
    {
        if (source == null)
        {
            OptionManager.TraceAudioOptions($"AmbientManager.{context} source=NULL");
            return;
        }

        var group = source.outputAudioMixerGroup;
        var mixer = group != null ? group.audioMixer : null;
        var clip = source.clip;

        OptionManager.TraceAudioOptions(
            $"AmbientManager.{context} source={source.name} " +
            $"volume={source.volume} mute={source.mute} playOnAwake={source.playOnAwake} isPlaying={source.isPlaying} " +
            $"group={(group != null ? group.name : "NULL")} mixer={(mixer != null ? mixer.name : "NULL")} " +
            $"clip={(clip != null ? clip.name : "NULL")}");
    }

}
