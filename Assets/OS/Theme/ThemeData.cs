using UnityEngine;

[CreateAssetMenu(menuName = "OS/Theme Data", fileName = "ThemeData")]
public class ThemeData : ScriptableObject
{
    [Header("Taskbar Sprites")]
    public Sprite taskbarBackgroundSprite;
    public Sprite taskbarPanelSprite;
    public Sprite startButtonSprite;
    public Sprite clockPanelSprite;
    public Sprite taskbarSeparatorSprite;
    public Sprite taskbarButtonNormalSprite;
    public Sprite taskbarButtonPressedSprite;

    [Header("Window Sprites")]
    public Sprite windowFrameSprite;
    public Sprite windowTitleBarSprite;
    public Sprite windowTitleBarActiveSprite;
    public Sprite windowTitleBarInactiveSprite;
    public Sprite windowBottomBarSprite;
    public Sprite windowBottomBarActiveSprite;
    public Sprite windowBottomBarInactiveSprite;
    public Sprite windowSideSprite;

    [Header("Optional Fallback Tint")]
    public Color windowTint = Color.white;
}
