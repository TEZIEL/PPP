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
            [SerializeField] public TMP_Text slotNameText;
            [SerializeField] public TMP_Text statusText; // legacy fallback
        }

        [Header("Refs")]
        [SerializeField] private VNRunner runner;
        [SerializeField] private VNPolicyController policy;
        [SerializeField] private VNDialogueView dialogueView;
        [SerializeField] private VNFadeController fadeController;
        [SerializeField] private VNOSBridge bridge;
        [SerializeField] private GameObject windowRoot;
        [SerializeField] private CanvasGroup windowCanvasGroup;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private SlotUI[] slots = new SlotUI[3];
        [Header("Selected Slot Metadata (Integrated Panel)")]
        [SerializeField] private TMP_Text selectedSlotNameText;
        [SerializeField] private TMP_Text selectedSlotInfoText;
        [SerializeField] private TMP_Text selectedSlotDateText;
        [SerializeField] private GameObject confirmPopupRoot;
        [SerializeField] private TMP_Text confirmMessageText;
        [SerializeField] private Button confirmYesButton;
        [SerializeField] private Button confirmNoButton;
        [SerializeField] private Button confirmOkButton;

        [Header("Fade")]
        [SerializeField, Min(0f)] private float loadFadeOutSeconds = 0.35f;
        [SerializeField, Min(0f)] private float loadFadeInSeconds = 0.35f;
        [SerializeField, Min(0f)] private float loadBlackHoldSeconds = 3f;

        [Header("Slot Selection Fallback")]
        [SerializeField] private Color selectedSlotButtonColor = new Color32(128, 128, 184, 255);
        [SerializeField] private Color selectedTextColor = Color.white;
        [SerializeField] private Color normalTextColor = new Color32(30, 30, 30, 255);

        private const string ModalReason = "SaveLoadWindow";
        private const string LoadingModalReason = "Loading";
        private bool modalPushed;
        private bool loadingModalPushed;
        private bool busy;
        private bool confirmOpen;
        private bool? lastDrinkModeActive;
        private bool? lastRunnerSaveAllowed;
        private int selectedSlotIndex = 0;
        private PendingAction pendingAction = PendingAction.None;
        private readonly System.Collections.Generic.HashSet<int> warnedHighlightBindings = new();
        private readonly System.Collections.Generic.Dictionary<int, Color> slotOriginalButtonColors = new();

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
            if (bridge == null) bridge = GetComponentInParent<VNOSBridge>(true);
            if (windowRoot == null)
            {
                var candidate = transform.Find("Image");
                windowRoot = candidate != null ? candidate.gameObject : gameObject;
            }
            if (windowCanvasGroup == null) windowCanvasGroup = windowRoot.GetComponent<CanvasGroup>();
            if (windowCanvasGroup == null) windowCanvasGroup = windowRoot.AddComponent<CanvasGroup>();

            BindButtons();
            EnsureSlotHighlights();
            SetConfirmPopupVisible(false);
            EnsureValidSelection();
            RefreshSlotStatus();
            RefreshSelectedSlotMetadata();
            RefreshSlotVisuals();
            RefreshActionButtonState();
            SetWindowVisible(false);
        }

        private void OnDisable()
        {
            ReleaseModal();
            ReleaseLoadingModal();
            busy = false;
            confirmOpen = false;
            pendingAction = PendingAction.None;
            lastDrinkModeActive = null;
            lastRunnerSaveAllowed = null;
            SetConfirmPopupVisible(false);
        }

        private void Update()
        {
            if (windowRoot == null || !windowRoot.activeInHierarchy)
                return;

            bool drinkModeActive = policy != null && policy.IsDrinkModeActive();
            bool runnerSaveAllowed = runner == null || runner.SaveAllowed;

            bool changed = !lastDrinkModeActive.HasValue || lastDrinkModeActive.Value != drinkModeActive
                || !lastRunnerSaveAllowed.HasValue || lastRunnerSaveAllowed.Value != runnerSaveAllowed;
            if (!changed)
                return;

            lastDrinkModeActive = drinkModeActive;
            lastRunnerSaveAllowed = runnerSaveAllowed;
            RefreshActionButtonState();
        }

        public void Open()
        {
            if (busy && !loadingModalPushed)
            {
                Debug.LogWarning("[VN][SaveLoad] Open requested while busy=true but loading modal is not active. Recovering busy flag.");
                busy = false;
            }

            if (busy)
                return;

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
            if (windowRoot != null && !windowRoot.activeSelf)
                windowRoot.SetActive(true);
            if (windowCanvasGroup == null)
            {
                if (windowRoot == null) windowRoot = gameObject;
                windowCanvasGroup = windowRoot.GetComponent<CanvasGroup>();
                if (windowCanvasGroup == null) windowCanvasGroup = windowRoot.AddComponent<CanvasGroup>();
            }

            ForceAutoOff("Open SaveLoad Modal");
            bridge?.ClearCloseRequestPending();
            AcquireModal();
            EnsureValidSelection();
            RefreshSlotStatus();
            RefreshSelectedSlotMetadata();
            RefreshSlotVisuals();
            lastDrinkModeActive = null;
            lastRunnerSaveAllowed = null;
            RefreshActionButtonState();
            SetWindowVisible(true);
        }

        public void Close()
        {
            if (busy)
                return;

            CloseImmediate();
            dialogueView?.LockInputFrames(2);
        }

        public void CloseImmediate()
        {
            if (confirmOpen)
            {
                confirmOpen = false;
                pendingAction = PendingAction.None;
                SetConfirmPopupVisible(false);
            }

            SetWindowVisible(false);
            bridge?.ClearCloseRequestPending();
            ReleaseModal();
            RefreshActionButtonState();
        }

        public void SelectSlot(int index)
        {
            if (busy || confirmOpen)
                return;

            if (index < 0 || index >= slots.Length)
                return;

            selectedSlotIndex = index;
            RefreshSelectedSlotMetadata();
            RefreshSlotVisuals();
            RefreshActionButtonState();
        }

        private void OnSaveButtonClicked()
        {
            if (!CanExecuteAction())
                return;
            if (policy != null && policy.IsDrinkModeActive())
            {
                ShowNotice("지금은 저장할 수 없습니다");
                return;
            }

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
            if (busy)
                yield break;

            busy = true;
            try
            {
                confirmOpen = false;
                pendingAction = PendingAction.None;
                SetConfirmPopupVisible(false);
                RefreshActionButtonState();

                AcquireModal();
                AcquireLoadingModal();
                ForceAutoOff($"Load slot {slotNumber}");

                if (fadeController != null)
                    yield return fadeController.FadeOut(loadFadeOutSeconds);

                // 검은 화면에서 창을 정리
                CloseImmediate();

                if (loadBlackHoldSeconds > 0f)
                    yield return new WaitForSecondsRealtime(loadBlackHoldSeconds);

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
            }
            finally
            {
                ReleaseLoadingModal();
                busy = false;
                dialogueView?.LockInputFrames(2);
            }
        }

        private void ExecuteSave()
        {
            int slotNumber = selectedSlotIndex + 1;

            ForceAutoOff($"Save slot {slotNumber}");
            bool hadModal = modalPushed;
            if (hadModal)
                ReleaseModal();

            bool ok = runner != null && runner.TrySaveNow($"VN_SAVE_{slotNumber}");

            if (hadModal)
                AcquireModal();

            if (!ok)
            {
                Debug.LogWarning($"[VN][SaveLoad] Save blocked/fail slot={slotNumber}");
                RefreshSlotStatus();
                RefreshSlotVisuals();
                RefreshActionButtonState();
                return;
            }

            bool copied = CopyDefaultSaveToSlot(slotNumber);
            if (copied)
                Debug.Log($"[VN][SaveLoad] Saved slot={slotNumber}");
            else
                Debug.LogWarning($"[VN][SaveLoad] Saved runtime state but failed to copy slot file. slot={slotNumber}");

            RefreshSlotStatus();
            RefreshSlotVisuals();
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
            RefreshSlotVisuals();
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

        private void AcquireLoadingModal()
        {
            if (loadingModalPushed || policy == null)
                return;

            policy.PushModal(LoadingModalReason);
            loadingModalPushed = true;
        }

        private void ReleaseLoadingModal()
        {
            if (!loadingModalPushed || policy == null)
                return;

            policy.PopModal(LoadingModalReason);
            loadingModalPushed = false;
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
                BindSlotHeaderText(slots[i], i);
            }
        }

        private void BindSlotHeaderText(SlotUI slot, int index)
        {
            if (slot == null)
                return;

            string slotName = GetSlotName(index);
            SetText(slot.slotNameText, slotName);

            // Legacy slot text는 슬롯 번호만 표시(통합 메타데이터 패널과 역할 분리).
            if (slot.statusText != null)
                slot.statusText.text = slotName;
        }

        private void RefreshSelectedSlotMetadata()
        {
            EnsureValidSelection();
            if (selectedSlotIndex < 0 || selectedSlotIndex >= slots.Length)
            {
                SetText(selectedSlotNameText, "슬롯00");
                SetText(selectedSlotInfoText, "비어있음");
                SetText(selectedSlotDateText, "비어있음");
                return;
            }

            string slotName = GetSlotName(selectedSlotIndex);
            string slotInfo = "비어있음";
            string slotDate = "비어있음";
            var state = ReadSlotState(selectedSlotIndex + 1);
            if (state != null)
            {
                slotInfo = ComposeSavePointInfo(state);
                slotDate = FormatSaveDate(state.saveTime);
            }

            SetText(selectedSlotNameText, slotName);
            SetText(selectedSlotInfoText, slotInfo);
            SetText(selectedSlotDateText, slotDate);
        }

        private void AutoBindIntegratedSlotMetadataTexts()
        {
            if (selectedSlotNameText != null && selectedSlotInfoText != null && selectedSlotDateText != null)
                return;

            var allTexts = windowRoot != null ? windowRoot.GetComponentsInChildren<TMP_Text>(true) : GetComponentsInChildren<TMP_Text>(true);
            var candidates = new System.Collections.Generic.List<TMP_Text>();
            foreach (var text in allTexts)
            {
                if (text == null)
                    continue;

                if (confirmMessageText != null && text == confirmMessageText)
                    continue;

                if (IsSlotText(text))
                    continue;
                if (IsActionButtonText(text))
                    continue;

                if (windowRoot != null && text.transform.IsChildOf(windowRoot.transform))
                    candidates.Add(text);
            }

            if (candidates.Count < 3)
                return;

            candidates.Sort((a, b) =>
            {
                float ay = ((RectTransform)a.transform).anchoredPosition.y;
                float by = ((RectTransform)b.transform).anchoredPosition.y;
                return by.CompareTo(ay);
            });

            if (selectedSlotNameText == null) selectedSlotNameText = candidates[0];
            if (selectedSlotInfoText == null) selectedSlotInfoText = candidates[1];
            if (selectedSlotDateText == null) selectedSlotDateText = candidates[2];
        }

        private bool IsSlotText(TMP_Text text)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot == null)
                    continue;

                if (slot.slotNameText == text || slot.statusText == text)
                    return true;

                if (slot.selectButton != null && text.transform.IsChildOf(slot.selectButton.transform))
                    return true;
            }

            return false;
        }

        private bool IsActionButtonText(TMP_Text text)
        {
            return IsChildOfButton(text, closeButton)
                   || IsChildOfButton(text, saveButton)
                   || IsChildOfButton(text, loadButton)
                   || IsChildOfButton(text, deleteButton)
                   || IsChildOfButton(text, confirmYesButton)
                   || IsChildOfButton(text, confirmNoButton)
                   || IsChildOfButton(text, confirmOkButton);
        }

        private static bool IsChildOfButton(TMP_Text text, Button button)
        {
            if (text == null || button == null)
                return false;

            return text.transform.IsChildOf(button.transform);
        }

        private static string GetSlotName(int index)
        {
            return $"슬롯{(index + 1).ToString("00")}";
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
                target.text = value;
        }

        private static string ComposeSavePointInfo(VNState state)
        {
            if (state == null)
                return "비어있음";

            if (!string.IsNullOrWhiteSpace(state.currentLabel))
                return state.currentLabel.Trim();
            if (!string.IsNullOrWhiteSpace(state.nodeId))
                return state.nodeId.Trim();
            if (!string.IsNullOrWhiteSpace(state.scriptId))
                return state.scriptId.Trim();
            return "비어있음";
        }

        private static string FormatSaveDate(string saveTime)
        {
            if (string.IsNullOrWhiteSpace(saveTime))
                return "비어있음";

            if (System.DateTime.TryParse(saveTime, out var parsed))
                return parsed.ToString("yyyy-MM-dd HH:mm");

            return saveTime;
        }

        private static VNState ReadSlotState(int slotNumber)
        {
            string path = GetSlotPath(slotNumber);
            if (!File.Exists(path))
                return null;

            try
            {
                string json = File.ReadAllText(path);
                return JsonUtility.FromJson<VNState>(json);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[VN][SaveLoad] Failed to read slot metadata. slot={slotNumber} reason={ex.Message}");
                return null;
            }
        }

        private void RefreshSlotVisuals()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot == null)
                    continue;

                bool selected = i == selectedSlotIndex;
                ApplySlotTextSelection(slot, selected);

                if (slot.selectedHighlight == null)
                {
                    EnsureSlotHighlight(i, slot);
                    if (slot.selectedHighlight == null)
                        continue;
                }

                bool highlightIsSlotRoot = slot.selectButton != null &&
                                           (slot.selectedHighlight == slot.selectButton.gameObject ||
                                            slot.selectButton.transform.IsChildOf(slot.selectedHighlight.transform));

                if (highlightIsSlotRoot)
                {
                    if (!warnedHighlightBindings.Contains(i))
                    {
                        warnedHighlightBindings.Add(i);
                        Debug.LogWarning($"[VN][SaveLoad] Slot {i + 1} selectedHighlight is bound to slot root. Keeping slot visible and toggling Graphic only.");
                    }

                    var graphic = slot.selectedHighlight.GetComponent<Graphic>();
                    if (graphic != null)
                    {
                        graphic.enabled = selected;
                        graphic.color = selectedSlotButtonColor;
                    }

                    continue;
                }

                slot.selectedHighlight.SetActive(selected);

                var highlightGraphic = slot.selectedHighlight.GetComponent<Graphic>();
                if (highlightGraphic != null)
                    highlightGraphic.color = selectedSlotButtonColor;
            }
        }

        private void EnsureSlotHighlights()
        {
            if (slots == null)
                return;

            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot == null || slot.selectedHighlight != null)
                    continue;

                EnsureSlotHighlight(i, slot);
            }
        }

        private void EnsureSlotHighlight(int slotIndex, SlotUI slot)
        {
            if (slot == null || slot.selectedHighlight != null)
                return;
            if (slot.selectButton == null)
                return;

            var root = slot.selectButton.transform as RectTransform;
            if (root == null)
                return;

            var highlightObj = new GameObject($"AutoSelectedHighlight_{slotIndex + 1}", typeof(RectTransform), typeof(Image));
            var rect = highlightObj.GetComponent<RectTransform>();
            rect.SetParent(root, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.SetAsFirstSibling();

            var img = highlightObj.GetComponent<Image>();
            img.color = selectedSlotButtonColor;
            img.raycastTarget = false;
            img.enabled = false;

            slot.selectedHighlight = highlightObj;
        }

        private void ApplySlotTextSelection(SlotUI slot, bool selected)
        {
            if (slot == null)
                return;

            var texts = slot.selectButton != null
                ? slot.selectButton.GetComponentsInChildren<TMP_Text>(true)
                : System.Array.Empty<TMP_Text>();

            Color targetColor = selected ? selectedTextColor : normalTextColor;
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null)
                    texts[i].color = targetColor;
            }

            if (slot.statusText != null)
                slot.statusText.color = targetColor;
        }

        private bool SlotHasSave(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Length)
                return false;

            return File.Exists(GetSlotPath(slotIndex + 1));
        }

        private void RefreshActionButtonState()
        {
            EnsureValidSelection();
            bool slotSelected = selectedSlotIndex >= 0 && selectedSlotIndex < slots.Length;
            bool interactable = slotSelected && !busy && !confirmOpen;
            bool hasSave = SlotHasSave(selectedSlotIndex);
            bool notDrinkMode = policy == null || !policy.IsDrinkModeActive();
            bool runnerReady = runner == null || runner.SaveAllowed;
            bool canSaveNow = interactable && notDrinkMode && runnerReady;

            if (saveButton != null) saveButton.interactable = canSaveNow;
            if (loadButton != null) loadButton.interactable = interactable && hasSave;
            if (deleteButton != null) deleteButton.interactable = interactable && hasSave;
        }

        private void EnsureValidSelection()
        {
            if (slots == null || slots.Length == 0)
            {
                selectedSlotIndex = -1;
                return;
            }

            if (selectedSlotIndex < 0 || selectedSlotIndex >= slots.Length)
                selectedSlotIndex = 0;
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
            RefreshSlotVisuals();
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
            RefreshSlotVisuals();
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
            RefreshSlotVisuals();
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
            RefreshSlotVisuals();
            RefreshActionButtonState();
        }

        private void SetConfirmPopupVisible(bool visible)
        {
            if (confirmPopupRoot != null)
                confirmPopupRoot.SetActive(visible);
        }

        private void SetWindowVisible(bool visible)
        {
            windowCanvasGroup.alpha = visible ? 1f : 0f;
            windowCanvasGroup.interactable = visible;
            windowCanvasGroup.blocksRaycasts = visible;
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
