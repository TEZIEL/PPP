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
    Scroll
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
            { OSSoundEvent.Scroll, scroll }
        };
    }

    public void PlayOS(OSSoundEvent e)
    {
        if (osMap.TryGetValue(e, out var clip))
        {
            osSource.PlayOneShot(clip);
        }
    }
}