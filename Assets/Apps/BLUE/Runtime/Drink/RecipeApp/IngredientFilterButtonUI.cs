using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PPP.BLUE.VN.RecipeApp
{
    /// <summary>
    /// 재료 필터 버튼 1개를 담당.
    /// 선택 상태 표시와 클릭 이벤트 전달만 담당한다.
    /// </summary>
    public sealed class IngredientFilterButtonUI : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private Image selectionBackground;
        [SerializeField] private Sprite normalSprite;
        [SerializeField] private Sprite selectedSprite;

        private string ingredientId;
        private bool isSelected;
        private Action<string> onClicked;

        public string IngredientId => ingredientId;
        public bool IsSelected => isSelected;

        private void Awake()
        {
            if (button != null)
                button.onClick.AddListener(HandleClick);
        }

        public void Setup(IngredientEntry data, Action<string> clickHandler)
        {
            if (data == null)
                return;

            ingredientId = data.id;
            onClicked = clickHandler;

            if (labelText != null)
                labelText.text = data.DisplayName;

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
                button.interactable = interactable;
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
            if (selectionBackground != null)
                selectionBackground.sprite = isSelected ? selectedSprite : normalSprite;
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
    }
}
