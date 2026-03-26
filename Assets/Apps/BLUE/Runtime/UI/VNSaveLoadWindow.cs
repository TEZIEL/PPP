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
            [SerializeField] public Button saveButton;
            [SerializeField] public Button loadButton;
            [SerializeField] public TMP_Text statusText;
        }

        [Header("Refs")]
        [SerializeField] private VNRunner runner;
        [SerializeField] private VNPolicyController policy;
        [SerializeField] private VNDialogueView dialogueView;
        [SerializeField] private VNFadeController fadeController;
        [SerializeField] private Button closeButton;
        [SerializeField] private SlotUI[] slots = new SlotUI[3];

        [Header("Fade")]
        [SerializeField, Min(0f)] private float loadFadeOutSeconds = 0.35f;
        [SerializeField, Min(0f)] private float loadFadeInSeconds = 0.35f;

        private const string ModalReason = "SaveLoadWindow";
        private bool modalPushed;
        private bool busy;

        private void Awake()
        {
            if (runner == null) runner = GetComponentInParent<VNRunner>(true);
            if (policy == null) policy = GetComponentInParent<VNPolicyController>(true);
            if (dialogueView == null) dialogueView = GetComponentInParent<VNDialogueView>(true);

            BindButtons();
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            ReleaseModal();
            busy = false;
        }

        public void Open()
        {
            if (busy)
                return;

            ForceAutoOff("Open SaveLoad Modal");
            AcquireModal();
            gameObject.SetActive(true);
            RefreshSlotStatus();
        }

        public void Close()
        {
            if (busy)
                return;

            gameObject.SetActive(false);
            ReleaseModal();
            dialogueView?.LockInputFrames(2);
        }

        private void OnClickSave(int slotIndex)
        {
            if (busy)
                return;

            ForceAutoOff($"Save slot {slotIndex + 1}");

            bool ok = runner != null && runner.TrySaveNow($"VN_SAVE_{slotIndex + 1}");
            if (!ok)
            {
                Debug.LogWarning($"[VN][SaveLoad] Save blocked/fail slot={slotIndex + 1}");
                RefreshSlotStatus();
                return;
            }

            bool copied = CopyDefaultSaveToSlot(slotIndex + 1);
            if (copied)
                Debug.Log($"[VN][SaveLoad] Saved slot={slotIndex + 1}");
            else
                Debug.LogWarning($"[VN][SaveLoad] Saved runtime state but failed to copy slot file. slot={slotIndex + 1}");

            RefreshSlotStatus();
        }

        private void OnClickLoad(int slotIndex)
        {
            if (busy)
                return;

            StartCoroutine(CoLoadSlot(slotIndex + 1));
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

        private void BindButtons()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
                closeButton.onClick.AddListener(Close);
            }

            for (int i = 0; i < slots.Length; i++)
            {
                int capture = i;
                var slot = slots[i];
                if (slot == null) continue;

                if (slot.saveButton != null)
                {
                    slot.saveButton.onClick.RemoveAllListeners();
                    slot.saveButton.onClick.AddListener(() => OnClickSave(capture));
                }

                if (slot.loadButton != null)
                {
                    slot.loadButton.onClick.RemoveAllListeners();
                    slot.loadButton.onClick.AddListener(() => OnClickLoad(capture));
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
