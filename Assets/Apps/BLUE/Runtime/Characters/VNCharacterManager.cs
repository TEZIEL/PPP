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
        private const string MelionCharacterId = "melion";

        [Header("Character Definitions")]
        [SerializeField] private List<VNCharacterDefinition> characterDefinitions = new();

        [Header("Character Definitions")]
        [SerializeField] private List<VNCharacterDefinition> characterDefinitions = new();

        [Header("Sprite Mapping")]
        [SerializeField] private List<VNCharacterSpriteMapping> spriteMappings = new();

        [Header("Layered Portrait Mapping")]
        [SerializeField] private List<VNLayeredExpressionMapping> layeredExpressionMappings = new();

        [Header("Optional Character Slots")]
        [SerializeField] private Image leftImage;
        [SerializeField] private Image centerImage;
        [SerializeField] private Image rightImage;
        [SerializeField] private Image portraitImage;

        [Header("Optional Layered Portrait Slots")]
        [SerializeField] private Image portraitBaseImage;
        [SerializeField] private Image portraitEyebrowImage;
        [SerializeField] private Image portraitEyeImage;
        [SerializeField] private Image portraitMouthImage;

        [Header("Layered Portrait Blink")]
        [SerializeField, Min(0f)] private float portraitBlinkInitialDelay = 5f;
        [SerializeField, Min(0f)] private float portraitBlinkIntervalMin = 3.5f;
        [SerializeField, Min(0f)] private float portraitBlinkIntervalMax = 6.5f;
        [SerializeField, Min(0.01f)] private float portraitBlinkFrameDuration = 0.06f;

        [Header("Fade")]
        [SerializeField, Min(0f)] private float fadeDuration = 0.25f;
        [SerializeField] private bool logFadeDebug;

        private readonly Dictionary<string, VNCharacterSpriteMapping> spriteLookup = new();
        private readonly Dictionary<string, VNLayeredExpressionMapping> layeredExpressionLookup = new();
        private readonly Dictionary<string, VNCharacterDefinition> characterDefinitionLookup = new(System.StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> speakerCharacterLookup = new(System.StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, VNCharacterState> activeStates = new(System.StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<Image, Coroutine> fadeCoroutines = new();
        private readonly Dictionary<string, VNCharacterState> pendingShows = new(System.StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> fadingOutPositions = new(System.StringComparer.OrdinalIgnoreCase);

        private Coroutine portraitBlinkCoroutine;
        private string portraitBlinkCharacterId;
        private string portraitBlinkExpressionId;

        private void Awake()
        {
            RebuildLookup();
            RebuildLayeredExpressionLookup();
            RebuildDefinitionLookup();
            ClearSlotImages();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            RebuildLookup();
            RebuildLayeredExpressionLookup();
            RebuildDefinitionLookup();
        }
#endif

        private void OnDisable()
        {
            StopLayeredPortraitBlink();
        }

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

        public bool TryGetLayeredExpressionMapping(string characterId, string expressionId, out VNLayeredExpressionMapping mapping)
        {
            mapping = null;

            if (string.IsNullOrWhiteSpace(characterId) || string.IsNullOrWhiteSpace(expressionId))
                return false;

            if (layeredExpressionLookup.Count == 0)
                RebuildLayeredExpressionLookup();

            return layeredExpressionLookup.TryGetValue(BuildKey(characterId, expressionId), out mapping) && mapping != null;
        }

        public bool TryGetCharacterDefinition(string characterId, out VNCharacterDefinition definition)
        {
            definition = null;

            if (string.IsNullOrWhiteSpace(characterId))
                return false;

            if (characterDefinitionLookup.Count == 0)
                RebuildDefinitionLookup();

            return characterDefinitionLookup.TryGetValue(characterId.Trim(), out definition) && definition != null;
        }

        public bool TryResolveCharacterIdForSpeaker(string speakerId, out string characterId)
        {
            characterId = string.Empty;

            if (string.IsNullOrWhiteSpace(speakerId))
                return false;

            if (speakerCharacterLookup.Count == 0)
                RebuildDefinitionLookup();

            return speakerCharacterLookup.TryGetValue(speakerId.Trim(), out characterId) && !string.IsNullOrWhiteSpace(characterId);
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

            if (useFade && fadingOutPositions.Contains(state.position))
            {
                QueuePendingShow(state);
                return;
            }

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

            string normalizedPosition = NormalizePosition(state.position);
            bool useLayeredPortrait = IsLayeredPortraitState(state);
            Sprite sprite = null;
            bool hasSprite = TryGetSprite(characterId, expressionId, out sprite);
            bool hasLayeredMapping = useLayeredPortrait && CanApplyLayeredPortrait(characterId, expressionId);

            if (!hasSprite && !hasLayeredMapping)
            {
                Debug.LogWarning($"[VNCharacterManager] Missing sprite or layered portrait mapping for characterId='{characterId}' expressionId='{expressionId}'. Keeping current expression.");
                return false;
            }

            state.expressionId = expressionId;

            if (pendingShows.TryGetValue(normalizedPosition, out VNCharacterState pendingState)
                && pendingState != null
                && string.Equals(pendingState.characterId, characterId, System.StringComparison.OrdinalIgnoreCase))
            {
                pendingState.expressionId = expressionId;
                return true;
            }

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
            if (pendingShows.TryGetValue(normalizedPosition, out VNCharacterState pendingState)
                && pendingState != null
                && string.Equals(pendingState.characterId, characterId, System.StringComparison.OrdinalIgnoreCase))
            {
                pendingShows.Remove(normalizedPosition);
            }

            if (normalizedPosition != PortraitPosition)
                FadeOutAndClearSlot(normalizedPosition);
            else
                StopLayeredPortraitBlink(characterId);

            activeStates.Remove(characterId);
        }

        public void ClearAll()
        {
            activeStates.Clear();
            pendingShows.Clear();
            fadingOutPositions.Clear();
            StopLayeredPortraitBlink();
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

        private void RebuildLayeredExpressionLookup()
        {
            layeredExpressionLookup.Clear();

            if (layeredExpressionMappings == null)
                return;

            foreach (VNLayeredExpressionMapping mapping in layeredExpressionMappings)
            {
                if (mapping == null)
                    continue;

                if (string.IsNullOrWhiteSpace(mapping.characterId) || string.IsNullOrWhiteSpace(mapping.expressionId))
                    continue;

                layeredExpressionLookup[BuildKey(mapping.characterId, mapping.expressionId)] = mapping;
            }
        }

        private void RebuildDefinitionLookup()
        {
            characterDefinitionLookup.Clear();
            speakerCharacterLookup.Clear();

            if (characterDefinitions == null)
                return;

            foreach (VNCharacterDefinition definition in characterDefinitions)
            {
                if (definition == null || string.IsNullOrWhiteSpace(definition.characterId))
                    continue;

                string characterId = definition.characterId.Trim();
                characterDefinitionLookup[characterId] = definition;
                speakerCharacterLookup[characterId] = characterId;

                if (definition.speakerIds == null)
                    continue;

                foreach (string speakerId in definition.speakerIds)
                {
                    if (string.IsNullOrWhiteSpace(speakerId))
                        continue;

                    speakerCharacterLookup[speakerId.Trim()] = characterId;
                }
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
            if (TryApplyLayeredPortrait(state, warnOnFallback: false))
                return;

            Image slot = GetSlotImage(normalizedPosition);
            if (slot == null)
            {
                if (normalizedPosition == PortraitPosition)
                    Debug.LogWarning("[VNCharacterManager] portraitImage is not assigned. Portrait sprite update skipped.");

                return;
            }

            if (normalizedPosition == PortraitPosition)
                ClearLayeredPortraitImages();

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
            if (TryApplyLayeredPortrait(state, warnOnFallback: false))
                return;

            Image slot = GetSlotImage(normalizedPosition);
            if (slot == null)
            {
                if (normalizedPosition == PortraitPosition)
                    Debug.LogWarning("[VNCharacterManager] portraitImage is not assigned. Portrait sprite update skipped.");

                return;
            }

            if (normalizedPosition == PortraitPosition)
                ClearLayeredPortraitImages();

            ApplySpriteToSlot(
                slot,
                sprite,
                normalizedPosition,
                useFade: false,
                forceAlphaOneWhenImmediate: false,
                stopExistingFade: false);
        }

        private bool TryApplyLayeredPortrait(VNCharacterState state, bool warnOnFallback)
        {
            if (state == null || !IsLayeredPortraitState(state))
                return false;

            if (!CanApplyLayeredPortrait(state.characterId, state.expressionId))
            {
                if (warnOnFallback)
                    Debug.LogWarning($"[VNCharacterManager] Missing layered portrait mapping or Image reference for characterId='{state.characterId}' expressionId='{state.expressionId}'. Falling back to portraitImage.");

                StopLayeredPortraitBlink(state.characterId);
                ClearLayeredPortraitImages();
                return false;
            }

            VNLayeredExpressionMapping mapping = layeredExpressionLookup[BuildKey(state.characterId, state.expressionId)];
            ApplySpriteToImage(portraitBaseImage, mapping.baseSprite);
            ApplySpriteToImage(portraitEyebrowImage, GetEyebrowOpenSprite(mapping));
            ApplySpriteToImage(portraitEyeImage, mapping.eyeOpenSprite);
            ApplySpriteToImage(portraitMouthImage, mapping.mouthClosedSprite);
            RestartLayeredPortraitBlink(state, mapping);

            if (portraitImage != null)
            {
                portraitImage.sprite = null;
                portraitImage.enabled = false;
            }

            return true;
        }

        private void RestartLayeredPortraitBlink(VNCharacterState state, VNLayeredExpressionMapping mapping)
        {
            StopLayeredPortraitBlink();

            if (!CanRunLayeredPortraitBlink(state, mapping) || !isActiveAndEnabled)
                return;

            portraitBlinkCharacterId = state.characterId;
            portraitBlinkExpressionId = state.expressionId;
            portraitBlinkCoroutine = StartCoroutine(RunLayeredPortraitBlink(state.characterId, state.expressionId));
        }

        private IEnumerator RunLayeredPortraitBlink(string characterId, string expressionId)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, portraitBlinkInitialDelay));

            while (IsLayeredPortraitBlinkCurrent(characterId, expressionId))
            {
                if (!TryGetLayeredExpressionMapping(characterId, expressionId, out VNLayeredExpressionMapping mapping)
                    || !CanRunLayeredPortraitBlink(characterId, expressionId, mapping))
                {
                    ClearLayeredPortraitBlinkHandle(characterId, expressionId);
                    yield break;
                }

                yield return PlayLayeredPortraitBlink(mapping);

                float minInterval = Mathf.Min(portraitBlinkIntervalMin, portraitBlinkIntervalMax);
                float maxInterval = Mathf.Max(portraitBlinkIntervalMin, portraitBlinkIntervalMax);
                float nextInterval = Mathf.Approximately(minInterval, maxInterval)
                    ? minInterval
                    : Random.Range(minInterval, maxInterval);

                yield return new WaitForSeconds(nextInterval);
            }

            ClearLayeredPortraitBlinkHandle(characterId, expressionId);
        }

        private IEnumerator PlayLayeredPortraitBlink(VNLayeredExpressionMapping mapping)
        {
            if (mapping == null)
                yield break;

            float frameDuration = Mathf.Max(0.01f, portraitBlinkFrameDuration);
            Sprite eyebrowOpen = GetEyebrowOpenSprite(mapping);
            Sprite eyebrowHalf = mapping.eyebrowBlinkHalfSprite;
            Sprite eyebrowClosed = mapping.eyebrowBlinkClosedSprite;
            Sprite eyeOpen = mapping.eyeOpenSprite;
            Sprite eyeHalf = mapping.eyeBlinkHalfSprite;
            Sprite eyeClosed = GetEyeBlinkClosedSprite(mapping);

            ApplyBlinkFrame(eyebrowHalf, eyeHalf);
            yield return new WaitForSeconds(frameDuration);
            ApplyBlinkFrame(eyebrowClosed, eyeClosed);
            yield return new WaitForSeconds(frameDuration);
            ApplyBlinkFrame(eyebrowHalf, eyeHalf);
            yield return new WaitForSeconds(frameDuration);
            ApplyBlinkFrame(eyebrowOpen, eyeOpen);
        }

        private void ApplyBlinkFrame(Sprite eyebrowSprite, Sprite eyeSprite)
        {
            ApplySpriteToImage(portraitEyebrowImage, eyebrowSprite);
            ApplySpriteToImage(portraitEyeImage, eyeSprite);
        }

        private bool CanRunLayeredPortraitBlink(VNCharacterState state, VNLayeredExpressionMapping mapping)
        {
            if (state == null || !IsMelionLayeredPortraitState(state))
                return false;

            if (!TryGetCharacterDefinition(state.characterId, out VNCharacterDefinition definition) || !definition.supportsBlink)
                return false;

            return CanRunLayeredPortraitBlink(state.characterId, state.expressionId, mapping);
        }

        private bool CanRunLayeredPortraitBlink(string characterId, string expressionId, VNLayeredExpressionMapping mapping)
        {
            return HasLayeredPortraitImages()
                && mapping != null
                && !string.IsNullOrWhiteSpace(characterId)
                && !string.IsNullOrWhiteSpace(expressionId)
                && GetEyebrowOpenSprite(mapping) != null
                && mapping.eyebrowBlinkHalfSprite != null
                && mapping.eyebrowBlinkClosedSprite != null
                && mapping.eyeOpenSprite != null
                && mapping.eyeBlinkHalfSprite != null
                && GetEyeBlinkClosedSprite(mapping) != null;
        }

        private bool IsLayeredPortraitBlinkCurrent(string characterId, string expressionId)
        {
            if (string.IsNullOrWhiteSpace(characterId) || string.IsNullOrWhiteSpace(expressionId))
                return false;

            if (!string.Equals(portraitBlinkCharacterId, characterId, System.StringComparison.OrdinalIgnoreCase)
                || !string.Equals(portraitBlinkExpressionId, expressionId, System.StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!activeStates.TryGetValue(characterId, out VNCharacterState state) || state == null || !state.visible)
                return false;

            return string.Equals(state.expressionId, expressionId, System.StringComparison.OrdinalIgnoreCase)
                && IsMelionLayeredPortraitState(state);
        }

        private bool IsMelionLayeredPortraitState(VNCharacterState state)
        {
            return state != null
                && string.Equals(state.characterId, MelionCharacterId, System.StringComparison.OrdinalIgnoreCase)
                && IsLayeredPortraitState(state);
        }

        private Sprite GetEyebrowOpenSprite(VNLayeredExpressionMapping mapping)
        {
            if (mapping == null)
                return null;

            return mapping.eyebrowOpenSprite != null ? mapping.eyebrowOpenSprite : mapping.eyebrowSprite;
        }

        private Sprite GetEyeBlinkClosedSprite(VNLayeredExpressionMapping mapping)
        {
            if (mapping == null)
                return null;

            return mapping.eyeBlinkClosedSprite != null ? mapping.eyeBlinkClosedSprite : mapping.eyeClosedSprite;
        }

        private void ClearLayeredPortraitBlinkHandle(string characterId, string expressionId)
        {
            if (!string.Equals(portraitBlinkCharacterId, characterId, System.StringComparison.OrdinalIgnoreCase)
                || !string.Equals(portraitBlinkExpressionId, expressionId, System.StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            portraitBlinkCoroutine = null;
            portraitBlinkCharacterId = null;
            portraitBlinkExpressionId = null;
        }

        private void StopLayeredPortraitBlink(string characterId = null)
        {
            if (!string.IsNullOrWhiteSpace(characterId)
                && !string.Equals(portraitBlinkCharacterId, characterId, System.StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (portraitBlinkCoroutine != null)
            {
                StopCoroutine(portraitBlinkCoroutine);
                portraitBlinkCoroutine = null;
            }

            portraitBlinkCharacterId = null;
            portraitBlinkExpressionId = null;
        }

        private bool CanApplyLayeredPortrait(string characterId, string expressionId)
        {
            return HasLayeredPortraitImages() && TryGetLayeredExpressionMapping(characterId, expressionId, out _);
        }

        private bool IsLayeredPortraitState(VNCharacterState state)
        {
            if (state == null || NormalizePosition(state.position) != PortraitPosition)
                return false;

            return TryGetCharacterDefinition(state.characterId, out VNCharacterDefinition definition)
                && definition.renderMode == VNCharacterRenderMode.LayeredPortrait;
        }

        private bool HasLayeredPortraitImages()
        {
            return portraitBaseImage != null
                && portraitEyebrowImage != null
                && portraitEyeImage != null
                && portraitMouthImage != null;
        }

        private void ApplySpriteToImage(Image image, Sprite sprite)
        {
            if (image == null)
                return;

            image.sprite = sprite;
            image.enabled = sprite != null;
        }

        private void ClearLayeredPortraitImages()
        {
            ApplySpriteToImage(portraitBaseImage, null);
            ApplySpriteToImage(portraitEyebrowImage, null);
            ApplySpriteToImage(portraitEyeImage, null);
            ApplySpriteToImage(portraitMouthImage, null);
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
            fadeCoroutines[slot] = StartCoroutine(FadeSlot(slot, normalizedPosition, 0f, 1f, fadeDuration, clearOnComplete: false));
        }

        private void QueuePendingShow(VNCharacterState state)
        {
            if (state == null || !state.visible)
                return;

            string normalizedPosition = NormalizePosition(state.position);
            if (normalizedPosition == PortraitPosition)
            {
                activeStates[state.characterId] = state;
                ApplyStateToSlot(state);
                return;
            }

            if (pendingShows.TryGetValue(normalizedPosition, out VNCharacterState previousPending)
                && previousPending != null
                && !string.Equals(previousPending.characterId, state.characterId, System.StringComparison.OrdinalIgnoreCase))
            {
                activeStates.Remove(previousPending.characterId);
            }

            pendingShows[normalizedPosition] = state;
            activeStates[state.characterId] = state;
            LogFadeDebug($"Queued pending show: characterId={state.characterId}, expressionId={state.expressionId}, position={normalizedPosition}");
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
            string normalizedPosition = NormalizePosition(position);
            Image slot = GetSlotImage(position);
            if (slot == null)
                return;

            StopSlotFade(slot);
            pendingShows.Remove(normalizedPosition);
            fadingOutPositions.Remove(normalizedPosition);
            slot.sprite = null;
            slot.enabled = false;
            SetAlpha(slot, 1f);
        }

        private void FadeOutAndClearSlot(string position)
        {
            string normalizedPosition = NormalizePosition(position);
            Image slot = GetSlotImage(normalizedPosition);
            if (slot == null)
                return;

            if (fadingOutPositions.Contains(normalizedPosition))
                return;

            StopSlotFade(slot);

            if (fadeDuration <= 0f || !slot.enabled || slot.sprite == null)
            {
                slot.sprite = null;
                slot.enabled = false;
                SetAlpha(slot, 1f);
                fadingOutPositions.Remove(normalizedPosition);
                ApplyPendingShow(normalizedPosition);
                return;
            }

            fadingOutPositions.Add(normalizedPosition);
            slot.gameObject.SetActive(true);
            LogFadeDebug($"Start fade out: slot={slot.name}, from alpha={slot.color.a:0.###}, to alpha=0, duration={fadeDuration:0.###}");
            fadeCoroutines[slot] = StartCoroutine(FadeSlot(slot, normalizedPosition, slot.color.a, 0f, fadeDuration, clearOnComplete: true));
        }

        private IEnumerator FadeSlot(Image slot, string normalizedPosition, float fromAlpha, float toAlpha, float duration, bool clearOnComplete)
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
                fadingOutPositions.Remove(normalizedPosition);
                ApplyPendingShow(normalizedPosition);
            }
        }

        private void ApplyPendingShow(string position)
        {
            string normalizedPosition = NormalizePosition(position);
            if (!pendingShows.TryGetValue(normalizedPosition, out VNCharacterState pendingState))
                return;

            pendingShows.Remove(normalizedPosition);

            if (pendingState == null || !pendingState.visible)
                return;

            if (!activeStates.TryGetValue(pendingState.characterId, out VNCharacterState activeState) || activeState == null || !activeState.visible)
                return;

            if (!string.Equals(NormalizePosition(activeState.position), normalizedPosition, System.StringComparison.OrdinalIgnoreCase))
                return;

            ApplyStateToSlot(activeState, useFade: true);
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
