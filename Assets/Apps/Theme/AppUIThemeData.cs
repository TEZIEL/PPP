using UnityEngine;

[CreateAssetMenu(menuName = "Apps/UI Theme Data", fileName = "AppUIThemeData")]
public class AppUIThemeData : ScriptableObject
{
    [System.Serializable]
    public struct ColorSlot
    {
        public Color background;
        public Color panel;
        public Color button;
        public Color buttonText;
        public Color accent;
        public Color bodyText;
        public Color mutedText;
    }

    [System.Serializable]
    public struct SpriteSlot
    {
        public Sprite background;
        public Sprite panel;
        public Sprite button;
        public Sprite accent;
    }

    [System.Serializable]
    public struct AppTheme
    {
        public ColorSlot colors;
        public SpriteSlot sprites;
    }

    [Header("BRIDGE")]
    public AppTheme bridge;

    [Header("BOX")]
    public AppTheme box;

    [Header("BluePrint")]
    public AppTheme blueprint;

    [Header("BUDDY")]
    public AppTheme buddy;

    [Header("BANDIT")]
    public AppTheme bandit;
}
