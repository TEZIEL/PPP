using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections;

namespace PPP.BLUE.VN
{
    public sealed class DrinkPanel : MonoBehaviour
    {
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
            dragMove.SetZOrderRoot(vnContentRoot);
            dragMove.SetDragTarget(target);
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

            drinkManager?.StartDrink(requestId);
            drinkManager?.HideConfirmPanel();
            drinkManager?.ResetIngredients();
            runner?.StopAutoExternal("DrinkPanel Open:" + (requestId ?? string.Empty));

            openCo = StartCoroutine(CoOpenSafe());
        }

        private void OnDisable()
        {
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
    }
}
