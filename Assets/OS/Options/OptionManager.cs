using UnityEngine;
using UnityEngine.Audio;

public class OptionManager : MonoBehaviour
{
    private const float MinVolume = 0.0001f;
    private const string MasterKey = "Options.Master";
    private const string BgmKey = "Options.BGM";
    private const string SfxKey = "Options.SFX";
    private const string AmbientKey = "Options.Ambient";

    public static OptionManager Instance { get; private set; }

    [SerializeField] private AudioMixer mixer;

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
    }

    public void SetPreviewMaster(float v)
    {
        preview.master = ClampVolume(v);
        ApplyToMixer(preview);
    }

    public void SetPreviewBgm(float v)
    {
        preview.bgm = ClampVolume(v);
        ApplyToMixer(preview);
    }

    public void SetPreviewSfx(float v)
    {
        preview.sfx = ClampVolume(v);
        ApplyToMixer(preview);
    }

    public void SetPreviewAmbient(float v)
    {
        preview.ambient = ClampVolume(v);
        ApplyToMixer(preview);
    }

    public void Apply()
    {
        applied = preview.Clone();
        Save();
    }

    public void Cancel()
    {
        ApplyToMixer(applied);
        preview = applied.Clone();
    }

    private void ApplyToMixer(OptionState state)
    {
        if (mixer == null)
            return;

        mixer.SetFloat("MasterVolume", LinearToDb(state.master));
        mixer.SetFloat("BGMVolume", LinearToDb(state.bgm));
        mixer.SetFloat("SFXVolume", LinearToDb(state.sfx));
        mixer.SetFloat("AmbientVolume", LinearToDb(state.ambient));
    }

    private static float ClampVolume(float value)
    {
        return Mathf.Clamp(value, MinVolume, 1f);
    }

    private static float LinearToDb(float value)
    {
        return Mathf.Log10(ClampVolume(value)) * 20f;
    }

    private void Save()
    {
        PlayerPrefs.SetFloat(MasterKey, applied.master);
        PlayerPrefs.SetFloat(BgmKey, applied.bgm);
        PlayerPrefs.SetFloat(SfxKey, applied.sfx);
        PlayerPrefs.SetFloat(AmbientKey, applied.ambient);
        PlayerPrefs.Save();
    }

    private void Load()
    {
        applied.master = PlayerPrefs.GetFloat(MasterKey, 1f);
        applied.bgm = PlayerPrefs.GetFloat(BgmKey, 1f);
        applied.sfx = PlayerPrefs.GetFloat(SfxKey, 1f);
        applied.ambient = PlayerPrefs.GetFloat(AmbientKey, 1f);

        applied.master = ClampVolume(applied.master);
        applied.bgm = ClampVolume(applied.bgm);
        applied.sfx = ClampVolume(applied.sfx);
        applied.ambient = ClampVolume(applied.ambient);

        preview = applied.Clone();
    }
}
