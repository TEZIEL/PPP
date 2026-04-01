using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public enum AmbientType
{
    None,

    Ocean,          // 파도
    Storm,          // 폭풍우
    LightRain,      // 잔잔한 빗소리
    MorningBirds,   // 아침 새
    NightCrickets,  // 밤 귀뚜라미

    Cafe,
    Subway,
    CityStreet,     // 번화가
    Dreamcore,
    Campfire        // 모닥불
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
    [SerializeField] private AudioClip ocean;
    [SerializeField] private AudioClip storm;
    [SerializeField] private AudioClip lightRain;
    [SerializeField] private AudioClip morningBirds;
    [SerializeField] private AudioClip nightCrickets;

    [SerializeField] private AudioClip cafe;
    [SerializeField] private AudioClip subway;
    [SerializeField] private AudioClip cityStreet;
    [SerializeField] private AudioClip dreamcore;
    [SerializeField] private AudioClip campfire;

    private Dictionary<AmbientType, AudioClip> map;

    private AmbientType current;

    private void Awake()
    {
        Instance = this;

        currentSource = sourceA;
        nextSource = sourceB;


        currentSource.volume = 1f; // 🔥 반드시 추가
        nextSource.volume = 0f;


        map = new Dictionary<AmbientType, AudioClip>()
        {
            { AmbientType.Ocean, ocean },
            { AmbientType.Storm, storm },
            { AmbientType.LightRain, lightRain },
            { AmbientType.MorningBirds, morningBirds },
            { AmbientType.NightCrickets, nightCrickets },

            { AmbientType.Cafe, cafe },
            { AmbientType.Subway, subway },
            { AmbientType.CityStreet, cityStreet },
            { AmbientType.Dreamcore, dreamcore },
            { AmbientType.Campfire, campfire }
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

}