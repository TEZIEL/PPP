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

    public void PlaySelected()
    {
        AmbientManager.Instance.Play((AmbientType)dropdown.value);
    }


    public void TogglePlay()
    {
        if (AmbientManager.Instance.IsPlaying())
        {
            AmbientManager.Instance.Stop();
            playButtonImage.sprite = playIcon;
        }
        else
        {
            AmbientManager.Instance.Play(types[dropdown.value]); // 🔥 변경
            playButtonImage.sprite = stopIcon;
        }
    }

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





    private void OnChanged(int index)
    {
        AmbientManager.Instance.Play(types[index]); // 🔥 핵심
        playButtonImage.sprite = stopIcon;          // UI 동기화
        dropdown.Hide();
    }
}