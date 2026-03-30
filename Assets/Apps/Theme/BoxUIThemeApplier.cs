using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BoxUIThemeApplier : AppUIThemeApplierBase
{
    [Header("BOX - Panels")]
    [SerializeField] private Image topToolbar;
    [SerializeField] private Image bodyPanel;
    [SerializeField] private Image scrollArea;
    [SerializeField] private Image[] innerBackgrounds = Array.Empty<Image>();

    [Header("BOX - Scroll")]
    [SerializeField] private Scrollbar[] scrollbars = Array.Empty<Scrollbar>();
    [SerializeField] private Button scrollUpButton;
    [SerializeField] private Button scrollDownButton;

    [Header("BOX - Text (Optional)")]
    [SerializeField] private TMP_Text[] texts = Array.Empty<TMP_Text>();

    public override void ApplyFromManager(AppUIThemeData data, string appId)
    {
        if (data == null || appId != "Folder")
            return;

        var t = data.box;

        ApplyImageSprite(topToolbar, t.topToolbarSprite);
        ApplyImageSprite(bodyPanel, t.bodyPanelSprite);
        ApplyImageSprite(scrollArea, t.scrollAreaSprite);

        for (int i = 0; i < innerBackgrounds.Length; i++)
            ApplyImageSprite(innerBackgrounds[i], t.innerBackgroundSprite);

        for (int i = 0; i < scrollbars.Length; i++)
            ApplyScrollbarSprites(scrollbars[i], t.scrollbarTrackSprite, t.scrollbarHandleSprite);

        ApplyButtonSprite(scrollUpButton, t.scrollUpButtonSprite);
        ApplyButtonSprite(scrollDownButton, t.scrollDownButtonSprite);

        for (int i = 0; i < texts.Length; i++)
            ApplyTextColor(texts[i], t.textColor);
    }
}
