using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace PPP.BLUE.VN
{
    public sealed class VNBacklogView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private VNBacklogItemView itemPrefab;
        [SerializeField] private TMP_Text fallbackSpeakerTemplate;
        [SerializeField] private TMP_Text fallbackBodyTemplate;

        private VNBacklogManager manager;
        private readonly Dictionary<string, VNBacklogItemView> itemByKey = new();
        private bool isOpen;
        public bool IsOpen => isOpen;
        public event Action<bool> OnOpenStateChanged;

        public void ConfigureFallbackTextTemplates(TMP_Text speakerTemplate, TMP_Text bodyTemplate)
        {
            if (speakerTemplate != null)
                fallbackSpeakerTemplate = speakerTemplate;
            if (bodyTemplate != null)
                fallbackBodyTemplate = bodyTemplate;
        }

        public void BindManager(VNBacklogManager backlogManager)
        {
            Debug.Log($"[VNBacklogView] BindManager called manager={(backlogManager != null ? "set" : "null")}");
            if (manager == backlogManager)
                return;

            UnbindManager();
            manager = backlogManager;
            if (manager == null)
                return;

            LogReferenceState("BindManager");
            manager.OnEntryChanged += HandleEntryChanged;
            manager.OnBacklogRestored += RebuildAll;
            RebuildAll();
        }

        public void UnbindManager()
        {
            if (manager == null)
                return;

            manager.OnEntryChanged -= HandleEntryChanged;
            manager.OnBacklogRestored -= RebuildAll;
            manager = null;
        }

        public void Toggle()
        {
            bool next = root != null ? !root.activeSelf : !gameObject.activeSelf;
            SetOpen(next);
        }

        public void SetOpen(bool open)
        {
            Debug.Log($"[VNBacklogView] SetOpen open={open}");
            if (root != null)
                root.SetActive(open);
            else
                gameObject.SetActive(open);

            if (isOpen != open)
            {
                isOpen = open;
                OnOpenStateChanged?.Invoke(open);
            }

            if (open)
                RebuildAll();
        }

        private void HandleEntryChanged(VNBacklogEntry entry)
        {
            Debug.Log($"[VNBacklogView] HandleEntryChanged key={(entry != null ? entry.CompositeKey : "<null>")}");
            if (entry == null || contentRoot == null)
                return;

            var item = GetOrCreateItem(entry.CompositeKey);
            if (item != null)
            {
                item.Bind(entry);
                item.transform.SetSiblingIndex(0); // 최신순 상단
            }
        }

        private VNBacklogItemView GetOrCreateItem(string compositeKey)
        {
            if (string.IsNullOrEmpty(compositeKey))
                return null;

            if (itemByKey.TryGetValue(compositeKey, out var existing) && existing != null)
                return existing;

            VNBacklogItemView created = itemPrefab != null
                ? Instantiate(itemPrefab, contentRoot)
                : CreateRuntimeItem(contentRoot);

            if (created == null)
                return null;

            itemByKey[compositeKey] = created;
            Debug.Log($"[VNBacklogView] GetOrCreateItem created key={compositeKey}");
            return created;
        }

        private void RebuildAll()
        {
            int entryCount = manager != null ? manager.GetEntriesForDisplay().Count : -1;
            Debug.Log($"[VNBacklogView] RebuildAll entries={entryCount}");
            if (manager == null || contentRoot == null)
                return;

            for (int i = contentRoot.childCount - 1; i >= 0; i--)
                Destroy(contentRoot.GetChild(i).gameObject);
            itemByKey.Clear();

            var entries = manager.GetEntriesForDisplay();
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var item = GetOrCreateItem(entry.CompositeKey);
                item?.Bind(entry);
            }
        }

        private void Awake()
        {
            if (contentRoot == null)
                contentRoot = transform.Find("Content") as RectTransform;
            isOpen = root != null ? root.activeSelf : gameObject.activeSelf;
            LogReferenceState("Awake");
        }

        private void LogReferenceState(string phase)
        {
            Debug.Log($"[VNBacklogView] {phase} refs root={(root != null)} contentRoot={(contentRoot != null)} itemPrefab={(itemPrefab != null)}");
        }

        private VNBacklogItemView CreateRuntimeItem(RectTransform parent)
        {
            if (parent == null)
                return null;

            var row = new GameObject("BacklogItem_Runtime", typeof(RectTransform), typeof(LayoutElement), typeof(VNBacklogItemView));
            var rowRect = row.GetComponent<RectTransform>();
            rowRect.SetParent(parent, false);
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.pivot = new Vector2(0.5f, 1f);

            var speakerTemplate = ResolveSpeakerTemplate();
            var bodyTemplate = ResolveBodyTemplate();
            var speakerGo = CreateRuntimeText("Speaker", rowRect, new Vector2(8f, -8f), speakerTemplate);
            var bodyGo = CreateRuntimeText("Body", rowRect, new Vector2(8f, -34f), bodyTemplate);
            var bodyRect = bodyGo.rectTransform;
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.sizeDelta = new Vector2(-16f, 0f);

            var view = row.GetComponent<VNBacklogItemView>();
            view.SetupRuntimeTexts(speakerGo, bodyGo);
            return view;
        }

        private TMP_Text ResolveSpeakerTemplate()
        {
            if (itemPrefab != null && itemPrefab.SpeakerTemplateText != null)
                return itemPrefab.SpeakerTemplateText;
            return fallbackSpeakerTemplate != null ? fallbackSpeakerTemplate : fallbackBodyTemplate;
        }

        private TMP_Text ResolveBodyTemplate()
        {
            if (itemPrefab != null && itemPrefab.BodyTemplateText != null)
                return itemPrefab.BodyTemplateText;
            return fallbackBodyTemplate != null ? fallbackBodyTemplate : fallbackSpeakerTemplate;
        }

        private static TextMeshProUGUI CreateRuntimeText(string name, RectTransform parent, Vector2 anchoredPos, TMP_Text template)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(-16f, 24f);

            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = string.Empty;
            if (template != null)
                CopyTextStyle(template, text);
            return text;
        }

        private static void CopyTextStyle(TMP_Text source, TMP_Text target)
        {
            if (source == null || target == null)
                return;

            target.font = source.font;
            target.fontSharedMaterial = source.fontSharedMaterial;
            target.fontSize = source.fontSize;
            target.fontStyle = source.fontStyle;
            target.alignment = source.alignment;
            target.enableWordWrapping = source.enableWordWrapping;
            target.overflowMode = source.overflowMode;
            target.color = source.color;
            target.raycastTarget = source.raycastTarget;
            target.richText = source.richText;
            target.isRightToLeftText = source.isRightToLeftText;
            target.enableAutoSizing = source.enableAutoSizing;
        }
    }
}
