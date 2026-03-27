using System;
using System.Collections.Generic;

namespace PPP.BLUE.VN
{
    public sealed class VNBacklogManager
    {
        private const string LogPrefix = "[VN/BACKLOG]";

        private readonly Dictionary<string, VNBacklogEntry> entriesByKey = new();
        private readonly List<VNBacklogEntry> entriesBySequence = new();
        private long nextSequence = 1;

        public event Action<VNBacklogEntry> OnEntryChanged;
        public event Action OnBacklogRestored;

        public VNBacklogEntry BeginOrGetEntry(VNBacklogKey key, string speaker)
        {
            if (key == null)
                return null;

            string composite = key.ToCompositeKey();
            if (entriesByKey.TryGetValue(composite, out var existing))
            {
                if (!string.IsNullOrEmpty(speaker))
                    existing.speaker = speaker;

                DebugLog($"duplicate append blocked key={composite}");
                return existing;
            }

            var entry = new VNBacklogEntry
            {
                scriptId = key.scriptId ?? string.Empty,
                nodeId = key.nodeId ?? string.Empty,
                lineIndex = key.lineIndex,
                speaker = speaker ?? string.Empty,
                text = string.Empty,
                isFinal = false,
                sequence = nextSequence++
            };

            entriesByKey[composite] = entry;
            entriesBySequence.Add(entry);
            DebugLog($"entry created key={composite} seq={entry.sequence}");
            RaiseChanged(entry);
            return entry;
        }

        public void UpdateEntryText(VNBacklogKey key, string partialText)
        {
            if (key == null)
                return;

            var entry = BeginOrGetEntry(key, speaker: string.Empty);
            if (entry == null)
                return;

            if (entry.isFinal)
            {
                DebugLog($"entry update ignored (already final) key={entry.CompositeKey}");
                return;
            }

            entry.text = partialText ?? string.Empty;
            entry.isFinal = false;
            DebugLog($"entry updated key={entry.CompositeKey} chars={entry.text.Length}");
            RaiseChanged(entry);
        }

        public void FinalizeEntry(VNBacklogKey key, string fullText)
        {
            if (key == null)
                return;

            var entry = BeginOrGetEntry(key, speaker: string.Empty);
            if (entry == null)
                return;

            string normalized = fullText ?? string.Empty;
            if (string.IsNullOrEmpty(normalized) && !string.IsNullOrEmpty(entry.text))
                normalized = entry.text;

            if (entry.isFinal && string.Equals(entry.text, normalized, StringComparison.Ordinal))
            {
                DebugLog($"entry finalize ignored (already final) key={entry.CompositeKey}");
                return;
            }

            entry.text = normalized;
            entry.isFinal = true;
            DebugLog($"entry finalized key={entry.CompositeKey} chars={entry.text.Length}");
            RaiseChanged(entry);
        }

        public IReadOnlyList<VNBacklogEntry> GetEntriesForDisplay()
        {
            var result = new List<VNBacklogEntry>(entriesBySequence.Count);
            for (int i = 0; i < entriesBySequence.Count; i++)
                result.Add(entriesBySequence[i].Clone());

            return result;
        }

        public VNBacklogState SerializeBacklog()
        {
            var state = new VNBacklogState
            {
                nextSequence = nextSequence,
                entries = new List<VNBacklogEntry>(entriesBySequence.Count)
            };

            for (int i = 0; i < entriesBySequence.Count; i++)
                state.entries.Add(entriesBySequence[i].Clone());

            DebugLog($"backlog saved entries={state.entries.Count}");
            return state;
        }

        public void RestoreBacklog(VNBacklogState state)
        {
            entriesByKey.Clear();
            entriesBySequence.Clear();

            if (state?.entries != null)
            {
                for (int i = 0; i < state.entries.Count; i++)
                {
                    var entry = state.entries[i];
                    if (entry == null)
                        continue;

                    var cloned = entry.Clone();
                    string composite = cloned.CompositeKey;
                    if (entriesByKey.ContainsKey(composite))
                    {
                        DebugLog($"duplicate append blocked key={composite}");
                        continue;
                    }

                    entriesByKey[composite] = cloned;
                    entriesBySequence.Add(cloned);
                }
            }

            nextSequence = Math.Max(1, state?.nextSequence ?? 1);
            if (entriesBySequence.Count > 0)
                nextSequence = Math.Max(nextSequence, entriesBySequence[entriesBySequence.Count - 1].sequence + 1);

            DebugLog($"backlog restored entries={entriesBySequence.Count}");
            OnBacklogRestored?.Invoke();
        }

        private void RaiseChanged(VNBacklogEntry entry)
        {
            OnEntryChanged?.Invoke(entry.Clone());
        }

        private static void DebugLog(string message)
        {
            UnityEngine.Debug.Log($"{LogPrefix} {message}");
        }
    }
}
