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

            Debug.Log("[TITLE_NEWGAME] clicked");
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

            transitionLocked = true;
            saveLoadWindow.OnBeforeLoadStateApplyUnderFade = HandleContinueBeforeLoadUnderFade;
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

            float fadeOutSeconds = saveLoadWindow != null ? saveLoadWindow.LoadFadeOutSeconds : titleTransitionFadeOut;
            float fadeInSeconds = saveLoadWindow != null ? saveLoadWindow.LoadFadeInSeconds : titleTransitionFadeIn;
            float holdSeconds = saveLoadWindow != null ? saveLoadWindow.LoadBlackHoldSeconds : 0f;

            if (fadeController != null)
            {
                fadeController.transform.SetAsLastSibling();
                Debug.Log("[TITLE_NEWGAME] FadeOut start");
                yield return fadeController.FadeOut(fadeOutSeconds);
                Debug.Log("[TITLE_NEWGAME] FadeOut complete");
            }

            if (holdSeconds > 0f)
            {
                Debug.Log($"[TITLE_NEWGAME] Delay start seconds={holdSeconds}");
                yield return new WaitForSecondsRealtime(holdSeconds);
                Debug.Log("[TITLE_NEWGAME] Delay end");
            }

            saveLoadWindow?.CloseImmediate();
            closePopupController?.Hide();
            dialogueView?.ClearForNewGame();

            if (titleRoot != null)
                titleRoot.SetActive(false);
            if (inGameRoot != null)
                inGameRoot.SetActive(true);
            Debug.Log("[TITLE_NEWGAME] root switch under black");

            SetState(VNAppState.InGame);
            dialogueView?.SetExternalInputBlocked(false);

            Debug.Log("[TITLE_NEWGAME] Fresh start");
            runner?.StartNewGameFromBeginning();

            string currentNodeId = string.Empty;
            string text = string.Empty;
            if (runner != null && runner.TryGetCurrentSayState(out currentNodeId, out var lineIndex, out text, out _))
                Debug.Log($"[TITLE_NEWGAME] Typing first line start text={text}");

            if (fadeController != null)
            {
                Debug.Log("[TITLE_NEWGAME] FadeIn start");
                yield return fadeController.FadeIn(fadeInSeconds);
                Debug.Log("[TITLE_NEWGAME] FadeIn complete");
            }

            transitionLocked = false;
            if (newGameButton != null)
                newGameButton.interactable = true;

            Debug.Log($"[TITLE_NEWGAME] input ready blocked={dialogueView?.IsExternalInputBlocked}");
        }

        private void HandleContinueLoadCompleted(bool ok)
        {
            Debug.Log("[TITLE] Continue slot load selected");
            saveLoadWindow.OnBeforeLoadStateApplyUnderFade = null;
            transitionLocked = false;

            if (!ok)
                return;

            Debug.Log($"[TITLE] Continue displayed={dialogueView?.IsLineDisplayed} inputLocked={dialogueView?.IsInputLocked} externalBlocked={dialogueView?.IsExternalInputBlocked}");
            Debug.Log("[TITLE] Continue InGame input unblocked");
            Debug.Log($"[VNPolicy] modal count after continue load={GetComponentInChildren<VNPolicyController>(true)?.ModalCount}");
        }

        private void HandleContinueBeforeLoadUnderFade()
        {
            Debug.Log("[TITLE] Continue SaveLoad close under black");
            if (titleRoot != null)
                titleRoot.SetActive(false);
            if (inGameRoot != null)
                inGameRoot.SetActive(true);

            Debug.Log("[TITLE] Continue root switch under black");
            SetState(VNAppState.InGame);
            Debug.Log("[TITLE] Continue input unblock before restore");
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
