using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuddyUIThemeApplier : AppUIThemeApplierBase
{
    [Header("BUDDY - Surfaces")]
    [SerializeField] private Image appBackground;
    [SerializeField] private Image profilePanel;
    [SerializeField] private Image actionPanel;

    [Header("BUDDY - Buttons")]
    [SerializeField] private Button[] actionButtons = Array.Empty<Button>();

    [Header("BUDDY - Text")]
    [SerializeField] private TMP_Text[] profileTexts = Array.Empty<TMP_Text>();

    public override void ApplyFromManager(AppUIThemeData data, string appId)
    {
        if (data == null || appId != "Melion")
            return;

        AutoWireIfNeeded();

        var slot = data.buddy;
        ApplyImageSlot(appBackground, slot.colors.background, slot.sprites.background);
        ApplyImageSlot(profilePanel, slot.colors.panel, slot.sprites.panel);
        ApplyImageSlot(actionPanel, slot.colors.accent, slot.sprites.accent);

        for (int i = 0; i < actionButtons.Length; i++)
            ApplyButtonColors(actionButtons[i], slot.colors.button, slot.colors.buttonText);

        for (int i = 0; i < profileTexts.Length; i++)
            ApplyTextSlot(profileTexts[i], slot.colors.bodyText);
    }

    private void AutoWireIfNeeded()
    {
        if (appBackground == null)
            appBackground = FindInChildrenByName<Image>(transform, "PlayerArea");

        if (profilePanel == null)
            profilePanel = FindInChildrenByName<Image>(transform, "BG_Full");

        if (actionPanel == null)
            actionPanel = FindInChildrenByName<Image>(transform, "PlayBar");
    }
}
