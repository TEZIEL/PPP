using System;
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

        [Header("Runtime")]
        [SerializeField] private VNRunner runner;
        [SerializeField] private DrinkDatabaseLoader databaseLoader;
        [SerializeField] private DrinkFailTracker failTracker;
        [SerializeField] private DrinkPanelUI panelUI;

        [Header("UI")]
        [SerializeField] private Button provideButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private IngredientButton[] ingredientButtons;

        [Header("Request")]
        [SerializeField] private string currentRequestId;

        private readonly Dictionary<string, int> currentIngredients = new Dictionary<string, int>(StringComparer.Ordinal);
        private int totalCount;

        private DrinkDatabase database;
        private DrinkRecipeValidator recipeValidator;
        private DrinkRequestEvaluator requestEvaluator;
        private DrinkRequest currentRequest;
        private bool artheonEnabled;

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
            currentIngredients.Clear();
            totalCount = 0;
            artheonEnabled = false;
            panelUI?.ClearGridAnimated(this);
            SetIngredientButtonsInteractable(true);
            panelUI?.SetWarningVisible(false);
            RefreshUi();
        }

        public void ProvideDrink()
        {
            string drinkId = recipeValidator.ValidateRecipe(currentIngredients, artheonEnabled, out bool blockedByArtheon);
            string result = blockedByArtheon ? "FAIL" : requestEvaluator.Evaluate(drinkId, currentRequest);
            string normalized = NormalizeResultForRunner(result);

            var produced = database?.FindDrink(drinkId);
            string producedName = produced != null ? produced.name : "Unknown Drink";
            panelUI?.ShowResult(result, producedName);

            bool showWarning = failTracker != null && failTracker.RegisterResult(result);
            panelUI?.SetWarningVisible(showWarning);

            runner?.ReturnFromCall(normalized);
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
    }
}
