using UnityEngine;

[CreateAssetMenu(menuName = "OS/Theme Data", fileName = "ThemeData")]
public class ThemeData : ScriptableObject
{
    [System.Serializable]
    public struct DesktopLauncherIconEntry
    {
        public string appId;
        public Sprite iconSprite;
    }

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

    public Sprite otherWindow1RootSprite;
    public Sprite otherWindow2RootSprite;
    public Sprite otherWindow3RootSprite;
    public Sprite otherWindow4RootSprite;
    public Sprite otherWindow5RootSprite;
    public Sprite otherWindow6RootSprite;
    public Sprite otherWindow7RootSprite;
    public Sprite otherWindow8RootSprite;
    public Sprite otherWindow9RootSprite;
    public Sprite otherWindow10RootSprite;
    public Sprite otherWindow11RootSprite;
    public Sprite otherWindow12RootSprite;
    public Sprite otherWindow13RootSprite;
    public Sprite otherWindow14RootSprite;
    public Sprite otherWindow15RootSprite;

    public Sprite windowMinimizeButtonNormalSprite;
    public Sprite windowMinimizeButtonHoverSprite;
    public Sprite windowMinimizeButtonPressedSprite;

    public Sprite windowPinButtonNormalSprite;
    public Sprite windowPinButtonHoverSprite;
    public Sprite windowPinButtonPressedSprite;
    public Sprite windowPinButtonOnSprite;

    [Header("Desktop Launcher")]
    public DesktopLauncherIconEntry[] desktopLauncherIcons;

    [Header("Desktop Context Menu Tint")]
    public Color desktopContextMenuButtonNormalTint = Color.white;
    public Color desktopContextMenuButtonHighlightedTint = new Color32(128, 128, 184, 255);
    public Color desktopContextMenuButtonSelectedTint = new Color32(128, 128, 184, 255);
    public Color desktopContextMenuButtonPressedTint = new Color32(180, 180, 180, 255);
    public Color desktopContextMenuButtonDisabledTint = new Color32(180, 180, 180, 128);

    [Header("Desktop Launcher Tint")]
    public Color desktopLauncherIconNormalTint = new Color32(0, 0, 0, 255);
    public Color desktopLauncherIconSelectedTint = new Color32(128, 128, 184, 255);

    [Header("Options Modal Sprites")]
    public Sprite optionsWindowRootSprite;
    public Sprite optionsTabSprite;
    public Sprite optionsSelectedTabSprite;
    public Sprite optionsButtonSprite;
    public Sprite optionsSelectedButtonSprite;
    public Sprite optionsDropdownSprite;
    public Sprite optionsSliderBackgroundSprite;
    public Sprite optionsSliderFillSprite;
    public Sprite optionsSliderHandleSprite;
    public Sprite optionsMuteOnSprite;
    public Sprite optionsMuteOffSprite;
    public Sprite optionsEtcField1Sprite;

    [Header("Options Modal Tint")]
    public Color optionsSelectedTint = new Color32(128, 128, 184, 255);
    public Color optionsPressedTint = new Color32(180, 180, 180, 255);

    [Header("Optional Fallback Tint")]
    public Color windowTint = Color.white;
}
