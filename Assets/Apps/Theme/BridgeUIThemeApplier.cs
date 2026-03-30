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

    [Header("BRIDGE - Progress Images")]
    [SerializeField] private Image progressTrackImage;
    [SerializeField] private Image progressFillImage;
    [SerializeField] private Image progressHandleImage;

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
    [SerializeField] private TMP_Text[] primaryTexts = Array.Empty<TMP_Text>();
    [SerializeField] private TMP_Text[] secondaryTexts = Array.Empty<TMP_Text>();

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

        for (int i = 0; i < ambientSlotPanels.Length; i++)
            ApplyImageSprite(ambientSlotPanels[i], t.ambientSlotSprite);

        for (int i = 0; i < primaryTexts.Length; i++)
            ApplyTextColor(primaryTexts[i], t.primaryTextColor);

        for (int i = 0; i < secondaryTexts.Length; i++)
            ApplyTextColor(secondaryTexts[i], t.secondaryTextColor);
    }
}
