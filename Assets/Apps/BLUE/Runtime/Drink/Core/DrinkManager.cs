using System;
using System.Collections;
using System.Collections.Generic;
using PPP.BLUE.VN.DrinkSystem;
using UnityEngine;
using UnityEngine.UI;

namespace PPP.BLUE.VN
{
    public sealed class DrinkManager : MonoBehaviour
    {
        private const int MaxIngredients = 16;
        private const string ArtheonIngredient = "INGREDIENT_ARTHEON";
        private const string WarmPrefix = "따뜻한 ";

        [Header("Runtime")]
        [SerializeField] private VNRunner runner;
        [SerializeField] private DrinkDatabaseLoader databaseLoader;
        [SerializeField] private DrinkPanelUI panelUI;

        [Header("UI")]
        [SerializeField] private Button provideButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private IngredientButton[] ingredientButtons;

        [Header("Timing")]
        [SerializeField] private float resetCooldownSeconds = 0.2f;

        [Header("Request")]
        [SerializeField] private string currentRequestId;

        private readonly Dictionary<string, int> currentIngredients = new Dictionary<string, int>(StringComparer.Ordinal);
        private int totalCount;

        private DrinkDatabase database;
        private DrinkRecipeValidator recipeValidator;
        private DrinkRequestEvaluator requestEvaluator;
        private DrinkRequest currentRequest;
        private bool artheonEnabled;
        private bool isResetInProgress;
        private bool isProvided;

        private void Awake()
        {
            database = databaseLoader != null ? databaseLoader.LoadDatabase() : new DrinkDatabase();
            recipeValidator = new DrinkRecipeValidator(database);
            requestEvaluator = new DrinkRequestEvaluator(database);
            currentRequest = database.FindRequest(currentRequestId);

            if (provideButton != null)
                provideButton.onClick.AddListener(ProvideDrink);

            if (resetButton != null)
                resetButton.onClick.AddListener(ResetIngredients);

            for (int i = 0; i < ingredientButtons.Length; i++)
            {
                if (ingredientButtons[i] != null)
                    ingredientButtons[i].BindManager(this);
            }

            panelUI?.ClearGridInstant();
            RefreshUi();
        }

        public void SetRequest(string requestId)
        {
            currentRequestId = requestId;
            currentRequest = database?.FindRequest(requestId);
        }

        public void AddIngredient(string ingredientID)
        {
            if (isResetInProgress || isProvided)
                return;

            if (string.IsNullOrEmpty(ingredientID))
                return;

            if (string.Equals(ingredientID, ArtheonIngredient, StringComparison.Ordinal))
            {
                ToggleArtheon();
                return;
            }

            if (totalCount >= MaxIngredients)
            {
                SetIngredientButtonsInteractable(false);
                return;
            }

            if (!currentIngredients.ContainsKey(ingredientID))
                currentIngredients[ingredientID] = 0;

            currentIngredients[ingredientID] += 1;
            panelUI?.FillNextSlot(totalCount, ingredientID);
            totalCount++;

            RefreshUi();

            if (totalCount >= MaxIngredients)
                SetIngredientButtonsInteractable(false);
        }

        public void ResetIngredients()
        {
            if (isResetInProgress)
                return;

            StartCoroutine(CoResetIngredients());
        }

        public void ProvideDrink()
        {
            if (isResetInProgress || isProvided || totalCount <= 0)
                return;

            isProvided = true;
            SetAllIngredientButtonsInteractable(false);
            if (provideButton != null)
                provideButton.interactable = false;

            string drinkId = recipeValidator.ValidateRecipe(currentIngredients, artheonEnabled, out bool blockedByArtheon);
            string result = blockedByArtheon ? "FAIL" : requestEvaluator.Evaluate(drinkId, currentRequest);
            string normalized = NormalizeResultForRunner(result);

            var produced = database?.FindDrink(drinkId);
            string producedName = produced != null ? produced.name : "Unknown Drink";
            if (artheonEnabled && produced != null && !string.IsNullOrEmpty(produced.name))
                producedName = WarmPrefix + produced.name;

            panelUI?.ShowResult(result, producedName);
            runner?.ReturnFromCall(normalized);
        }

        private IEnumerator CoResetIngredients()
        {
            isResetInProgress = true;
            isProvided = false;

            SetAllIngredientButtonsInteractable(false);
            if (provideButton != null)
                provideButton.interactable = false;
            if (resetButton != null)
                resetButton.interactable = false;

            currentIngredients.Clear();
            totalCount = 0;
            artheonEnabled = false;
            panelUI?.ClearGridAnimated(this);
            RefreshUi();

            float animationDuration = panelUI != null ? panelUI.GetResetAnimationDuration() : 0f;
            if (animationDuration > 0f)
                yield return new WaitForSeconds(animationDuration);

            if (resetCooldownSeconds > 0f)
                yield return new WaitForSeconds(resetCooldownSeconds);

            SetAllIngredientButtonsInteractable(true);
            if (resetButton != null)
                resetButton.interactable = true;

            isResetInProgress = false;
            UpdateProvideButtonState();
        }

        private string NormalizeResultForRunner(string result)
        {
            if (string.Equals(result, "PERFECT", StringComparison.OrdinalIgnoreCase))
                return "PERFECT";
            if (string.Equals(result, "SUCCESS", StringComparison.OrdinalIgnoreCase))
                return "SUCCESS";
            return "FAIL";
        }

        private void RefreshUi()
        {
            panelUI?.UpdateTotalCount(totalCount, MaxIngredients);
            UpdateProvideButtonState();

            for (int i = 0; i < ingredientButtons.Length; i++)
            {
                if (ingredientButtons[i] == null)
                    continue;

                string ingredientId = ingredientButtons[i].IngredientID;
                if (string.Equals(ingredientId, ArtheonIngredient, StringComparison.Ordinal))
                {
                    ingredientButtons[i].SetModifierState(artheonEnabled);
                    continue;
                }

                currentIngredients.TryGetValue(ingredientId, out int count);
                ingredientButtons[i].RefreshLabel(count);
            }
        }

        private void ToggleArtheon()
        {
            artheonEnabled = !artheonEnabled;
            RefreshUi();
        }

        private void UpdateProvideButtonState()
        {
            if (provideButton == null)
                return;

            provideButton.interactable = !isResetInProgress && !isProvided && totalCount > 0;
        }

        private void SetIngredientButtonsInteractable(bool interactable)
        {
            for (int i = 0; i < ingredientButtons.Length; i++)
            {
                if (ingredientButtons[i] == null)
                    continue;

                if (string.Equals(ingredientButtons[i].IngredientID, ArtheonIngredient, StringComparison.Ordinal))
                    continue;

                ingredientButtons[i].SetInteractable(interactable);
            }
        }

        private void SetAllIngredientButtonsInteractable(bool interactable)
        {
            for (int i = 0; i < ingredientButtons.Length; i++)
            {
                if (ingredientButtons[i] != null)
                    ingredientButtons[i].SetInteractable(interactable);
            }
        }
    }
}
