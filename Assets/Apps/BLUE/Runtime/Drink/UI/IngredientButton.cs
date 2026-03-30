using TMPro;
using System.Collections;
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
        [SerializeField] private float hotkeyPressScale = 0.92f;
        [SerializeField] private float hotkeyPressDuration = 0.08f;

        public string IngredientID => ingredientID;
        private Coroutine hotkeyPressCo;

        private void Awake()
        {
            ApplyNavigationNone();

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
            {
                ApplyNavigationNone();
                button.interactable = interactable;
            }
        }

        public void SetModifierState(bool enabled)
        {
            if (!string.Equals(ingredientID, ArtheonIngredient, System.StringComparison.Ordinal))
                return;

            if (stateTarget != null)
                stateTarget.color = enabled ? enabledColor : defaultColor;

            if (label != null)
                label.text = "ARTHEON";
        }

        public void RefreshLabel(int count)
        {
            if (label == null)
                return;

            if (string.Equals(ingredientID, ArtheonIngredient, System.StringComparison.Ordinal))
            {
                label.text = "ARTHEON";
                return;
            }

            var displayId = string.IsNullOrEmpty(ingredientID) ? "UNKNOWN" : ingredientID.Replace("INGREDIENT_", string.Empty);
            label.text = displayId;
        }

        public void PlayHotkeyPressFeedback()
        {
            if (!isActiveAndEnabled || button == null || !button.IsInteractable())
                return;

            ApplyNavigationNone();
        }

        private void OnClick()
        {
            

            float pitch = GetPitchByIngredient();

            SoundManager.Instance.PlayOSWithPitch(OSSoundEvent.IngredientFill1, pitch);

            manager?.AddIngredientFromClick(ingredientID);
        }

        private float GetPitchByIngredient()
        {
            switch (ingredientID)
            {
                case "INGREDIENT_VELTRINE": return 1.000f; // C4
                case "INGREDIENT_ZYPHRATE": return 1.122f; // D4
                case "INGREDIENT_KRATYLEN": return 1.260f; // E4
                case "INGREDIENT_MORVION": return 1.335f; // F4
                case "INGREDIENT_REDULINE": return 1.498f; // G4
                case "INGREDIENT_CYMENTOL": return 1.682f; // A4
                case "INGREDIENT_BRAXIUM": return 1.888f; // B4
                case "INGREDIENT_ARTHEON": return 2.000f; // C5
                default: return 1f;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            manager?.SetIngredientHover(ingredientID, true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            manager?.SetIngredientHover(ingredientID, false);
        }

        private IEnumerator CoPlayHotkeyPressFeedback()
        {
            Vector3 originalScale = transform.localScale;
            Vector3 pressedScale = originalScale * Mathf.Max(0.1f, hotkeyPressScale);
            float duration = Mathf.Max(0.01f, hotkeyPressDuration);
            float half = duration * 0.5f;

            float t = 0f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / half);
                transform.localScale = Vector3.LerpUnclamped(originalScale, pressedScale, p);
                yield return null;
            }

            t = 0f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / half);
                transform.localScale = Vector3.LerpUnclamped(pressedScale, originalScale, p);
                yield return null;
            }

            transform.localScale = originalScale;
            hotkeyPressCo = null;
        }

        private void ApplyNavigationNone()
        {
            if (button == null)
                return;

            Navigation navigation = button.navigation;
            if (navigation.mode == Navigation.Mode.None)
                return;

            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
        }

    }
}
