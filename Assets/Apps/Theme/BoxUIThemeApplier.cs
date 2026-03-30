using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BoxUIThemeApplier : AppUIThemeApplierBase
{
    [Header("BOX - Surfaces")]
    [SerializeField] private Image appBackground;
    [SerializeField] private Image topToolbar;
    [SerializeField] private Image bodyPanel;

    [Header("BOX - Buttons")]
    [SerializeField] private Button[] toolbarButtons = Array.Empty<Button>();
    [SerializeField] private Button[] actionButtons = Array.Empty<Button>();

    [Header("BOX - Text")]
    [SerializeField] private TMP_Text[] primaryTexts = Array.Empty<TMP_Text>();

    public override void ApplyFromManager(AppUIThemeData data, string appId)
    {
        if (data == null || appId != "Folder")
            return;

        AutoWireIfNeeded();

        var slot = data.box;
        ApplyImageSlot(appBackground, slot.colors.background, slot.sprites.background);
        ApplyImageSlot(topToolbar, slot.colors.panel, slot.sprites.panel);
        ApplyImageSlot(bodyPanel, slot.colors.panel, slot.sprites.panel);

        for (int i = 0; i < toolbarButtons.Length; i++)
            ApplyButtonColors(toolbarButtons[i], slot.colors.button, slot.colors.buttonText);

        for (int i = 0; i < actionButtons.Length; i++)
            ApplyButtonColors(actionButtons[i], slot.colors.button, slot.colors.buttonText);

        for (int i = 0; i < primaryTexts.Length; i++)
            ApplyTextSlot(primaryTexts[i], slot.colors.bodyText);
    }

    private void AutoWireIfNeeded()
    {
        if (appBackground == null)
            appBackground = FindInChildrenByName<Image>(transform, "PlayerArea");

        if (topToolbar == null)
            topToolbar = FindInChildrenByName<Image>(transform, "PlayBar");

        if (bodyPanel == null)
            bodyPanel = FindInChildrenByName<Image>(transform, "BG_Full");
    }
}
