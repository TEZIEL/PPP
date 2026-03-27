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
        private bool lastKnownOpen;
        private bool isRebuilding;
        private bool pendingRebuild;
        private Coroutine deferredLayoutCoroutine;
        public bool IsOpen => root != null ? root.activeInHierarchy : gameObject.activeInHierarchy;
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

            SyncOpenState();

            if (open)
                RebuildAll();
        }

        private void HandleEntryChanged(VNBacklogEntry entry)
        {
            Debug.Log($"[VNBacklogView] HandleEntryChanged key={(entry != null ? entry.CompositeKey : "<null>")}");
            if (entry == null || contentRoot == null)
                return;

            if (isRebuilding)
            {
                pendingRebuild = true;
                return;
            }

            if (!itemByKey.ContainsKey(entry.CompositeKey))
            {
                RebuildAll();
                return;
            }

            var item = GetOrCreateItem(entry.CompositeKey);
            if (item != null)
            {
                NormalizeItemLayout(item);
                item.Bind(entry);
                RebuildItemLayout(item);
                RebuildContentLayout(alignToTop: false);
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
            if (isRebuilding)
            {
                pendingRebuild = true;
                return;
            }

            int entryCount = manager != null ? manager.GetEntriesForDisplay().Count : -1;
            Debug.Log($"[VNBacklogView] RebuildAll entries={entryCount}");
            if (manager == null || contentRoot == null)
                return;

            isRebuilding = true;
            try
            {
                for (int i = contentRoot.childCount - 1; i >= 0; i--)
                    Destroy(contentRoot.GetChild(i).gameObject);
                itemByKey.Clear();

                var entries = manager.GetEntriesForDisplay();
                for (int i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    var item = GetOrCreateItem(entry.CompositeKey);
                    if (item == null)
                        continue;

                    NormalizeItemLayout(item);
                    item.Bind(entry);
                    RebuildItemLayout(item);
                }
            }
            finally
            {
                isRebuilding = false;
            }

            ScheduleDeferredLayout(alignToTop: true);

            if (pendingRebuild)
            {
                pendingRebuild = false;
                RebuildAll();
            }
        }

        private void Awake()
        {
            if (contentRoot == null)
                contentRoot = FindContentRoot();
            lastKnownOpen = IsOpen;
            EnsureContentLayoutComponents();
            LogReferenceState("Awake");
        }

        private void LateUpdate()
        {
            SyncOpenState();
        }

        private void LogReferenceState(string phase)
        {
            Debug.Log($"[VNBacklogView] {phase} refs root={(root != null)} contentRoot={(contentRoot != null)} itemPrefab={(itemPrefab != null)}");
        }

        private VNBacklogItemView CreateRuntimeItem(RectTransform parent)
        {
            if (parent == null)
                return null;

            var row = new GameObject("BacklogItem_Runtime", typeof(RectTransform), typeof(LayoutElement), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(VNBacklogItemView));
            var rowRect = row.GetComponent<RectTransform>();
            rowRect.SetParent(parent, false);
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.pivot = new Vector2(0.5f, 1f);
            rowRect.sizeDelta = new Vector2(0f, 0f);

            var rowLayout = row.GetComponent<VerticalLayoutGroup>();
            rowLayout.childAlignment = TextAnchor.UpperLeft;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = false;
            rowLayout.spacing = 4f;
            rowLayout.padding = new RectOffset(8, 8, 6, 8);

            var rowFitter = row.GetComponent<ContentSizeFitter>();
            rowFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            rowFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var rowElement = row.GetComponent<LayoutElement>();
            rowElement.minHeight = 24f;
            rowElement.flexibleHeight = 0f;

            var speakerTemplate = ResolveSpeakerTemplate();
            var bodyTemplate = ResolveBodyTemplate();
            var speakerGo = CreateRuntimeText("Speaker", rowRect, speakerTemplate, isBody: false);
            var bodyGo = CreateRuntimeText("Body", rowRect, bodyTemplate, isBody: true);

            var view = row.GetComponent<VNBacklogItemView>();
            view.SetupRuntimeTexts(speakerGo, bodyGo);
            NormalizeItemLayout(view);
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

        private static TextMeshProUGUI CreateRuntimeText(string name, RectTransform parent, TMP_Text template, bool isBody)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement), typeof(ContentSizeFitter));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, 0f);

            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = string.Empty;
            if (template != null)
                CopyTextStyle(template, text);
            text.alignment = TextAlignmentOptions.TopLeft;
            text.enableWordWrapping = isBody;
            text.overflowMode = TextOverflowModes.Overflow;

            var fitter = go.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var element = go.GetComponent<LayoutElement>();
            element.minHeight = isBody ? 22f : 18f;
            element.flexibleHeight = 0f;
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
            target.margin = source.margin;
            target.lineSpacing = source.lineSpacing;
            target.characterSpacing = source.characterSpacing;
            target.wordSpacing = source.wordSpacing;
        }

        private RectTransform FindContentRoot()
        {
            var direct = transform.Find("Content") as RectTransform;
            if (direct != null)
                return direct;

            var allRects = GetComponentsInChildren<RectTransform>(true);
            for (int i = 0; i < allRects.Length; i++)
            {
                if (allRects[i] != null && allRects[i].name == "Content")
                    return allRects[i];
            }

            return null;
        }

        private void EnsureContentLayoutComponents()
        {
            if (contentRoot == null)
                return;

            var layout = contentRoot.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
                layout = contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            if (layout.spacing <= 0f)
                layout.spacing = 6f;

            var fitter = contentRoot.GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = contentRoot.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollRect = contentRoot.GetComponentInParent<ScrollRect>();
            if (scrollRect != null)
            {
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
                if (scrollRect.viewport != null && scrollRect.viewport.GetComponent<RectMask2D>() == null)
                    scrollRect.viewport.gameObject.AddComponent<RectMask2D>();
                if (scrollRect.verticalScrollbar == null)
                {
                    var scrollbar = scrollRect.GetComponentInChildren<Scrollbar>(true);
                    if (scrollbar != null)
                        scrollRect.verticalScrollbar = scrollbar;
                }
                scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            }

            var viewport = contentRoot.parent as RectTransform;
            if (viewport != null)
            {
                if (viewport.GetComponent<RectMask2D>() == null && viewport.GetComponent<Mask>() == null)
                    viewport.gameObject.AddComponent<RectMask2D>();
            }
        }

        private void RebuildItemLayout(VNBacklogItemView item)
        {
            if (item == null)
                return;

            var rect = item.transform as RectTransform;
            if (rect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }

        private void RebuildContentLayout(bool alignToTop)
        {
            if (isRebuilding)
            {
                pendingRebuild = true;
                return;
            }

            if (contentRoot == null)
                return;

            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
            Canvas.ForceUpdateCanvases();
            if (alignToTop && IsOpen)
            {
                if (!TryGetValidScrollRect(out var scrollRect))
                    return;

                scrollRect.verticalNormalizedPosition = 1f; // 최신순(상단) 정책 고정
            }
        }

        private void ScheduleDeferredLayout(bool alignToTop)
        {
            if (deferredLayoutCoroutine != null)
                StopCoroutine(deferredLayoutCoroutine);

            deferredLayoutCoroutine = StartCoroutine(CoDeferredLayout(alignToTop));
        }

        private System.Collections.IEnumerator CoDeferredLayout(bool alignToTop)
        {
            yield return null; // Destroy 반영 후
            deferredLayoutCoroutine = null;
            if (this == null || !isActiveAndEnabled)
                yield break;

            RebuildContentLayout(alignToTop);
        }

        private bool TryGetValidScrollRect(out ScrollRect scrollRect)
        {
            scrollRect = null;
            if (contentRoot == null)
                return false;

            var sr = contentRoot.GetComponentInParent<ScrollRect>();
            if (sr == null || sr.viewport == null || sr.content == null)
                return false;
            if (sr.content != contentRoot)
                return false;

            scrollRect = sr;
            return true;
        }

        private void SyncOpenState()
        {
            bool openNow = IsOpen;
            if (lastKnownOpen == openNow)
                return;

            lastKnownOpen = openNow;
            OnOpenStateChanged?.Invoke(openNow);
        }

        private void NormalizeItemLayout(VNBacklogItemView item)
        {
            if (item == null)
                return;

            var rootRect = item.transform as RectTransform;
            if (rootRect != null)
            {
                rootRect.anchorMin = new Vector2(0f, 1f);
                rootRect.anchorMax = new Vector2(1f, 1f);
                rootRect.pivot = new Vector2(0.5f, 1f);
                rootRect.sizeDelta = new Vector2(0f, rootRect.sizeDelta.y);
            }

            var rootLayout = item.GetComponent<VerticalLayoutGroup>();
            if (rootLayout == null)
                rootLayout = item.gameObject.AddComponent<VerticalLayoutGroup>();
            rootLayout.childAlignment = TextAnchor.UpperLeft;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childForceExpandHeight = false;
            rootLayout.spacing = 4f;
            rootLayout.padding = new RectOffset(8, 8, 6, 8);

            var rootFitter = item.GetComponent<ContentSizeFitter>();
            if (rootFitter == null)
                rootFitter = item.gameObject.AddComponent<ContentSizeFitter>();
            rootFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            rootFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var rootElement = item.GetComponent<LayoutElement>();
            if (rootElement == null)
                rootElement = item.gameObject.AddComponent<LayoutElement>();
            rootElement.minHeight = 24f;
            rootElement.flexibleHeight = 0f;

            NormalizeTextLayout(item.SpeakerTemplateText, isBody: false);
            NormalizeTextLayout(item.BodyTemplateText, isBody: true);
        }

        private static void NormalizeTextLayout(TMP_Text text, bool isBody)
        {
            if (text == null)
                return;

            text.alignment = TextAlignmentOptions.TopLeft;
            text.enableWordWrapping = isBody;
            text.overflowMode = TextOverflowModes.Overflow;
            text.enableAutoSizing = false;

            if (text.rectTransform != null)
            {
                var rect = text.rectTransform;
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(0f, rect.sizeDelta.y);
            }

            var fitter = text.GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = text.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var element = text.GetComponent<LayoutElement>();
            if (element == null)
                element = text.gameObject.AddComponent<LayoutElement>();
            element.minHeight = isBody ? 22f : 18f;
            element.flexibleHeight = 0f;
        }
    }
}
