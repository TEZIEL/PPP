using UnityEngine;

[CreateAssetMenu(menuName = "Apps/UI Theme Data", fileName = "AppUIThemeData")]
public class AppUIThemeData : ScriptableObject
{
    [System.Serializable]
    public struct BlueprintTheme
    {
        [Header("Top Filter")]
        public Sprite filterButtonSprite;
        public Sprite filterButtonSelectedSprite;

        [Header("Scroll/List")]
        public Sprite scrollViewBackgroundSprite;
        public Sprite scrollUpButtonSprite;
        public Sprite scrollDownButtonSprite;
        public Sprite scrollbarBackgroundSprite;
        public Sprite scrollbarHandleSprite;

        [Header("Panels")]
        public Sprite mainPanelBackgroundSprite;
        public Sprite detailPanelBackgroundSprite;

        [Header("Text (Optional)")]
        public Color primaryTextColor;
        public Color secondaryTextColor;
    }

    [System.Serializable]
    public struct BlueprintListItemTheme
    {
        [Header("Item Sprite")]
        public Sprite itemBackgroundSprite;
        public Sprite iconFrameSprite;
        public Sprite actionButtonSprite;

        [Header("Item Text (Optional)")]
        public Color titleTextColor;
        public Color bodyTextColor;
        public Color metaTextColor;
    }

    [System.Serializable]
    public struct BridgeTheme
    {
        [Header("Player/List")]
        public Sprite playerPanelSprite;
        public Sprite listBackgroundSprite;
        public Sprite listViewportSprite;

        [Header("Controls")]
        public Sprite playbackControlButtonSprite;
        public Sprite actionButtonSprite;

        [Header("Progress/Scroll")]
        public Sprite progressFillSprite;
        public Sprite progressHandleSprite;
        public Sprite scrollbarTrackSprite;
        public Sprite scrollbarHandleSprite;

        [Header("Dropdown/Ambient")]
        public Sprite dropdownBackgroundSprite;
        public Sprite dropdownButtonSprite;
        public Sprite ambientSlotSprite;

        [Header("Text (Optional)")]
        public Color primaryTextColor;
        public Color secondaryTextColor;
    }

    [System.Serializable]
    public struct BuddyTheme
    {
        [Header("Panels")]
        public Sprite pinAreaSprite;
        public Sprite profileCardSprite;
        public Sprite listAreaSprite;
        public Sprite bottomActionPanelSprite;
        public Sprite innerPanelSprite;

        [Header("Buttons")]
        public Sprite actionButtonSprite;

        [Header("Text (Optional)")]
        public Color primaryTextColor;
    }

    [System.Serializable]
    public struct BanditTheme
    {
        [Header("Main")]
        public Sprite swipeViewportSprite;
        public Sprite infoPanelSprite;
        public Sprite frameSprite;
        public Sprite innerBackgroundSprite;

        [Header("Buttons")]
        public Sprite actionButtonSprite;

        [Header("Text (Optional)")]
        public Color textColor;
    }

    [System.Serializable]
    public struct BoxTheme
    {
        [Header("Panels")]
        public Sprite topToolbarSprite;
        public Sprite bodyPanelSprite;
        public Sprite scrollAreaSprite;
        public Sprite innerBackgroundSprite;

        [Header("Scroll")]
        public Sprite scrollbarTrackSprite;
        public Sprite scrollbarHandleSprite;
        public Sprite scrollUpButtonSprite;
        public Sprite scrollDownButtonSprite;

        [Header("Text (Optional)")]
        public Color textColor;
    }

    [Header("BluePrint")]
    public BlueprintTheme blueprint;

    [Header("BluePrint List Item")]
    public BlueprintListItemTheme blueprintListItem;

    [Header("BRIDGE")]
    public BridgeTheme bridge;

    [Header("BUDDY")]
    public BuddyTheme buddy;

    [Header("BANDIT")]
    public BanditTheme bandit;

    [Header("BOX")]
    public BoxTheme box;
}
