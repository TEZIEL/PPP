using System.Collections.Generic;
using UnityEngine;

namespace PPP.BLUE.VN
{
    public sealed class VNBacklogView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private VNBacklogItemView itemPrefab;

        private VNBacklogManager manager;
        private readonly Dictionary<string, VNBacklogItemView> itemByKey = new();

        public void BindManager(VNBacklogManager backlogManager)
        {
            if (manager == backlogManager)
                return;

            UnbindManager();
            manager = backlogManager;
            if (manager == null)
                return;

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
            if (root != null)
                root.SetActive(open);
            else
                gameObject.SetActive(open);

            if (open)
                RebuildAll();
        }

        private void HandleEntryChanged(VNBacklogEntry entry)
        {
            if (entry == null || contentRoot == null || itemPrefab == null)
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

            var created = Instantiate(itemPrefab, contentRoot);
            itemByKey[compositeKey] = created;
            return created;
        }

        private void RebuildAll()
        {
            if (manager == null || contentRoot == null || itemPrefab == null)
                return;

            for (int i = contentRoot.childCount - 1; i >= 0; i--)
                Destroy(contentRoot.GetChild(i).gameObject);
            itemByKey.Clear();

            var entries = manager.GetEntriesForDisplay();
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var item = GetOrCreateItem(entry.CompositeKey);
                item.Bind(entry);
            }
        }
    }
}
