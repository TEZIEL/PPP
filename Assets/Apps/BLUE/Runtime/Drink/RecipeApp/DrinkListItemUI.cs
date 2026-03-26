using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PPP.BLUE.VN.RecipeApp
{
    /// <summary>
    /// 스크롤 리스트의 음료 아이템 1개를 담당.
    /// </summary>
    public sealed class DrinkListItemUI : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text subText;
        [SerializeField] private Image thumbnail;

        private DrinkEntry current;
        private Action<DrinkEntry> onClicked;

        private void Awake()
        {
            if (button != null)
                button.onClick.AddListener(HandleClick);
        }

        public void Setup(DrinkEntry drink, Sprite image, Action<DrinkEntry> clickHandler)
        {
            current = drink;
            onClicked = clickHandler;

            if (nameText != null)
                nameText.text = drink?.name ?? "(이름 없음)";

            if (subText != null)
                subText.text = drink?.category ?? string.Empty;

            if (thumbnail != null)
            {
                thumbnail.sprite = image;
                thumbnail.enabled = image != null;
            }
        }

        private void HandleClick()
        {
            if (current == null)
                return;

            onClicked?.Invoke(current);
        }
    }
}
