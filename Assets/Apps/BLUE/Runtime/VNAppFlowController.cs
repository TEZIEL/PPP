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

        public void OnNewGameClicked()
        {
            if (State != VNAppState.Title)
                return;

            Debug.Log("[TITLE] NewGame clicked");
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
            Debug.Log($"[TITLE] Open SaveLoad ContinueLoadOnly, keep TitleRoot={(titleRoot != null && titleRoot.activeSelf)} InGameRoot={(inGameRoot != null && inGameRoot.activeSelf)}");
        }

        public void OnExitClicked()
        {
            if (State != VNAppState.Title)
                return;

            closePopupController?.ShowExitConfirm();
        }

        public void RequestReturnToTitleFromInGame()
        {
            if (State != VNAppState.InGame)
                return;

            closePopupController?.ShowReturnToTitleConfirm(ReturnToTitle);
        }

        public void ReturnToTitle()
        {
            Debug.Log("[TITLE] ReturnToTitle start");
            if (saveLoadWindow != null)
                saveLoadWindow.CloseImmediate();

            dialogueView?.ClearForNewGame();

            SetState(VNAppState.Title);
            RefreshContinueButton();
            Debug.Log($"[TITLE] ReturnToTitle complete TitleRoot={(titleRoot != null && titleRoot.activeSelf)} InGameRoot={(inGameRoot != null && inGameRoot.activeSelf)}");
        }

        private IEnumerator CoStartNewGame()
        {
            SetState(VNAppState.Transition);

            if (fadeController != null)
                yield return fadeController.FadeOut(titleTransitionFadeOut);
            Debug.Log("[TITLE] FadeOut complete");

            if (titleRoot != null)
                titleRoot.SetActive(false);
            if (inGameRoot != null)
                inGameRoot.SetActive(true);

            Debug.Log("[TITLE] Clear runtime start");
            saveLoadWindow?.CloseImmediate();
            dialogueView?.ClearForNewGame();

            if (runner != null)
                Debug.Log($"[TITLE] Script loaded scriptId={runner.CurrentScriptId}");

            if (runner != null)
                Debug.Log($"[TITLE] Before Begin pointer={runner.CurrentPointer}");

            runner?.StartNewGameFromBeginning();

            if (runner != null && runner.TryGetCurrentSayState(out var currentNodeId, out var lineIndex, out var text, out var speaker))
            {
                Debug.Log($"[TITLE] After Begin pointer={runner.CurrentPointer} currentNode={currentNodeId} lineIndex={lineIndex}");
                Debug.Log($"[TITLE] First Say speaker={speaker} text={text}");
            }
            SetState(VNAppState.InGame);

            if (fadeController != null)
                yield return fadeController.FadeIn(titleTransitionFadeIn);
        }

        private void HandleContinueLoadCompleted(bool ok)
        {
            if (!ok || State == VNAppState.InGame)
                return;

            Debug.Log("[TITLE] Continue load completed");
            SetState(VNAppState.InGame);
            Debug.Log("[TITLE] Enter InGame after continue load");
        }

        private void HandleCloseRequested()
        {
            if (State == VNAppState.Title)
                closePopupController?.ShowExitConfirm();
            else if (State == VNAppState.InGame)
                RequestReturnToTitleFromInGame();
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
