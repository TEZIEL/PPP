using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PPP.BLUE.VN
{
    public sealed class VNSaveLoadWindow : MonoBehaviour
    {
        [System.Serializable]
        private sealed class SlotUI
        {
            [SerializeField] public Button selectButton;
            [SerializeField] public GameObject selectedHighlight;
            [SerializeField] public TMP_Text statusText;
        }

        [Header("Refs")]
        [SerializeField] private VNRunner runner;
        [SerializeField] private VNPolicyController policy;
        [SerializeField] private VNDialogueView dialogueView;
        [SerializeField] private VNFadeController fadeController;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private SlotUI[] slots = new SlotUI[3];
        [SerializeField] private GameObject confirmPopupRoot;
        [SerializeField] private TMP_Text confirmMessageText;
        [SerializeField] private Button confirmYesButton;
        [SerializeField] private Button confirmNoButton;
        [SerializeField] private Button confirmOkButton;

        [Header("Fade")]
        [SerializeField, Min(0f)] private float loadFadeOutSeconds = 0.35f;
        [SerializeField, Min(0f)] private float loadFadeInSeconds = 0.35f;

        private const string ModalReason = "SaveLoadWindow";
        private bool modalPushed;
        private bool isClosing = false;
        private bool busy;
        private bool confirmOpen;
        private int selectedSlotIndex = -1;
        private PendingAction pendingAction = PendingAction.None;

        private enum PendingAction
        {
            None,
            Save,
            Load,
            Delete,
        }

        private void Awake()
        {
            if (runner == null) runner = GetComponentInParent<VNRunner>(true);
            if (policy == null) policy = GetComponentInParent<VNPolicyController>(true);
            if (dialogueView == null) dialogueView = GetComponentInParent<VNDialogueView>(true);

            BindButtons();
            SetConfirmPopupVisible(false);
            RefreshSlotVisuals();
            RefreshActionButtonState();
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            if (!isClosing)
                return;

            ReleaseModal();

            busy = false;
            confirmOpen = false;
            pendingAction = PendingAction.None;
            SetConfirmPopupVisible(false);
        }

        public void Open()
        {
            if (busy)
                return;

            ForceAutoOff("Open SaveLoad Modal");
            AcquireModal();
            gameObject.SetActive(true);
            RefreshSlotStatus();
            RefreshSlotVisuals();
            RefreshActionButtonState();
        }

        public void Close()
        {
            if (busy)
                return;

            isClosing = true;

            ReleaseModal();
            gameObject.SetActive(false);

            isClosing = false;

            dialogueView?.LockInputFrames(2);
        }

        public void SelectSlot(int index)
        {
            if (busy || confirmOpen)
                return;

            if (index < 0 || index >= slots.Length)
                return;

            selectedSlotIndex = index;
            RefreshSlotVisuals();
            RefreshActionButtonState();
        }

        private void OnSaveButtonClicked()
        {
            if (!CanExecuteAction())
                return;

            bool exists = SlotHasSave(selectedSlotIndex);
            ShowConfirm(exists ? "덮어쓰시겠습니까?" : "저장하시겠습니까?", PendingAction.Save);
        }

        private void OnLoadButtonClicked()
        {
            if (!CanExecuteAction())
                return;

            if (!SlotHasSave(selectedSlotIndex))
            {
                ShowNotice("세이브 데이터가 없습니다.");
                return;
            }

            ShowConfirm("불러오시겠습니까?", PendingAction.Load);
        }

        private void OnDeleteButtonClicked()
        {
            if (!CanExecuteAction())
                return;

            if (!SlotHasSave(selectedSlotIndex))
            {
                ShowNotice("삭제할 세이브 데이터가 없습니다.");
                return;
            }

            ShowConfirm("삭제하시겠습니까?", PendingAction.Delete);
        }

        private bool CanExecuteAction()
        {
            if (busy || confirmOpen)
                return false;
            if (selectedSlotIndex < 0 || selectedSlotIndex >= slots.Length)
                return false;
            return true;
        }

        private IEnumerator CoLoadSlot(int slotNumber)
        {
            busy = true;
            AcquireModal();
            ForceAutoOff($"Load slot {slotNumber}");

            if (fadeController != null)
                yield return fadeController.FadeOut(loadFadeOutSeconds);

            bool copied = CopySlotToDefaultSave(slotNumber);
            bool ok = false;

            if (copied && runner != null)
                ok = runner.TryLoadNow($"VN_SAVE_{slotNumber}");

            if (!copied)
                Debug.LogWarning($"[VN][SaveLoad] Load failed. slot file missing slot={slotNumber}");
            else
                Debug.Log(ok
                    ? $"[VN][SaveLoad] Loaded slot={slotNumber}"
                    : $"[VN][SaveLoad] Load blocked/fail slot={slotNumber}");

            if (fadeController != null)
                yield return fadeController.FadeIn(loadFadeInSeconds);

            busy = false;
            gameObject.SetActive(false);
            ReleaseModal();
            dialogueView?.LockInputFrames(2);
        }

        private void ExecuteSave()
        {
            int slotNumber = selectedSlotIndex + 1;

            ForceAutoOff($"Save slot {slotNumber}");
            bool ok = runner != null && runner.TrySaveNow($"VN_SAVE_{slotNumber}");

            if (!ok)
            {
                Debug.LogWarning($"[VN][SaveLoad] Save blocked/fail slot={slotNumber}");
                RefreshSlotStatus();
                RefreshActionButtonState();
                return;
            }

            bool copied = CopyDefaultSaveToSlot(slotNumber);
            if (copied)
                Debug.Log($"[VN][SaveLoad] Saved slot={slotNumber}");
            else
                Debug.LogWarning($"[VN][SaveLoad] Saved runtime state but failed to copy slot file. slot={slotNumber}");

            RefreshSlotStatus();
            RefreshActionButtonState();
        }

        private void ExecuteDelete()
        {
            int slotNumber = selectedSlotIndex + 1;
            string path = GetSlotPath(slotNumber);

            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log($"[VN][SaveLoad] Deleted slot={slotNumber}");
            }
            else
            {
                Debug.LogWarning($"[VN][SaveLoad] Delete skipped. slot missing slot={slotNumber}");
            }

            RefreshSlotStatus();
            RefreshActionButtonState();
        }

        private void BindButtons()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
                closeButton.onClick.AddListener(Close);
            }

            if (saveButton != null)
            {
                saveButton.onClick.RemoveAllListeners();
                saveButton.onClick.AddListener(OnSaveButtonClicked);
            }

            if (loadButton != null)
            {
                loadButton.onClick.RemoveAllListeners();
                loadButton.onClick.AddListener(OnLoadButtonClicked);
            }

            if (deleteButton != null)
            {
                deleteButton.onClick.RemoveAllListeners();
                deleteButton.onClick.AddListener(OnDeleteButtonClicked);
            }

            if (confirmYesButton != null)
            {
                confirmYesButton.onClick.RemoveAllListeners();
                confirmYesButton.onClick.AddListener(OnConfirmYesClicked);
            }

            if (confirmNoButton != null)
            {
                confirmNoButton.onClick.RemoveAllListeners();
                confirmNoButton.onClick.AddListener(OnConfirmCancelClicked);
            }

            if (confirmOkButton != null)
            {
                confirmOkButton.onClick.RemoveAllListeners();
                confirmOkButton.onClick.AddListener(OnConfirmCancelClicked);
            }

            for (int i = 0; i < slots.Length; i++)
            {
                int capture = i;
                var slot = slots[i];
                if (slot == null) continue;

                if (slot.selectButton != null)
                {
                    slot.selectButton.onClick.RemoveAllListeners();
                    slot.selectButton.onClick.AddListener(() => SelectSlot(capture));
                }
            }
        }

        private void AcquireModal()
        {
            if (modalPushed || policy == null)
                return;

            policy.PushModal(ModalReason);
            modalPushed = true;
        }

        private void ReleaseModal()
        {
            if (!modalPushed || policy == null)
                return;

            policy.PopModal(ModalReason);
            modalPushed = false;
        }

        private void ForceAutoOff(string reason)
        {
            runner?.ForceAutoOff(reason);
            runner?.SetUiSkipHeld(false, "SaveLoadWindow");
        }

        private void RefreshSlotStatus()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot?.statusText == null)
                    continue;

                bool exists = File.Exists(GetSlotPath(i + 1));
                slot.statusText.text = exists ? "Saved" : "Empty";
            }
        }

        private void RefreshSlotVisuals()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot?.selectedHighlight == null)
                    continue;

                slot.selectedHighlight.SetActive(i == selectedSlotIndex);
            }
        }

        private bool SlotHasSave(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Length)
                return false;

            return File.Exists(GetSlotPath(slotIndex + 1));
        }

        private void RefreshActionButtonState()
        {
            bool slotSelected = selectedSlotIndex >= 0 && selectedSlotIndex < slots.Length;
            bool interactable = slotSelected && !busy && !confirmOpen;
            bool hasSave = SlotHasSave(selectedSlotIndex);

            if (saveButton != null) saveButton.interactable = interactable;
            if (loadButton != null) loadButton.interactable = interactable && hasSave;
            if (deleteButton != null) deleteButton.interactable = interactable && hasSave;
        }

        private void ShowConfirm(string message, PendingAction action)
        {
            pendingAction = action;
            confirmOpen = true;

            if (confirmMessageText != null)
                confirmMessageText.text = message;

            if (confirmYesButton != null) confirmYesButton.gameObject.SetActive(true);
            if (confirmNoButton != null) confirmNoButton.gameObject.SetActive(true);
            if (confirmOkButton != null) confirmOkButton.gameObject.SetActive(false);

            SetConfirmPopupVisible(true);
            RefreshActionButtonState();
        }

        private void ShowNotice(string message)
        {
            pendingAction = PendingAction.None;
            confirmOpen = true;

            if (confirmMessageText != null)
                confirmMessageText.text = message;

            if (confirmYesButton != null) confirmYesButton.gameObject.SetActive(false);
            if (confirmNoButton != null) confirmNoButton.gameObject.SetActive(false);
            if (confirmOkButton != null) confirmOkButton.gameObject.SetActive(true);

            SetConfirmPopupVisible(true);
            RefreshActionButtonState();
        }

        private void OnConfirmYesClicked()
        {
            if (!confirmOpen)
                return;

            var action = pendingAction;
            pendingAction = PendingAction.None;
            confirmOpen = false;
            SetConfirmPopupVisible(false);
            RefreshActionButtonState();

            switch (action)
            {
                case PendingAction.Save:
                    ExecuteSave();
                    break;
                case PendingAction.Load:
                    StartCoroutine(CoLoadSlot(selectedSlotIndex + 1));
                    break;
                case PendingAction.Delete:
                    ExecuteDelete();
                    break;
            }
        }

        private void OnConfirmCancelClicked()
        {
            pendingAction = PendingAction.None;
            confirmOpen = false;
            SetConfirmPopupVisible(false);
            RefreshActionButtonState();
        }

        private void SetConfirmPopupVisible(bool visible)
        {
            if (confirmPopupRoot != null)
                confirmPopupRoot.SetActive(visible);
        }

        private static string GetDefaultSavePath()
        {
            return Path.Combine(Application.persistentDataPath, "VN_SAVE.json");
        }

        private static string GetSlotPath(int slotNumber)
        {
            return Path.Combine(Application.persistentDataPath, $"VN_SAVE_{slotNumber}.json");
        }

        private static bool CopyDefaultSaveToSlot(int slotNumber)
        {
            var src = GetDefaultSavePath();
            var dst = GetSlotPath(slotNumber);

            if (!File.Exists(src))
                return false;

            File.Copy(src, dst, overwrite: true);
            return true;
        }

        private static bool CopySlotToDefaultSave(int slotNumber)
        {
            var src = GetSlotPath(slotNumber);
            var dst = GetDefaultSavePath();

            if (!File.Exists(src))
                return false;

            File.Copy(src, dst, overwrite: true);
            return true;
        }
    }
}
