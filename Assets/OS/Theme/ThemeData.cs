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
    public Sprite windowCloseButtonNormalSprite;
    public Sprite windowCloseButtonHoverSprite;
    public Sprite windowCloseButtonPressedSprite;

    public Sprite windowMinimizeButtonNormalSprite;
    public Sprite windowMinimizeButtonHoverSprite;
    public Sprite windowMinimizeButtonPressedSprite;

    public Sprite windowPinButtonNormalSprite;
    public Sprite windowPinButtonHoverSprite;
    public Sprite windowPinButtonPressedSprite;
    public Sprite windowPinButtonOnSprite;

    [Header("Optional Fallback Tint")]
    public Color windowTint = Color.white;
}
