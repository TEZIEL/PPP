using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PPP.BLUE.VN.DrinkSystem
{
    public sealed class IngredientButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private const string ArtheonIngredient = "INGREDIENT_ARTHEON";

        [SerializeField] private string ingredientID;
        [SerializeField] private DrinkManager manager;
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image stateTarget;
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private Color enabledColor = new Color(0.65f, 0.45f, 1f, 1f);

        public string IngredientID => ingredientID;

        private void Awake()
        {
            if (button != null)
                button.onClick.AddListener(OnClick);

            RefreshLabel(0);
            SetModifierState(false);
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

        public void SetModifierState(bool enabled)
        {
            if (!string.Equals(ingredientID, ArtheonIngredient, System.StringComparison.Ordinal))
                return;

            if (stateTarget != null)
                stateTarget.color = enabled ? enabledColor : defaultColor;

            if (label != null)
                label.text = $"ARTHEON {(enabled ? "ON" : "OFF")}";
        }

        public void RefreshLabel(int count)
        {
            if (label == null)
                return;

            if (string.Equals(ingredientID, ArtheonIngredient, System.StringComparison.Ordinal))
            {
                label.text = "ARTHEON OFF";
                return;
            }

            var displayId = string.IsNullOrEmpty(ingredientID) ? "UNKNOWN" : ingredientID.Replace("INGREDIENT_", string.Empty);
            label.text = displayId;
        }

        private void OnClick()
        {
            manager?.AddIngredient(ingredientID);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            manager?.SetIngredientHover(ingredientID, true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            manager?.SetIngredientHover(ingredientID, false);
        }
    }
}
