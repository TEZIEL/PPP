using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PPP.BLUE.VN
{
    public sealed class VNCharacterManager : MonoBehaviour
    {
        private const string LeftPosition = "left";
        private const string CenterPosition = "center";
        private const string RightPosition = "right";
        private const string PortraitPosition = "portrait";

        [Header("Sprite Mapping")]
        [SerializeField] private List<VNCharacterSpriteMapping> spriteMappings = new();

        [Header("Optional Character Slots")]
        [SerializeField] private Image leftImage;
        [SerializeField] private Image centerImage;
        [SerializeField] private Image rightImage;
        [SerializeField] private Image portraitImage;

        [Header("Fade")]
        [SerializeField, Min(0f)] private float fadeDuration = 0.25f;
        [SerializeField] private bool logFadeDebug;

        private readonly Dictionary<string, VNCharacterSpriteMapping> spriteLookup = new();
        private readonly Dictionary<string, VNCharacterState> activeStates = new(System.StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<Image, Coroutine> fadeCoroutines = new();

        private void Awake()
        {
            RebuildLookup();
            ClearSlotImages();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            RebuildLookup();
        }
#endif

        public Sprite GetSprite(string characterId, string expressionId)
        {
            TryGetSprite(characterId, expressionId, out Sprite sprite);
            return sprite;
        }

        public bool TryGetSprite(string characterId, string expressionId, out Sprite sprite)
        {
            sprite = null;

            if (string.IsNullOrWhiteSpace(characterId) || string.IsNullOrWhiteSpace(expressionId))
                return false;

            if (spriteLookup.Count == 0)
                RebuildLookup();

            if (!spriteLookup.TryGetValue(BuildKey(characterId, expressionId), out VNCharacterSpriteMapping mapping))
                return false;

            sprite = mapping.sprite;
            return sprite != null;
        }

        public void ShowCharacter(string characterId, string expressionId, string position)
        {
            if (string.IsNullOrWhiteSpace(characterId))
                return;

            if (string.IsNullOrWhiteSpace(expressionId))
                expressionId = "normal";

            if (string.IsNullOrWhiteSpace(position))
                position = CenterPosition;

            var state = new VNCharacterState
            {
                characterId = characterId,
                expressionId = expressionId,
                position = NormalizePosition(position),
                visible = true,
            };

            bool isPortrait = state.position == PortraitPosition;
            bool useFade = !isPortrait;
            LogFadeDebug($"ShowCharacter called: characterId={characterId}, expressionId={expressionId}, position={state.position}, isPortrait={isPortrait}, useFade={useFade}");

            activeStates[characterId] = state;
            ApplyStateToSlot(state, useFade: useFade);
        }

        public void ChangeExpression(string characterId, string expressionId)
        {
            TryChangeExpression(characterId, expressionId);
        }

        public bool TryChangeExpression(string characterId, string expressionId)
        {
            if (string.IsNullOrWhiteSpace(characterId) || string.IsNullOrWhiteSpace(expressionId))
                return false;

            if (!activeStates.TryGetValue(characterId, out VNCharacterState state) || state == null || !state.visible)
                return false;

            if (!TryGetSprite(characterId, expressionId, out Sprite sprite))
            {
                Debug.LogWarning($"[VNCharacterManager] Missing sprite mapping for characterId='{characterId}' expressionId='{expressionId}'. Keeping current expression.");
                return false;
            }

            state.expressionId = expressionId;
            ApplyStateToSlot(state, sprite);
            return true;
        }

        public bool IsCharacterVisible(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId))
                return false;

            return activeStates.TryGetValue(characterId, out VNCharacterState state) && state != null && state.visible;
        }

        public void HideCharacter(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId))
                return;

            if (!activeStates.TryGetValue(characterId, out VNCharacterState state))
                return;

            state.visible = false;

            string normalizedPosition = NormalizePosition(state.position);
            if (normalizedPosition != PortraitPosition)
                FadeOutAndClearSlot(normalizedPosition);

            activeStates.Remove(characterId);
        }

        public void ClearAll()
        {
            activeStates.Clear();
            StopAllSlotFades();
            ClearSlotImages();
        }

        public List<VNCharacterState> CaptureState()
        {
            var states = new List<VNCharacterState>();

            foreach (VNCharacterState state in activeStates.Values)
            {
                states.Add(new VNCharacterState
                {
                    characterId = state.characterId,
                    expressionId = state.expressionId,
                    position = state.position,
                    visible = state.visible,
                });
            }

            return states;
        }

        public void RestoreState(List<VNCharacterState> states)
        {
            ClearAll();

            if (states == null)
                return;

            foreach (VNCharacterState state in states)
            {
                if (state == null || string.IsNullOrWhiteSpace(state.characterId))
                    continue;

                var restoredState = new VNCharacterState
                {
                    characterId = state.characterId,
                    expressionId = string.IsNullOrWhiteSpace(state.expressionId) ? "normal" : state.expressionId,
                    position = NormalizePosition(state.position),
                    visible = state.visible,
                };

                activeStates[restoredState.characterId] = restoredState;

                if (restoredState.visible)
                    ApplyStateToSlot(restoredState);
            }
        }

        private void RebuildLookup()
        {
            spriteLookup.Clear();

            if (spriteMappings == null)
                return;

            foreach (VNCharacterSpriteMapping mapping in spriteMappings)
            {
                if (mapping == null)
                    continue;

                if (string.IsNullOrWhiteSpace(mapping.characterId) || string.IsNullOrWhiteSpace(mapping.expressionId))
                    continue;

                spriteLookup[BuildKey(mapping.characterId, mapping.expressionId)] = mapping;
            }
        }

        private void ApplyStateToSlot(VNCharacterState state)
        {
            ApplyStateToSlot(state, useFade: false);
        }

        private void ApplyStateToSlot(VNCharacterState state, bool useFade)
        {
            if (state == null || !state.visible)
                return;

            string normalizedPosition = NormalizePosition(state.position);
            Image slot = GetSlotImage(normalizedPosition);
            if (slot == null)
            {
                if (normalizedPosition == PortraitPosition)
                    Debug.LogWarning("[VNCharacterManager] portraitImage is not assigned. Portrait sprite update skipped.");

                return;
            }

            Sprite sprite = GetSprite(state.characterId, state.expressionId);
            ApplySpriteToSlot(
                slot,
                sprite,
                normalizedPosition,
                useFade,
                forceAlphaOneWhenImmediate: true,
                stopExistingFade: true);
        }

        private void ApplyStateToSlot(VNCharacterState state, Sprite sprite)
        {
            if (state == null || !state.visible)
                return;

            string normalizedPosition = NormalizePosition(state.position);
            Image slot = GetSlotImage(normalizedPosition);
            if (slot == null)
            {
                if (normalizedPosition == PortraitPosition)
                    Debug.LogWarning("[VNCharacterManager] portraitImage is not assigned. Portrait sprite update skipped.");

                return;
            }

            ApplySpriteToSlot(
                slot,
                sprite,
                normalizedPosition,
                useFade: false,
                forceAlphaOneWhenImmediate: false,
                stopExistingFade: false);
        }

        private void ApplySpriteToSlot(
            Image slot,
            Sprite sprite,
            string normalizedPosition,
            bool useFade,
            bool forceAlphaOneWhenImmediate,
            bool stopExistingFade)
        {
            if (slot == null)
                return;

            if (stopExistingFade)
                StopSlotFade(slot);

            if (normalizedPosition != PortraitPosition)
                slot.gameObject.SetActive(true);

            slot.enabled = sprite != null;
            slot.sprite = sprite;

            if (normalizedPosition == PortraitPosition)
                return;

            if (sprite == null)
            {
                if (forceAlphaOneWhenImmediate)
                    SetAlpha(slot, 1f);

                return;
            }

            if (!useFade || fadeDuration <= 0f)
            {
                if (forceAlphaOneWhenImmediate)
                    SetAlpha(slot, 1f);

                return;
            }

            SetAlpha(slot, 0f);
            LogFadeDebug($"Start fade in: slot={slot.name}, from alpha=0, to alpha=1, duration={fadeDuration:0.###}");
            fadeCoroutines[slot] = StartCoroutine(FadeSlot(slot, 0f, 1f, fadeDuration, clearOnComplete: false));
        }

        private Image GetSlotImage(string position)
        {
            switch (NormalizePosition(position))
            {
                case LeftPosition:
                    return leftImage;
                case RightPosition:
                    return rightImage;
                case PortraitPosition:
                    return portraitImage;
                default:
                    return centerImage;
            }
        }

        private void ClearSlot(string position)
        {
            Image slot = GetSlotImage(position);
            if (slot == null)
                return;

            StopSlotFade(slot);
            slot.sprite = null;
            slot.enabled = false;
            SetAlpha(slot, 1f);
        }

        private void FadeOutAndClearSlot(string position)
        {
            Image slot = GetSlotImage(position);
            if (slot == null)
                return;

            StopSlotFade(slot);

            if (fadeDuration <= 0f || !slot.enabled || slot.sprite == null)
            {
                slot.sprite = null;
                slot.enabled = false;
                SetAlpha(slot, 1f);
                return;
            }

            slot.gameObject.SetActive(true);
            LogFadeDebug($"Start fade out: slot={slot.name}, from alpha={slot.color.a:0.###}, to alpha=0, duration={fadeDuration:0.###}");
            fadeCoroutines[slot] = StartCoroutine(FadeSlot(slot, slot.color.a, 0f, fadeDuration, clearOnComplete: true));
        }

        private IEnumerator FadeSlot(Image slot, float fromAlpha, float toAlpha, float duration, bool clearOnComplete)
        {
            if (slot == null)
                yield break;

            if (duration <= 0f)
            {
                SetAlpha(slot, toAlpha);
            }
            else
            {
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    if (slot == null)
                        yield break;

                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    SetAlpha(slot, Mathf.Lerp(fromAlpha, toAlpha, t));
                    yield return null;
                }

                SetAlpha(slot, toAlpha);
            }

            fadeCoroutines.Remove(slot);

            LogFadeDebug($"Fade complete: slot={slot.name}, final alpha={slot.color.a:0.###}");

            if (clearOnComplete)
            {
                slot.sprite = null;
                slot.enabled = false;
                SetAlpha(slot, 1f);
            }
        }

        private void StopSlotFade(Image slot)
        {
            if (slot == null)
                return;

            if (!fadeCoroutines.TryGetValue(slot, out Coroutine coroutine) || coroutine == null)
                return;

            StopCoroutine(coroutine);
            fadeCoroutines.Remove(slot);
        }

        private void StopAllSlotFades()
        {
            foreach (Coroutine coroutine in fadeCoroutines.Values)
            {
                if (coroutine != null)
                    StopCoroutine(coroutine);
            }

            fadeCoroutines.Clear();
        }

        private void LogFadeDebug(string message)
        {
            if (!logFadeDebug)
                return;

            Debug.Log($"[VNCharacterFade] {message}");
        }

        private static void SetAlpha(Image slot, float alpha)
        {
            if (slot == null)
                return;

            Color color = slot.color;
            color.a = alpha;
            slot.color = color;
        }

        private void ClearSlotImages()
        {
            ClearSlot(LeftPosition);
            ClearSlot(CenterPosition);
            ClearSlot(RightPosition);
        }

        private static string NormalizePosition(string position)
        {
            if (string.IsNullOrWhiteSpace(position))
                return CenterPosition;

            string normalized = position.Trim().ToLowerInvariant();
            if (normalized == LeftPosition || normalized == CenterPosition || normalized == RightPosition || normalized == PortraitPosition)
                return normalized;

            return CenterPosition;
        }

        private static string BuildKey(string characterId, string expressionId)
        {
            return $"{characterId.Trim().ToLowerInvariant()}::{expressionId.Trim().ToLowerInvariant()}";
        }
    }
}
