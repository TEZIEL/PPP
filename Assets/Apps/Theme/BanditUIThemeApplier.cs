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
    [SerializeField] private Image swipeViewport2;
    [SerializeField] private Image infoPanel2;
    [SerializeField] private Image frameImage2;
    [SerializeField] private Image swipeViewport3;
    [SerializeField] private Image infoPanel3;
    [SerializeField] private Image frameImage3;
    [SerializeField] private Image swipeViewport4;
    [SerializeField] private Image infoPanel4;
    [SerializeField] private Image frameImage4;
    [SerializeField] private Image frameImage5;
    [SerializeField] private Image swipeViewport5;
    [SerializeField] private Image infoPanel5;
    [SerializeField] private Image frameImage6;
    [SerializeField] private Image swipeViewport6;

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
        ApplyImageSprite(swipeViewport2, t.swipeViewportSprite2);
        ApplyImageSprite(infoPanel2, t.infoPanelSprite2);
        ApplyImageSprite(frameImage2, t.frameSprite2);
        ApplyImageSprite(swipeViewport3, t.swipeViewportSprite3);
        ApplyImageSprite(infoPanel3, t.infoPanelSprite3);
        ApplyImageSprite(frameImage3, t.frameSprite3);
        ApplyImageSprite(swipeViewport4, t.swipeViewportSprite4);
        ApplyImageSprite(infoPanel4, t.infoPanelSprite4);
        ApplyImageSprite(frameImage4, t.frameSprite4);
        ApplyImageSprite(frameImage5, t.frameSprite5);
        ApplyImageSprite(swipeViewport5, t.swipeViewportSprite5);
        ApplyImageSprite(infoPanel5, t.infoPanelSprite5);
        ApplyImageSprite(frameImage6, t.frameSprite6);
        ApplyImageSprite(swipeViewport6, t.swipeViewportSprite6);

        for (int i = 0; i < innerBackgrounds.Length; i++)
            ApplyImageSprite(innerBackgrounds[i], t.innerBackgroundSprite);

        for (int i = 0; i < actionButtons.Length; i++)
            ApplyButtonSprite(actionButtons[i], t.actionButtonSprite);

        for (int i = 0; i < texts.Length; i++)
            ApplyTextColor(texts[i], t.textColor);
    }
}
