using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BlueprintUIThemeApplier : AppUIThemeApplierBase
{
    [Header("BluePrint / Recipe - Filters")]
    [SerializeField] private Button[] filterButtons = Array.Empty<Button>();

    [Header("BluePrint / Recipe - Scroll/List")]
    [SerializeField] private Image scrollViewBackground;
    [SerializeField] private Button scrollUpButton;
    [SerializeField] private Button scrollDownButton;
    [SerializeField] private Image[] listItemBackgrounds = Array.Empty<Image>();

    [Header("BluePrint / Recipe - Panels")]
    [SerializeField] private Image mainPanelBackground;
    [SerializeField] private Image detailPanelBackground;

    [Header("BluePrint / Recipe - Text (Optional)")]
    [SerializeField] private TMP_Text[] primaryTexts = Array.Empty<TMP_Text>();
    [SerializeField] private TMP_Text[] secondaryTexts = Array.Empty<TMP_Text>();

    public override void ApplyFromManager(AppUIThemeData data, string appId)
    {
        if (data == null || appId != "recipe")
            return;

        var t = data.blueprint;

        for (int i = 0; i < filterButtons.Length; i++)
            ApplyButtonSprite(filterButtons[i], t.filterButtonSprite);

        ApplyImageSprite(scrollViewBackground, t.scrollViewBackgroundSprite);
        ApplyButtonSprite(scrollUpButton, t.scrollUpButtonSprite);
        ApplyButtonSprite(scrollDownButton, t.scrollDownButtonSprite);

        for (int i = 0; i < listItemBackgrounds.Length; i++)
            ApplyImageSprite(listItemBackgrounds[i], t.listItemBackgroundSprite);

        ApplyImageSprite(mainPanelBackground, t.mainPanelBackgroundSprite);
        ApplyImageSprite(detailPanelBackground, t.detailPanelBackgroundSprite);

        for (int i = 0; i < primaryTexts.Length; i++)
            ApplyTextColor(primaryTexts[i], t.primaryTextColor);

        for (int i = 0; i < secondaryTexts.Length; i++)
            ApplyTextColor(secondaryTexts[i], t.secondaryTextColor);
    }
}
