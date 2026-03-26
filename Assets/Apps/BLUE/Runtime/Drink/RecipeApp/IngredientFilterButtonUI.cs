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
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color selectedColor = new Color(0.65f, 0.95f, 1f, 1f);

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

        private void HandleClick()
        {
            if (string.IsNullOrWhiteSpace(ingredientId))
                return;

            onClicked?.Invoke(ingredientId);
        }

        private void RefreshVisual()
        {
            if (selectionBackground != null)
                selectionBackground.color = isSelected ? selectedColor : normalColor;
        }
    }
}
