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
        }

        private void OnDisable()
        {
            if (bridge != null)
                bridge.OnCloseRequested -= HandleCloseRequested;
        }

        public void OnNewGameClicked()
        {
            if (State != VNAppState.Title)
                return;

            StartCoroutine(CoStartNewGame());
        }

        public void OnContinueClicked()
        {
            if (State != VNAppState.Title || saveLoadWindow == null)
                return;

            if (!saveLoadWindow.HasAnySaveSlots())
            {
                RefreshContinueButton();
                return;
            }

            SetState(VNAppState.Transition);
            saveLoadWindow.Open(VNSaveLoadWindow.OpenMode.ContinueLoadOnly);
            SetState(VNAppState.InGame);
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
            if (saveLoadWindow != null)
                saveLoadWindow.CloseImmediate();

            SetState(VNAppState.Title);
            RefreshContinueButton();
        }

        private IEnumerator CoStartNewGame()
        {
            SetState(VNAppState.Transition);

            if (fadeController != null)
                yield return fadeController.FadeOut(titleTransitionFadeOut);

            runner?.StartNewGameFromBeginning();
            SetState(VNAppState.InGame);

            if (fadeController != null)
                yield return fadeController.FadeIn(titleTransitionFadeIn);
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
