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
            if (manager == null)
                manager = GetComponentInParent<DrinkManager>(true);

            manager?.AddIngredientById(ingredientId);
        }
    }
}
