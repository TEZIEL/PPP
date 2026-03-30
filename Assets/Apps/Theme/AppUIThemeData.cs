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
        public Sprite mainPanel2BackgroundSprite;

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
        public Sprite iconFrame2Sprite;
        public Sprite iconFrame3Sprite;
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
        public Sprite swipeViewportSprite2;
        public Sprite infoPanelSprite2;
        public Sprite frameSprite2;
        public Sprite swipeViewportSprite3;
        public Sprite infoPanelSprite3;
        public Sprite frameSprite3;
        public Sprite swipeViewportSprite4;
        public Sprite infoPanelSprite4;
        public Sprite frameSprite4;
        public Sprite frameSprite5;
        public Sprite swipeViewportSprite5;
        public Sprite infoPanelSprite5;
        public Sprite frameSprite6;
        public Sprite swipeViewportSprite6;
        public Sprite pinOffSprite;
        public Sprite pinOnSprite;

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
