[System.Serializable]
public class OptionState
{
    public float master = 1f;
    public float bgm = 1f;
    public float sfx = 1f;
    public float ambient = 1f;

    // 🔥 추가 (이게 핵심)
    public bool masterMuted;
    public bool bgmMuted;
    public bool sfxMuted;
    public bool ambientMuted;

    public OptionState Clone()
    {
        return new OptionState
        {
            master = this.master,
            bgm = this.bgm,
            sfx = this.sfx,
            ambient = this.ambient,

            // 🔥 이것도 반드시 포함
            masterMuted = this.masterMuted,
            bgmMuted = this.bgmMuted,
            sfxMuted = this.sfxMuted,
            ambientMuted = this.ambientMuted
        };
    }
}