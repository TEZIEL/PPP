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
    MusicPlay,

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

    [Header("VN Clips")]
    public AudioClip vnTypingClip;

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

    [Header("Music App Clips")]
    public AudioClip musicPlay;

    private Dictionary<OSSoundEvent, AudioClip> osMap;

    private void Awake()
    {
        Instance = this;
        TraceAudioSource("Awake before EnsureSavedAudioSettingsApplied", osSource);
        TraceAudioSource("Awake vnSource", vnSource);
        TraceAudioSource("Awake vnTypingSource", vnTypingSource);
        OptionManager.EnsureSavedAudioSettingsApplied(osSource != null ? osSource.outputAudioMixerGroup?.audioMixer : null);
        TraceAudioSource("Awake after EnsureSavedAudioSettingsApplied", osSource);

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
            { OSSoundEvent.ProvideComplete, provideComplete },
            { OSSoundEvent.MusicPlay, musicPlay }

        };
    }

    public void PlayOSWithPitch(OSSoundEvent e, float pitch)
    {
        TraceAudioSource($"PlayOSWithPitch {e} before EnsureSavedAudioSettingsApplied", osSource);
        OptionManager.EnsureSavedAudioSettingsApplied(osSource != null ? osSource.outputAudioMixerGroup?.audioMixer : null);
        TraceAudioSource($"PlayOSWithPitch {e} after EnsureSavedAudioSettingsApplied", osSource);

        if (osMap.TryGetValue(e, out var clip))
        {
            var temp = gameObject.AddComponent<AudioSource>();
            temp.clip = clip;
            temp.pitch = pitch;
            if (osSource != null)
            {
                temp.outputAudioMixerGroup = osSource.outputAudioMixerGroup;
                temp.volume = osSource.volume;
            }
            TraceAudioSource($"PlayOSWithPitch {e} temp before Play clip={(clip != null ? clip.name : "NULL")} pitch={pitch}", temp);
            temp.Play();

            Destroy(temp, clip.length);
        }
    }

    public void PlayOS(OSSoundEvent e)
    {
        TraceAudioSource($"PlayOS {e} before EnsureSavedAudioSettingsApplied", osSource);
        OptionManager.EnsureSavedAudioSettingsApplied(osSource != null ? osSource.outputAudioMixerGroup?.audioMixer : null);
        TraceAudioSource($"PlayOS {e} before PlayOneShot", osSource);

        if (osMap.TryGetValue(e, out var clip))
        {
            osSource.PlayOneShot(clip);
        }
    }

    public void PlayVNTyping(float pitch = 1f, float volumeScale = 1f)
    {
        if (vnTypingSource == null || vnTypingClip == null)
            return;

        OptionManager.EnsureSavedAudioSettingsApplied(vnTypingSource.outputAudioMixerGroup != null
            ? vnTypingSource.outputAudioMixerGroup.audioMixer
            : null);

        vnTypingSource.Stop();
        vnTypingSource.pitch = pitch;
        vnTypingSource.PlayOneShot(vnTypingClip, volumeScale);
    }

    private void TraceAudioSource(string context, AudioSource source)
    {
        if (source == null)
        {
            OptionManager.TraceAudioOptions($"SoundManager.{context} source=NULL");
            return;
        }

        var group = source.outputAudioMixerGroup;
        var mixer = group != null ? group.audioMixer : null;
        var clip = source.clip;

        OptionManager.TraceAudioOptions(
            $"SoundManager.{context} source={source.name} " +
            $"volume={source.volume} mute={source.mute} playOnAwake={source.playOnAwake} isPlaying={source.isPlaying} " +
            $"group={(group != null ? group.name : "NULL")} mixer={(mixer != null ? mixer.name : "NULL")} " +
            $"clip={(clip != null ? clip.name : "NULL")}");
    }
}
