using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PPP.BLUE.VN
{
    public sealed class VNAppFlowController : MonoBehaviour
    {
        public enum VNAppState
        {
            Title,
            InGame,
            Transition,
        }

        [Header("Refs")]
        [SerializeField] private VNRunner runner;
        [SerializeField] private VNDialogueView dialogueView;
        [SerializeField] private VNSaveLoadWindow saveLoadWindow;
        [SerializeField] private VNFadeController fadeController;
        [SerializeField] private VNClosePopupController closePopupController;
        [SerializeField] private VNOSBridge bridge;

        [Header("Title UI")]
        [SerializeField] private GameObject titleRoot;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private GameObject inGameRoot;

        [Header("Transition")]
        [SerializeField, Min(0f)] private float titleTransitionFadeOut = 0.35f;
        [SerializeField, Min(0f)] private float titleTransitionFadeIn = 0.35f;

        public VNAppState State { get; private set; } = VNAppState.Title;
        private bool transitionLocked;

        private void Awake()
        {
            if (runner == null) runner = GetComponentInParent<VNRunner>(true);
            if (dialogueView == null) dialogueView = GetComponentInParent<VNDialogueView>(true);
            if (saveLoadWindow == null) saveLoadWindow = GetComponentInParent<VNSaveLoadWindow>(true);
            if (fadeController == null) fadeController = GetComponentInParent<VNFadeController>(true);
            if (closePopupController == null) closePopupController = GetComponentInParent<VNClosePopupController>(true);
            if (bridge == null) bridge = GetComponentInParent<VNOSBridge>(true);

            BindButtons();
            SetState(VNAppState.Title);
            RefreshContinueButton();
        }

        private void OnEnable()
        {
            if (bridge != null)
            {
                bridge.OnCloseRequested -= HandleCloseRequested;
                bridge.OnCloseRequested += HandleCloseRequested;
            }

            SetState(VNAppState.Title);
            RefreshContinueButton();
            if (saveLoadWindow != null)
            {
                saveLoadWindow.OnLoadCompleted -= HandleContinueLoadCompleted;
                saveLoadWindow.OnLoadCompleted += HandleContinueLoadCompleted;
            }
        }

        private void OnDisable()
        {
            if (bridge != null)
                bridge.OnCloseRequested -= HandleCloseRequested;
            if (saveLoadWindow != null)
                saveLoadWindow.OnLoadCompleted -= HandleContinueLoadCompleted;
        }


        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape))
                return;

            if (State == VNAppState.Title)
            {
                RequestExitFromTitle("Esc");
                return;
            }

            if (State == VNAppState.InGame)
                RequestReturnToTitleFromInGame("Esc");
        }

        public void OnNewGameClicked()
        {
            if (State != VNAppState.Title || transitionLocked)
            {
                Debug.Log($"[TITLE] NewGame ignored state={State} locked={transitionLocked}");
                return;
            }

            transitionLocked = true;
            if (newGameButton != null)
                newGameButton.interactable = false;

            Debug.Log($"[TITLE] NewGame clicked state={State}");
            StartCoroutine(CoStartNewGame());
        }

        public void OnContinueClicked()
        {
            if (State != VNAppState.Title || saveLoadWindow == null)
                return;

            Debug.Log($"[TITLE] Continue clicked state={State}");

            if (!saveLoadWindow.HasAnySaveSlots())
            {
                RefreshContinueButton();
                return;
            }

            saveLoadWindow.Open(VNSaveLoadWindow.OpenMode.ContinueLoadOnly);
            Debug.Log($"[TITLE] Open SaveLoad ContinueLoadOnly TitleRoot={(titleRoot != null && titleRoot.activeSelf)} InGameRoot={(inGameRoot != null && inGameRoot.activeSelf)}");
        }

        public void OnExitClicked()
        {
            RequestExitFromTitle("Button");
        }

        public void RequestReturnToTitleFromInGame(string source = "Button")
        {
            if (State != VNAppState.InGame)
                return;

            Debug.Log($"[TITLE] Show return-to-title confirm source={source}");
            closePopupController?.ShowReturnToTitleConfirm(ReturnToTitle);
        }

        public void RequestExitFromTitle(string source)
        {
            Debug.Log($"[TITLE] Exit requested source={source} state={State}");
            if (State != VNAppState.Title || transitionLocked)
                return;

            Debug.Log($"[TITLE] Show exit confirm source={source}");
            closePopupController?.ShowExitConfirm(() =>
            {
                Debug.Log($"[TITLE] Exit confirmed source={source}");
                Debug.Log($"[TITLE] Force close requested source={source}");
                closePopupController?.RequestCloseFromPopup();
            }, () => Debug.Log($"[TITLE] Exit cancelled source={source}"));
        }

        public void ReturnToTitle()
        {
            Debug.Log($"[TITLE] ReturnToTitle start state={State}");
            if (saveLoadWindow != null)
                saveLoadWindow.CloseImmediate();

            dialogueView?.ClearForNewGame();

            SetState(VNAppState.Title);
            RefreshContinueButton();
            Debug.Log($"[TITLE] ReturnToTitle complete TitleRoot={(titleRoot != null && titleRoot.activeSelf)} InGameRoot={(inGameRoot != null && inGameRoot.activeSelf)}");
        }

        private IEnumerator CoStartNewGame()
        {
            transitionLocked = true;
            SetState(VNAppState.Transition);

            if (fadeController != null)
                yield return fadeController.FadeOut(titleTransitionFadeOut);
            Debug.Log("[TITLE] FadeOut complete");

            saveLoadWindow?.CloseImmediate();
            closePopupController?.Hide();
            dialogueView?.ClearForNewGame();
            Debug.Log("[TITLE] Fresh runtime reset complete");

            if (titleRoot != null)
                titleRoot.SetActive(false);
            if (inGameRoot != null)
                inGameRoot.SetActive(true);

            SetState(VNAppState.InGame);
            Debug.Log("[TITLE] InGame input unblocked");

            runner?.StartNewGameFromBeginning();
            Debug.Log($"[TITLE] Script fresh loaded scriptId={runner?.CurrentScriptId}");

            dialogueView?.OnStateLoadedForValidation();

            if (runner != null && runner.TryGetCurrentSayState(out var currentNodeId, out var lineIndex, out var text, out _))
                Debug.Log($"[TITLE] Begin complete pointer={runner.CurrentPointer} node={currentNodeId} firstSay={text}");

            if (fadeController != null)
                yield return fadeController.FadeIn(titleTransitionFadeIn);

            transitionLocked = false;
            if (newGameButton != null)
                newGameButton.interactable = true;
        }

        private void HandleContinueLoadCompleted(bool ok)
        {
            Debug.Log("[TITLE] Continue slot load selected");
            if (!ok || State == VNAppState.InGame)
                return;

            if (titleRoot != null)
                titleRoot.SetActive(false);
            if (inGameRoot != null)
                inGameRoot.SetActive(true);

            Debug.Log($"[TITLE] Continue root switched TitleRoot={(titleRoot != null && titleRoot.activeSelf)} InGameRoot={(inGameRoot != null && inGameRoot.activeSelf)}");
            SetState(VNAppState.InGame);
            Debug.Log("[TITLE] Continue input unblock before restore");

            dialogueView?.OnStateLoadedForValidation();
            if (runner != null && runner.TryGetCurrentSayState(out var currentNodeId, out var lineIndex, out _, out _))
                Debug.Log($"[TITLE] Restore complete pointer={runner.CurrentPointer} node={currentNodeId}");

            Debug.Log($"[TITLE] Continue displayed={dialogueView?.IsLineDisplayed} inputLocked={dialogueView?.IsInputLocked} externalBlocked={dialogueView?.IsExternalInputBlocked}");
            Debug.Log("[TITLE] Continue InGame input unblocked");
            Debug.Log($"[VNPolicy] modal count after continue load={GetComponentInChildren<VNPolicyController>(true)?.ModalCount}");
        }

        private void HandleCloseRequested()
        {
            Debug.Log($"[TITLE] Close requested state={State}");
            if (State == VNAppState.Title)
            {
                Debug.Log("[TITLE] Show exit confirm");
                RequestExitFromTitle("X");
            }
            else if (State == VNAppState.InGame)
            {
                Debug.Log("[TITLE] Show return-to-title confirm");
                RequestReturnToTitleFromInGame("X");
            }
        }

        private void SetState(VNAppState next)
        {
            State = next;
            bool title = next == VNAppState.Title;
            bool inGame = next == VNAppState.InGame;

            if (titleRoot != null)
                titleRoot.SetActive(title);
            if (inGameRoot != null)
                inGameRoot.SetActive(inGame);
            if (dialogueView != null)
                dialogueView.SetExternalInputBlocked(next != VNAppState.InGame);

            if (continueButton != null && title)
                RefreshContinueButton();
        }

        private void RefreshContinueButton()
        {
            if (continueButton == null)
                return;

            continueButton.interactable = saveLoadWindow != null && saveLoadWindow.HasAnySaveSlots();
        }

        private void BindButtons()
        {
            if (newGameButton != null)
            {
                newGameButton.onClick.RemoveListener(OnNewGameClicked);
                newGameButton.onClick.AddListener(OnNewGameClicked);
            }

            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(OnContinueClicked);
                continueButton.onClick.AddListener(OnContinueClicked);
            }

            if (exitButton != null)
            {
                exitButton.onClick.RemoveListener(OnExitClicked);
                exitButton.onClick.AddListener(OnExitClicked);
            }
        }
    }
}
