using System;
using System.Collections;
using System.Collections.Generic;
using PPP.BLUE.VN.DrinkSystem;
using PPP.BLUE.VN.RecipeApp;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PPP.BLUE.VN
{
    public sealed class DrinkManager : MonoBehaviour
    {
        [Serializable]
        public sealed class DrinkRuntimeState
        {
            public string currentRequestId;
            public bool isActive;
        }

        [Serializable]
        private struct DrinkImageEntry
        {
            public string imageKey;
            public Sprite sprite;
        }

        private const int MaxIngredients = 16;
        private const string ArtheonIngredient = "INGREDIENT_ARTHEON";
        private const string WarmPrefix = "따뜻한 ";

        [Header("Runtime")]
        [SerializeField] private VNRunner runner;
        [SerializeField] private VNPolicyController policy;
        [SerializeField] private DrinkDatabaseLoader databaseLoader;
        [SerializeField] private DrinkPanelUI panelUI;
        [SerializeField] private GameObject panelToCloseOnProvide;
        [SerializeField] private DrinkPanel drinkPanel;

        [Header("UI")]
        [SerializeField] private Button provideButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private IngredientButton[] ingredientButtons;
        [SerializeField] private Selectable[] gridInteractables = Array.Empty<Selectable>();
        [SerializeField] private DrinkImageEntry[] drinkImageEntries = Array.Empty<DrinkImageEntry>();

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
        private readonly Dictionary<string, Sprite> drinkImageMap = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        private int totalCount;

        private DrinkDatabase database;
        private DrinkRecipeValidator recipeValidator;
        private DrinkRequestEvaluator requestEvaluator;
        private DrinkRequest currentRequest;
        private bool artheonEnabled;
        private bool isResetInProgress;
        private bool isProvided;
        private bool isDrinkSessionActive;

        private string pendingResult = "fail";
        private string pendingNormalizedResult = "fail";
        private string pendingDrinkName = "Unknown Drink";
        private string pendingDrinkId = string.Empty;
        private bool hasPendingProvide;
        private bool resultLocked;
        private bool confirmCompleted;
        private string hoveredIngredientId;
        private string selectedIngredientId;
        private int lastIngredientInputFrame = -1;

        private enum IngredientInputSource
        {
            Keyboard,
            Click
        }

        private void Awake()
        {
            database = databaseLoader != null ? databaseLoader.LoadDatabase() : new DrinkDatabase();
            recipeValidator = new DrinkRecipeValidator(database);
            requestEvaluator = new DrinkRequestEvaluator(database);
            currentRequest = database.FindRequest(currentRequestId);
            LogDrink($"request bind inputRequestId={currentRequestId} resolvedRequestId={(currentRequest != null ? currentRequest.requestID : "(null)")}");
            RebuildDrinkImageMap();

            if (policy == null)
                policy = GetComponentInParent<VNPolicyController>(true);

            if (provideButton != null)
                provideButton.onClick.AddListener(OnMakeDrink);

            if (resetButton != null)
                resetButton.onClick.AddListener(() => ResetIngredients(true));

            if (confirmRemakeButton != null)
                confirmRemakeButton.onClick.AddListener(OnRemakeDrink);

            if (confirmProvideButton != null)
                confirmProvideButton.onClick.AddListener(OnConfirmProvide);

            for (int i = 0; i < ingredientButtons.Length; i++)
            {
                if (ingredientButtons[i] != null)
                    ingredientButtons[i].BindManager(this);
            }
            SetSelectedIngredient(null);

            HideConfirmPanel();
            panelUI?.ClearGridInstant();
            panelUI?.ClearIngredientHoverInfo();
            RefreshUi();
        }




        private void Update()
        {
            HandleIngredientHotkeys();
            HandleDrinkActionHotkeys();
        }

        public void SetRequest(string requestId)
        {
            StartDrink(requestId);
        }

        public void StartDrink(string requestId)
        {
            Debug.Log("[TRACE 5] StartDrink called with " + requestId);
            currentRequestId = requestId;
            isDrinkSessionActive = true;
            Debug.Log("[TRACE 6] Before FindRequest id=" + requestId);
            currentRequest = database?.FindRequest(requestId);
            Debug.Log("[TRACE 7] After FindRequest result=" + (currentRequest != null ? currentRequest.requestID : "NULL"));
            Debug.Log("[Drink] StartDrink requestId=" + (requestId ?? string.Empty));
            LogDrink($"request set inputRequestId={currentRequestId} resolvedRequestId={(currentRequest != null ? currentRequest.requestID : "(null)")}");
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

        public void AddIngredientFromClick(string ingredientId)
        {
            TryAddIngredientFromInput(ingredientId, IngredientInputSource.Click);
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

        private void HandleIngredientHotkeys()
        {
            if (!CanUseIngredientHotkeys())
                return;

            if (Input.GetKeyDown(KeyCode.Q))
                TryAddIngredientFromInput("INGREDIENT_VELTRINE", IngredientInputSource.Keyboard);
            else if (Input.GetKeyDown(KeyCode.W))
                TryAddIngredientFromInput("INGREDIENT_ZYPHRATE", IngredientInputSource.Keyboard);
            else if (Input.GetKeyDown(KeyCode.E))
                TryAddIngredientFromInput("INGREDIENT_KRATYLEN", IngredientInputSource.Keyboard);
            else if (Input.GetKeyDown(KeyCode.R))
                TryAddIngredientFromInput("INGREDIENT_MORVION", IngredientInputSource.Keyboard);
            else if (Input.GetKeyDown(KeyCode.A))
                TryAddIngredientFromInput("INGREDIENT_REDULINE", IngredientInputSource.Keyboard);
            else if (Input.GetKeyDown(KeyCode.S))
                TryAddIngredientFromInput("INGREDIENT_CYMENTOL", IngredientInputSource.Keyboard);
            else if (Input.GetKeyDown(KeyCode.D))
                TryAddIngredientFromInput("INGREDIENT_BRAXIUM", IngredientInputSource.Keyboard);
            else if (Input.GetKeyDown(KeyCode.F))
                TryAddIngredientFromInput("INGREDIENT_ARTHEON", IngredientInputSource.Keyboard);
        }

        private bool CanUseIngredientHotkeys()
        {
            if (policy == null || !policy.CanAcceptVNInput())
                return false;

            if (!policy.IsInDrinkMode)
                return false;

            if (drinkPanel != null && !drinkPanel.IsOpenOrOpening)
                return false;

            if (resultLocked || IsConfirmOpen())
                return false;

            return true;
        }

        private void HandleDrinkActionHotkeys()
        {
            if (!CanUseDrinkActionHotkeys())
                return;

            if (Input.GetKeyDown(KeyCode.Z))
            {
                if (IsConfirmOpen())
                {
                    if (confirmProvideButton != null && confirmProvideButton.interactable)
                        OnConfirmProvide();
                }
                else
                {
                    if (provideButton != null && provideButton.interactable)
                        OnMakeDrink();
                }
            }
            else if (Input.GetKeyDown(KeyCode.X))
            {
                if (IsConfirmOpen())
                {
                    if (confirmRemakeButton != null && confirmRemakeButton.interactable)
                        OnRemakeDrink();
                }
                else
                {
                    if (resetButton != null && resetButton.interactable)
                        ResetIngredients(true);
                }
            }
        }

        private bool CanUseDrinkActionHotkeys()
        {
            if (policy == null || !policy.CanAcceptVNInput())
                return false;

            if (!policy.IsInDrinkMode)
                return false;

            if (drinkPanel != null && !drinkPanel.IsOpenOrOpening)
                return false;

            return true;
        }

        private void TryAddIngredientFromInput(string ingredientId, IngredientInputSource source)
        {
            if (string.IsNullOrEmpty(ingredientId))
                return;

            if (lastIngredientInputFrame == Time.frameCount)
                return;

            if (source == IngredientInputSource.Keyboard && !CanUseIngredientHotkeys())
                return;

            if (source == IngredientInputSource.Keyboard)
            {
                PlayHotkeyButtonFeedback(ingredientId);

                // 🔥 추가 (핵심)
                PlayIngredientSound(ingredientId);
            }

            SetSelectedIngredient(ingredientId);

            lastIngredientInputFrame = Time.frameCount;
            AddIngredient(ingredientId);
        }

        private void PlayIngredientSound(string ingredientID)
        {
            float pitch = GetPitchByIngredient(ingredientID);

            SoundManager.Instance.PlayOSWithPitch(OSSoundEvent.IngredientFill1, pitch);
        }

        private float GetPitchByIngredient(string ingredientID)
        {
            switch (ingredientID)
            {
                case "INGREDIENT_VELTRINE": return 1.000f;
                case "INGREDIENT_ZYPHRATE": return 1.122f;
                case "INGREDIENT_KRATYLEN": return 1.260f;
                case "INGREDIENT_MORVION": return 1.335f;
                case "INGREDIENT_REDULINE": return 1.498f;
                case "INGREDIENT_CYMENTOL": return 1.682f;
                case "INGREDIENT_BRAXIUM": return 1.888f;
                case "INGREDIENT_ARTHEON": return 2.000f;
                default: return 1f;
            }
        }

        private void PlayHotkeyButtonFeedback(string ingredientId)
        {
            for (int i = 0; i < ingredientButtons.Length; i++)
            {
                var ingredientButton = ingredientButtons[i];
                if (ingredientButton == null)
                    continue;

                if (!string.Equals(ingredientButton.IngredientID, ingredientId, StringComparison.Ordinal))
                    continue;

                ingredientButton.PlayHotkeyPressFeedback();
                return;
            }
        }

        public void SetIngredientHover(string ingredientId, bool isHovering)
        {
            hoveredIngredientId = isHovering ? ingredientId : null;
            UpdateHoveredIngredientInfo();
        }

        public void ResetIngredients(bool playSound = false)
        {
            if (playSound)
                SoundManager.Instance.PlayOS(OSSoundEvent.Retry);


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

            bool hasRecipe = pendingDrinkName != "Unknown Drink";

            if (hasRecipe)
            {
                SoundManager.Instance.PlayOS(OSSoundEvent.CraftSuccess);
            }
            else
            {
                SoundManager.Instance.PlayOS(OSSoundEvent.CraftFail);
            }

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
            SoundManager.Instance.PlayOS(OSSoundEvent.Retry); // ❌ 여기 문제


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
            Debug.Log("[DRINK COMPLETE CALLED]");

            if (pendingDrinkName == "Unknown Drink")
            {
                SoundManager.Instance.PlayOS(OSSoundEvent.CraftFailProvide); // 🔥 경고음

                Debug.Log("[Drink] Provide blocked (Unknown)");
                return;
            }

            if (confirmCompleted)
                return;

            if (!hasPendingProvide)
                return;

            confirmCompleted = true;

            SoundManager.Instance.PlayOS(OSSoundEvent.ProvideComplete); // 🔥 추가
            
            isProvided = true;
            isDrinkSessionActive = false;
            SetAllIngredientButtonsInteractable(false);

            if (provideButton != null)
                provideButton.interactable = false;

            if (runner != null)
                runner.SetVar("lastDrink", MapResultToLastDrinkValue(pendingNormalizedResult));

            RecipeAppController.UnlockRecipeFromServe(pendingDrinkId);

            LogDrink("ConfirmProvide result=" + pendingResult);
            LogResult(pendingResult);
            Debug.Log("[VN_TEST] Drink result=" + pendingResult + " request=" + currentRequestId);

            string returnResult = string.IsNullOrEmpty(pendingNormalizedResult) ? pendingResult : pendingNormalizedResult;
            Debug.Log($"[DRINK RESULT] {returnResult}");

            // 🔹 먼저 UI 정리
            drinkPanel?.Close();

            if (confirmPanel != null)
                confirmPanel.SetActive(false);

            // 🔹 마지막에 VN 복귀
            if (runner != null)
            {
                runner.ReturnFromCall(returnResult);
            }
            else
            {
                Debug.LogError("[Drink] VNRunner reference missing");
            }
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

            result = blockedByArtheon ? "fail" : requestEvaluator.Evaluate(drinkId, currentRequest, currentRequestId);
            normalizedResult = NormalizeResultForRunner(result);
            pendingDrinkId = drinkId ?? string.Empty;

            var produced = database?.FindDrink(drinkId);
            producedName = produced != null ? produced.name : "Unknown Drink";
            if (artheonEnabled && produced != null && !string.IsNullOrEmpty(produced.name))
                producedName = WarmPrefix + produced.name;

            bool isFailResult = string.Equals(normalizedResult, "fail", StringComparison.OrdinalIgnoreCase);
            panelUI?.ShowResult(result, producedName, ResolveDrinkSprite(produced), isFailResult);

            bool isUnknown = string.IsNullOrEmpty(drinkId);

            if (confirmProvideButton != null)
                confirmProvideButton.interactable = !isUnknown;
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
            SetSelectedIngredient(null);
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
            if (string.Equals(result, "great", StringComparison.OrdinalIgnoreCase))
                return 1;

            if (string.Equals(result, "success", StringComparison.OrdinalIgnoreCase))
                return 2;

            return 3;
        }

        private string NormalizeResultForRunner(string result)
        {
            if (string.Equals(result, "great", StringComparison.OrdinalIgnoreCase))
                return "great";
            if (string.Equals(result, "success", StringComparison.OrdinalIgnoreCase))
                return "success";
            return "fail";
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

            UpdateHoveredIngredientInfo();
        }

        private void ToggleArtheon()
        {
            artheonEnabled = !artheonEnabled;
            RefreshUi();
        }

        private void RebuildDrinkImageMap()
        {
            drinkImageMap.Clear();

            if (drinkImageEntries == null)
                return;

            for (int i = 0; i < drinkImageEntries.Length; i++)
            {
                var entry = drinkImageEntries[i];
                if (string.IsNullOrWhiteSpace(entry.imageKey) || entry.sprite == null)
                    continue;

                drinkImageMap[entry.imageKey.Trim()] = entry.sprite;
            }
        }

        private Sprite ResolveDrinkSprite(DrinkData drink)
        {
            if (drink == null || string.IsNullOrWhiteSpace(drink.imageKey))
                return null;

            return drinkImageMap.TryGetValue(drink.imageKey.Trim(), out var sprite) ? sprite : null;
        }

        private void UpdateProvideButtonState()
        {
            if (provideButton == null)
                return;

            provideButton.interactable = !isResetInProgress && !isProvided && !IsConfirmOpen() && totalCount > 0;
        }

        private void UpdateHoveredIngredientInfo()
        {
            if (panelUI == null)
                return;

            if (string.IsNullOrEmpty(hoveredIngredientId))
            {
                panelUI.ClearIngredientHoverInfo();
                return;
            }

            string displayName = GetIngredientDisplayName(hoveredIngredientId);
            string description = GetIngredientDescription(hoveredIngredientId);

            if (string.Equals(hoveredIngredientId, ArtheonIngredient, StringComparison.Ordinal))
            {
                panelUI.ShowIngredientHoverInfo($"{displayName} {(artheonEnabled ? "ON" : "OFF")}", description);
                return;
            }

            currentIngredients.TryGetValue(hoveredIngredientId, out int count);
            panelUI.ShowIngredientHoverInfo($"{displayName} x {count:00}", description);
        }

        private string GetIngredientDisplayName(string ingredientId)
        {
            var info = database?.FindIngredient(ingredientId);
            if (info != null && !string.IsNullOrWhiteSpace(info.displayName))
                return info.displayName;

            if (string.IsNullOrWhiteSpace(ingredientId))
                return "UNKNOWN";

            return ingredientId.Replace("INGREDIENT_", string.Empty);
        }

        private string GetIngredientDescription(string ingredientId)
        {
            var info = database?.FindIngredient(ingredientId);
            return info != null ? (info.description ?? string.Empty) : string.Empty;
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

        public void CancelDrinkSession()
        {
            LogDrink("Drink session cancelled");

            currentIngredients.Clear();
            totalCount = 0;
            SetSelectedIngredient(null);

            hasPendingProvide = false;
            resultLocked = false;
            confirmCompleted = false;
            isProvided = false;

            artheonEnabled = false;
            isDrinkSessionActive = false;

            HideConfirmPanel();
            panelUI?.ClearGridInstant();

            RefreshUi();

            if (runner != null)
                runner.ReturnFromCall("fail");
        }

        private void SetAllIngredientButtonsInteractable(bool interactable)
        {
            for (int i = 0; i < ingredientButtons.Length; i++)
            {
                if (ingredientButtons[i] != null)
                    ingredientButtons[i].SetInteractable(interactable);
            }
        }

        private void SetSelectedIngredient(string ingredientId)
        {
            selectedIngredientId = ingredientId;

            for (int i = 0; i < ingredientButtons.Length; i++)
            {
                var ingredientButton = ingredientButtons[i];
                if (ingredientButton == null)
                    continue;

                bool selected = !string.IsNullOrEmpty(selectedIngredientId)
                                && string.Equals(ingredientButton.IngredientID, selectedIngredientId, StringComparison.Ordinal);
                ingredientButton.SetSelectedState(selected);
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

        public DrinkRuntimeState ExportState()
        {
            return new DrinkRuntimeState
            {
                currentRequestId = currentRequestId,
                isActive = isDrinkSessionActive
            };
        }

        public void RestoreState(DrinkRuntimeState state)
        {
            if (state == null)
                return;

            currentRequestId = state.currentRequestId;
            currentRequest = database?.FindRequest(currentRequestId);
            isDrinkSessionActive = state.isActive;

            LogDrink($"state restored requestId={currentRequestId ?? string.Empty} active={isDrinkSessionActive}");
        }
    }
}
