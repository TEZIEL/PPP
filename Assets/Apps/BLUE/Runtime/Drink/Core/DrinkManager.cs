using System;
using System.Collections;
using System.Collections.Generic;
using PPP.BLUE.VN.DrinkSystem;
using TMPro;
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
        [SerializeField] private VNPolicyController policy;
        [SerializeField] private DrinkDatabaseLoader databaseLoader;
        [SerializeField] private DrinkPanelUI panelUI;
        [SerializeField] private GameObject panelToCloseOnProvide;

        [Header("UI")]
        [SerializeField] private Button provideButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private IngredientButton[] ingredientButtons;
        [SerializeField] private Selectable[] gridInteractables = Array.Empty<Selectable>();

        [Header("Confirm Dialog")]
        [SerializeField] private GameObject confirmPanel;
        [SerializeField] private TMP_Text confirmDrinkNameText;
        [SerializeField] private Button confirmRemakeButton;
        [SerializeField] private Button confirmProvideButton;

        [Header("Timing")]
        [SerializeField] private float resetCooldownSeconds = 0.2f;

        [Header("Request")]
        [SerializeField] private string currentRequestId;

        [Header("Debug")]
        [SerializeField] private bool debugDrinkSystem = true;

        private readonly Dictionary<string, int> currentIngredients = new Dictionary<string, int>(StringComparer.Ordinal);
        private int totalCount;

        private DrinkDatabase database;
        private DrinkRecipeValidator recipeValidator;
        private DrinkRequestEvaluator requestEvaluator;
        private DrinkRequest currentRequest;
        private bool artheonEnabled;
        private bool isResetInProgress;
        private bool isProvided;

        private string pendingResult = "FAIL";
        private string pendingNormalizedResult = "FAIL";
        private string pendingDrinkName = "Unknown Drink";
        private bool hasPendingProvide;
        private bool resultLocked;
        private bool confirmCompleted;

        private void Awake()
        {
            database = databaseLoader != null ? databaseLoader.LoadDatabase() : new DrinkDatabase();
            recipeValidator = new DrinkRecipeValidator(database);
            requestEvaluator = new DrinkRequestEvaluator(database);
            currentRequest = database.FindRequest(currentRequestId);

            if (policy == null)
                policy = GetComponentInParent<VNPolicyController>(true);

            if (provideButton != null)
                provideButton.onClick.AddListener(OnMakeDrink);

            if (resetButton != null)
                resetButton.onClick.AddListener(ResetIngredients);

            if (confirmRemakeButton != null)
                confirmRemakeButton.onClick.AddListener(OnRemakeDrink);

            if (confirmProvideButton != null)
                confirmProvideButton.onClick.AddListener(OnConfirmProvide);

            for (int i = 0; i < ingredientButtons.Length; i++)
            {
                if (ingredientButtons[i] != null)
                    ingredientButtons[i].BindManager(this);
            }

            HideConfirmPanel();
            panelUI?.ClearGridInstant();
            RefreshUi();
        }

        public void SetRequest(string requestId)
        {
            currentRequestId = requestId;
            currentRequest = database?.FindRequest(requestId);
        }

        public void HideConfirmPanel()
        {
            hasPendingProvide = false;
            confirmCompleted = false;
            if (confirmPanel != null)
                confirmPanel.SetActive(false);
        }

        public void AddIngredientById(string ingredientId)
        {
            AddIngredient(ingredientId);
        }

        public void AddIngredient(string ingredientID)
        {
            if (resultLocked)
                return;

            if (isResetInProgress || isProvided || IsConfirmOpen())
                return;

            if (string.IsNullOrEmpty(ingredientID))
                return;

            if (string.Equals(ingredientID, ArtheonIngredient, StringComparison.Ordinal))
            {
                ToggleArtheon();
                LogDrink($"modifier toggled artheon={artheonEnabled}");
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

            LogDrink($"ingredient added={ingredientID}");
            LogDrink($"total={totalCount} artheon={artheonEnabled}");

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

        public void ResetDrink()
        {
            ResetIngredients();
        }

        public string EvaluateCurrentDrink()
        {
            EvaluateCurrentDrinkInternal(out string result, out string normalized, out string drinkName);
            pendingResult = result;
            pendingNormalizedResult = normalized;
            pendingDrinkName = drinkName;
            hasPendingProvide = true;
            return result;
        }

        public string GetCurrentDrinkName()
        {
            return string.IsNullOrEmpty(pendingDrinkName) ? "Unknown Drink" : pendingDrinkName;
        }

        public void ProvideDrink()
        {
            OnMakeDrink();
        }

        public void OnMakeDrink()
        {
            if (resultLocked)
                return;

            if (isResetInProgress || isProvided || totalCount <= 0)
                return;

            string result = EvaluateCurrentDrink();
            if (confirmDrinkNameText != null)
                confirmDrinkNameText.text = GetCurrentDrinkName();

            if (confirmPanel != null)
                confirmPanel.SetActive(true);

            resultLocked = true;
            SetInteractionLocked(true);

            LogDrink("MakeDrink result=" + result);
        }

        public void OnRemakeDrink()
        {
            resultLocked = false;
            confirmCompleted = false;
            SetInteractionLocked(false);

            if (confirmPanel != null)
                confirmPanel.SetActive(false);

            ResetDrink();
            LogDrink("Remake requested");
        }

        public void OnConfirmProvide()
        {
            if (confirmCompleted)
                return;

            if (!hasPendingProvide)
                return;

            confirmCompleted = true;

            isProvided = true;
            SetAllIngredientButtonsInteractable(false);
            if (provideButton != null)
                provideButton.interactable = false;

            if (runner != null)
                runner.SetVar("lastDrink", MapResultToLastDrinkValue(pendingNormalizedResult));

            LogDrink("ConfirmProvide result=" + pendingResult);
            LogResult(pendingResult);
            Debug.Log("[VN_TEST] Drink result=" + pendingResult + " request=" + currentRequestId);

            policy?.ExitDrinkMode();
            policy?.PopModal("DrinkPanel");

            if (runner != null)
            {
                runner.ReturnFromCall(pendingResult);
                
            }
            else
            {
                Debug.LogError("[Drink] VNRunner reference missing");
            }

            if (confirmPanel != null)
                confirmPanel.SetActive(false);

            if (panelToCloseOnProvide != null)
                panelToCloseOnProvide.SetActive(false);
        }

        private void EvaluateCurrentDrinkInternal(out string result, out string normalizedResult, out string producedName)
        {
            LogRecipe("validation started");
            string drinkId = recipeValidator.ValidateRecipe(currentIngredients, artheonEnabled, out bool blockedByArtheon);
            if (string.IsNullOrEmpty(drinkId))
                LogRecipe("unknown");
            else
                LogRecipe($"matched drinkID={drinkId}");

            LogRequest(currentRequest);

            result = blockedByArtheon ? "FAIL" : requestEvaluator.Evaluate(drinkId, currentRequest);
            normalizedResult = NormalizeResultForRunner(result);

            var produced = database?.FindDrink(drinkId);
            producedName = produced != null ? produced.name : "Unknown Drink";
            if (artheonEnabled && produced != null && !string.IsNullOrEmpty(produced.name))
                producedName = WarmPrefix + produced.name;

            panelUI?.ShowResult(result, producedName);
        }

        private IEnumerator CoResetIngredients()
        {
            isResetInProgress = true;
            isProvided = false;
            hasPendingProvide = false;
            resultLocked = false;
            confirmCompleted = false;

            SetAllIngredientButtonsInteractable(false);
            if (provideButton != null)
                provideButton.interactable = false;
            if (resetButton != null)
                resetButton.interactable = false;

            if (confirmPanel != null)
                confirmPanel.SetActive(false);

            currentIngredients.Clear();
            totalCount = 0;
            artheonEnabled = false;
            LogDrink("reset started total=0 artheon=false");
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

        private static int MapResultToLastDrinkValue(string result)
        {
            if (string.Equals(result, "PERFECT", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(result, "GREAT", StringComparison.OrdinalIgnoreCase))
                return 1;

            if (string.Equals(result, "SUCCESS", StringComparison.OrdinalIgnoreCase))
                return 2;

            return 3;
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

            provideButton.interactable = !isResetInProgress && !isProvided && !IsConfirmOpen() && totalCount > 0;
        }

        private bool IsConfirmOpen()
        {
            return confirmPanel != null && confirmPanel.activeSelf;
        }

        private void LogDrink(string message)
        {
            if (!debugDrinkSystem) return;
            Debug.Log($"[Drink] {message}");
        }

        private void LogRecipe(string message)
        {
            if (!debugDrinkSystem) return;
            Debug.Log($"[Recipe] {message}");
        }

        private void LogRequest(DrinkRequest request)
        {
            if (!debugDrinkSystem) return;

            string requestId = request != null ? request.requestID : "(null)";
            string requestType = request != null ? request.type.ToString() : "(null)";
            string expectedDrink = request != null ? request.drinkID : string.Empty;
            string expectedTags = request != null && request.tags != null && request.tags.Count > 0
                ? string.Join(",", request.tags)
                : "(none)";

            Debug.Log($"[Request] requestID={requestId} requestType={requestType} expectedDrink={expectedDrink} expectedTags={expectedTags}");
        }

        private void LogResult(string result)
        {
            if (!debugDrinkSystem) return;
            Debug.Log($"[Result] {result}");
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

        public void SetInteractionLocked(bool locked)
        {
            bool interactable = !locked;

            SetAllIngredientButtonsInteractable(interactable);

            for (int i = 0; i < gridInteractables.Length; i++)
            {
                if (gridInteractables[i] != null)
                    gridInteractables[i].interactable = interactable;
            }

            if (provideButton != null)
                provideButton.interactable = interactable;

            if (resetButton != null)
                resetButton.interactable = interactable;

            if (confirmProvideButton != null)
                confirmProvideButton.interactable = locked;

            if (confirmRemakeButton != null)
                confirmRemakeButton.interactable = locked;
        }
    }
}
