using System.Collections;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PPP.BLUE.VN.DrinkSystem
{
    public sealed class DrinkPanelUI : MonoBehaviour
    {
        [Serializable]
        private struct IngredientSlotVisual
        {
            public string ingredientId;
            public Sprite filledSprite;
            public Sprite selectedSprite;
        }

        private enum SlotSpriteState
        {
            Empty = 0,
            Filled = 1,
            Selected = 2,
        }

        [Header("Texts")]
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private TMP_Text producedDrinkText;
        [SerializeField] private TMP_Text ingredientCountText;
        [SerializeField] private TMP_Text ingredientHoverTitleText;
        [SerializeField] private TMP_Text ingredientHoverDescriptionText;

        [Header("Result Image")]
        [SerializeField] private Image producedDrinkImage;
        [SerializeField] private Sprite unknownDrinkSprite;
        [SerializeField] private Sprite failDrinkSprite;
        [SerializeField] private bool preferProducedDrinkSpriteOnFail = true;

        [Header("Grid (4x4)")]
        [SerializeField] private Image[] slotImages = new Image[16];
        [SerializeField] private Sprite emptySlotSprite;
        [SerializeField] private IngredientSlotVisual[] ingredientSlotVisuals;
        [SerializeField] private Color emptyColor = new Color(1f, 1f, 1f, 0.1f);
        [SerializeField] private Color veltrineColor = new Color(0.95f, 0.5f, 0.75f, 1f);
        [SerializeField] private Color zyphrateColor = new Color(0.95f, 0.9f, 0.35f, 1f);
        [SerializeField] private Color kratylenColor = new Color(0.4f, 0.6f, 0.95f, 1f);
        [SerializeField] private Color morvionColor = new Color(0.35f, 0.35f, 0.35f, 1f);
        [SerializeField] private Color redulineColor = new Color(0.95f, 0.35f, 0.35f, 1f);
        [SerializeField] private Color cymentolColor = new Color(0.35f, 0.95f, 0.95f, 1f);
        [SerializeField] private Color braxiumColor = new Color(0.7f, 0.5f, 0.95f, 1f);
        [SerializeField] private Color artheonColor = new Color(0.9f, 0.8f, 1f, 1f);
        [SerializeField] private float slotClearDelaySeconds = 0.025f;

        private Coroutine resetAnimation;
        private readonly Dictionary<string, IngredientSlotVisual> slotVisualByIngredient = new Dictionary<string, IngredientSlotVisual>(StringComparer.Ordinal);

        private void Awake()
        {
            RebuildVisualMap();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            RebuildVisualMap();
        }
#endif

        public void UpdateTotalCount(int current, int max)
        {
            if (ingredientCountText != null)
                ingredientCountText.text = $"{current} / {max}";
        }

        public void ShowIngredientHoverInfo(string title, string description)
        {
            if (ingredientHoverTitleText != null)
                ingredientHoverTitleText.text = title ?? string.Empty;

            if (ingredientHoverDescriptionText != null)
                ingredientHoverDescriptionText.text = description ?? string.Empty;
        }

        public void ClearIngredientHoverInfo()
        {
            ShowIngredientHoverInfo(string.Empty, string.Empty);
        }

        public void ShowResult(string result, string drinkName, Sprite drinkSprite = null, bool isFailResult = false)
        {
            if (resultText != null)
                resultText.text = result;

            if (producedDrinkText != null)
                producedDrinkText.text = string.IsNullOrEmpty(drinkName) ? "Unknown Drink" : drinkName;

            if (producedDrinkImage != null)
            {
                Sprite targetSprite = null;
                if (isFailResult)
                {
                    if (preferProducedDrinkSpriteOnFail && drinkSprite != null)
                        targetSprite = drinkSprite;
                    else if (failDrinkSprite != null)
                        targetSprite = failDrinkSprite;
                    else if (drinkSprite != null)
                        targetSprite = drinkSprite;
                }
                else
                {
                    targetSprite = drinkSprite;
                }

                if (targetSprite == null)
                    targetSprite = unknownDrinkSprite;

                producedDrinkImage.sprite = targetSprite;
                producedDrinkImage.enabled = targetSprite != null;
            }
        }

        public float GetResetAnimationDuration()
        {
            return slotImages.Length * Mathf.Max(0f, slotClearDelaySeconds);
        }

        public void FillNextSlot(int index, string ingredientId)
        {
            if (index < 0 || index >= slotImages.Length || slotImages[index] == null)
                return;

            if (TryGetSlotSprite(ingredientId, SlotSpriteState.Filled, out var filledSprite))
            {
                slotImages[index].sprite = filledSprite;
                slotImages[index].color = Color.white;
                return;
            }

            slotImages[index].color = GetIngredientColor(ingredientId);
        }

        public void SetSlotSelected(int index, string ingredientId, bool selected)
        {
            if (index < 0 || index >= slotImages.Length || slotImages[index] == null)
                return;

            if (!selected)
            {
                FillNextSlot(index, ingredientId);
                return;
            }

            if (TryGetSlotSprite(ingredientId, SlotSpriteState.Selected, out var selectedSprite))
            {
                slotImages[index].sprite = selectedSprite;
                slotImages[index].color = Color.white;
                return;
            }

            FillNextSlot(index, ingredientId);
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
                    SetEmptyVisual(slotImages[i]);
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
                    SetEmptyVisual(slotImages[i]);
                yield return new WaitForSeconds(slotClearDelaySeconds);
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

        private void RebuildVisualMap()
        {
            slotVisualByIngredient.Clear();

            if (ingredientSlotVisuals == null)
                return;

            for (int i = 0; i < ingredientSlotVisuals.Length; i++)
            {
                var visual = ingredientSlotVisuals[i];
                if (string.IsNullOrEmpty(visual.ingredientId) || visual.filledSprite == null)
                    continue;

                slotVisualByIngredient[visual.ingredientId] = visual;
            }
        }

        private bool TryGetSlotSprite(string ingredientId, SlotSpriteState state, out Sprite sprite)
        {
            if (string.IsNullOrEmpty(ingredientId))
            {
                sprite = null;
                return false;
            }

            if (!slotVisualByIngredient.TryGetValue(ingredientId, out var visual))
            {
                sprite = null;
                return false;
            }

            sprite = state switch
            {
                SlotSpriteState.Selected => visual.selectedSprite != null ? visual.selectedSprite : visual.filledSprite,
                SlotSpriteState.Filled => visual.filledSprite,
                _ => null
            };

            return sprite != null;
        }

        private void SetEmptyVisual(Image slot)
        {
            if (slot == null)
                return;

            if (emptySlotSprite != null)
            {
                slot.sprite = emptySlotSprite;
                slot.color = Color.white;
                return;
            }

            slot.color = emptyColor;
        }
    }
}
