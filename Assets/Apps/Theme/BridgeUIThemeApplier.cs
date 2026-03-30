using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BridgeUIThemeApplier : AppUIThemeApplierBase
{
    [Header("BRIDGE - Player / Frame Images")]
    [SerializeField] private Image playerMainPanelImage;
    [SerializeField] private Image albumArtBackgroundImage;
    [SerializeField] private Image albumArtFrameImage;
    [SerializeField] private Image playerHeaderFrameImage;
    [SerializeField] private Image playerFooterFrameImage;
    [SerializeField] private Image dividerLineImage;
    [SerializeField] private Image[] extraFrameImages = Array.Empty<Image>();

    [SerializeField] private Image extraImage1;
    [SerializeField] private Image extraImage2;
    [SerializeField] private Image extraImage3;
    [SerializeField] private Image extraImage4;
    [SerializeField] private Image extraImage5;
    [SerializeField] private Image extraImage6;
    [SerializeField] private Image extraImage7;
    [SerializeField] private Image extraImage8;
    [SerializeField] private Image extraImage9;
    [SerializeField] private Image extraImage10;
    [SerializeField] private Image extraImage11;
    [SerializeField] private Image extraImage12;
    [SerializeField] private Image extraImage13;
    [SerializeField] private Image extraImage14;
    [SerializeField] private Image extraImage15;
    [SerializeField] private Image extraImage16;

    [Header("BRIDGE - Progress Images")]
    [SerializeField] private Image progressTrackImage;
    [SerializeField] private Image progressFillImage;
    [SerializeField] private Image progressHandleImage;
    [SerializeField] private Image progressHandleImage2;

    [Header("BRIDGE - Button Groups")]
    [SerializeField] private Button[] transportButtons = Array.Empty<Button>();
    [SerializeField] private Button[] playPauseButtons = Array.Empty<Button>();
    [SerializeField] private Button[] playbackToggleButtons = Array.Empty<Button>();
    [SerializeField] private Button[] actionButtons = Array.Empty<Button>();
    [SerializeField] private Button[] utilityButtons = Array.Empty<Button>();

    [Header("BRIDGE - List / Scroll")]
    [SerializeField] private Image listPanelImage;
    [SerializeField] private Image listViewportImage;
    [SerializeField] private Image listHeaderFrameImage;
   
    [SerializeField] private Scrollbar[] listScrollbars = Array.Empty<Scrollbar>();

    [Header("BRIDGE - Dropdown / Ambient")]
    [SerializeField] private TMP_Dropdown[] ambientDropdowns = Array.Empty<TMP_Dropdown>();
    [SerializeField] private Button[] dropdownTriggerButtons = Array.Empty<Button>();
    [SerializeField] private Image[] ambientSlotPanels = Array.Empty<Image>();

    [Header("BRIDGE - Text (Optional, minimal)")]
    [Header("BRIDGE - Player/List")]
    [SerializeField] private Image playerPanel;
    [SerializeField] private Image listBackground;
    [SerializeField] private Image listViewport;

    [Header("BRIDGE - Buttons")]
    [SerializeField] private Button[] playbackControlButtons = Array.Empty<Button>();


    [SerializeField] private UIButtonSpriteToggle toggle1;
    [SerializeField] private UIButtonSpriteToggle toggle2;
    [SerializeField] private UIButtonSpriteToggle toggle3;
    [SerializeField] private UIButtonSpriteToggle toggle4;
    [SerializeField] private UIButtonSpriteToggle toggle5;
    [SerializeField] private UIButtonSpriteToggle toggle6;
    [SerializeField] private UIButtonSpriteToggle toggle7;
    [SerializeField] private UIButtonSpriteToggle toggle8;
    [SerializeField] private UIButtonSpriteToggle toggle9;
    [SerializeField] private UIButtonSpriteToggle toggle10;

    [Header("BRIDGE - Dropdown/Ambient")]
    [SerializeField] private TMP_Dropdown[] dropdowns = Array.Empty<TMP_Dropdown>();
    [SerializeField] private Button[] dropdownButtons = Array.Empty<Button>();
    

    [Header("BRIDGE - Text (Optional)")]
    [SerializeField] private TMP_Text[] primaryTexts = Array.Empty<TMP_Text>();
    [SerializeField] private TMP_Text[] secondaryTexts = Array.Empty<TMP_Text>();


    private void ApplyToggleSprites(
     UIButtonSpriteToggle toggle,
     Sprite onBackground,
     Sprite offBackground,
     Sprite onIcon,
     Sprite offIcon)
    {
        if (toggle == null)
            return;

        toggle.SetOnBackgroundSprite(onBackground);
        toggle.SetOffBackgroundSprite(offBackground);
        toggle.SetOnIconSprite(onIcon);
        toggle.SetOffIconSprite(offIcon);
    }

    public override void ApplyFromManager(AppUIThemeData data, string appId)
    {
        if (data == null || appId != "Music")
            return;

        var t = data.bridge;

        ApplyImageSprite(playerMainPanelImage, t.playerMainPanelSprite);
        ApplyImageSprite(albumArtBackgroundImage, t.albumArtBackgroundSprite);
        ApplyImageSprite(albumArtFrameImage, t.albumArtFrameSprite);
        ApplyImageSprite(playerHeaderFrameImage, t.playerHeaderFrameSprite);
        ApplyImageSprite(playerFooterFrameImage, t.playerFooterFrameSprite);
        ApplyImageSprite(dividerLineImage, t.dividerLineSprite);

        for (int i = 0; i < extraFrameImages.Length; i++)
            ApplyImageSprite(extraFrameImages[i], t.playerMainPanelSprite);

        ApplyImageSprite(progressTrackImage, t.progressTrackSprite);
        ApplyImageSprite(progressFillImage, t.progressFillSprite);
        ApplyImageSprite(progressHandleImage, t.progressHandleSprite);

        for (int i = 0; i < transportButtons.Length; i++)
            ApplyButtonSprite(transportButtons[i], t.transportButtonSprite);

        for (int i = 0; i < playPauseButtons.Length; i++)
            ApplyButtonSprite(playPauseButtons[i], t.playPauseButtonSprite);

        for (int i = 0; i < playbackToggleButtons.Length; i++)
            ApplyButtonSprite(playbackToggleButtons[i], t.playbackToggleButtonSprite);
        ApplyImageSprite(playerPanel, t.playerPanelSprite);
        ApplyImageSprite(listBackground, t.listBackgroundSprite);
        ApplyImageSprite(listViewport, t.listViewportSprite);

        ApplyImageSprite(extraImage1, t.extraSprite1);
        ApplyImageSprite(extraImage2, t.extraSprite2);
        ApplyImageSprite(extraImage3, t.extraSprite3);
        ApplyImageSprite(extraImage4, t.extraSprite4);
        ApplyImageSprite(extraImage5, t.extraSprite5);
        ApplyImageSprite(extraImage6, t.extraSprite6);
        ApplyImageSprite(extraImage7, t.extraSprite7);
        ApplyImageSprite(extraImage8, t.extraSprite8);
        ApplyImageSprite(extraImage9, t.extraSprite9);
        ApplyImageSprite(extraImage10, t.extraSprite10);
        ApplyImageSprite(extraImage11, t.extraSprite11);
        ApplyImageSprite(extraImage12, t.extraSprite12);
        ApplyImageSprite(extraImage13, t.extraSprite13);
        ApplyImageSprite(extraImage14, t.extraSprite14);
        ApplyImageSprite(extraImage15, t.extraSprite15);
        ApplyImageSprite(extraImage16, t.extraSprite16);

        

        for (int i = 0; i < actionButtons.Length; i++)
            ApplyButtonSprite(actionButtons[i], t.actionButtonSprite);

        for (int i = 0; i < utilityButtons.Length; i++)
            ApplyButtonSprite(utilityButtons[i], t.utilityButtonSprite);

        ApplyImageSprite(listPanelImage, t.listPanelSprite);
        ApplyImageSprite(listViewportImage, t.listViewportSprite);
        ApplyImageSprite(listHeaderFrameImage, t.listHeaderFrameSprite);

        for (int i = 0; i < listScrollbars.Length; i++)
            ApplyScrollbarSprites(listScrollbars[i], t.scrollbarTrackSprite, t.scrollbarHandleSprite);

        for (int i = 0; i < ambientDropdowns.Length; i++)
            ApplyDropdownSprites(ambientDropdowns[i], t.dropdownBackgroundSprite, t.dropdownButtonSprite);

        for (int i = 0; i < dropdownTriggerButtons.Length; i++)
            ApplyButtonSprite(dropdownTriggerButtons[i], t.dropdownButtonSprite);
        ApplyImageSprite(progressFillImage, t.progressFillSprite);
        ApplyImageSprite(progressHandleImage, t.progressHandleSprite);
        ApplyImageSprite(progressHandleImage2, t.progressHandleSprite2);


        for (int i = 0; i < dropdowns.Length; i++)
            ApplyDropdownSprites(dropdowns[i], t.dropdownBackgroundSprite, t.dropdownButtonSprite);

        for (int i = 0; i < dropdownButtons.Length; i++)
            ApplyButtonSprite(dropdownButtons[i], t.dropdownButtonSprite);

        for (int i = 0; i < ambientSlotPanels.Length; i++)
            ApplyImageSprite(ambientSlotPanels[i], t.ambientSlotSprite);

        for (int i = 0; i < primaryTexts.Length; i++)
            ApplyTextColor(primaryTexts[i], t.primaryTextColor);

        for (int i = 0; i < secondaryTexts.Length; i++)
            ApplyTextColor(secondaryTexts[i], t.secondaryTextColor);
    }
}
