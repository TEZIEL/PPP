using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

public class AmbientDropdownBinder : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdown;
    [SerializeField] private Image playButtonImage;
    [SerializeField] private Sprite playIcon;   // ▶
    [SerializeField] private Sprite stopIcon;   // ⏸
    private bool isLocked;

    private List<AmbientType> types = new List<AmbientType>();

    private void Awake()
    {
        SetupOptions();
        dropdown.onValueChanged.AddListener(OnChanged);
    }

    private void SetupOptions()
    {
        dropdown.ClearOptions();
        types.Clear();

        var options = new List<string>();

        foreach (AmbientType type in System.Enum.GetValues(typeof(AmbientType)))
        {
            if (type == AmbientType.None)
                continue;

            types.Add(type);
            options.Add(GetDisplayName(type));
        }

        dropdown.AddOptions(options);
    }

    public void TogglePlay()
    {
        if (AmbientManager.Instance == null)
            return;

        if (isLocked)
            return; // 🔥 여기 중요

        if (AmbientManager.Instance.IsPlaying())
        {
            AmbientManager.Instance.Stop();

            if (playButtonImage != null)
                playButtonImage.sprite = playIcon;
        }
        else
        {
            AmbientManager.Instance.Play(types[dropdown.value]);

            if (playButtonImage != null)
                playButtonImage.sprite = stopIcon;
        }
    }

    private void Unlock()
    {
        isLocked = false;
    }

    private void OnChanged(int index)
    {
        if (AmbientManager.Instance == null)
            return;

        isLocked = true; // 🔥 핵심

        AmbientManager.Instance.Play(types[index]);

        if (playButtonImage != null)
            playButtonImage.sprite = stopIcon;

        dropdown.Hide();

        Invoke(nameof(Unlock), 0.25f); // 🔥 살짝 길게
    }



    public void SetPlayIcons(Sprite play, Sprite stop)
    {
        if (play != null)
            playIcon = play;

        if (stop != null)
            stopIcon = stop;

        
    }

    // 🎯 표시 이름
    private string GetDisplayName(AmbientType type)
    {
        switch (type)
        {
            case AmbientType.Wave: return "Wave";
            case AmbientType.Firework: return "Firework";
            case AmbientType.Lightrain: return "Lightrain";
            case AmbientType.Heavyrain: return "Heavyrain";
            case AmbientType.Morning: return "Morning";
            case AmbientType.Night: return "Night";
            case AmbientType.Campfire: return "Campfire";
            case AmbientType.Cafe: return "Cafe";
            case AmbientType.Subway: return "Subway";
            case AmbientType.Grocery: return "Grocery";
            default: return "None";
        }
    }
}