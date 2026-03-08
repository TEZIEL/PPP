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
        [SerializeField] private VNPolicyController policy;
        [SerializeField] private WindowShortcutController shortcutController;
        [SerializeField] private WindowManager windowManager;
        [SerializeField] private DrinkManager drinkManager;

        private VNRunner Runner => GetComponentInParent<VNRunner>(true);

        private Coroutine openCo;
        private bool isOpen;
        private bool isOpening;
        private bool callSubscribed;

        public bool IsOpenOrOpening => isOpen || isOpening;

        private void Awake()
        {
            if (root != null)
                root.SetActive(false);
        }

        private void OnEnable()
        {
            var runner = Runner;
            if (runner == null || callSubscribed)
                return;

            runner.OnCall += HandleVNCall;
            callSubscribed = true;
        }

        private void HandleVNCall(string target, string arg)
        {
            if (!string.Equals(target, "Drink", StringComparison.OrdinalIgnoreCase))
                return;

            Open(arg);
        }

        public void Open(string requestId)
        {
            if (IsOpenOrOpening)
            {
                Debug.Log("[DrinkPanel] Open ignored (already open/opening)");
                return;
            }

            Debug.Log($"[DrinkPanel] Open requested requestId={requestId ?? ""}");
            Debug.Log("[VN_TEST] DrinkPanel Open request=" + (requestId ?? string.Empty));

            drinkManager?.SetRequest(requestId);
            drinkManager?.HideConfirmPanel();
            drinkManager?.ResetIngredients();
            Runner?.StopAutoExternal("DrinkPanel Open:" + (requestId ?? string.Empty));

            if (openCo != null)
                StopCoroutine(openCo);

            openCo = StartCoroutine(CoOpenSafe());
        }

        public void OnDrinkGreat()
        {
            Runner?.ReturnFromCall("great");
            Runner?.Next();
        }

        public void OnDrinkSuccess()
        {
            Runner?.ReturnFromCall("success");
            Runner?.Next();
        }

        public void OnDrinkFail()
        {
            Runner?.ReturnFromCall("fail");
            Runner?.Next();
        }

        private void OnDisable()
        {
            var runner = Runner;
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
