using UnityEngine;
using System.Collections.Generic;

public enum OSSoundEvent
{
    Click,
    Open,
    Close,
    Minimize,
    Pin,
    Restore,
    Scroll,

    // 🔥 추가
    Save,
    Load,
    Delete,
    FadeOut,

    IngredientFill1,
    IngredientFill2,
    Retry,
    CraftFail,
    CraftFailProvide,
    CraftSuccess,
    ProvideComplete
}

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Audio Sources")]
    public AudioSource osSource;
    public AudioSource vnSource;
    public AudioSource vnTypingSource;

    [Header("OS Clips")]
    public AudioClip click;
    public AudioClip open;
    public AudioClip close;
    public AudioClip minimize;
    public AudioClip pin;
    public AudioClip restore;
    public AudioClip scroll;


    [Header("Game Clips")]
    public AudioClip save;
    public AudioClip load;
    public AudioClip delete;
    public AudioClip fadeOut;

    public AudioClip ingredient1;
    public AudioClip ingredient2;
    public AudioClip retry;
    public AudioClip craftFail;
    public AudioClip craftFailProvide;
    public AudioClip craftSuccess;
    public AudioClip provideComplete;


    private Dictionary<OSSoundEvent, AudioClip> osMap;

    private void Awake()
    {
        Instance = this;

        osMap = new Dictionary<OSSoundEvent, AudioClip>()
        {
            { OSSoundEvent.Click, click },
            { OSSoundEvent.Open, open },
            { OSSoundEvent.Close, close },
            { OSSoundEvent.Minimize, minimize },
            { OSSoundEvent.Pin, pin },
            { OSSoundEvent.Restore, restore },
            { OSSoundEvent.Scroll, scroll },

            { OSSoundEvent.Save, save },
            { OSSoundEvent.Load, load },
            { OSSoundEvent.Delete, delete },
            { OSSoundEvent.FadeOut, fadeOut },
            { OSSoundEvent.IngredientFill1, ingredient1 },
            { OSSoundEvent.IngredientFill2, ingredient2 },
            { OSSoundEvent.Retry, retry },
            { OSSoundEvent.CraftFail, craftFail },
            { OSSoundEvent.CraftFailProvide, craftFailProvide },
            { OSSoundEvent.CraftSuccess, craftSuccess },
            { OSSoundEvent.ProvideComplete, provideComplete }

        };
    }

    public void PlayOSWithPitch(OSSoundEvent e, float pitch)
    {
        if (osMap.TryGetValue(e, out var clip))
        {
            var temp = gameObject.AddComponent<AudioSource>();
            temp.clip = clip;
            temp.pitch = pitch;
            temp.Play();

            Destroy(temp, clip.length);
        }
    }

    public void PlayOS(OSSoundEvent e)
    {
        if (osMap.TryGetValue(e, out var clip))
        {
            osSource.PlayOneShot(clip);
        }
    }
}