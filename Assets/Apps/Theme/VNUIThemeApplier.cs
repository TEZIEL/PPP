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
    [SerializeField] private Image otherWindow3Root;
    [SerializeField] private Image otherWindow4Root;
    [SerializeField] private Image otherWindow5Root;
    [SerializeField] private Image otherWindow6Root;
    [SerializeField] private Image otherWindow7Root;
    [SerializeField] private Image otherWindow8Root;
    [SerializeField] private Image otherWindow9Root;
    [SerializeField] private Image otherWindow10Root;
    [SerializeField] private Image otherWindow11Root;
    [SerializeField] private Image otherWindow12Root;
    [SerializeField] private Image otherWindow13Root;
    [SerializeField] private Image otherWindow14Root;
    [SerializeField] private Image otherWindow15Root;
    [SerializeField] private Image otherWindow16Root;
    [SerializeField] private Image otherWindow17Root;
    [SerializeField] private Image otherWindow18Root;
    [SerializeField] private Image otherWindow19Root;
    [SerializeField] private Image otherWindow20Root;
    [SerializeField] private Image otherWindow21Root;
    [SerializeField] private Image otherWindow22Root;
    [SerializeField] private Image otherWindow23Root;
    [SerializeField] private Image otherWindow24Root;
    [SerializeField] private Image otherWindow25Root;
    [SerializeField] private Image otherWindow26Root;
    [SerializeField] private Image otherWindow27Root;
    [SerializeField] private Image otherWindow28Root;
    [SerializeField] private Image otherWindow29Root;
    [SerializeField] private Image otherWindow30Root;
    [SerializeField] private Image otherWindow31Root;
    [SerializeField] private Image otherWindow32Root;
    [SerializeField] private Image otherWindow33Root;
    [SerializeField] private Image otherWindow34Root;
    [SerializeField] private Image otherWindow35Root;
    [SerializeField] private Image otherWindow36Root;
    [SerializeField] private Image otherWindow37Root;
    [SerializeField] private Image otherWindow38Root;
    [SerializeField] private Image otherWindow39Root;
    [SerializeField] private Image otherWindow40Root;

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
        ApplyImageSprite(otherWindow3Root, t.otherWindow3RootSprite);
        ApplyImageSprite(otherWindow4Root, t.otherWindow4RootSprite);
        ApplyImageSprite(otherWindow5Root, t.otherWindow5RootSprite);
        ApplyImageSprite(otherWindow6Root, t.otherWindow6RootSprite);
        ApplyImageSprite(otherWindow7Root, t.otherWindow7RootSprite);
        ApplyImageSprite(otherWindow8Root, t.otherWindow8RootSprite);
        ApplyImageSprite(otherWindow9Root, t.otherWindow9RootSprite);
        ApplyImageSprite(otherWindow10Root, t.otherWindow10RootSprite);
        ApplyImageSprite(otherWindow11Root, t.otherWindow11RootSprite);
        ApplyImageSprite(otherWindow12Root, t.otherWindow12RootSprite);
        ApplyImageSprite(otherWindow13Root, t.otherWindow13RootSprite);
        ApplyImageSprite(otherWindow14Root, t.otherWindow14RootSprite);
        ApplyImageSprite(otherWindow15Root, t.otherWindow15RootSprite);
        ApplyImageSprite(otherWindow16Root, t.otherWindow16RootSprite);
        ApplyImageSprite(otherWindow17Root, t.otherWindow17RootSprite);
        ApplyImageSprite(otherWindow18Root, t.otherWindow18RootSprite);
        ApplyImageSprite(otherWindow19Root, t.otherWindow19RootSprite);
        ApplyImageSprite(otherWindow20Root, t.otherWindow20RootSprite);
        ApplyImageSprite(otherWindow21Root, t.otherWindow21RootSprite);
        ApplyImageSprite(otherWindow22Root, t.otherWindow22RootSprite);
        ApplyImageSprite(otherWindow23Root, t.otherWindow23RootSprite);
        ApplyImageSprite(otherWindow24Root, t.otherWindow24RootSprite);
        ApplyImageSprite(otherWindow25Root, t.otherWindow25RootSprite);
        ApplyImageSprite(otherWindow26Root, t.otherWindow26RootSprite);
        ApplyImageSprite(otherWindow27Root, t.otherWindow27RootSprite);
        ApplyImageSprite(otherWindow28Root, t.otherWindow28RootSprite);
        ApplyImageSprite(otherWindow29Root, t.otherWindow29RootSprite);
        ApplyImageSprite(otherWindow30Root, t.otherWindow30RootSprite);
        ApplyImageSprite(otherWindow31Root, t.otherWindow31RootSprite);
        ApplyImageSprite(otherWindow32Root, t.otherWindow32RootSprite);
        ApplyImageSprite(otherWindow33Root, t.otherWindow33RootSprite);
        ApplyImageSprite(otherWindow34Root, t.otherWindow34RootSprite);
        ApplyImageSprite(otherWindow35Root, t.otherWindow35RootSprite);
        ApplyImageSprite(otherWindow36Root, t.otherWindow36RootSprite);
        ApplyImageSprite(otherWindow37Root, t.otherWindow37RootSprite);
        ApplyImageSprite(otherWindow38Root, t.otherWindow38RootSprite);
        ApplyImageSprite(otherWindow39Root, t.otherWindow39RootSprite);
        ApplyImageSprite(otherWindow40Root, t.otherWindow40RootSprite);

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
