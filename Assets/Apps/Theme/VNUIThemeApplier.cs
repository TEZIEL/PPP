using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class VNUIThemeApplier : AppUIThemeApplierBase
{
    [Header("Save/Load Window")]
    [SerializeField] private Image saveLoadWindowRoot;
    [SerializeField] private Button[] saveLoadSlotButtons = Array.Empty<Button>();
    [SerializeField] private Image[] saveLoadSelectedSlotVisuals = Array.Empty<Image>();
    [SerializeField] private Button[] saveLoadButtons = Array.Empty<Button>();

    [Header("Backlog Window")]
    [SerializeField] private Image backlogRoot;
    [SerializeField] private Image[] backlogItemRoots = Array.Empty<Image>();
    [SerializeField] private Button[] backlogButtons = Array.Empty<Button>();

    [Header("Exit Modal")]
    [SerializeField] private Image exitModalRoot;
    [SerializeField] private Button[] exitModalButtons = Array.Empty<Button>();

    [Header("Drink UI")]
    [SerializeField] private Image drinkRoot;
    [SerializeField] private Image drinkGrid;
    [SerializeField] private Button[] ingredientButtons = Array.Empty<Button>();
    [SerializeField] private Image[] ingredientSelectedVisuals = Array.Empty<Image>();

    [Header("Other Windows")]
    [SerializeField] private Image otherWindow1Root;
    [SerializeField] private Image otherWindow2Root;

    [Header("Dialogue")]
    [SerializeField] private Image dialogueRoot;
    [SerializeField] private Image dialogueButtonContainer;
    [SerializeField] private Button[] dialogueButtons = Array.Empty<Button>();

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

        ApplyFromManager(manager.CurrentTheme, "VN");
    }

    public override void ApplyFromManager(AppUIThemeData data, string appId)
    {
        if (data == null || !IsVNAppId(appId))
            return;

        var t = data.vn;

        ApplyImageSprite(saveLoadWindowRoot, t.saveLoadWindowRootSprite);
        ApplyImageArray(backlogItemRoots, t.backlogItemSprite);
        ApplyImageArray(saveLoadSelectedSlotVisuals, t.saveLoadSelectedSlotSprite);
        ApplyButtonGroup(saveLoadSlotButtons, t.saveLoadSlotSprite, t.saveLoadSelectedSlotSprite);
        ApplyButtonGroup(saveLoadButtons, t.saveLoadButtonSprite, null);

        ApplyImageSprite(backlogRoot, t.backlogRootSprite);
        ApplyButtonGroup(backlogButtons, t.backlogButtonSprite, null);

        ApplyImageSprite(exitModalRoot, t.exitModalRootSprite);
        ApplyButtonGroup(exitModalButtons, t.exitModalButtonSprite, null);

        ApplyImageSprite(drinkRoot, t.drinkRootSprite);
        ApplyImageSprite(drinkGrid, t.drinkGridSprite);
        ApplyImageArray(ingredientSelectedVisuals, t.ingredientSelectedButtonSprite);
        ApplyButtonGroup(ingredientButtons, t.ingredientButtonSprite, t.ingredientSelectedButtonSprite);

        ApplyImageSprite(otherWindow1Root, t.otherWindow1RootSprite);
        ApplyImageSprite(otherWindow2Root, t.otherWindow2RootSprite);

        ApplyImageSprite(dialogueRoot, t.dialogueRootSprite);
        ApplyImageSprite(dialogueButtonContainer, t.dialogueButtonContainerSprite);
        ApplyButtonGroup(dialogueButtons, t.dialogueButtonSprite, t.dialogueSelectedButtonSprite);
    }

    private static bool IsVNAppId(string appId)
    {
        if (string.IsNullOrWhiteSpace(appId))
            return false;

        return string.Equals(appId, "vn", StringComparison.OrdinalIgnoreCase)
               || string.Equals(appId, "blue", StringComparison.OrdinalIgnoreCase)
               || string.Equals(appId, "blue.vn", StringComparison.OrdinalIgnoreCase);
    }

    private static void ApplyImageArray(Image[] targets, Sprite sprite)
    {
        if (targets == null || sprite == null)
            return;

        for (int i = 0; i < targets.Length; i++)
            ApplyImageSprite(targets[i], sprite);
    }

    private static void ApplyButtonGroup(Button[] targets, Sprite normalSprite, Sprite selectedSprite)
    {
        if (targets == null)
            return;

        for (int i = 0; i < targets.Length; i++)
        {
            var button = targets[i];
            if (button == null)
                continue;

            ApplyButtonSprite(button, normalSprite);

            if (selectedSprite == null)
                continue;

            var state = button.spriteState;
            state.highlightedSprite = selectedSprite;
            state.pressedSprite = selectedSprite;
            state.selectedSprite = selectedSprite;
            button.spriteState = state;
            button.transition = Selectable.Transition.SpriteSwap;
        }
    }
}
