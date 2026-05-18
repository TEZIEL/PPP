using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PPP.BLUE.VN.RecipeApp;

public class BlueprintUIThemeApplier : AppUIThemeApplierBase
{
    [Header("BluePrint / Recipe - Filters")]
    [SerializeField] private IngredientFilterButtonUI[] filterButtons = Array.Empty<IngredientFilterButtonUI>();
   

    [Header("BluePrint / Recipe - Scroll")]
    [SerializeField] private Image scrollViewBackground;
    [SerializeField] private Image scrollbarBackground;
    [SerializeField] private Image scrollbarHandle;
    [SerializeField] private Scrollbar scrollbar;
    [SerializeField] private Button scrollUpButton;
    [SerializeField] private Button scrollDownButton;

    [SerializeField] private Image extraImage1;
    [SerializeField] private Image extraImage2;
    [SerializeField] private Image extraImage3;
    [SerializeField] private Image extraImage4;
    [SerializeField] private Image extraImage5;
    [SerializeField] private Image extraImage6;
    [SerializeField] private Image extraImage7;
    [SerializeField] private Image extraImage8;
    [SerializeField] private Image extraImage9;
    [SerializeField] private Image extraImage10;
    [SerializeField] private Image extraImage11;
    [SerializeField] private Image extraImage12;
    [SerializeField] private Image extraImage13;
    [SerializeField] private Image extraImage14;

    [Header("BluePrint / Recipe - Panels")]
    [SerializeField] private Image mainPanelBackground;
    [SerializeField] private Image detailPanelBackground;
    [SerializeField] private Image mainPanel2BackgroundSprite;

    [Header("BluePrint / Recipe - Text (Optional)")]
    [SerializeField] private TMP_Text[] primaryTexts = Array.Empty<TMP_Text>();
    [SerializeField] private TMP_Text[] secondaryTexts = Array.Empty<TMP_Text>();

    private void OnEnable()
    {
        var manager = AppUIThemeManager.Instance;
        if (manager != null)
            manager.OnThemeChanged += HandleThemeChanged;

        ApplyCurrentTheme();
    }

    private void OnDisable()
    {
        var manager = AppUIThemeManager.Instance;
        if (manager != null)
            manager.OnThemeChanged -= HandleThemeChanged;
    }

    private void HandleThemeChanged()
    {
        ApplyCurrentTheme();
    }

    private void ApplyCurrentTheme()
    {
        var manager = AppUIThemeManager.Instance;
        if (manager == null || manager.CurrentTheme == null)
            return;

        ApplyFromManager(manager.CurrentTheme, "recipe");
    }

    public override void ApplyFromManager(AppUIThemeData data, string appId)
    {
        if (data == null || appId != "recipe")
            return;

        var t = data.blueprint;

        for (int i = 0; i < filterButtons.Length; i++)
        {
            var filter = filterButtons[i];
            if (filter == null)
                continue;

            filter.ApplyThemeSprites(t.filterButtonSprite, t.filterButtonSelectedSprite);
        }
            

        ApplyImageSprite(scrollViewBackground, t.scrollViewBackgroundSprite);
        ApplyImageSprite(scrollbarBackground, t.scrollbarBackgroundSprite);
        ApplyImageSprite(scrollbarHandle, t.scrollbarHandleSprite);
        ApplyScrollbarSprites(scrollbar, t.scrollbarBackgroundSprite, t.scrollbarHandleSprite);
        ApplyButtonSprite(scrollUpButton, t.scrollUpButtonSprite);
        ApplyButtonSprite(scrollDownButton, t.scrollDownButtonSprite);

        ApplyImageSprite(extraImage1, t.extraSprite1);
        ApplyImageSprite(extraImage2, t.extraSprite2);
        ApplyImageSprite(extraImage3, t.extraSprite3);
        ApplyImageSprite(extraImage4, t.extraSprite4);
        ApplyImageSprite(extraImage5, t.extraSprite5);
        ApplyImageSprite(extraImage6, t.extraSprite6);
        ApplyImageSprite(extraImage7, t.extraSprite7);
        ApplyImageSprite(extraImage8, t.extraSprite8);
        ApplyImageSprite(extraImage9, t.extraSprite9);
        ApplyImageSprite(extraImage10, t.extraSprite10);
        ApplyImageSprite(extraImage11, t.extraSprite11);
        ApplyImageSprite(extraImage12, t.extraSprite12);
        ApplyImageSprite(extraImage13, t.extraSprite13);
        ApplyImageSprite(extraImage14, t.extraSprite14);

        ApplyImageSprite(mainPanelBackground, t.mainPanelBackgroundSprite);
        ApplyImageSprite(detailPanelBackground, t.detailPanelBackgroundSprite);

        for (int i = 0; i < primaryTexts.Length; i++)
            ApplyTextColor(primaryTexts[i], t.primaryTextColor);

        for (int i = 0; i < secondaryTexts.Length; i++)
            ApplyTextColor(secondaryTexts[i], t.secondaryTextColor);
    }
}
