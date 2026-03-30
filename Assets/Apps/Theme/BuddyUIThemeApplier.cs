using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuddyUIThemeApplier : AppUIThemeApplierBase
{
    [Header("BUDDY - Main")]
    [SerializeField] private Image pinArea;
    [SerializeField] private Image profileCard;
    [SerializeField] private Image listArea;
    [SerializeField] private Image bottomActionPanel;
    [SerializeField] private Image[] innerPanels = Array.Empty<Image>();

    [Header("BUDDY - Buttons")]
    [SerializeField] private Button[] actionButtons = Array.Empty<Button>();

    [Header("BUDDY - Text (Optional)")]
    [SerializeField] private TMP_Text[] texts = Array.Empty<TMP_Text>();

    public override void ApplyFromManager(AppUIThemeData data, string appId)
    {
        if (data == null || appId != "Melion")
            return;

        var t = data.buddy;

        ApplyImageSprite(pinArea, t.pinAreaSprite);
        ApplyImageSprite(profileCard, t.profileCardSprite);
        ApplyImageSprite(listArea, t.listAreaSprite);
        ApplyImageSprite(bottomActionPanel, t.bottomActionPanelSprite);

        for (int i = 0; i < innerPanels.Length; i++)
            ApplyImageSprite(innerPanels[i], t.innerPanelSprite);

        for (int i = 0; i < actionButtons.Length; i++)
            ApplyButtonSprite(actionButtons[i], t.actionButtonSprite);

        for (int i = 0; i < texts.Length; i++)
            ApplyTextColor(texts[i], t.primaryTextColor);
    }
}
