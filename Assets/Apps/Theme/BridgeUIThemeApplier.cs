using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BridgeUIThemeApplier : AppUIThemeApplierBase
{
    [Header("BRIDGE - Surfaces")]
    [SerializeField] private Image rootBackground;
    [SerializeField] private Image playerPanel;
    [SerializeField] private Image listBackground;
    [SerializeField] private Image listViewport;

    [Header("BRIDGE - Buttons")]
    [SerializeField] private Button[] transportButtons;
    [SerializeField] private Button[] listButtons;

    [Header("BRIDGE - Text")]
    [SerializeField] private TMP_Text[] primaryTexts;
    [SerializeField] private TMP_Text[] secondaryTexts;

    public override void ApplyFromManager(AppUIThemeData data, string appId)
    {
        if (data == null || appId != "Music")
            return;

        AutoWireIfNeeded();

        var slot = data.bridge;
        ApplyImageSlot(rootBackground, slot.colors.background, slot.sprites.background);
        ApplyImageSlot(playerPanel, slot.colors.panel, slot.sprites.panel);
        ApplyImageSlot(listBackground, slot.colors.panel, slot.sprites.panel);
        ApplyImageSlot(listViewport, slot.colors.panel);

        for (int i = 0; i < transportButtons.Length; i++)
            ApplyButtonColors(transportButtons[i], slot.colors.button, slot.colors.buttonText);

        for (int i = 0; i < listButtons.Length; i++)
            ApplyButtonColors(listButtons[i], slot.colors.button, slot.colors.buttonText);

        for (int i = 0; i < primaryTexts.Length; i++)
            ApplyTextSlot(primaryTexts[i], slot.colors.bodyText);

        for (int i = 0; i < secondaryTexts.Length; i++)
            ApplyTextSlot(secondaryTexts[i], slot.colors.mutedText);
    }

    private void AutoWireIfNeeded()
    {
        if (rootBackground == null)
            rootBackground = FindInChildrenByName<Image>(transform, "PlayerArea");

        if (playerPanel == null)
            playerPanel = FindInChildrenByName<Image>(transform, "PlayBar");

        if (listBackground == null)
            listBackground = FindInChildrenByName<Image>(transform, "BG_Full");

        if (listViewport == null)
            listViewport = FindInChildrenByName<Image>(transform, "Viewport");
    }
}
