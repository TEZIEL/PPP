using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace PPP.BLUE.VN
{
    public sealed class VNTextTyper : MonoBehaviour
    {
        [SerializeField] private TMP_Text target;
        [SerializeField] private float charsPerSecond = 40f;
        [SerializeField] private bool enableTypingSfx = true;
        [SerializeField, Min(0f)] private float typingSfxInterval = 0.5f;

        [Header("Typing SFX")]
        [SerializeField] private bool enableTypingSfx = true;
        [SerializeField, Min(0.01f), Tooltip("Minimum seconds between VN typing SFX plays. Higher values reduce rapid chatter.")]
        private float typingSfxInterval = 0.075f;

        public bool IsTyping { get; private set; }

        private Coroutine co;
        private string fullTextCache = "";
        private Action onCompletedCache;
        private Action<string> onUpdatedCache;
        private int typingToken = 0;
        private bool suppressTypingSfx;
        private float lastTypingSfxTime = -999f;

        public void SetTarget(TMP_Text t) => target = t;

        public void StartTyping(string fullText, Action onCompleted, Action<string> onUpdated = null, bool suppressTypingSfx = false)
        {
            StopAllCoroutines();

            if (target == null)
            {
                Debug.LogError("[VNTextTyper] target TMP_Text is null.");
                return;
            }

            CancelTyping();
            fullTextCache = fullText ?? "";
            onCompletedCache = onCompleted;
            onUpdatedCache = onUpdated;
            this.suppressTypingSfx = suppressTypingSfx;
            lastTypingSfxTime = -999f;
            target.maxVisibleCharacters = int.MaxValue;

            typingToken++;
            int token = typingToken;
            co = StartCoroutine(CoType(fullTextCache, token));
        }

        public void ForceComplete()
        {
            if (target == null)
                return;

            StopAllCoroutines();
            typingToken++;
            co = null;

            target.text = fullTextCache;

            target.ForceMeshUpdate(); // 🔥 중요

            target.maxVisibleCharacters = int.MaxValue; // 다음 라인 타이핑이 잘리지 않도록 초기화

            IsTyping = false;

            onUpdatedCache?.Invoke(fullTextCache);

            var cb = onCompletedCache;
            onCompletedCache = null;
            onUpdatedCache = null;
            cb?.Invoke();
        }

        public void CompleteWithPreview(float previewRatio = 0.35f, int minVisibleChars = 3, string trailing = "…")
        {
            if (target == null)
                return;

            CancelTyping();

            string full = fullTextCache ?? string.Empty;
            if (full.Length == 0)
            {
                target.text = string.Empty;
            }
            else
            {
                int currentVisible = target.text != null ? target.text.Length : 0;
                int ratioVisible = Mathf.CeilToInt(full.Length * Mathf.Clamp01(previewRatio));
                int desiredVisible = Mathf.Max(currentVisible + 1, ratioVisible, Mathf.Max(1, minVisibleChars));
                desiredVisible = Mathf.Clamp(desiredVisible, 1, full.Length);

                if (desiredVisible < full.Length)
                {
                    target.text = full.Substring(0, desiredVisible) + (string.IsNullOrEmpty(trailing) ? "" : trailing);
                }
                else
                {
                    target.text = full;
                }
            }

            IsTyping = false;
            onUpdatedCache?.Invoke(target.text);

            var cb = onCompletedCache;
            onCompletedCache = null;
            onUpdatedCache = null;
            cb?.Invoke();
        }

        public void SkipToEnd()
        {
            if (!IsTyping || target == null)
                return;

            CancelTyping();
            target.text = fullTextCache;
            IsTyping = false;
            onUpdatedCache?.Invoke(target.text);

            var cb = onCompletedCache;
            onCompletedCache = null;
            onUpdatedCache = null;
            cb?.Invoke();
        }

        public void StopTyping()
        {
            CancelTyping();
            IsTyping = false;
            onCompletedCache = null;
            onUpdatedCache = null;
            fullTextCache = "";
        }

        private void CancelTyping()
        {
            typingToken++;
            co = null;
            IsTyping = false;
        }

        private IEnumerator CoType(string fullText, int token)
        {
            IsTyping = true;
            target.text = "";

            if (fullText.Length == 0)
            {
                if (token != typingToken)
                    yield break;

                IsTyping = false;
                co = null;
                onUpdatedCache?.Invoke(target.text);

                var cb0 = onCompletedCache;
                onCompletedCache = null;
                onUpdatedCache = null;
                cb0?.Invoke();
                yield break;
            }

            float accumulator = 0f;
            int index = 0;

            while (index < fullText.Length)
            {
                if (token != typingToken)
                    yield break;

                accumulator += charsPerSecond * Time.unscaledDeltaTime;

                int emit = Mathf.FloorToInt(accumulator);
                if (emit > 0)
                {
                    accumulator -= emit;

                    int previous = index;
                    int next = Mathf.Min(index + emit, fullText.Length);
                    target.text = fullText.Substring(0, next);
                    TryPlayTypingSfxForRange(fullText, previous, next);
                    onUpdatedCache?.Invoke(target.text);
                    index = next;
                }

                yield return null;
            }

            if (token != typingToken)
                yield break;

            co = null;
            IsTyping = false;
            onUpdatedCache?.Invoke(target.text);

            var cb = onCompletedCache;
            onCompletedCache = null;
            onUpdatedCache = null;
            cb?.Invoke();
        }

        private void TryPlayTypingSfxForRange(string fullText, int fromInclusive, int toExclusive)
        {
            if (!enableTypingSfx || suppressTypingSfx)
                return;

            if (SoundManager.Instance == null)
                return;

            if (Time.unscaledTime - lastTypingSfxTime < typingSfxInterval)
                return;

            if (!ContainsSoundableCharacter(fullText, fromInclusive, toExclusive))
                return;

            SoundManager.Instance.PlayVNTyping();
            lastTypingSfxTime = Time.unscaledTime;
        }

        private static bool ContainsSoundableCharacter(string text, int fromInclusive, int toExclusive)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            int start = Mathf.Clamp(fromInclusive, 0, text.Length);
            int end = Mathf.Clamp(toExclusive, start, text.Length);

            for (int i = start; i < end; i++)
            {
                if (IsSoundableTypingCharacter(text[i]))
                    return true;
            }

            return false;
        }

        private static bool IsSoundableTypingCharacter(char c)
        {
            if (char.IsWhiteSpace(c))
                return false;

            if (char.IsPunctuation(c))
                return false;

            if (char.IsSymbol(c))
                return false;

            return true;
        }
    }
}
