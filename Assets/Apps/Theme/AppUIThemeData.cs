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
        [Header("Player / Frame")]
        public Sprite playerMainPanelSprite;
        public Sprite albumArtBackgroundSprite;
        public Sprite albumArtFrameSprite;
        public Sprite playerHeaderFrameSprite;
        public Sprite playerFooterFrameSprite;
        public Sprite dividerLineSprite;

        [Header("Progress")]
        public Sprite progressTrackSprite;
        public Sprite progressFillSprite;
        public Sprite progressHandleSprite;
        public Sprite progressHandleSprite2;

        [Header("Buttons")]
        public Sprite transportButtonSprite;
        public Sprite playPauseButtonSprite;
        public Sprite playbackToggleButtonSprite;
        public Sprite actionButtonSprite;
        public Sprite utilityButtonSprite;

        [Header("List / Scroll")]
        public Sprite listPanelSprite;
        public Sprite listViewportSprite;
        public Sprite listHeaderFrameSprite;
        public Sprite scrollbarTrackSprite;
        public Sprite scrollbarHandleSprite;

        [Header("Dropdown / Ambient")]
        [Header("Player/List")]
        public Sprite playerPanelSprite;
        public Sprite listBackgroundSprite;

        public Sprite extraSprite1;
        public Sprite extraSprite2;
        public Sprite extraSprite3;
        public Sprite extraSprite4;
        public Sprite extraSprite5;
        public Sprite extraSprite6;
        public Sprite extraSprite7;
        public Sprite extraSprite8;

        public Sprite extraSprite9;
        public Sprite extraSprite10;
        public Sprite extraSprite11;
        public Sprite extraSprite12;
        public Sprite extraSprite13;
        public Sprite extraSprite14;
        public Sprite extraSprite15;
        public Sprite extraSprite16;
        public Sprite extraSprite17;
        public Sprite extraSprite18;
        public Sprite extraSprite19;
        public Sprite extraSprite20;

        public Sprite toggleOnBackground1;
        public Sprite toggleOffBackground1;
        public Sprite toggleOnIcon1;
        public Sprite toggleOffIcon1;

        public Sprite toggleOnBackground2;
        public Sprite toggleOffBackground2;
        public Sprite toggleOnIcon2;
        public Sprite toggleOffIcon2;

        public Sprite toggleOnBackground3;
        public Sprite toggleOffBackground3;
        public Sprite toggleOnIcon3;
        public Sprite toggleOffIcon3;

        [Header("Dropdown/Ambient")]
        public Sprite dropdownBackgroundSprite;
        public Sprite dropdownButtonSprite;
        public Sprite ambientSlotSprite;

        public Sprite dropdownItemNormalSprite;
        public Sprite dropdownItemSelectedSprite;
        public Sprite dropdownItemPressedSprite;
        public Sprite dropdownItemDisabledSprite;

        public Sprite dropdownCheckmarkNormalSprite;
        public Sprite dropdownCheckmarkSelectedSprite;
        public Sprite dropdownCheckmarkPressedSprite;
        public Sprite dropdownCheckmarkDisabledSprite;

        public Sprite ambientPlayIcon;
        public Sprite ambientStopIcon;

        [Header("Track Item")]
        public Sprite trackItemNormalSprite;
        public Sprite trackItemSelectedSprite;
        public Sprite trackItemPressedSprite;
        public Sprite trackItemDisabledSprite;

        public Sprite trackCheckmarkNormalSprite;
        public Sprite trackCheckmarkSelectedSprite;
        public Sprite trackCheckmarkPressedSprite;
        public Sprite trackCheckmarkDisabledSprite;

        public Color trackTextNormalColor;
        public Color trackTextSelectedColor;
        public Color trackTextPressedColor;
        public Color trackTextDisabledColor;

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

    [System.Serializable]
    public struct VNTheme
    {
        [Header("Save/Load")]
        public Sprite saveLoadWindowRootSprite;
        public Sprite saveLoadSlotSprite;
        public Sprite saveLoadSelectedSlotSprite;
        public Sprite saveLoadButtonSprite;

        [Header("Backlog")]
        public Sprite backlogRootSprite;
        public Sprite backlogItemSprite;
        public Sprite backlogButtonSprite;

        [Header("Exit Modal")]
        public Sprite exitModalRootSprite;
        public Sprite exitModalButtonSprite;

        [Header("Drink")]
        public Sprite drinkRootSprite;
        public Sprite drinkGridSprite;
        public Sprite ingredientButtonSprite;
        public Sprite ingredientSelectedButtonSprite;

        [Header("Dialogue")]
        public Sprite dialogueRootSprite;
        public Sprite dialogueButtonContainerSprite;
        public Sprite dialogueButtonSprite;
        public Sprite dialogueSelectedButtonSprite;

        [Header("Other Windows")]
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
        public Sprite otherWindow16RootSprite;
        public Sprite otherWindow17RootSprite;
        public Sprite otherWindow18RootSprite;
        public Sprite otherWindow19RootSprite;
        public Sprite otherWindow20RootSprite;
        public Sprite otherWindow21RootSprite;
        public Sprite otherWindow22RootSprite;
        public Sprite otherWindow23RootSprite;
        public Sprite otherWindow24RootSprite;
        public Sprite otherWindow25RootSprite;
        public Sprite otherWindow26RootSprite;
        public Sprite otherWindow27RootSprite;
        public Sprite otherWindow28RootSprite;
        public Sprite otherWindow29RootSprite;
        public Sprite otherWindow30RootSprite;
        public Sprite otherWindow31RootSprite;
        public Sprite otherWindow32RootSprite;
        public Sprite otherWindow33RootSprite;
        public Sprite otherWindow34RootSprite;
        public Sprite otherWindow35RootSprite;
        public Sprite otherWindow36RootSprite;
        public Sprite otherWindow37RootSprite;
        public Sprite otherWindow38RootSprite;
        public Sprite otherWindow39RootSprite;
        public Sprite otherWindow40RootSprite;
        public Sprite otherWindow41RootSprite;
        public Sprite otherWindow42RootSprite;
        public Sprite otherWindow43RootSprite;
        public Sprite otherWindow44RootSprite;
        public Sprite otherWindow45RootSprite;
        public Sprite otherWindow46RootSprite;
        public Sprite otherWindow47RootSprite;
        public Sprite otherWindow48RootSprite;
        public Sprite otherWindow49RootSprite;
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

    [Header("VN")]
    public VNTheme vn;
}
