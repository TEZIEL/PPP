using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections;

namespace PPP.BLUE.VN
{
    public sealed class DrinkPanel : MonoBehaviour
    {
        public const string IngredientWindowId = "DrinkIngredients";
        public const string GridWindowId = "DrinkGrid";
        private const string LegacyIngredientWindowId = "drink_ingredient";
        private const string LegacyGridWindowId = "drink_grid";

        [Header("Refs")]
        [SerializeField] private GameObject root;
        [SerializeField] private VNRunner runner;
        [SerializeField] private VNPolicyController policy;
        [SerializeField] private WindowShortcutController shortcutController;
        [SerializeField] private WindowManager windowManager;
        [SerializeField] private DrinkManager drinkManager;
        [SerializeField] private RectTransform ingredientPanelRoot;
        [SerializeField] private RectTransform drinkGridRoot;
        [SerializeField] private RectTransform vnContentRoot;

        [Header("Ingredient/Grid Z-Order")]
        [SerializeField] private bool enableWeakZOrder = true;
        [SerializeField] private bool applyInitialSiblingOrder = true;
        [SerializeField] private int ingredientPanelInitialSiblingIndex = 1;
        [SerializeField] private int drinkGridInitialSiblingIndex = 0;

        private Coroutine openCo;
        private bool isOpen;
        private bool isOpening;
        private bool callSubscribed;
        private readonly System.Collections.Generic.List<VNWindowStateData> cachedWindowStates = new System.Collections.Generic.List<VNWindowStateData>();

        public bool IsOpenOrOpening => isOpen || isOpening;

        private void Awake()
        {
            EnsureClampedDragMove(ingredientPanelRoot, vnContentRoot, ingredientPanelInitialSiblingIndex);
            EnsureClampedDragMove(drinkGridRoot, vnContentRoot, drinkGridInitialSiblingIndex);

            if (root != null)
                root.SetActive(false);
        }

        private void EnsureClampedDragMove(RectTransform target, RectTransform parentArea, int initialSiblingIndex)
        {
            if (target == null)
                return;

            var dragMove = target.GetComponent<UIDragMoveClamped>();
            if (dragMove == null)
                dragMove = target.gameObject.AddComponent<UIDragMoveClamped>();

            dragMove.SetParentArea(parentArea);
            dragMove.ConfigureZOrder(enableWeakZOrder, applyInitialSiblingOrder, initialSiblingIndex);
        }

        private void OnEnable()
        {
            if (runner == null || callSubscribed)
                return;

            runner.OnCall += HandleVNCall;
            callSubscribed = true;
        }

        private void HandleVNCall(string target, string arg)
        {
            if (!string.Equals(target, "Drink", StringComparison.OrdinalIgnoreCase))
                return;

            Debug.Log($"[DrinkPanel] HandleVNCall target={target} arg={arg ?? string.Empty}");
            Open(arg);
        }

        public void Open(string requestId)
        {
            Debug.Log("[TRACE 4] DrinkPanel.Open called with " + requestId);
            Debug.Log("[DrinkPanel] Open");

            // 🔴 이전 코루틴 강제 종료
            if (openCo != null)
            {
                StopCoroutine(openCo);
                openCo = null;
                isOpening = false;
            }

            if (isOpen)
            {
                Debug.Log("[DrinkPanel] Open ignored (already open)");
                return;
            }

            Debug.Log($"[DrinkPanel] Open requested requestId={requestId ?? ""}");
            Debug.Log("[VN_TEST] DrinkPanel Open request=" + (requestId ?? string.Empty));

            ApplyWindowStates(cachedWindowStates);
            drinkManager?.StartDrink(requestId);
            drinkManager?.HideConfirmPanel();
            drinkManager?.ResetIngredients();
            runner?.StopAutoExternal("DrinkPanel Open:" + (requestId ?? string.Empty));

            openCo = StartCoroutine(CoOpenSafe());
        }

        private void OnDisable()
        {
            CaptureCurrentWindowStatesToCache();

            if (runner != null && callSubscribed)
            {
                runner.OnCall -= HandleVNCall;
                callSubscribed = false;
            }

            if (isOpen)
            {
                policy?.ExitDrinkMode();
                policy?.PopModal("DrinkPanel");
                isOpen = false;
            }

            isOpening = false;

            if (openCo != null)
            {
                StopCoroutine(openCo);
                openCo = null;
            }

            if (root != null)
                root.SetActive(false);


        }

        public void Close()
        {
            Debug.Log("[DrinkPanel] Close");

            isOpen = false;
            isOpening = false;
            CaptureCurrentWindowStatesToCache();

            policy?.ExitDrinkMode();
            policy?.PopModal("DrinkPanel");

            drinkManager?.ResetDrink();
            drinkManager?.HideConfirmPanel();

            if (root != null)
                root.SetActive(false);
        }

        private IEnumerator CoOpenSafe()
        {
            isOpening = true;

            float modalWaitStart = Time.unscaledTime;
            const float modalWaitTimeout = 3f;

            while (policy != null && policy.IsModalOpen)
            {
                if (Time.unscaledTime - modalWaitStart > modalWaitTimeout)
                {
                    Debug.LogWarning("[DrinkPanel] Modal wait timeout — forcing open");
                    break;
                }

                yield return null;
            }

            if (isOpen)
            {
                isOpening = false;
                openCo = null;
                yield break;
            }

            if (root != null)
                root.SetActive(true);

            isOpen = true;
            Debug.Log("[DrinkPanel] Opened");

            policy?.PushModal("DrinkPanel");
            policy?.EnterDrinkMode();

            shortcutController?.LockForSeconds(0.05f);
            windowManager?.LockCloseForSeconds(0.05f);
            EventSystem.current?.SetSelectedGameObject(null);

            isOpening = false;
            openCo = null;
        }

        private void CaptureCurrentWindowStatesToCache()
        {
            cachedWindowStates.Clear();
            CollectWindowStates(cachedWindowStates);
        }

        public void CollectWindowStates(System.Collections.Generic.List<VNWindowStateData> states)
        {
            if (states == null)
                return;

            CollectSingleWindowState(ingredientPanelRoot, IngredientWindowId, states);
            CollectSingleWindowState(drinkGridRoot, GridWindowId, states);
        }

        public void ApplyWindowStates(System.Collections.Generic.IReadOnlyList<VNWindowStateData> states)
        {
            if (states == null || states.Count == 0)
                return;

            ApplySingleWindowState(ingredientPanelRoot, IngredientWindowId, states);
            ApplySingleWindowState(drinkGridRoot, GridWindowId, states);
        }

        private static void CollectSingleWindowState(RectTransform target, string windowId, System.Collections.Generic.List<VNWindowStateData> states)
        {
            if (target == null || string.IsNullOrWhiteSpace(windowId))
                return;

            var drag = target.GetComponent<UIDragMoveClamped>();
            if (drag != null && drag.TryGetWindowState(windowId, out var saved))
            {
                states.Add(saved);
                return;
            }

            var fallbackState = new VNWindowStateData
            {
                siblingIndex = target.GetSiblingIndex()
            };
            fallbackState.SetState(windowId, target.anchoredPosition.x, target.anchoredPosition.y, false);
            states.Add(fallbackState);
        }

        private static void ApplySingleWindowState(RectTransform target, string windowId, System.Collections.Generic.IReadOnlyList<VNWindowStateData> states)
        {
            if (target == null || string.IsNullOrWhiteSpace(windowId))
                return;

            var saved = FindWindowState(states, windowId);
            if (saved == null)
                return;

            var drag = target.GetComponent<UIDragMoveClamped>();
            if (drag != null)
            {
                drag.ApplyWindowState(saved);
                return;
            }

            target.anchoredPosition = new Vector2(saved.GetX(), saved.GetY());
            if (target.parent != null)
            {
                int clampedIndex = Mathf.Clamp(saved.siblingIndex, 0, target.parent.childCount - 1);
                target.SetSiblingIndex(clampedIndex);
            }
        }

        private static VNWindowStateData FindWindowState(System.Collections.Generic.IReadOnlyList<VNWindowStateData> states, string windowId)
        {
            for (int i = 0; i < states.Count; i++)
            {
                var row = states[i];
                if (row == null || string.IsNullOrWhiteSpace(row.GetId()))
                    continue;

                if (MatchesWindowId(windowId, row.GetId()))
                    return row;
            }

            return null;
        }

        private static bool MatchesWindowId(string expectedId, string actualId)
        {
            if (string.Equals(expectedId, actualId, StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.Equals(expectedId, IngredientWindowId, StringComparison.OrdinalIgnoreCase))
                return string.Equals(actualId, LegacyIngredientWindowId, StringComparison.OrdinalIgnoreCase);

            if (string.Equals(expectedId, GridWindowId, StringComparison.OrdinalIgnoreCase))
                return string.Equals(actualId, LegacyGridWindowId, StringComparison.OrdinalIgnoreCase);

            return false;
        }
    }
}
