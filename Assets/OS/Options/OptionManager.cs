using System;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

[DefaultExecutionOrder(-10000)]
public class OptionManager : MonoBehaviour
{
    public const string AudioOptionsTracePrefix = "[AudioOptionsTrace]";

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
    [SerializeField] private WindowManager windowManager;

    [SerializeField] private AudioMixer mixer;

    public static OptionManager Instance { get; private set; }

    private static bool hasAppliedStartupAudioSettings;

    private OptionState applied = new OptionState();
    private OptionState preview = new OptionState();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetStartupAudioSettingsAppliedFlag()
    {
        hasAppliedStartupAudioSettings = false;
        TraceAudioOptions("ResetStartupAudioSettingsAppliedFlag before scene load");
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        TraceAudioOptions($"OptionManager.Awake begin mixer={DescribeMixer(mixer)}");
        Load();
        ApplyAllVolumeSettings();
        ResolveThemeManagers();
        InitializeThemeDropdown();
        ResolveBackgroundManager();
        InitializeBackgroundDropdowns();
        ApplyThemeSelection(applied.themeOptionIndex);
        backgroundManager?.Apply();
        LoadGlobalCustomization();
        UpdateUI(); // 🔥 초기 UI
        TraceAudioOptions("OptionManager.Awake end");
    }

    private void Start()
    {
        // AudioSource/BGM/SFX managers can finish their Awake initialization after this component.
        // Re-apply saved runtime volume once at Start so restored PlayerPrefs do not remain UI-only.
        TraceAudioOptions($"OptionManager.Start reapply mixer={DescribeMixer(mixer)}");
        ApplyAllVolumeSettings();
    }

    // 🔥 MUTE TOGGLE

    public void ToggleMasterMute()
    {
        preview.masterMuted = !preview.masterMuted;
        TraceAudioOptions($"ToggleMasterMute preview={DescribeState(preview)}");
        PersistPreviewAudioSettings();
        UpdateUI();
    }

    public void ToggleBgmMute()
    {
        preview.bgmMuted = !preview.bgmMuted;
        TraceAudioOptions($"ToggleBgmMute preview={DescribeState(preview)}");
        PersistPreviewAudioSettings();
        UpdateUI();
    }

    public void ToggleSfxMute()
    {
        preview.sfxMuted = !preview.sfxMuted;
        TraceAudioOptions($"ToggleSfxMute preview={DescribeState(preview)}");
        PersistPreviewAudioSettings();
        UpdateUI();
    }

    public void ToggleAmbientMute()
    {
        preview.ambientMuted = !preview.ambientMuted;
        TraceAudioOptions($"ToggleAmbientMute preview={DescribeState(preview)}");
        PersistPreviewAudioSettings();
        UpdateUI();
    }

    // 🔥 SLIDER

    public void SetPreviewMaster(float v)
    {
        float sliderValue = v;
        float volume = SliderValueToVolume(v);

        preview.master = ClampVolume(volume);
        preview.masterMuted = false;

        TraceAudioOptions($"SetPreviewMaster slider={sliderValue} preview={DescribeState(preview)}");
        PersistPreviewAudioSettings();
        UpdateUI();
    }

    public void SetPreviewBgm(float v)
    {
        float sliderValue = v;
        float volume = SliderValueToVolume(v);

        preview.bgm = ClampVolume(volume);
        preview.bgmMuted = false;

        TraceAudioOptions($"SetPreviewBgm slider={sliderValue} preview={DescribeState(preview)}");
        PersistPreviewAudioSettings();
        UpdateUI();
    }

    public void SetPreviewSfx(float v)
    {
        float sliderValue = v;
        float volume = SliderValueToVolume(v);

        preview.sfx = ClampVolume(volume);
        preview.sfxMuted = false;

        TraceAudioOptions($"SetPreviewSfx slider={sliderValue} preview={DescribeState(preview)}");
        PersistPreviewAudioSettings();
        UpdateUI();
    }

    public void SetPreviewAmbient(float v)
    {
        float sliderValue = v;
        float volume = SliderValueToVolume(v);

        preview.ambient = ClampVolume(volume);
        preview.ambientMuted = false;

        TraceAudioOptions($"SetPreviewAmbient slider={sliderValue} preview={DescribeState(preview)}");
        PersistPreviewAudioSettings();
        UpdateUI();
    }

    // 🔥 APPLY / CANCEL

    public void Apply()
    {
        applied = preview.Clone();
        TraceAudioOptions($"Apply clicked applied={DescribeState(applied)}");
        ApplyThemeSelection(applied.themeOptionIndex);
        backgroundManager?.Apply();
        Save();
        SaveGlobalCustomization();
    }

  

    public void Cancel()
    {
        TraceAudioOptions($"Cancel clicked applied={DescribeState(applied)} previewBefore={DescribeState(preview)}");
        ApplyThemeSelection(applied.themeOptionIndex);
        backgroundManager?.Cancel();
        ApplyToMixer(applied);
        preview = applied.Clone();
        SyncBackgroundDropdownsToPending();
        UpdateUI();
    }

    private void PersistPreviewAudioSettings()
    {
        CopyAudioSettings(preview, applied);
        ApplyToMixer(applied);
        SaveAudioSettingsOnly();
    }

    private static void CopyAudioSettings(OptionState source, OptionState target)
    {
        if (source == null || target == null)
            return;

        target.master = source.master;
        target.bgm = source.bgm;
        target.sfx = source.sfx;
        target.ambient = source.ambient;
        target.masterMuted = source.masterMuted;
        target.bgmMuted = source.bgmMuted;
        target.sfxMuted = source.sfxMuted;
        target.ambientMuted = source.ambientMuted;
    }

    // 🔥 MIXER

    public void ReapplyAudioSettings()
    {
        TraceAudioOptions("ReapplyAudioSettings called");
        ApplyAllVolumeSettings();
    }

    public static void EnsureSavedAudioSettingsApplied(AudioMixer targetMixer)
    {
        TraceAudioOptions($"EnsureSavedAudioSettingsApplied target={DescribeMixer(targetMixer)} alreadyApplied={hasAppliedStartupAudioSettings}");
        ApplySavedAudioSettingsToMixer(targetMixer);
    }

    public static void ApplySavedAudioSettingsToMixer(AudioMixer targetMixer)
    {
        OptionState savedState = LoadSavedAudioState();
        TraceAudioOptions($"ApplySavedAudioSettingsToMixer target={DescribeMixer(targetMixer)} saved={DescribeState(savedState)}");
        ApplyStateToMixer(targetMixer, savedState, "ApplySavedAudioSettingsToMixer");
    }

    private void ApplyAllVolumeSettings()
    {
        TraceAudioOptions($"ApplyAllVolumeSettings applied={DescribeState(applied)} mixer={DescribeMixer(mixer)}");
        ApplyToMixer(applied);
    }

    private void ApplyToMixer(OptionState state)
    {
        TraceAudioOptions($"ApplyToMixer mixer={DescribeMixer(mixer)} state={DescribeState(state)}");
        ApplyStateToMixer(mixer, state, "ApplyToMixer");
    }

    private static void ApplyStateToMixer(AudioMixer targetMixer, OptionState state, string context)
    {
        if (targetMixer == null || state == null)
        {
            TraceAudioOptions($"{context} skipped target={DescribeMixer(targetMixer)} stateNull={state == null}");
            return;
        }

        float master = state.masterMuted ? MinVolume : state.master;
        float bgm = state.bgmMuted ? MinVolume : state.bgm;
        float sfx = state.sfxMuted ? MinVolume : state.sfx;
        float ambient = state.ambientMuted ? MinVolume : state.ambient;

        float masterDb = LinearToDb(master);
        float bgmDb = LinearToDb(bgm);
        float sfxDb = LinearToDb(sfx);
        float ambientDb = LinearToDb(ambient);

        bool masterOk = targetMixer.SetFloat("MasterVolume", masterDb);
        bool bgmOk = targetMixer.SetFloat("BGMVolume", bgmDb);
        bool sfxOk = targetMixer.SetFloat("SFXVolume", sfxDb);
        bool ambientOk = targetMixer.SetFloat("AmbientVolume", ambientDb);

        TraceAudioOptions($"{context} SetFloat MasterVolume={masterDb} ok={masterOk} mixer={DescribeMixer(targetMixer)}");
        TraceAudioOptions($"{context} SetFloat BGMVolume={bgmDb} ok={bgmOk} mixer={DescribeMixer(targetMixer)}");
        TraceAudioOptions($"{context} SetFloat SFXVolume={sfxDb} ok={sfxOk} mixer={DescribeMixer(targetMixer)}");
        TraceAudioOptions($"{context} SetFloat AmbientVolume={ambientDb} ok={ambientOk} mixer={DescribeMixer(targetMixer)}");
        hasAppliedStartupAudioSettings = true;
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
        TraceAudioOptions($"Save PlayerPrefs state={DescribeState(applied)}");
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

    private void SaveAudioSettingsOnly()
    {
        TraceAudioOptions($"SaveAudioSettingsOnly PlayerPrefs audio={DescribeState(applied)}");
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
        applied = LoadSavedAudioState();
        applied.themeOptionIndex = PlayerPrefs.GetInt(ThemeSelectionKey, ResolveCurrentThemeOptionIndex());

        preview = applied.Clone();
        TraceAudioOptions($"Load applied={DescribeState(applied)} preview={DescribeState(preview)}");
    }

    private static OptionState LoadSavedAudioState()
    {
        OptionState state = new OptionState
        {
            master = PlayerPrefs.GetFloat(MasterKey, 1f),
            bgm = PlayerPrefs.GetFloat(BgmKey, 1f),
            sfx = PlayerPrefs.GetFloat(SfxKey, 1f),
            ambient = PlayerPrefs.GetFloat(AmbientKey, 1f),
            masterMuted = PlayerPrefs.GetInt(MasterMuteKey, 0) == 1,
            bgmMuted = PlayerPrefs.GetInt(BgmMuteKey, 0) == 1,
            sfxMuted = PlayerPrefs.GetInt(SfxMuteKey, 0) == 1,
            ambientMuted = PlayerPrefs.GetInt(AmbientMuteKey, 0) == 1
        };

        TraceAudioOptions(
            $"PlayerPrefs {MasterKey}={PlayerPrefs.GetFloat(MasterKey, -999f)} " +
            $"{BgmKey}={PlayerPrefs.GetFloat(BgmKey, -999f)} " +
            $"{SfxKey}={PlayerPrefs.GetFloat(SfxKey, -999f)} " +
            $"{AmbientKey}={PlayerPrefs.GetFloat(AmbientKey, -999f)} " +
            $"muteMaster={PlayerPrefs.GetInt(MasterMuteKey, -999)} " +
            $"muteBgm={PlayerPrefs.GetInt(BgmMuteKey, -999)} " +
            $"muteSfx={PlayerPrefs.GetInt(SfxMuteKey, -999)} " +
            $"muteAmbient={PlayerPrefs.GetInt(AmbientMuteKey, -999)}");

        return state;
    }



    // 🔥 UI

    private static float VolumeToSliderValue(float volume)
    {
        return 1f - ClampVolume(volume);
    }

    private static float SliderValueToVolume(float sliderValue)
    {
        return 1f - Mathf.Clamp01(sliderValue);
    }

    private void UpdateUI()
    {
        TraceAudioOptions($"UpdateUI begin applied={DescribeState(applied)} preview={DescribeState(preview)}");
        SetSliderValueWithoutNotify(masterSlider, VolumeToSliderValue(preview.master));
        SetSliderValueWithoutNotify(bgmSlider, VolumeToSliderValue(preview.bgm));
        SetSliderValueWithoutNotify(sfxSlider, VolumeToSliderValue(preview.sfx));
        SetSliderValueWithoutNotify(ambientSlider, VolumeToSliderValue(preview.ambient));

        masterMuteImage.sprite = preview.masterMuted ? muteOnSprite : muteOffSprite;
        bgmMuteImage.sprite = preview.bgmMuted ? muteOnSprite : muteOffSprite;
        sfxMuteImage.sprite = preview.sfxMuted ? muteOnSprite : muteOffSprite;
        ambientMuteImage.sprite = preview.ambientMuted ? muteOnSprite : muteOffSprite;
        TraceAudioOptions(
            $"UpdateUI end sliders master={DescribeSlider(masterSlider)} " +
            $"bgm={DescribeSlider(bgmSlider)} sfx={DescribeSlider(sfxSlider)} ambient={DescribeSlider(ambientSlider)}");
    }

    private static void SetSliderValueWithoutNotify(Slider slider, float value)
    {
        if (slider == null)
        {
            TraceAudioOptions($"SetSliderValueWithoutNotify skipped null slider value={value}");
            return;
        }

        TraceAudioOptions($"SetSliderValueWithoutNotify slider={slider.name} value={value} before={slider.value}");
        slider.SetValueWithoutNotify(value);
    }

    public void OnOpen()
    {
        TraceAudioOptions($"OnOpen begin applied={DescribeState(applied)} previewBefore={DescribeState(preview)} mixer={DescribeMixer(mixer)}");
        preview = applied.Clone();
        SyncThemeDropdownToState(preview.themeOptionIndex);
        backgroundManager?.OnOpen();
        SyncBackgroundDropdownsToPending();
        UpdateUI();
        TraceAudioOptions($"OnOpen end preview={DescribeState(preview)}");
    }

    public static void TraceAudioOptions(string message)
    {
        Debug.Log($"{AudioOptionsTracePrefix} {message}");
    }

    private static string DescribeState(OptionState state)
    {
        if (state == null)
            return "NULL";

        return $"master={state.master} bgm={state.bgm} sfx={state.sfx} ambient={state.ambient} " +
               $"muteMaster={state.masterMuted} muteBgm={state.bgmMuted} muteSfx={state.sfxMuted} muteAmbient={state.ambientMuted} " +
               $"theme={state.themeOptionIndex}";
    }

    private static string DescribeMixer(AudioMixer targetMixer)
    {
        return targetMixer != null ? targetMixer.name : "NULL";
    }

    private static string DescribeSlider(Slider slider)
    {
        return slider != null ? $"{slider.name}:{slider.value}" : "NULL";
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

    private void ResolveWindowManager()
    {
        if (windowManager == null)
            windowManager = FindObjectOfType<WindowManager>(true);
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


    public CustomizationSaveData CaptureCustomizationSaveData()
    {
        GetAppliedBackgroundSelection(out int sky, out int building, out int highlight);
        int themeIndex = GetAppliedThemeOptionIndex();

        return new CustomizationSaveData
        {
            version = 1,
            uiThemeOptionIndex = themeIndex,
            appUIThemeOptionIndex = themeIndex,
            backgroundSkyIndex = sky,
            backgroundBuildingIndex = building,
            backgroundHighlightIndex = highlight
        };
    }

    public void ApplyCustomizationSaveData(CustomizationSaveData data)
    {
        if (data == null)
            data = CustomizationSaveSystem.CreateDefault();

        ApplyCustomizationState(
            data.uiThemeOptionIndex,
            data.backgroundSkyIndex,
            data.backgroundBuildingIndex,
            data.backgroundHighlightIndex);
    }

    public void LoadGlobalCustomization()
    {
        var data = CustomizationSaveSystem.Load();
        ApplyCustomizationSaveData(data);
    }

    public void SaveGlobalCustomization()
    {
        CustomizationSaveSystem.Save(CaptureCustomizationSaveData());
    }


    public int GetAppliedThemeOptionIndex()
    {
        if (!IsValidThemeOptionIndex(applied.themeOptionIndex))
            return 0;

        return applied.themeOptionIndex;
    }

    public void GetAppliedBackgroundSelection(out int skyIndex, out int buildingIndex, out int highlightIndex)
    {
        if (backgroundManager == null)
            ResolveBackgroundManager();

        skyIndex = backgroundManager != null ? backgroundManager.CurrentSky : 0;
        buildingIndex = backgroundManager != null ? backgroundManager.CurrentBuilding : 0;
        highlightIndex = backgroundManager != null ? backgroundManager.CurrentHighlight : 0;
    }

    public void ApplyCustomizationState(int themeOptionIndex, int skyIndex, int buildingIndex, int highlightIndex)
    {
        ResolveBackgroundManager();

        int resolvedThemeIndex = IsValidThemeOptionIndex(themeOptionIndex) ? themeOptionIndex : 0;
        applied.themeOptionIndex = resolvedThemeIndex;
        preview.themeOptionIndex = resolvedThemeIndex;

        ApplyThemeSelection(resolvedThemeIndex);
        SyncThemeDropdownToState(resolvedThemeIndex);

        if (backgroundManager != null)
            backgroundManager.SetAppliedState(skyIndex, buildingIndex, highlightIndex);

        SyncBackgroundDropdownsToPending();
        UpdateUI();
    }

    public void ResetCustomizationToDefault()
    {
        ApplyCustomizationState(0, 0, 0, 0);
        Save();
        SaveGlobalCustomization();
    }
}
