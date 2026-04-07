using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace PPP.BLUE.VN.RecipeApp
{
    /// <summary>
    /// 재료 필터 버튼 1개를 담당.
    /// 선택 상태 표시와 클릭 이벤트 전달만 담당한다.
    /// </summary>
    public sealed class IngredientFilterButtonUI : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler

    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private Image selectionBackground;
        [SerializeField] private Image stateIndicatorImage;
        [SerializeField] private Sprite normalSprite;
        [SerializeField] private Sprite selectedSprite;

        [SerializeField] private Color normalColor = new Color32(255, 255, 255, 255);
        [SerializeField] private Color selectedColor = new Color32(255, 255, 255, 255);
        [SerializeField] private Color highlightedColor = new Color32(230, 230, 230, 255);
        [SerializeField] private Color pressedColor = new Color32(200, 200, 200, 255);
        [SerializeField] private Color disabledColor = new Color32(200, 200, 200, 128);
        [SerializeField] private Color defaultTextColor = Color.white;
        [SerializeField] private Color pressedTextColor = Color.white;
        [SerializeField] private Color selectedTextColor = Color.white;

        private string ingredientId;
        private bool isSelected;
        private bool isPressed;
        private Action<string> onClicked;

        public string IngredientId => ingredientId;
        public bool IsSelected => isSelected;

        private void Awake()
        {
            ApplyNavigationNone();
            ApplyCurrentTheme();

            if (button != null)
                button.onClick.AddListener(HandleClick);
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

        public void Setup(IngredientEntry data, Action<string> clickHandler)
        {
            if (data == null)
                return;

            ingredientId = data.id;
            onClicked = clickHandler;
            ApplyNavigationNone();

            if (labelText != null)
            {
                labelText.richText = true;
                labelText.text = RecipeIngredientTextFormatter.FormatIngredientDisplayName(data.DisplayName, data.DisplayColorHex);
            }

            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;
            RefreshVisual();
        }

        public void SetInteractable(bool interactable)
        {
            if (button != null)
            {
                ApplyNavigationNone();
                button.interactable = interactable;
            }

            RefreshVisual();
        }

        public void ApplyThemeSprites(Sprite normal, Sprite selected, Sprite selectionBackgroundSprite = null)
        {
            if (normal != null)
                normalSprite = normal;

            if (selected != null)
                selectedSprite = selected;

            if (selectionBackground != null && selectionBackgroundSprite != null)
                selectionBackground.sprite = selectionBackgroundSprite;

            NormalizeButtonTransition();
            RefreshVisual();
        }


        private void HandleClick()
        {
            if (string.IsNullOrWhiteSpace(ingredientId))
                return;

           
            onClicked?.Invoke(ingredientId);
        }

        private void RefreshVisual()
        {
            if (selectionBackground == null)
            {
                RefreshStateIndicator();
                RefreshLabelColor();
                return;
            }

            selectionBackground.sprite = isSelected ? selectedSprite : normalSprite;

            bool interactable = button == null || button.interactable;

            if (!interactable)
                selectionBackground.color = disabledColor;
            else if (isPressed)
                selectionBackground.color = pressedColor;
            else if (isSelected)
                selectionBackground.color = selectedColor;
            else
                selectionBackground.color = normalColor;

            RefreshStateIndicator();
            RefreshLabelColor();
        }


        public void OnPointerEnter(PointerEventData eventData)
        {
            if (button == null || !button.interactable || selectionBackground == null) return;
            if (isPressed) return;
            selectionBackground.color = highlightedColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            RefreshVisual();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (button == null || !button.interactable) return;
            isPressed = true;
            RefreshVisual();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (button == null || !button.interactable) return;

            isPressed = false;
            RefreshVisual();
        }

        private void NormalizeButtonTransition()
        {
            if (button == null)
                return;

            button.transition = Selectable.Transition.None;

            var c = button.colors;
            c.normalColor = Color.white;
            c.highlightedColor = Color.white;
            c.pressedColor = Color.white;
            c.selectedColor = Color.white;
            c.disabledColor = Color.white;
            button.colors = c;
        }

        private void ApplyNavigationNone()
        {
            if (button == null)
                return;

            var navigation = button.navigation;
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
            Color selected = selectedColor;
            Color pressed = pressedColor;
            Color defaultText = defaultTextColor;
            Color pressedText = pressedTextColor;
            Color selectedText = selectedTextColor;
            Sprite indicatorSprite = stateIndicatorImage != null ? stateIndicatorImage.sprite : null;

            var themeManager = AppUIThemeManager.Instance;
            if (themeManager != null && themeManager.CurrentTheme != null)
            {
                var blueprintTheme = themeManager.CurrentTheme.blueprint;
                if (IsConfiguredTint(blueprintTheme.ingredientSelectedColor))
                    selected = blueprintTheme.ingredientSelectedColor;
                if (IsConfiguredTint(blueprintTheme.ingredientPressedColor))
                    pressed = blueprintTheme.ingredientPressedColor;
                if (IsConfiguredTint(blueprintTheme.ingredientDefaultTextColor))
                    defaultText = blueprintTheme.ingredientDefaultTextColor;
                if (IsConfiguredTint(blueprintTheme.ingredientPressedTextColor))
                    pressedText = blueprintTheme.ingredientPressedTextColor;
                if (IsConfiguredTint(blueprintTheme.ingredientSelectedTextColor))
                    selectedText = blueprintTheme.ingredientSelectedTextColor;
                if (blueprintTheme.ingredientStateIndicatorSprite != null)
                    indicatorSprite = blueprintTheme.ingredientStateIndicatorSprite;
            }

            selectedColor = selected;
            pressedColor = pressed;
            defaultTextColor = defaultText;
            pressedTextColor = pressedText;
            selectedTextColor = selectedText;
            if (stateIndicatorImage != null && indicatorSprite != null)
                stateIndicatorImage.sprite = indicatorSprite;
            RefreshVisual();
        }

        private static bool IsConfiguredTint(Color color)
        {
            return color.a > 0f;
        }

        private void RefreshLabelColor()
        {
            if (labelText == null)
                return;

            if (isPressed)
                labelText.color = pressedTextColor;
            else if (isSelected)
                labelText.color = selectedTextColor;
            else
                labelText.color = defaultTextColor;
        }

        private void RefreshStateIndicator()
        {
            if (stateIndicatorImage == null)
                return;

            stateIndicatorImage.gameObject.SetActive(isPressed || isSelected);
        }

       
    }
}
