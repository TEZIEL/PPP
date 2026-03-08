using UnityEngine;
using UnityEngine.UI;

namespace PPP.BLUE.VN
{
    public sealed class DrinkIngredientButton : MonoBehaviour
    {
        [SerializeField] private string ingredientId;

        private DrinkManager manager;
        private Button button;

        private void Awake()
        {
            manager = GetComponentInParent<DrinkManager>(true);
            button = GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(OnClick);
        }

        public void OnClick()
        {
            Debug.Log("Add Ingredient: " + ingredientId);
            if (manager == null)
            {
                manager = GetComponentInParent<DrinkManager>(true);
                if (manager == null)
                {
                    Debug.LogError("DrinkManager not found for ingredient button");
                    return;
                }
            }

            manager?.AddIngredientById(ingredientId);
        }
    }
}
