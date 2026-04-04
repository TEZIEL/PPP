using System;
using TMPro;
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
    private const string ThemeSelectionKey = "Options.ThemeSelection";

    [Serializable]
    public struct ThemeOptionEntry
    {
        public string displayName;
        public ThemeData osTheme;
        public AppUIThemeData appUIThemeData;
    }

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
    [Header("Theme Options")]
    [SerializeField] private TMP_Dropdown themeDropdown;
    [SerializeField] private ThemeOptionEntry[] themeOptions = Array.Empty<ThemeOptionEntry>();
    [SerializeField] private ThemeManager themeManager;
    [SerializeField] private AppUIThemeManager appUIThemeManager;
    [Header("Background Options")]
    [SerializeField] private TMP_Dropdown skyDropdown;
    [SerializeField] private TMP_Dropdown buildingDropdown;
    [SerializeField] private TMP_Dropdown highlightDropdown;
    [SerializeField] private BackgroundManager backgroundManager;

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
        ResolveThemeManagers();
        InitializeThemeDropdown();
        ResolveBackgroundManager();
        InitializeBackgroundDropdowns();
        ApplyThemeSelection(applied.themeOptionIndex);
        backgroundManager?.Apply();
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
        ApplyThemeSelection(applied.themeOptionIndex);
        backgroundManager?.Apply();
        Save();
    }

  

    public void Cancel()
    {
        ApplyThemeSelection(applied.themeOptionIndex);
        backgroundManager?.Cancel();
        ApplyToMixer(applied);
        preview = applied.Clone();
        SyncBackgroundDropdownsToPending();
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
        PlayerPrefs.SetInt(ThemeSelectionKey, applied.themeOptionIndex);

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
        applied.themeOptionIndex = PlayerPrefs.GetInt(ThemeSelectionKey, ResolveCurrentThemeOptionIndex());

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
        SyncThemeDropdownToState(preview.themeOptionIndex);
        backgroundManager?.OnOpen();
        SyncBackgroundDropdownsToPending();
        UpdateUI();
    }

    public void ApplyThemeVisuals(ThemeData theme)
    {
        if (theme == null)
            return;

        if (theme.optionsMuteOnSprite != null)
            muteOnSprite = theme.optionsMuteOnSprite;

        if (theme.optionsMuteOffSprite != null)
            muteOffSprite = theme.optionsMuteOffSprite;

        UpdateUI();
    }

    public void OnThemeDropdownChanged(int index)
    {
        if (!IsValidThemeOptionIndex(index))
            return;

        preview.themeOptionIndex = index;
        ApplyThemeSelection(preview.themeOptionIndex);
    }

    public void OnSkyDropdownChanged(int index)
    {
        backgroundManager?.SetSky(index);
    }

    public void OnBuildingDropdownChanged(int index)
    {
        backgroundManager?.SetBuilding(index);
    }

    public void OnHighlightDropdownChanged(int index)
    {
        backgroundManager?.SetHighlight(index);
    }

    private void InitializeThemeDropdown()
    {
        if (themeDropdown == null)
            return;

        themeDropdown.onValueChanged.RemoveListener(OnThemeDropdownChanged);

        if (themeOptions == null || themeOptions.Length == 0)
        {
            themeDropdown.ClearOptions();
            themeDropdown.onValueChanged.AddListener(OnThemeDropdownChanged);
            return;
        }

        var names = new System.Collections.Generic.List<string>(themeOptions.Length);
        for (int i = 0; i < themeOptions.Length; i++)
            names.Add(string.IsNullOrWhiteSpace(themeOptions[i].displayName) ? $"Theme {i + 1}" : themeOptions[i].displayName);

        themeDropdown.ClearOptions();
        themeDropdown.AddOptions(names);

        if (!IsValidThemeOptionIndex(applied.themeOptionIndex))
            applied.themeOptionIndex = 0;
        if (!IsValidThemeOptionIndex(preview.themeOptionIndex))
            preview.themeOptionIndex = applied.themeOptionIndex;

        SyncThemeDropdownToState(preview.themeOptionIndex);
        themeDropdown.onValueChanged.AddListener(OnThemeDropdownChanged);
    }

    private void SyncThemeDropdownToState(int index)
    {
        if (themeDropdown == null || !IsValidThemeOptionIndex(index))
            return;

        themeDropdown.SetValueWithoutNotify(index);
    }

    private void ResolveBackgroundManager()
    {
        if (backgroundManager == null)
            backgroundManager = BackgroundManager.Instance != null ? BackgroundManager.Instance : FindObjectOfType<BackgroundManager>(true);
    }

    private void InitializeBackgroundDropdowns()
    {
        ResolveBackgroundManager();
        InitializeBackgroundDropdown(skyDropdown, "Sky", OnSkyDropdownChanged, backgroundManager?.GetSkyOptions());
        InitializeBackgroundDropdown(buildingDropdown, "Building", OnBuildingDropdownChanged, backgroundManager?.GetBuildingOptions());
        InitializeBackgroundDropdown(highlightDropdown, "Highlight", OnHighlightDropdownChanged, backgroundManager?.GetHighlightOptions());
        SyncBackgroundDropdownsToPending();
    }

    private void InitializeBackgroundDropdown(TMP_Dropdown dropdown, string prefix, UnityEngine.Events.UnityAction<int> callback, Sprite[] options)
    {
        if (dropdown == null)
            return;

        dropdown.onValueChanged.RemoveListener(callback);

        if (backgroundManager != null)
            backgroundManager.InitializeDropdown(dropdown, prefix, options);
        else
            dropdown.ClearOptions();

        dropdown.onValueChanged.AddListener(callback);
    }

    private void SyncBackgroundDropdownsToPending()
    {
        if (backgroundManager == null)
            return;

        SetDropdownValueWithoutNotify(skyDropdown, backgroundManager.PendingSky);
        SetDropdownValueWithoutNotify(buildingDropdown, backgroundManager.PendingBuilding);
        SetDropdownValueWithoutNotify(highlightDropdown, backgroundManager.PendingHighlight);
    }

    private static void SetDropdownValueWithoutNotify(TMP_Dropdown dropdown, int index)
    {
        if (dropdown == null || dropdown.options == null || dropdown.options.Count == 0)
            return;

        int clamped = Mathf.Clamp(index, 0, dropdown.options.Count - 1);
        dropdown.SetValueWithoutNotify(clamped);
    }

    private void ResolveThemeManagers()
    {
        if (themeManager == null)
            themeManager = ThemeManager.Instance != null ? ThemeManager.Instance : FindObjectOfType<ThemeManager>(true);

        if (appUIThemeManager == null)
            appUIThemeManager = AppUIThemeManager.Instance != null ? AppUIThemeManager.Instance : FindObjectOfType<AppUIThemeManager>(true);
    }

    private int ResolveCurrentThemeOptionIndex()
    {
        ResolveThemeManagers();

        if (themeOptions == null || themeOptions.Length == 0)
            return -1;

        for (int i = 0; i < themeOptions.Length; i++)
        {
            var option = themeOptions[i];
            if (option.osTheme == null || option.appUIThemeData == null)
                continue;

            if (themeManager != null && appUIThemeManager != null &&
                themeManager.CurrentTheme == option.osTheme &&
                appUIThemeManager.CurrentTheme == option.appUIThemeData)
            {
                return i;
            }
        }

        for (int i = 0; i < themeOptions.Length; i++)
        {
            var option = themeOptions[i];
            if (themeManager != null && option.osTheme == themeManager.CurrentTheme)
                return i;
        }

        return 0;
    }

    private void ApplyThemeSelection(int index)
    {
        if (!IsValidThemeOptionIndex(index))
            return;

        ResolveThemeManagers();
        var option = themeOptions[index];

        if (themeManager != null && option.osTheme != null)
            themeManager.SetTheme(option.osTheme, true);

        if (appUIThemeManager != null && option.appUIThemeData != null)
            appUIThemeManager.SetTheme(option.appUIThemeData);
    }

    private bool IsValidThemeOptionIndex(int index)
    {
        return themeOptions != null && index >= 0 && index < themeOptions.Length;
    }
}
