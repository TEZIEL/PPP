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
        [SerializeField] private Sprite normalSprite;
        [SerializeField] private Sprite selectedSprite;

        [SerializeField] private Color normalColor = new Color32(255, 255, 255, 255);
        [SerializeField] private Color highlightedColor = new Color32(230, 230, 230, 255);
        [SerializeField] private Color pressedColor = new Color32(200, 200, 200, 255);
        [SerializeField] private Color disabledColor = new Color32(200, 200, 200, 128);

        private string ingredientId;
        private bool isSelected;
        private Action<string> onClicked;

        public string IngredientId => ingredientId;
        public bool IsSelected => isSelected;

        private void Awake()
        {
            ApplyNavigationNone();

            if (button != null)
                button.onClick.AddListener(HandleClick);
        }

        public void Setup(IngredientEntry data, Action<string> clickHandler)
        {
            if (data == null)
                return;

            ingredientId = data.id;
            onClicked = clickHandler;
            ApplyNavigationNone();

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

            ClearUiSelectionIfSelfSelected();
            onClicked?.Invoke(ingredientId);
        }

        private void RefreshVisual()
        {
            if (selectionBackground == null)
                return;

            selectionBackground.sprite = isSelected ? selectedSprite : normalSprite;

            bool interactable = button == null || button.interactable;

            if (!interactable)
                selectionBackground.color = disabledColor;
            else
                selectionBackground.color = normalColor;
        }


        public void OnPointerEnter(PointerEventData eventData)
        {
            if (button == null || !button.interactable || selectionBackground == null) return;
            selectionBackground.color = highlightedColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            RefreshVisual();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (button == null || !button.interactable || selectionBackground == null) return;
            selectionBackground.color = pressedColor;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!button.interactable) return;

            // 클릭 후 상태 복구
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

        private void ClearUiSelectionIfSelfSelected()
        {
            if (button == null)
                return;

            var eventSystem = EventSystem.current;
            if (eventSystem == null)
                return;

            if (eventSystem.currentSelectedGameObject == button.gameObject)
                eventSystem.SetSelectedGameObject(null);
        }

       
    }
}
