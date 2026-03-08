using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PPP.BLUE.VN.DrinkSystem
{
    public sealed class IngredientButton : MonoBehaviour
    {
        [SerializeField] private string ingredientID;
        [SerializeField] private DrinkManager manager;
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;

        public string IngredientID => ingredientID;

        private void Awake()
        {
            if (button != null)
                button.onClick.AddListener(OnClick);
            RefreshLabel(0);
        }

        public void BindManager(DrinkManager target)
        {
            manager = target;
        }

        public void SetInteractable(bool interactable)
        {
            if (button != null)
                button.interactable = interactable;
        }

        public void RefreshLabel(int count)
        {
            if (label == null)
                return;

            var displayId = string.IsNullOrEmpty(ingredientID) ? "UNKNOWN" : ingredientID.Replace("INGREDIENT_", string.Empty);
            label.text = $"{displayId} x{count}";
        }

        private void OnClick()
        {
            manager?.AddIngredient(ingredientID);
        }
    }
}
