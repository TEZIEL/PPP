using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class OptionManager : MonoBehaviour
{
    private const float MinVolume = 0.0001f;

    private const string MasterKey = "Options.Master";
    private const string BgmKey = "Options.BGM";
    private const string SfxKey = "Options.SFX";
    private const string AmbientKey = "Options.Ambient";

    private const string MasterMuteKey = "Options.MasterMute";
    private const string BgmMuteKey = "Options.BgmMute";
    private const string SfxMuteKey = "Options.SfxMute";
    private const string AmbientMuteKey = "Options.AmbientMute";

    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider ambientSlider;

    [SerializeField] private Image masterMuteImage;
    [SerializeField] private Image bgmMuteImage;
    [SerializeField] private Image sfxMuteImage;
    [SerializeField] private Image ambientMuteImage;

    [SerializeField] private Sprite muteOnSprite;
    [SerializeField] private Sprite muteOffSprite;

    [SerializeField] private AudioMixer mixer;

    public static OptionManager Instance { get; private set; }

    private OptionState applied = new OptionState();
    private OptionState preview = new OptionState();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        Load();
        ApplyToMixer(applied);
        UpdateUI(); // 🔥 초기 UI
    }

    // 🔥 MUTE TOGGLE

    public void ToggleMasterMute()
    {
        preview.masterMuted = !preview.masterMuted;
        ApplyToMixer(preview);
        UpdateUI();
    }

    public void ToggleBgmMute()
    {
        preview.bgmMuted = !preview.bgmMuted;
        ApplyToMixer(preview);
        UpdateUI();
    }

    public void ToggleSfxMute()
    {
        preview.sfxMuted = !preview.sfxMuted;
        ApplyToMixer(preview);
        UpdateUI();
    }

    public void ToggleAmbientMute()
    {
        preview.ambientMuted = !preview.ambientMuted;
        ApplyToMixer(preview);
        UpdateUI();
    }

    // 🔥 SLIDER

    public void SetPreviewMaster(float v)
    {
        v = 1f - v;

        preview.master = ClampVolume(v);
        preview.masterMuted = false;

        ApplyToMixer(preview);
        UpdateUI();
    }

    public void SetPreviewBgm(float v)
    {
        v = 1f - v;

        preview.bgm = ClampVolume(v);
        preview.bgmMuted = false;

        ApplyToMixer(preview);
        UpdateUI();
    }

    public void SetPreviewSfx(float v)
    {
        v = 1f - v;

        preview.sfx = ClampVolume(v);
        preview.sfxMuted = false;

        ApplyToMixer(preview);
        UpdateUI();
    }

    public void SetPreviewAmbient(float v)
    {
        v = 1f - v;

        preview.ambient = ClampVolume(v);
        preview.ambientMuted = false;

        ApplyToMixer(preview);
        UpdateUI();
    }

    // 🔥 APPLY / CANCEL

    public void Apply()
    {
        applied = preview.Clone();
        Save();
    }

  

    public void Cancel()
    {
        ApplyToMixer(applied);
        preview = applied.Clone();
        UpdateUI();
    }

    // 🔥 MIXER

    private void ApplyToMixer(OptionState state)
    {
        if (mixer == null)
            return;

        float master = state.masterMuted ? MinVolume : state.master;
        float bgm = state.bgmMuted ? MinVolume : state.bgm;
        float sfx = state.sfxMuted ? MinVolume : state.sfx;
        float ambient = state.ambientMuted ? MinVolume : state.ambient;

        mixer.SetFloat("MasterVolume", LinearToDb(master));
        mixer.SetFloat("BGMVolume", LinearToDb(bgm));
        mixer.SetFloat("SFXVolume", LinearToDb(sfx));
        mixer.SetFloat("AmbientVolume", LinearToDb(ambient));
    }

    private static float ClampVolume(float value)
    {
        return Mathf.Clamp(value, MinVolume, 1f);
    }

    private static float LinearToDb(float value)
    {
        return Mathf.Log10(ClampVolume(value)) * 20f;
    }

    // 🔥 SAVE / LOAD

    private void Save()
    {
        PlayerPrefs.SetFloat(MasterKey, applied.master);
        PlayerPrefs.SetFloat(BgmKey, applied.bgm);
        PlayerPrefs.SetFloat(SfxKey, applied.sfx);
        PlayerPrefs.SetFloat(AmbientKey, applied.ambient);

        PlayerPrefs.SetInt(MasterMuteKey, applied.masterMuted ? 1 : 0);
        PlayerPrefs.SetInt(BgmMuteKey, applied.bgmMuted ? 1 : 0);
        PlayerPrefs.SetInt(SfxMuteKey, applied.sfxMuted ? 1 : 0);
        PlayerPrefs.SetInt(AmbientMuteKey, applied.ambientMuted ? 1 : 0);

        PlayerPrefs.Save();
    }

    private void Load()
    {
        applied.master = PlayerPrefs.GetFloat(MasterKey, 1f);
        applied.bgm = PlayerPrefs.GetFloat(BgmKey, 1f);
        applied.sfx = PlayerPrefs.GetFloat(SfxKey, 1f);
        applied.ambient = PlayerPrefs.GetFloat(AmbientKey, 1f);

        applied.masterMuted = PlayerPrefs.GetInt(MasterMuteKey, 0) == 1;
        applied.bgmMuted = PlayerPrefs.GetInt(BgmMuteKey, 0) == 1;
        applied.sfxMuted = PlayerPrefs.GetInt(SfxMuteKey, 0) == 1;
        applied.ambientMuted = PlayerPrefs.GetInt(AmbientMuteKey, 0) == 1;

        preview = applied.Clone();
    }



    // 🔥 UI

    private void UpdateUI()
    {
        masterSlider.value = 1f - preview.master;
        bgmSlider.value = 1f - preview.bgm;
        sfxSlider.value = 1f - preview.sfx;
        ambientSlider.value = 1f - preview.ambient;

        masterMuteImage.sprite = preview.masterMuted ? muteOnSprite : muteOffSprite;
        bgmMuteImage.sprite = preview.bgmMuted ? muteOnSprite : muteOffSprite;
        sfxMuteImage.sprite = preview.sfxMuted ? muteOnSprite : muteOffSprite;
        ambientMuteImage.sprite = preview.ambientMuted ? muteOnSprite : muteOffSprite;
    }

    public void OnOpen()
    {
        preview = applied.Clone();
        UpdateUI();
    }
}