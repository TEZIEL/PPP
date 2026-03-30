using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BridgeUIThemeApplier : AppUIThemeApplierBase
{
    [Header("BRIDGE - Player/List")]
    [SerializeField] private Image playerPanel;
    [SerializeField] private Image listBackground;
    [SerializeField] private Image listViewport;

    [Header("BRIDGE - Buttons")]
    [SerializeField] private Button[] playbackControlButtons = Array.Empty<Button>();
    [SerializeField] private Button[] actionButtons = Array.Empty<Button>();

    [Header("BRIDGE - Progress/Scroll")]
    [SerializeField] private Image progressFillImage;
    [SerializeField] private Image progressHandleImage;
    [SerializeField] private Scrollbar[] scrollbars = Array.Empty<Scrollbar>();

    [Header("BRIDGE - Dropdown/Ambient")]
    [SerializeField] private TMP_Dropdown[] dropdowns = Array.Empty<TMP_Dropdown>();
    [SerializeField] private Button[] dropdownButtons = Array.Empty<Button>();
    [SerializeField] private Image[] ambientSlotPanels = Array.Empty<Image>();

    [Header("BRIDGE - Text (Optional)")]
    [SerializeField] private TMP_Text[] primaryTexts = Array.Empty<TMP_Text>();
    [SerializeField] private TMP_Text[] secondaryTexts = Array.Empty<TMP_Text>();

    public override void ApplyFromManager(AppUIThemeData data, string appId)
    {
        if (data == null || appId != "Music")
            return;

        var t = data.bridge;

        ApplyImageSprite(playerPanel, t.playerPanelSprite);
        ApplyImageSprite(listBackground, t.listBackgroundSprite);
        ApplyImageSprite(listViewport, t.listViewportSprite);

        for (int i = 0; i < playbackControlButtons.Length; i++)
            ApplyButtonSprite(playbackControlButtons[i], t.playbackControlButtonSprite);

        for (int i = 0; i < actionButtons.Length; i++)
            ApplyButtonSprite(actionButtons[i], t.actionButtonSprite);

        ApplyImageSprite(progressFillImage, t.progressFillSprite);
        ApplyImageSprite(progressHandleImage, t.progressHandleSprite);

        for (int i = 0; i < scrollbars.Length; i++)
            ApplyScrollbarSprites(scrollbars[i], t.scrollbarTrackSprite, t.scrollbarHandleSprite);

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
