using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PPP.BLUE.VN.DrinkSystem
{
    public sealed class DrinkPanelUI : MonoBehaviour
    {
        [Header("Texts")]
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private TMP_Text producedDrinkText;
        [SerializeField] private TMP_Text previewText;
        [SerializeField] private TMP_Text ingredientCountText;
        [SerializeField] private TMP_Text warningText;

        [Header("Grid (4x4)")]
        [SerializeField] private Image[] slotImages = new Image[16];
        [SerializeField] private Color emptyColor = new Color(1f, 1f, 1f, 0.1f);
        [SerializeField] private Color veltrineColor = new Color(0.95f, 0.5f, 0.75f, 1f);
        [SerializeField] private Color zyphrateColor = new Color(0.95f, 0.9f, 0.35f, 1f);
        [SerializeField] private Color kratylenColor = new Color(0.4f, 0.6f, 0.95f, 1f);
        [SerializeField] private Color morvionColor = new Color(0.35f, 0.35f, 0.35f, 1f);
        [SerializeField] private Color redulineColor = new Color(0.95f, 0.35f, 0.35f, 1f);
        [SerializeField] private Color cymentolColor = new Color(0.35f, 0.95f, 0.95f, 1f);
        [SerializeField] private Color braxiumColor = new Color(0.7f, 0.5f, 0.95f, 1f);
        [SerializeField] private Color artheonColor = new Color(0.9f, 0.8f, 1f, 1f);

        private Coroutine resetAnimation;

        public void ShowPrediction(string predictedName)
        {
            if (previewText != null)
                previewText.text = string.IsNullOrEmpty(predictedName) ? "Unknown Drink" : predictedName;
        }

        public void UpdateTotalCount(int current, int max)
        {
            if (ingredientCountText != null)
                ingredientCountText.text = $"{current} / {max}";
        }

        public void ShowResult(string result, string drinkName)
        {
            if (resultText != null)
                resultText.text = result;

            if (producedDrinkText != null)
                producedDrinkText.text = string.IsNullOrEmpty(drinkName) ? "Unknown Drink" : drinkName;
        }

        public void SetWarningVisible(bool visible)
        {
            if (warningText != null)
                warningText.gameObject.SetActive(visible);
        }

        public void FillNextSlot(int index, string ingredientId)
        {
            if (index < 0 || index >= slotImages.Length || slotImages[index] == null)
                return;

            slotImages[index].color = GetIngredientColor(ingredientId);
        }

        public void ClearGridInstant()
        {
            if (resetAnimation != null)
            {
                StopCoroutine(resetAnimation);
                resetAnimation = null;
            }

            for (int i = 0; i < slotImages.Length; i++)
            {
                if (slotImages[i] != null)
                    slotImages[i].color = emptyColor;
            }
        }

        public void ClearGridAnimated(MonoBehaviour owner)
        {
            if (owner == null)
            {
                ClearGridInstant();
                return;
            }

            if (resetAnimation != null)
                owner.StopCoroutine(resetAnimation);

            resetAnimation = owner.StartCoroutine(CoClearGridAnimated());
        }

        private IEnumerator CoClearGridAnimated()
        {
            for (int i = slotImages.Length - 1; i >= 0; i--)
            {
                if (slotImages[i] != null)
                    slotImages[i].color = emptyColor;
                yield return new WaitForSeconds(0.025f);
            }

            resetAnimation = null;
        }

        private Color GetIngredientColor(string ingredientId)
        {
            switch (ingredientId)
            {
                case "INGREDIENT_VELTRINE": return veltrineColor;
                case "INGREDIENT_ZYPHRATE": return zyphrateColor;
                case "INGREDIENT_KRATYLEN": return kratylenColor;
                case "INGREDIENT_MORVION": return morvionColor;
                case "INGREDIENT_REDULINE": return redulineColor;
                case "INGREDIENT_CYMENTOL": return cymentolColor;
                case "INGREDIENT_BRAXIUM": return braxiumColor;
                case "INGREDIENT_ARTHEON": return artheonColor;
                default: return Color.white;
            }
        }
    }
}
