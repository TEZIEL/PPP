using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BanditUIThemeApplier : AppUIThemeApplierBase
{
    [Header("BANDIT - Main")]
    [SerializeField] private Image swipeViewport;
    [SerializeField] private Image infoPanel;
    [SerializeField] private Image frameImage;
    [SerializeField] private Image[] innerBackgrounds = Array.Empty<Image>();

    [Header("BANDIT - Buttons")]
    [SerializeField] private Button[] actionButtons = Array.Empty<Button>();

    [Header("BANDIT - Text (Optional)")]
    [SerializeField] private TMP_Text[] texts = Array.Empty<TMP_Text>();

    public override void ApplyFromManager(AppUIThemeData data, string appId)
    {
        if (data == null || appId != "Fidget")
            return;

        var t = data.bandit;

        ApplyImageSprite(swipeViewport, t.swipeViewportSprite);
        ApplyImageSprite(infoPanel, t.infoPanelSprite);
        ApplyImageSprite(frameImage, t.frameSprite);

        for (int i = 0; i < innerBackgrounds.Length; i++)
            ApplyImageSprite(innerBackgrounds[i], t.innerBackgroundSprite);

        for (int i = 0; i < actionButtons.Length; i++)
            ApplyButtonSprite(actionButtons[i], t.actionButtonSprite);

        for (int i = 0; i < texts.Length; i++)
            ApplyTextColor(texts[i], t.textColor);
    }
}
