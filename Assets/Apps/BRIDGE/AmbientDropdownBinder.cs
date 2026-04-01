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
            case AmbientType.Ocean: return "파도";
            case AmbientType.Storm: return "폭풍우";
            case AmbientType.LightRain: return "잔잔한 비";
            case AmbientType.MorningBirds: return "아침 새";
            case AmbientType.NightCrickets: return "밤 귀뚜라미";
            case AmbientType.Cafe: return "카페";
            case AmbientType.Subway: return "지하철";
            case AmbientType.CityStreet: return "번화가";
            case AmbientType.Dreamcore: return "드림코어";
            case AmbientType.Campfire: return "모닥불";
            default: return "없음";
        }
    }
}