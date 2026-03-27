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

        public bool IsTyping { get; private set; }

        private Coroutine co;
        private string fullTextCache = "";
        private Action onCompletedCache;
        private Action<string> onUpdatedCache;
        private int typingToken = 0;

        public void SetTarget(TMP_Text t) => target = t;

        public void StartTyping(string fullText, Action onCompleted, Action<string> onUpdated = null)
        {
            if (target == null)
            {
                Debug.LogError("[VNTextTyper] target TMP_Text is null.");
                return;
            }

            CancelTyping();
            fullTextCache = fullText ?? "";
            onCompletedCache = onCompleted;
            onUpdatedCache = onUpdated;

            typingToken++;
            int token = typingToken;
            co = StartCoroutine(CoType(fullTextCache, token));
        }

        public void ForceComplete()
        {
            if (!IsTyping || target == null)
                return;

            StopAllCoroutines();
            typingToken++;
            co = null;
            target.maxVisibleCharacters = 999999;
            target.text = fullTextCache;
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

                    int next = Mathf.Min(index + emit, fullText.Length);
                    target.text = fullText.Substring(0, next);
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
    }
}
