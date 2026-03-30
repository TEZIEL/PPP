using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BlueprintUIThemeApplier : AppUIThemeApplierBase
{
    [Header("BluePrint - Surfaces")]
    [SerializeField] private Image topToolbar;
    [SerializeField] private Image listBackground;
    [SerializeField] private Image detailPanel;

    [Header("BluePrint - Buttons")]
    [SerializeField] private Button[] filterButtons = Array.Empty<Button>();
    [SerializeField] private Button[] listButtons = Array.Empty<Button>();

    [Header("BluePrint - Text")]
    [SerializeField] private TMP_Text[] primaryTexts = Array.Empty<TMP_Text>();
    [SerializeField] private TMP_Text[] secondaryTexts = Array.Empty<TMP_Text>();

    public override void ApplyFromManager(AppUIThemeData data, string appId)
    {
        if (data == null || appId != "recipe")
            return;

        AutoWireIfNeeded();

        var slot = data.blueprint;
        ApplyImageSlot(topToolbar, slot.colors.panel, slot.sprites.panel);
        ApplyImageSlot(listBackground, slot.colors.background, slot.sprites.background);
        ApplyImageSlot(detailPanel, slot.colors.panel, slot.sprites.panel);

        for (int i = 0; i < filterButtons.Length; i++)
            ApplyButtonColors(filterButtons[i], slot.colors.button, slot.colors.buttonText);

        for (int i = 0; i < listButtons.Length; i++)
            ApplyButtonColors(listButtons[i], slot.colors.button, slot.colors.buttonText);

        for (int i = 0; i < primaryTexts.Length; i++)
            ApplyTextSlot(primaryTexts[i], slot.colors.bodyText);

        for (int i = 0; i < secondaryTexts.Length; i++)
            ApplyTextSlot(secondaryTexts[i], slot.colors.mutedText);
    }

    private void AutoWireIfNeeded()
    {
        if (topToolbar == null)
            topToolbar = FindInChildrenByName<Image>(transform, "PlayBar");

        if (listBackground == null)
            listBackground = FindInChildrenByName<Image>(transform, "BG_Full");

        if (detailPanel == null)
            detailPanel = FindInChildrenByName<Image>(transform, "Image");
    }
}
