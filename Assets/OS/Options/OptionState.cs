[System.Serializable]
public class OptionState
{
    public float master = 1f;
    public float bgm = 1f;
    public float sfx = 1f;
    public float ambient = 1f;

    public OptionState Clone()
    {
        return new OptionState
        {
            master = master,
            bgm = bgm,
            sfx = sfx,
            ambient = ambient
        };
    }
}
