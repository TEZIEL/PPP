using TMPro;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PPP.BLUE.VN.DrinkSystem
{
    public sealed class IngredientButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private const string ArtheonIngredient = "INGREDIENT_ARTHEON";

        [SerializeField] private string ingredientID;
        [SerializeField] private DrinkManager manager;
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image stateTarget;
        [SerializeField] private Image stateIndicatorImage;
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private Color enabledColor = new Color(0.65f, 0.45f, 1f, 1f);
        [SerializeField] private Color selectedTintColor = Color.white;
        [SerializeField] private Color pressedTintColor = Color.white;
        [SerializeField] private Color defaultTextColor = Color.white;
        [SerializeField] private Color selectedTextColor = Color.white;
        [SerializeField] private Color pressedTextColor = Color.white;
        [SerializeField] private float hotkeyPressScale = 0.92f;
        [SerializeField] private float hotkeyPressDuration = 0.08f;

        public string IngredientID => ingredientID;
        private Coroutine hotkeyPressCo;
        private bool isPressed;
        private bool isSelected;
        private Sprite defaultButtonSprite;

        private void Awake()
        {
            ApplyNavigationNone();
            ApplyCurrentTheme();

            if (button != null)
            {
                button.onClick.AddListener(OnClick);
                if (button.image != null)
                    defaultButtonSprite = button.image.sprite;
            }

            RefreshLabel(0);
            SetModifierState(false);
            RefreshStateVisual();
        }

        private void OnEnable()
        {
            var themeManager = AppUIThemeManager.Instance;
            if (themeManager != null)
                themeManager.OnThemeChanged += HandleThemeChanged;

            ApplyCurrentTheme();
        }

        private void OnDisable()
        {
            var themeManager = AppUIThemeManager.Instance;
            if (themeManager != null)
                themeManager.OnThemeChanged -= HandleThemeChanged;
        }

        private void OnEnable()
        {
            var themeManager = AppUIThemeManager.Instance;
            if (themeManager != null)
                themeManager.OnThemeChanged += HandleThemeChanged;

            ApplyCurrentTheme();
        }

        private void OnDisable()
        {
            var themeManager = AppUIThemeManager.Instance;
            if (themeManager != null)
                themeManager.OnThemeChanged -= HandleThemeChanged;
        }

        
        
       

        public void BindManager(DrinkManager target)
        {
            manager = target;
        }

        public void SetInteractable(bool interactable)
        {
            if (button != null)
            {
                ApplyNavigationNone();
                button.interactable = interactable;
            }

            if (!interactable)
            {
                isPressed = false;
                isSelected = false;
            }

            RefreshStateVisual();
        }

        public void SetModifierState(bool enabled)
        {
            if (!string.Equals(ingredientID, ArtheonIngredient, System.StringComparison.Ordinal))
                return;

            if (stateTarget != null)
                stateTarget.color = enabled ? enabledColor : defaultColor;

            if (label != null)
                label.text = "ARTHEON";

            RefreshStateVisual();
        }

        public void RefreshLabel(int count)
        {
            if (label == null)
                return;

            if (string.Equals(ingredientID, ArtheonIngredient, System.StringComparison.Ordinal))
            {
                label.text = "ARTHEON";
                return;
            }

            var displayId = string.IsNullOrEmpty(ingredientID) ? "UNKNOWN" : ingredientID.Replace("INGREDIENT_", string.Empty);
            label.text = displayId;
            RefreshStateVisual();
        }

        public void PlayHotkeyPressFeedback()
        {
            if (!isActiveAndEnabled || button == null || !button.IsInteractable())
                return;

            ApplyNavigationNone();
        }

        private void OnClick()
        {
            

            float pitch = GetPitchByIngredient();

            SoundManager.Instance.PlayOSWithPitch(OSSoundEvent.IngredientFill1, pitch);

            manager?.AddIngredientFromClick(ingredientID);
            RefreshStateVisual();
        }

        private float GetPitchByIngredient()
        {
            switch (ingredientID)
            {
                case "INGREDIENT_VELTRINE": return 1.000f; // C4
                case "INGREDIENT_ZYPHRATE": return 1.122f; // D4
                case "INGREDIENT_KRATYLEN": return 1.260f; // E4
                case "INGREDIENT_MORVION": return 1.335f; // F4
                case "INGREDIENT_REDULINE": return 1.498f; // G4
                case "INGREDIENT_CYMENTOL": return 1.682f; // A4
                case "INGREDIENT_BRAXIUM": return 1.888f; // B4
                case "INGREDIENT_ARTHEON": return 2.000f; // C5
                default: return 1f;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            manager?.SetIngredientHover(ingredientID, true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            manager?.SetIngredientHover(ingredientID, false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (button == null || !button.interactable)
                return;

            isPressed = true;
            RefreshStateVisual();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPressed = false;
            RefreshStateVisual();
        }

        public void SetSelectedState(bool selected)
        {
            isSelected = selected;
            RefreshStateVisual();
        }

        private IEnumerator CoPlayHotkeyPressFeedback()
        {
            Vector3 originalScale = transform.localScale;
            Vector3 pressedScale = originalScale * Mathf.Max(0.1f, hotkeyPressScale);
            float duration = Mathf.Max(0.01f, hotkeyPressDuration);
            float half = duration * 0.5f;

            float t = 0f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / half);
                transform.localScale = Vector3.LerpUnclamped(originalScale, pressedScale, p);
                yield return null;
            }

            t = 0f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / half);
                transform.localScale = Vector3.LerpUnclamped(pressedScale, originalScale, p);
                yield return null;
            }

            transform.localScale = originalScale;
            hotkeyPressCo = null;
        }

        private void ApplyNavigationNone()
        {
            if (button == null)
                return;

            Navigation navigation = button.navigation;
            if (navigation.mode == Navigation.Mode.None)
                return;

            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
        }

        private void HandleThemeChanged()
        {
            ApplyCurrentTheme();
        }

        public void ApplyCurrentTheme()
        {
            if (button == null)
                return;

            Color selected = selectedTintColor;
            Color pressed = pressedTintColor;
            Color defaultText = defaultTextColor;
            Color pressedText = pressedTextColor;
            Color selectedText = selectedTextColor;
            Sprite indicatorSprite = stateIndicatorImage != null ? stateIndicatorImage.sprite : null;

            var themeManager = AppUIThemeManager.Instance;
            if (themeManager != null && themeManager.CurrentTheme != null)
            {
                var vnTheme = themeManager.CurrentTheme.vn;
                if (IsConfiguredTint(vnTheme.ingredientSelectedColor))
                    selected = vnTheme.ingredientSelectedColor;
                if (IsConfiguredTint(vnTheme.ingredientPressedColor))
                    pressed = vnTheme.ingredientPressedColor;
                defaultText = ResolveThemeTint(vnTheme.ingredientDefaultTextColor, defaultText);
                pressedText = ResolveThemeTint(vnTheme.ingredientPressedTextColor, pressedText);
                selectedText = ResolveThemeTint(vnTheme.ingredientSelectedTextColor, selectedText);
                if (vnTheme.ingredientStateIndicatorSprite != null)
                    indicatorSprite = vnTheme.ingredientStateIndicatorSprite;
            }

            var colors = button.colors;
            colors.selectedColor = selected;
            colors.pressedColor = pressed;
            button.colors = colors;
            defaultTextColor = defaultText;
            pressedTextColor = pressedText;
            selectedTextColor = selectedText;
            if (stateIndicatorImage != null && indicatorSprite != null)
                stateIndicatorImage.sprite = indicatorSprite;
            if (button.image != null && !isPressed && !isSelected)
                defaultButtonSprite = button.image.sprite;

            RefreshStateVisual();
        }

        private static bool IsConfiguredTint(Color color)
        {
            return color.a > 0f;
        }

        private static Color ResolveThemeTint(Color themeColor, Color fallback)
        {
            return IsConfiguredTint(themeColor) ? themeColor : fallback;
        }

        private void RefreshStateVisual()
        {
            RefreshButtonVisual();

            if (label != null)
            {
                if (isPressed)
                    label.color = pressedTextColor;
                else if (isSelected)
                    label.color = selectedTextColor;
                else
                    label.color = defaultTextColor;
            }

            if (stateIndicatorImage != null)
                stateIndicatorImage.gameObject.SetActive(isPressed || isSelected);
        }

        private void RefreshButtonVisual()
        {
            if (button == null)
                return;

            var colors = button.colors;
            var targetGraphic = button.targetGraphic;
            if (targetGraphic != null)
            {
                Color graphicColor;
                if (!button.interactable)
                    graphicColor = colors.disabledColor;
                else if (isPressed)
                    graphicColor = colors.pressedColor;
                else if (isSelected)
                    graphicColor = colors.selectedColor;
                else
                    graphicColor = colors.normalColor;

                targetGraphic.color = graphicColor;
            }

            if (button.image == null)
                return;

            var spriteState = button.spriteState;
            if (!button.interactable)
            {
                if (spriteState.disabledSprite != null)
                    button.image.sprite = spriteState.disabledSprite;
                return;
            }

            if (isPressed && spriteState.pressedSprite != null)
            {
                button.image.sprite = spriteState.pressedSprite;
                return;
            }

            if (isSelected && spriteState.selectedSprite != null)
            {
                button.image.sprite = spriteState.selectedSprite;
                return;
            }

            if (defaultButtonSprite != null)
                button.image.sprite = defaultButtonSprite;
        }

    }
}
