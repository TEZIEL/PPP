using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BanditUIThemeApplier : AppUIThemeApplierBase
{
    [Header("BANDIT - Surfaces")]
    [SerializeField] private Image swipeViewport;
    [SerializeField] private Image frameImage;
    [SerializeField] private Image infoPanel;

    [Header("BANDIT - Buttons")]
    [SerializeField] private Button[] actionButtons = Array.Empty<Button>();

    [Header("BANDIT - Text")]
    [SerializeField] private TMP_Text[] infoTexts = Array.Empty<TMP_Text>();

    public override void ApplyFromManager(AppUIThemeData data, string appId)
    {
        if (data == null || appId != "Fidget")
            return;

        AutoWireIfNeeded();

        var slot = data.bandit;
        ApplyImageSlot(swipeViewport, slot.colors.background, slot.sprites.background);
        ApplyImageSlot(frameImage, slot.colors.panel, slot.sprites.panel);
        ApplyImageSlot(infoPanel, slot.colors.panel, slot.sprites.panel);

        for (int i = 0; i < actionButtons.Length; i++)
            ApplyButtonColors(actionButtons[i], slot.colors.button, slot.colors.buttonText);

        for (int i = 0; i < infoTexts.Length; i++)
            ApplyTextSlot(infoTexts[i], slot.colors.bodyText);
    }

    private void AutoWireIfNeeded()
    {
        if (swipeViewport == null)
            swipeViewport = FindInChildrenByName<Image>(transform, "SwipeViewport");

        if (frameImage == null)
            frameImage = FindInChildrenByName<Image>(transform, "Frame");

        if (infoPanel == null)
            infoPanel = FindInChildrenByName<Image>(transform, "Information");
    }
}
