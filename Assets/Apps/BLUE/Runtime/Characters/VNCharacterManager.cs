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

        [Header("Sprite Mapping")]
        [SerializeField] private List<VNCharacterSpriteMapping> spriteMappings = new();

        [Header("Layered Portrait Mapping")]
        [SerializeField] private List<VNLayeredExpressionMapping> layeredExpressionMappings = new();

        [Header("Optional Character Slots")]
        [SerializeField] private Image leftImage;
        [SerializeField] private Image centerImage;
        [SerializeField] private Image rightImage;
        [SerializeField] private Image portraitImage;

        [Header("Optional Layered Character Slots")]
        [SerializeField] private List<VNLayeredCharacterSlot> layeredCharacterSlots = new();

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

        [Header("Layered Character Blink")]
        [SerializeField, Min(0f)] private float layeredCharacterBlinkInitialDelay = 5f;
        [SerializeField, Min(0f)] private float layeredCharacterBlinkMinInterval = 4f;
        [SerializeField, Min(0f)] private float layeredCharacterBlinkMaxInterval = 7f;
        [SerializeField, Min(0.01f)] private float layeredCharacterBlinkFrameInterval = 0.06f;

        [Header("Layered Portrait Mouth")]
        [SerializeField, Min(0.01f)] private float portraitMouthFrameInterval = 0.1f;

        [Header("Layered Character Mouth")]
        [SerializeField, Min(0.01f)] private float layeredCharacterMouthFrameInterval = 0.1f;

        [Header("Fade")]
        [SerializeField, Min(0f)] private float fadeDuration = 0.25f;
        [SerializeField] private bool logFadeDebug;

        private readonly Dictionary<string, VNCharacterSpriteMapping> spriteLookup = new();
        private readonly Dictionary<string, VNLayeredExpressionMapping> layeredExpressionLookup = new();
        private readonly Dictionary<string, VNCharacterDefinition> characterDefinitionLookup = new(System.StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> speakerCharacterLookup = new(System.StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, VNCharacterState> activeStates = new(System.StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<Image, Coroutine> fadeCoroutines = new();
        private readonly Dictionary<string, Coroutine> layeredCharacterFadeCoroutines = new(System.StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Coroutine> layeredCharacterBlinkCoroutines = new(System.StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> layeredCharacterBlinkCharacterIds = new(System.StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> layeredCharacterBlinkExpressionIds = new(System.StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Coroutine> layeredCharacterMouthCoroutines = new(System.StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> layeredCharacterMouthCharacterIds = new(System.StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> layeredCharacterMouthExpressionIds = new(System.StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> layeredCharacterMouthOpenSpriteIndices = new(System.StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> layeredCharacterMouthRequestedCharacterIds = new(System.StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, VNCharacterState> pendingShows = new(System.StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> fadingOutPositions = new(System.StringComparer.OrdinalIgnoreCase);

        private static readonly Vector2 BottomCenterAnchor = new(0.5f, 0f);

        private Coroutine portraitBlinkCoroutine;
        private string portraitBlinkCharacterId;
        private string portraitBlinkExpressionId;
        private Coroutine portraitMouthCoroutine;
        private string portraitMouthCharacterId;
        private string portraitMouthExpressionId;
        private bool portraitMouthAnimationRequested;
        private string portraitMouthRequestedCharacterId;
        private int portraitMouthOpenSpriteIndex;

        [System.Serializable]
        public sealed class VNLayeredCharacterSlot
        {
            public string position = CenterPosition;
            public RectTransform visualRoot;
            public RectTransform partsRoot;
            public Image baseImage;
            public Image eyebrowImage;
            public Image eyeImage;
            public Image mouthImage;
        }

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
            StopAllMouthAnimations();
            StopAllLayeredCharacterBlinks(restoreOpenFrame: true);
            StopLayeredPortraitBlink(restoreOpenFrame: true);
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

        public bool IsMouthAnimationSupported(string characterId)
        {
            if (TryGetActiveMouthState(characterId, out VNCharacterState portraitState))
            {
                if (!TryGetCharacterDefinition(portraitState.characterId, out VNCharacterDefinition portraitDefinition) || !portraitDefinition.supportsMouth)
                    return false;

                return TryGetLayeredExpressionMapping(portraitState.characterId, portraitState.expressionId, out VNLayeredExpressionMapping portraitMapping)
                    && CanRunLayeredPortraitMouth(portraitState.characterId, portraitState.expressionId, portraitMapping);
            }

            if (!TryGetActiveLayeredCharacterMouthState(characterId, out VNCharacterState layeredState, out VNLayeredCharacterSlot layeredSlot, out VNLayeredExpressionMapping layeredMapping))
                return false;

            if (!TryGetCharacterDefinition(layeredState.characterId, out VNCharacterDefinition layeredDefinition) || !layeredDefinition.supportsMouth)
                return false;

            return CanRunLayeredCharacterMouth(NormalizePosition(layeredState.position), layeredState.characterId, layeredState.expressionId, layeredSlot, layeredMapping);
        }

        public void StartMouthAnimation(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId))
                return;

            if (IsMelionCharacterId(characterId))
            {
                StartLayeredPortraitMouthAnimation(characterId);
                return;
            }

            StartLayeredCharacterMouthAnimation(characterId);
        }

        public void StopMouthAnimation(string characterId, bool applyClosed = true)
        {
            if (IsMelionCharacterId(characterId))
                StopMouthAnimationInternal(characterId, applyClosed, clearRequest: true);

            StopLayeredCharacterMouthAnimation(characterId, applyClosed, clearRequest: true);
        }

        public void StopAllMouthAnimations()
        {
            StopMouthAnimationInternal(null, applyClosed: true, clearRequest: true);
            StopAllLayeredCharacterMouthAnimations(applyClosed: true, clearRequest: true);
        }

        private void StartLayeredPortraitMouthAnimation(string characterId)
        {
            portraitMouthAnimationRequested = true;
            portraitMouthRequestedCharacterId = MelionCharacterId;

            StopMouthAnimationInternal(characterId, applyClosed: true, clearRequest: false);

            if (!TryGetActiveMouthState(characterId, out VNCharacterState state))
                return;

            if (!TryGetCharacterDefinition(state.characterId, out VNCharacterDefinition definition) || !definition.supportsMouth)
                return;

            if (!TryGetLayeredExpressionMapping(state.characterId, state.expressionId, out VNLayeredExpressionMapping mapping)
                || !CanRunLayeredPortraitMouth(state.characterId, state.expressionId, mapping)
                || !isActiveAndEnabled)
            {
                ApplyLayeredPortraitMouthClosed(state.characterId, state.expressionId);
                return;
            }

            portraitMouthCharacterId = state.characterId;
            portraitMouthExpressionId = state.expressionId;
            portraitMouthOpenSpriteIndex = 0;
            portraitMouthCoroutine = StartCoroutine(RunLayeredPortraitMouth(state.characterId, state.expressionId));
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

            if (state.position != PortraitPosition)
            {
                StopLayeredCharacterMouth(state.position, applyClosed: true, clearRequest: true);
                StopLayeredCharacterBlink(state.position, restoreOpenFrame: true);
            }

            activeStates[characterId] = state;
            if (IsMelionLayeredPortraitState(state))
                StopMouthAnimation(characterId, applyClosed: false);

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
            bool useLayeredCharacter = IsLayeredCharacterState(state);
            Sprite sprite = null;
            bool hasSprite = TryGetSprite(characterId, expressionId, out sprite);
            bool hasLayeredMapping = (useLayeredPortrait && CanApplyLayeredPortrait(characterId, expressionId))
                || (useLayeredCharacter && CanApplyLayeredCharacter(normalizedPosition, characterId, expressionId));

            if (!hasSprite && !hasLayeredMapping)
            {
                Debug.LogWarning($"[VNCharacterManager] Missing sprite or layered mapping for characterId='{characterId}' expressionId='{expressionId}'. Keeping current expression.");
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
            {
                StopLayeredCharacterMouth(normalizedPosition, applyClosed: true, clearRequest: true);
                StopLayeredCharacterBlink(normalizedPosition, restoreOpenFrame: true);
                FadeOutAndClearSlot(normalizedPosition);
            }
            else
            {
                StopMouthAnimation(characterId, applyClosed: true);
                StopLayeredPortraitBlink(characterId, restoreOpenFrame: true);
            }

            activeStates.Remove(characterId);
        }

        public void ClearAll()
        {
            StopAllMouthAnimations();
            StopAllLayeredCharacterBlinks(restoreOpenFrame: true);
            activeStates.Clear();
            pendingShows.Clear();
            fadingOutPositions.Clear();
            StopLayeredPortraitBlink(restoreOpenFrame: true);
            StopAllSlotFades();
            ClearSlotImages();
            ClearLayeredPortraitImages();
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

        public bool IsCharacterTransitioning(string position)
        {
            string normalizedPosition = NormalizePosition(position);

            if (normalizedPosition == PortraitPosition)
                return false;

            if (fadingOutPositions.Contains(normalizedPosition))
                return true;

            if (pendingShows.ContainsKey(normalizedPosition))
                return true;

            if (layeredCharacterFadeCoroutines.TryGetValue(normalizedPosition, out Coroutine layeredCoroutine)
                && layeredCoroutine != null)
            {
                return true;
            }

            Image slot = GetSlotImage(normalizedPosition);
            if (slot != null
                && fadeCoroutines.TryGetValue(slot, out Coroutine spriteCoroutine)
                && spriteCoroutine != null)
            {
                return true;
            }

            return false;
        }

        public IEnumerator WaitForShowFade(string position)
        {
            string normalizedPosition = NormalizePosition(position);

            if (normalizedPosition == PortraitPosition)
                yield break;

            int guardFrameCount = 0;
            const int maxGuardFrames = 600;

            while (isActiveAndEnabled
                && gameObject != null
                && gameObject.activeInHierarchy
                && IsCharacterTransitioning(normalizedPosition))
            {
                guardFrameCount++;
                if (guardFrameCount > maxGuardFrames)
                {
                    Debug.LogWarning($"[VNCharacterManager] WaitForShowFade timeout. position={normalizedPosition}");
                    yield break;
                }

                yield return null;
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

            if (TryApplyLayeredCharacter(
                state,
                useFade,
                forceAlphaOneWhenImmediate: true,
                stopExistingFade: true,
                warnOnFallback: false))
            {
                return;
            }

            Image slot = GetSlotImage(normalizedPosition);
            if (slot == null)
            {
                if (normalizedPosition == PortraitPosition)
                    Debug.LogWarning("[VNCharacterManager] portraitImage is not assigned. Portrait sprite update skipped.");

                return;
            }

            if (normalizedPosition == PortraitPosition)
                ClearLayeredPortraitImages();
            else
            {
                StopLayeredCharacterMouth(normalizedPosition, applyClosed: false, clearRequest: true);
                StopLayeredCharacterBlink(normalizedPosition, restoreOpenFrame: false);
                ClearLayeredCharacterSlotImages(normalizedPosition);
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
            if (TryApplyLayeredPortrait(state, warnOnFallback: false))
                return;

            if (TryApplyLayeredCharacter(
                state,
                useFade: false,
                forceAlphaOneWhenImmediate: false,
                stopExistingFade: false,
                warnOnFallback: false))
            {
                return;
            }

            Image slot = GetSlotImage(normalizedPosition);
            if (slot == null)
            {
                if (normalizedPosition == PortraitPosition)
                    Debug.LogWarning("[VNCharacterManager] portraitImage is not assigned. Portrait sprite update skipped.");

                return;
            }

            if (normalizedPosition == PortraitPosition)
                ClearLayeredPortraitImages();
            else
            {
                StopLayeredCharacterMouth(normalizedPosition, applyClosed: false, clearRequest: true);
                StopLayeredCharacterBlink(normalizedPosition, restoreOpenFrame: false);
                ClearLayeredCharacterSlotImages(normalizedPosition);
            }

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

                StopLayeredPortraitBlink(state.characterId, restoreOpenFrame: false);
                ClearLayeredPortraitImages();
                return false;
            }

            VNLayeredExpressionMapping mapping = layeredExpressionLookup[BuildKey(state.characterId, state.expressionId)];
            bool restartMouthAnimation = ShouldRestartMouthAnimationAfterLayeredApply(state.characterId);
            StopMouthAnimationInternal(state.characterId, applyClosed: false, clearRequest: false);
            StopLayeredPortraitBlink(state.characterId, restoreOpenFrame: false);
            ApplySpriteToImage(portraitBaseImage, mapping.baseSprite);
            ApplySpriteToImage(portraitEyebrowImage, GetEyebrowOpenSprite(mapping));
            ApplySpriteToImage(portraitEyeImage, mapping.eyeOpenSprite);
            ApplySpriteToImage(portraitMouthImage, mapping.mouthClosedSprite);
            StartLayeredPortraitBlink(state, mapping);
            if (restartMouthAnimation)
                StartMouthAnimation(state.characterId);

            if (portraitImage != null)
            {
                portraitImage.sprite = null;
                portraitImage.enabled = false;
            }

            return true;
        }

        private bool TryApplyLayeredCharacter(
            VNCharacterState state,
            bool useFade,
            bool forceAlphaOneWhenImmediate,
            bool stopExistingFade,
            bool warnOnFallback)
        {
            if (state == null || !IsLayeredCharacterState(state))
                return false;

            string normalizedPosition = NormalizePosition(state.position);
            if (!TryGetLayeredCharacterSlot(normalizedPosition, out VNLayeredCharacterSlot layeredSlot)
                || !HasLayeredCharacterImages(layeredSlot)
                || !TryGetLayeredExpressionMapping(state.characterId, state.expressionId, out VNLayeredExpressionMapping mapping))
            {
                if (warnOnFallback)
                    Debug.LogWarning($"[VNCharacterManager] Missing layered character slot or mapping for characterId='{state.characterId}' expressionId='{state.expressionId}' position='{normalizedPosition}'. Falling back to full sprite slot.");

                StopLayeredCharacterMouth(normalizedPosition, applyClosed: false, clearRequest: true);
                StopLayeredCharacterBlink(normalizedPosition, restoreOpenFrame: false);
                ClearLayeredCharacterSlotImages(normalizedPosition);
                return false;
            }

            Image fullSpriteSlot = GetSlotImage(normalizedPosition);
            if (fullSpriteSlot != null)
            {
                StopSlotFade(fullSpriteSlot);
                fullSpriteSlot.sprite = null;
                fullSpriteSlot.enabled = false;
                SetAlpha(fullSpriteSlot, 1f);
            }

            bool restartLayeredCharacterMouth = ShouldRestartLayeredCharacterMouthAfterApply(state.characterId);
            StopLayeredCharacterMouth(normalizedPosition, applyClosed: false, clearRequest: false);
            StopLayeredCharacterBlink(normalizedPosition, restoreOpenFrame: false);
            UpdateLayeredCharacterRootLayout(layeredSlot, mapping.baseSprite);
            ApplyLayeredCharacterSprites(layeredSlot, mapping);
            ApplyLayeredCharacterVisibility(
                layeredSlot,
                normalizedPosition,
                useFade,
                forceAlphaOneWhenImmediate,
                stopExistingFade);
            StartLayeredCharacterBlink(state, layeredSlot, mapping);
            if (restartLayeredCharacterMouth)
                StartMouthAnimation(state.characterId);
            return true;
        }

        private void UpdateLayeredCharacterRootLayout(VNLayeredCharacterSlot slot, Sprite baseSprite)
        {
            if (slot == null)
                return;

            ConfigureBottomCenterRoot(slot.visualRoot);
            ConfigureBottomCenterRoot(slot.partsRoot);

            if (slot.partsRoot == null || baseSprite == null)
                return;

            // Layered character parts are authored against a shared per-character canvas.
            // Keep PartsRoot sized from the base sprite only so blink/mouth frame swaps do not jitter the RectTransform.
            slot.partsRoot.sizeDelta = baseSprite.rect.size;
        }

        private static void ConfigureBottomCenterRoot(RectTransform rectTransform)
        {
            if (rectTransform == null)
                return;

            rectTransform.anchorMin = BottomCenterAnchor;
            rectTransform.anchorMax = BottomCenterAnchor;
            rectTransform.pivot = BottomCenterAnchor;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localScale = Vector3.one;
        }

        private void ApplyLayeredCharacterSprites(VNLayeredCharacterSlot slot, VNLayeredExpressionMapping mapping)
        {
            ApplySpriteToImage(slot.baseImage, mapping?.baseSprite);
            ApplySpriteToImage(slot.eyebrowImage, GetEyebrowOpenSprite(mapping));
            ApplySpriteToImage(slot.eyeImage, mapping?.eyeOpenSprite);
            ApplySpriteToImage(slot.mouthImage, mapping?.mouthClosedSprite);
        }

        private void ApplyLayeredCharacterVisibility(
            VNLayeredCharacterSlot slot,
            string normalizedPosition,
            bool useFade,
            bool forceAlphaOneWhenImmediate,
            bool stopExistingFade)
        {
            if (slot == null)
                return;

            if (stopExistingFade)
                StopLayeredCharacterFade(normalizedPosition);

            SetLayeredCharacterActive(slot, true);

            if (!useFade || fadeDuration <= 0f)
            {
                if (forceAlphaOneWhenImmediate)
                    SetLayeredCharacterAlpha(slot, 1f);

                return;
            }

            SetLayeredCharacterAlpha(slot, 0f);
            LogFadeDebug($"Start layered character fade in: position={normalizedPosition}, from alpha=0, to alpha=1, duration={fadeDuration:0.###}");
            layeredCharacterFadeCoroutines[normalizedPosition] = StartCoroutine(FadeLayeredCharacterSlot(slot, normalizedPosition, 0f, 1f, fadeDuration, clearOnComplete: false));
        }


        private void StartLayeredCharacterBlink(VNCharacterState state, VNLayeredCharacterSlot slot, VNLayeredExpressionMapping mapping)
        {
            if (!CanRunLayeredCharacterBlink(state, slot, mapping) || !isActiveAndEnabled)
            {
                RestoreLayeredCharacterBlinkOpenFrame(state, slot, mapping);
                return;
            }

            string normalizedPosition = NormalizePosition(state.position);
            StopLayeredCharacterBlink(normalizedPosition, restoreOpenFrame: false);
            RestoreLayeredCharacterBlinkOpenFrame(state, slot, mapping);

            layeredCharacterBlinkCharacterIds[normalizedPosition] = state.characterId;
            layeredCharacterBlinkExpressionIds[normalizedPosition] = state.expressionId;
            layeredCharacterBlinkCoroutines[normalizedPosition] = StartCoroutine(RunLayeredCharacterBlink(normalizedPosition, state.characterId, state.expressionId));
        }

        private IEnumerator RunLayeredCharacterBlink(string normalizedPosition, string characterId, string expressionId)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, layeredCharacterBlinkInitialDelay));

            while (IsLayeredCharacterBlinkCurrent(normalizedPosition, characterId, expressionId))
            {
                if (!TryGetLayeredCharacterSlot(normalizedPosition, out VNLayeredCharacterSlot slot)
                    || !TryGetLayeredExpressionMapping(characterId, expressionId, out VNLayeredExpressionMapping mapping)
                    || !CanRunLayeredCharacterBlink(normalizedPosition, characterId, expressionId, slot, mapping))
                {
                    RestoreLayeredCharacterBlinkOpenFrame(normalizedPosition, characterId, expressionId);
                    ClearLayeredCharacterBlinkHandle(normalizedPosition, characterId, expressionId);
                    yield break;
                }

                yield return PlayLayeredCharacterBlink(normalizedPosition, characterId, expressionId, mapping);

                if (!IsLayeredCharacterBlinkCurrent(normalizedPosition, characterId, expressionId))
                    break;

                float minInterval = Mathf.Min(layeredCharacterBlinkMinInterval, layeredCharacterBlinkMaxInterval);
                float maxInterval = Mathf.Max(layeredCharacterBlinkMinInterval, layeredCharacterBlinkMaxInterval);
                float nextInterval = Mathf.Approximately(minInterval, maxInterval)
                    ? minInterval
                    : Random.Range(minInterval, maxInterval);

                yield return new WaitForSeconds(nextInterval);
            }

            RestoreLayeredCharacterBlinkOpenFrame(normalizedPosition, characterId, expressionId);
            ClearLayeredCharacterBlinkHandle(normalizedPosition, characterId, expressionId);
        }

        private IEnumerator PlayLayeredCharacterBlink(string normalizedPosition, string characterId, string expressionId, VNLayeredExpressionMapping mapping)
        {
            if (mapping == null)
                yield break;

            float frameInterval = Mathf.Max(0.01f, layeredCharacterBlinkFrameInterval);
            Sprite eyebrowOpen = GetEyebrowOpenSprite(mapping);
            Sprite eyebrowHalf = mapping.eyebrowBlinkHalfSprite;
            Sprite eyebrowClosed = mapping.eyebrowBlinkClosedSprite;
            Sprite eyeOpen = mapping.eyeOpenSprite;
            Sprite eyeHalf = mapping.eyeBlinkHalfSprite;
            Sprite eyeClosed = GetEyeBlinkClosedSprite(mapping);

            if (!TryApplyLayeredCharacterBlinkFrame(normalizedPosition, characterId, expressionId, eyebrowHalf, eyeHalf))
                yield break;

            yield return new WaitForSeconds(frameInterval);
            if (!TryApplyLayeredCharacterBlinkFrame(normalizedPosition, characterId, expressionId, eyebrowClosed, eyeClosed))
                yield break;

            yield return new WaitForSeconds(frameInterval);
            if (!TryApplyLayeredCharacterBlinkFrame(normalizedPosition, characterId, expressionId, eyebrowHalf, eyeHalf))
                yield break;

            yield return new WaitForSeconds(frameInterval);
            TryApplyLayeredCharacterBlinkFrame(normalizedPosition, characterId, expressionId, eyebrowOpen, eyeOpen);
        }

        private bool TryApplyLayeredCharacterBlinkFrame(string normalizedPosition, string characterId, string expressionId, Sprite eyebrowSprite, Sprite eyeSprite)
        {
            if (!IsLayeredCharacterBlinkCurrent(normalizedPosition, characterId, expressionId)
                || !TryGetLayeredCharacterSlot(normalizedPosition, out VNLayeredCharacterSlot slot)
                || !HasActiveLayeredCharacterBlinkImages(slot))
            {
                return false;
            }

            ApplySpriteToImage(slot.eyebrowImage, eyebrowSprite);
            ApplySpriteToImage(slot.eyeImage, eyeSprite);
            return true;
        }

        private bool CanRunLayeredCharacterBlink(VNCharacterState state, VNLayeredCharacterSlot slot, VNLayeredExpressionMapping mapping)
        {
            if (state == null || !IsLayeredCharacterState(state))
                return false;

            if (!TryGetCharacterDefinition(state.characterId, out VNCharacterDefinition definition) || !definition.supportsBlink)
                return false;

            return CanRunLayeredCharacterBlink(NormalizePosition(state.position), state.characterId, state.expressionId, slot, mapping);
        }

        private bool CanRunLayeredCharacterBlink(string normalizedPosition, string characterId, string expressionId, VNLayeredCharacterSlot slot, VNLayeredExpressionMapping mapping)
        {
            return NormalizePosition(normalizedPosition) != PortraitPosition
                && HasActiveLayeredCharacterBlinkImages(slot)
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

        private bool IsLayeredCharacterBlinkCurrent(string normalizedPosition, string characterId, string expressionId)
        {
            normalizedPosition = NormalizePosition(normalizedPosition);
            if (normalizedPosition == PortraitPosition
                || string.IsNullOrWhiteSpace(characterId)
                || string.IsNullOrWhiteSpace(expressionId))
            {
                return false;
            }

            if (!layeredCharacterBlinkCharacterIds.TryGetValue(normalizedPosition, out string currentCharacterId)
                || !layeredCharacterBlinkExpressionIds.TryGetValue(normalizedPosition, out string currentExpressionId)
                || !string.Equals(currentCharacterId, characterId, System.StringComparison.OrdinalIgnoreCase)
                || !string.Equals(currentExpressionId, expressionId, System.StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!activeStates.TryGetValue(characterId, out VNCharacterState state) || state == null || !state.visible)
                return false;

            return string.Equals(NormalizePosition(state.position), normalizedPosition, System.StringComparison.OrdinalIgnoreCase)
                && string.Equals(state.expressionId, expressionId, System.StringComparison.OrdinalIgnoreCase)
                && IsLayeredCharacterState(state);
        }

        private void RestoreLayeredCharacterBlinkOpenFrame(VNCharacterState state, VNLayeredCharacterSlot slot, VNLayeredExpressionMapping mapping)
        {
            if (state == null || slot == null || mapping == null || !IsLayeredCharacterState(state))
                return;

            ApplySpriteToImage(slot.eyebrowImage, GetEyebrowOpenSprite(mapping));
            ApplySpriteToImage(slot.eyeImage, mapping.eyeOpenSprite);
        }

        private void RestoreLayeredCharacterBlinkOpenFrame(string normalizedPosition, string characterId, string expressionId)
        {
            normalizedPosition = NormalizePosition(normalizedPosition);
            if (normalizedPosition == PortraitPosition
                || string.IsNullOrWhiteSpace(characterId)
                || string.IsNullOrWhiteSpace(expressionId))
            {
                return;
            }

            if (!TryGetLayeredCharacterSlot(normalizedPosition, out VNLayeredCharacterSlot slot))
                return;

            if (!TryGetLayeredExpressionMapping(characterId, expressionId, out VNLayeredExpressionMapping mapping))
                return;

            ApplySpriteToImage(slot.eyebrowImage, GetEyebrowOpenSprite(mapping));
            ApplySpriteToImage(slot.eyeImage, mapping.eyeOpenSprite);
        }

        private bool HasActiveLayeredCharacterBlinkImages(VNLayeredCharacterSlot slot)
        {
            return slot != null && IsActiveImage(slot.eyebrowImage) && IsActiveImage(slot.eyeImage);
        }

        private void ClearLayeredCharacterBlinkHandle(string normalizedPosition, string characterId, string expressionId)
        {
            normalizedPosition = NormalizePosition(normalizedPosition);
            if (!layeredCharacterBlinkCharacterIds.TryGetValue(normalizedPosition, out string currentCharacterId)
                || !layeredCharacterBlinkExpressionIds.TryGetValue(normalizedPosition, out string currentExpressionId)
                || !string.Equals(currentCharacterId, characterId, System.StringComparison.OrdinalIgnoreCase)
                || !string.Equals(currentExpressionId, expressionId, System.StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            layeredCharacterBlinkCoroutines.Remove(normalizedPosition);
            layeredCharacterBlinkCharacterIds.Remove(normalizedPosition);
            layeredCharacterBlinkExpressionIds.Remove(normalizedPosition);
        }

        private void StopLayeredCharacterBlink(string position, bool restoreOpenFrame)
        {
            string normalizedPosition = NormalizePosition(position);
            if (normalizedPosition == PortraitPosition)
                return;

            layeredCharacterBlinkCharacterIds.TryGetValue(normalizedPosition, out string stoppedCharacterId);
            layeredCharacterBlinkExpressionIds.TryGetValue(normalizedPosition, out string stoppedExpressionId);

            if (layeredCharacterBlinkCoroutines.TryGetValue(normalizedPosition, out Coroutine coroutine) && coroutine != null)
                StopCoroutine(coroutine);

            layeredCharacterBlinkCoroutines.Remove(normalizedPosition);
            layeredCharacterBlinkCharacterIds.Remove(normalizedPosition);
            layeredCharacterBlinkExpressionIds.Remove(normalizedPosition);

            if (restoreOpenFrame)
                RestoreLayeredCharacterBlinkOpenFrame(normalizedPosition, stoppedCharacterId, stoppedExpressionId);
        }

        private void StopAllLayeredCharacterBlinks(bool restoreOpenFrame)
        {
            var positions = new List<string>(layeredCharacterBlinkCoroutines.Keys);
            for (int i = 0; i < positions.Count; i++)
                StopLayeredCharacterBlink(positions[i], restoreOpenFrame);

            layeredCharacterBlinkCoroutines.Clear();
            layeredCharacterBlinkCharacterIds.Clear();
            layeredCharacterBlinkExpressionIds.Clear();
        }

        private bool ShouldRestartMouthAnimationAfterLayeredApply(string characterId)
        {
            return IsMelionCharacterId(characterId)
                && (IsMouthAnimationCurrent(characterId)
                    || (portraitMouthAnimationRequested && IsMelionCharacterId(portraitMouthRequestedCharacterId)));
        }

        private IEnumerator RunLayeredPortraitMouth(string characterId, string expressionId)
        {
            float frameInterval = Mathf.Max(0.01f, portraitMouthFrameInterval);

            while (IsLayeredPortraitMouthCurrent(characterId, expressionId))
            {
                if (!TryGetLayeredExpressionMapping(characterId, expressionId, out VNLayeredExpressionMapping mapping)
                    || !CanRunLayeredPortraitMouth(characterId, expressionId, mapping))
                {
                    ApplyLayeredPortraitMouthClosed(characterId, expressionId);
                    ClearLayeredPortraitMouthHandle(characterId, expressionId);
                    yield break;
                }

                Sprite openSprite = GetNextMouthOpenSprite(mapping);
                if (openSprite == null || !TryApplyMouthFrame(characterId, expressionId, openSprite))
                    break;

                yield return new WaitForSeconds(frameInterval);

                if (!TryApplyMouthFrame(characterId, expressionId, mapping.mouthClosedSprite))
                    break;

                yield return new WaitForSeconds(frameInterval);
            }

            ApplyLayeredPortraitMouthClosed(characterId, expressionId);
            ClearLayeredPortraitMouthHandle(characterId, expressionId);
        }

        private Sprite GetNextMouthOpenSprite(VNLayeredExpressionMapping mapping)
        {
            if (mapping?.mouthOpenSprites == null || mapping.mouthOpenSprites.Count == 0)
                return null;

            int count = mapping.mouthOpenSprites.Count;
            for (int i = 0; i < count; i++)
            {
                int index = Mathf.Abs(portraitMouthOpenSpriteIndex++) % count;
                Sprite sprite = mapping.mouthOpenSprites[index];
                if (sprite != null)
                    return sprite;
            }

            return null;
        }

        private bool TryApplyMouthFrame(string characterId, string expressionId, Sprite sprite)
        {
            if (!IsLayeredPortraitMouthCurrent(characterId, expressionId) || !IsActiveImage(portraitMouthImage))
                return false;

            ApplySpriteToImage(portraitMouthImage, sprite);
            return true;
        }

        private void ApplyLayeredPortraitMouthClosed(string characterId, string expressionId)
        {
            if (string.IsNullOrWhiteSpace(characterId) || string.IsNullOrWhiteSpace(expressionId))
                return;

            if (!TryGetLayeredExpressionMapping(characterId, expressionId, out VNLayeredExpressionMapping mapping))
                return;

            if (mapping?.mouthClosedSprite == null || !IsActiveImage(portraitMouthImage))
                return;

            ApplySpriteToImage(portraitMouthImage, mapping.mouthClosedSprite);
        }

        private void StopMouthAnimationInternal(string characterId, bool applyClosed, bool clearRequest)
        {
            if (!string.IsNullOrWhiteSpace(characterId)
                && !IsMelionCharacterId(characterId)
                && !string.Equals(portraitMouthCharacterId, characterId, System.StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(characterId)
                && !string.IsNullOrWhiteSpace(portraitMouthCharacterId)
                && !string.Equals(portraitMouthCharacterId, characterId, System.StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string stoppedCharacterId = portraitMouthCharacterId;
            string stoppedExpressionId = portraitMouthExpressionId;

            if (portraitMouthCoroutine != null)
            {
                StopCoroutine(portraitMouthCoroutine);
                portraitMouthCoroutine = null;
            }

            portraitMouthCharacterId = null;
            portraitMouthExpressionId = null;
            portraitMouthOpenSpriteIndex = 0;

            if (clearRequest)
            {
                portraitMouthAnimationRequested = false;
                portraitMouthRequestedCharacterId = null;
            }

            if (!applyClosed)
                return;

            if (!string.IsNullOrWhiteSpace(stoppedCharacterId) && !string.IsNullOrWhiteSpace(stoppedExpressionId))
            {
                ApplyLayeredPortraitMouthClosed(stoppedCharacterId, stoppedExpressionId);
                return;
            }

            if (!string.IsNullOrWhiteSpace(characterId)
                && activeStates.TryGetValue(characterId, out VNCharacterState state)
                && state != null)
            {
                ApplyLayeredPortraitMouthClosed(state.characterId, state.expressionId);
            }
        }

        private bool IsLayeredPortraitMouthCurrent(string characterId, string expressionId)
        {
            if (string.IsNullOrWhiteSpace(characterId) || string.IsNullOrWhiteSpace(expressionId))
                return false;

            if (!string.Equals(portraitMouthCharacterId, characterId, System.StringComparison.OrdinalIgnoreCase)
                || !string.Equals(portraitMouthExpressionId, expressionId, System.StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!activeStates.TryGetValue(characterId, out VNCharacterState state) || state == null || !state.visible)
                return false;

            return string.Equals(state.expressionId, expressionId, System.StringComparison.OrdinalIgnoreCase)
                && IsMelionLayeredPortraitState(state);
        }

        private bool IsMouthAnimationCurrent(string characterId)
        {
            return !string.IsNullOrWhiteSpace(characterId)
                && !string.IsNullOrWhiteSpace(portraitMouthCharacterId)
                && string.Equals(portraitMouthCharacterId, characterId, System.StringComparison.OrdinalIgnoreCase);
        }

        private void ClearLayeredPortraitMouthHandle(string characterId, string expressionId)
        {
            if (!string.Equals(portraitMouthCharacterId, characterId, System.StringComparison.OrdinalIgnoreCase)
                || !string.Equals(portraitMouthExpressionId, expressionId, System.StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            portraitMouthCoroutine = null;
            portraitMouthCharacterId = null;
            portraitMouthExpressionId = null;
            portraitMouthOpenSpriteIndex = 0;
        }


        private void StartLayeredCharacterMouthAnimation(string characterId)
        {
            if (!TryGetActiveLayeredCharacterMouthState(characterId, out VNCharacterState state, out VNLayeredCharacterSlot slot, out VNLayeredExpressionMapping mapping))
                return;

            if (!TryGetCharacterDefinition(state.characterId, out VNCharacterDefinition definition) || !definition.supportsMouth)
                return;

            layeredCharacterMouthRequestedCharacterIds.Add(state.characterId);
            string normalizedPosition = NormalizePosition(state.position);
            StopLayeredCharacterMouth(normalizedPosition, applyClosed: true, clearRequest: false);

            if (!CanRunLayeredCharacterMouth(normalizedPosition, state.characterId, state.expressionId, slot, mapping) || !isActiveAndEnabled)
            {
                ApplyLayeredCharacterMouthClosed(normalizedPosition, state.characterId, state.expressionId);
                return;
            }

            layeredCharacterMouthCharacterIds[normalizedPosition] = state.characterId;
            layeredCharacterMouthExpressionIds[normalizedPosition] = state.expressionId;
            layeredCharacterMouthOpenSpriteIndices[normalizedPosition] = 0;
            layeredCharacterMouthCoroutines[normalizedPosition] = StartCoroutine(RunLayeredCharacterMouth(normalizedPosition, state.characterId, state.expressionId));
        }

        private IEnumerator RunLayeredCharacterMouth(string normalizedPosition, string characterId, string expressionId)
        {
            float frameInterval = Mathf.Max(0.01f, layeredCharacterMouthFrameInterval);

            while (IsLayeredCharacterMouthCurrent(normalizedPosition, characterId, expressionId))
            {
                if (!TryGetLayeredCharacterSlot(normalizedPosition, out VNLayeredCharacterSlot slot)
                    || !TryGetLayeredExpressionMapping(characterId, expressionId, out VNLayeredExpressionMapping mapping)
                    || !CanRunLayeredCharacterMouth(normalizedPosition, characterId, expressionId, slot, mapping))
                {
                    ApplyLayeredCharacterMouthClosed(normalizedPosition, characterId, expressionId);
                    ClearLayeredCharacterMouthHandle(normalizedPosition, characterId, expressionId);
                    yield break;
                }

                Sprite openSprite = GetNextLayeredCharacterMouthOpenSprite(normalizedPosition, mapping);
                if (openSprite == null || !TryApplyLayeredCharacterMouthFrame(normalizedPosition, characterId, expressionId, openSprite))
                    break;

                yield return new WaitForSeconds(frameInterval);

                if (!TryApplyLayeredCharacterMouthFrame(normalizedPosition, characterId, expressionId, mapping.mouthClosedSprite))
                    break;

                yield return new WaitForSeconds(frameInterval);
            }

            ApplyLayeredCharacterMouthClosed(normalizedPosition, characterId, expressionId);
            ClearLayeredCharacterMouthHandle(normalizedPosition, characterId, expressionId);
        }

        private Sprite GetNextLayeredCharacterMouthOpenSprite(string normalizedPosition, VNLayeredExpressionMapping mapping)
        {
            if (mapping?.mouthOpenSprites == null || mapping.mouthOpenSprites.Count == 0)
                return null;

            normalizedPosition = NormalizePosition(normalizedPosition);
            if (!layeredCharacterMouthOpenSpriteIndices.TryGetValue(normalizedPosition, out int nextIndex))
                nextIndex = 0;

            int count = mapping.mouthOpenSprites.Count;
            for (int i = 0; i < count; i++)
            {
                int index = Mathf.Abs(nextIndex++) % count;
                Sprite sprite = mapping.mouthOpenSprites[index];
                if (sprite != null)
                {
                    layeredCharacterMouthOpenSpriteIndices[normalizedPosition] = nextIndex;
                    return sprite;
                }
            }

            layeredCharacterMouthOpenSpriteIndices[normalizedPosition] = nextIndex;
            return null;
        }

        private bool TryApplyLayeredCharacterMouthFrame(string normalizedPosition, string characterId, string expressionId, Sprite sprite)
        {
            if (!IsLayeredCharacterMouthCurrent(normalizedPosition, characterId, expressionId)
                || !TryGetLayeredCharacterSlot(normalizedPosition, out VNLayeredCharacterSlot slot)
                || !IsActiveImage(slot.mouthImage))
            {
                return false;
            }

            ApplySpriteToImage(slot.mouthImage, sprite);
            return true;
        }

        private void ApplyLayeredCharacterMouthClosed(string normalizedPosition, string characterId, string expressionId)
        {
            normalizedPosition = NormalizePosition(normalizedPosition);
            if (normalizedPosition == PortraitPosition
                || string.IsNullOrWhiteSpace(characterId)
                || string.IsNullOrWhiteSpace(expressionId))
            {
                return;
            }

            if (!TryGetLayeredCharacterSlot(normalizedPosition, out VNLayeredCharacterSlot slot))
                return;

            if (!TryGetLayeredExpressionMapping(characterId, expressionId, out VNLayeredExpressionMapping mapping))
                return;

            if (mapping?.mouthClosedSprite == null || !IsActiveImage(slot.mouthImage))
                return;

            ApplySpriteToImage(slot.mouthImage, mapping.mouthClosedSprite);
        }

        private bool CanRunLayeredCharacterMouth(string normalizedPosition, string characterId, string expressionId, VNLayeredCharacterSlot slot, VNLayeredExpressionMapping mapping)
        {
            return NormalizePosition(normalizedPosition) != PortraitPosition
                && slot != null
                && IsActiveImage(slot.mouthImage)
                && mapping != null
                && !string.IsNullOrWhiteSpace(characterId)
                && !string.IsNullOrWhiteSpace(expressionId)
                && mapping.mouthClosedSprite != null
                && HasAnyMouthOpenSprite(mapping);
        }

        private bool IsLayeredCharacterMouthCurrent(string normalizedPosition, string characterId, string expressionId)
        {
            normalizedPosition = NormalizePosition(normalizedPosition);
            if (normalizedPosition == PortraitPosition
                || string.IsNullOrWhiteSpace(characterId)
                || string.IsNullOrWhiteSpace(expressionId))
            {
                return false;
            }

            if (!layeredCharacterMouthCharacterIds.TryGetValue(normalizedPosition, out string currentCharacterId)
                || !layeredCharacterMouthExpressionIds.TryGetValue(normalizedPosition, out string currentExpressionId)
                || !string.Equals(currentCharacterId, characterId, System.StringComparison.OrdinalIgnoreCase)
                || !string.Equals(currentExpressionId, expressionId, System.StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!activeStates.TryGetValue(characterId, out VNCharacterState state) || state == null || !state.visible)
                return false;

            return string.Equals(NormalizePosition(state.position), normalizedPosition, System.StringComparison.OrdinalIgnoreCase)
                && string.Equals(state.expressionId, expressionId, System.StringComparison.OrdinalIgnoreCase)
                && IsLayeredCharacterState(state);
        }

        private void ClearLayeredCharacterMouthHandle(string normalizedPosition, string characterId, string expressionId)
        {
            normalizedPosition = NormalizePosition(normalizedPosition);
            if (!layeredCharacterMouthCharacterIds.TryGetValue(normalizedPosition, out string currentCharacterId)
                || !layeredCharacterMouthExpressionIds.TryGetValue(normalizedPosition, out string currentExpressionId)
                || !string.Equals(currentCharacterId, characterId, System.StringComparison.OrdinalIgnoreCase)
                || !string.Equals(currentExpressionId, expressionId, System.StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            layeredCharacterMouthCoroutines.Remove(normalizedPosition);
            layeredCharacterMouthCharacterIds.Remove(normalizedPosition);
            layeredCharacterMouthExpressionIds.Remove(normalizedPosition);
            layeredCharacterMouthOpenSpriteIndices.Remove(normalizedPosition);
        }

        private void StopLayeredCharacterMouthAnimation(string characterId, bool applyClosed, bool clearRequest)
        {
            if (string.IsNullOrWhiteSpace(characterId))
                return;

            var positions = new List<string>();
            foreach (KeyValuePair<string, string> entry in layeredCharacterMouthCharacterIds)
            {
                if (string.Equals(entry.Value, characterId, System.StringComparison.OrdinalIgnoreCase))
                    positions.Add(entry.Key);
            }

            for (int i = 0; i < positions.Count; i++)
                StopLayeredCharacterMouth(positions[i], applyClosed, clearRequest);

            if (clearRequest)
                layeredCharacterMouthRequestedCharacterIds.Remove(characterId);

            if (positions.Count == 0
                && applyClosed
                && activeStates.TryGetValue(characterId, out VNCharacterState state)
                && state != null
                && IsLayeredCharacterState(state))
            {
                ApplyLayeredCharacterMouthClosed(NormalizePosition(state.position), state.characterId, state.expressionId);
            }
        }

        private void StopLayeredCharacterMouth(string position, bool applyClosed, bool clearRequest)
        {
            string normalizedPosition = NormalizePosition(position);
            if (normalizedPosition == PortraitPosition)
                return;

            layeredCharacterMouthCharacterIds.TryGetValue(normalizedPosition, out string stoppedCharacterId);
            layeredCharacterMouthExpressionIds.TryGetValue(normalizedPosition, out string stoppedExpressionId);

            if (layeredCharacterMouthCoroutines.TryGetValue(normalizedPosition, out Coroutine coroutine) && coroutine != null)
                StopCoroutine(coroutine);

            layeredCharacterMouthCoroutines.Remove(normalizedPosition);
            layeredCharacterMouthCharacterIds.Remove(normalizedPosition);
            layeredCharacterMouthExpressionIds.Remove(normalizedPosition);
            layeredCharacterMouthOpenSpriteIndices.Remove(normalizedPosition);

            if (clearRequest && !string.IsNullOrWhiteSpace(stoppedCharacterId))
                layeredCharacterMouthRequestedCharacterIds.Remove(stoppedCharacterId);

            if (applyClosed)
                ApplyLayeredCharacterMouthClosed(normalizedPosition, stoppedCharacterId, stoppedExpressionId);
        }

        private void StopAllLayeredCharacterMouthAnimations(bool applyClosed, bool clearRequest)
        {
            var positions = new List<string>(layeredCharacterMouthCoroutines.Keys);
            foreach (KeyValuePair<string, string> entry in layeredCharacterMouthCharacterIds)
            {
                if (!positions.Contains(entry.Key))
                    positions.Add(entry.Key);
            }

            for (int i = 0; i < positions.Count; i++)
                StopLayeredCharacterMouth(positions[i], applyClosed, clearRequest);

            layeredCharacterMouthCoroutines.Clear();
            layeredCharacterMouthCharacterIds.Clear();
            layeredCharacterMouthExpressionIds.Clear();
            layeredCharacterMouthOpenSpriteIndices.Clear();

            if (clearRequest)
                layeredCharacterMouthRequestedCharacterIds.Clear();
        }

        private bool ShouldRestartLayeredCharacterMouthAfterApply(string characterId)
        {
            return !string.IsNullOrWhiteSpace(characterId)
                && layeredCharacterMouthRequestedCharacterIds.Contains(characterId);
        }

        private bool TryGetActiveLayeredCharacterMouthState(string characterId, out VNCharacterState state, out VNLayeredCharacterSlot slot, out VNLayeredExpressionMapping mapping)
        {
            state = null;
            slot = null;
            mapping = null;

            if (string.IsNullOrWhiteSpace(characterId))
                return false;

            if (!activeStates.TryGetValue(characterId, out state) || state == null || !state.visible || !IsLayeredCharacterState(state))
                return false;

            string normalizedPosition = NormalizePosition(state.position);
            return TryGetLayeredCharacterSlot(normalizedPosition, out slot)
                && HasLayeredCharacterImages(slot)
                && TryGetLayeredExpressionMapping(state.characterId, state.expressionId, out mapping);
        }

        private bool CanRunLayeredPortraitMouth(string characterId, string expressionId, VNLayeredExpressionMapping mapping)
        {
            return IsMelionCharacterId(characterId)
                && IsActiveImage(portraitMouthImage)
                && mapping != null
                && !string.IsNullOrWhiteSpace(expressionId)
                && mapping.mouthClosedSprite != null
                && HasAnyMouthOpenSprite(mapping);
        }

        private bool HasAnyMouthOpenSprite(VNLayeredExpressionMapping mapping)
        {
            if (mapping?.mouthOpenSprites == null)
                return false;

            for (int i = 0; i < mapping.mouthOpenSprites.Count; i++)
            {
                if (mapping.mouthOpenSprites[i] != null)
                    return true;
            }

            return false;
        }

        private bool TryGetActiveMouthState(string characterId, out VNCharacterState state)
        {
            state = null;

            if (!IsMelionCharacterId(characterId))
                return false;

            return activeStates.TryGetValue(MelionCharacterId, out state)
                && state != null
                && state.visible
                && IsMelionLayeredPortraitState(state);
        }

        private void StartLayeredPortraitBlink(VNCharacterState state, VNLayeredExpressionMapping mapping)
        {
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
                    RestoreLayeredPortraitBlinkOpenFrame(characterId, expressionId);
                    ClearLayeredPortraitBlinkHandle(characterId, expressionId);
                    yield break;
                }

                yield return PlayLayeredPortraitBlink(characterId, expressionId, mapping);

                if (!IsLayeredPortraitBlinkCurrent(characterId, expressionId))
                    break;

                float minInterval = Mathf.Min(portraitBlinkIntervalMin, portraitBlinkIntervalMax);
                float maxInterval = Mathf.Max(portraitBlinkIntervalMin, portraitBlinkIntervalMax);
                float nextInterval = Mathf.Approximately(minInterval, maxInterval)
                    ? minInterval
                    : Random.Range(minInterval, maxInterval);

                yield return new WaitForSeconds(nextInterval);
            }

            RestoreLayeredPortraitBlinkOpenFrame(characterId, expressionId);
            ClearLayeredPortraitBlinkHandle(characterId, expressionId);
        }

        private IEnumerator PlayLayeredPortraitBlink(string characterId, string expressionId, VNLayeredExpressionMapping mapping)
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

            if (!TryApplyBlinkFrame(characterId, expressionId, eyebrowHalf, eyeHalf))
                yield break;

            yield return new WaitForSeconds(frameDuration);
            if (!TryApplyBlinkFrame(characterId, expressionId, eyebrowClosed, eyeClosed))
                yield break;

            yield return new WaitForSeconds(frameDuration);
            if (!TryApplyBlinkFrame(characterId, expressionId, eyebrowHalf, eyeHalf))
                yield break;

            yield return new WaitForSeconds(frameDuration);
            TryApplyBlinkFrame(characterId, expressionId, eyebrowOpen, eyeOpen);
        }

        private bool TryApplyBlinkFrame(string characterId, string expressionId, Sprite eyebrowSprite, Sprite eyeSprite)
        {
            if (!IsLayeredPortraitBlinkCurrent(characterId, expressionId) || !HasActiveLayeredPortraitBlinkImages())
                return false;

            ApplySpriteToImage(portraitEyebrowImage, eyebrowSprite);
            ApplySpriteToImage(portraitEyeImage, eyeSprite);
            return true;
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
            return HasActiveLayeredPortraitBlinkImages()
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
                && IsMelionCharacterId(state.characterId)
                && IsLayeredPortraitState(state);
        }

        private bool IsMelionCharacterId(string characterId)
        {
            return string.Equals(characterId, MelionCharacterId, System.StringComparison.OrdinalIgnoreCase);
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

        private void RestoreLayeredPortraitBlinkOpenFrame(string characterId, string expressionId)
        {
            if (string.IsNullOrWhiteSpace(characterId) || string.IsNullOrWhiteSpace(expressionId))
                return;

            if (!TryGetLayeredExpressionMapping(characterId, expressionId, out VNLayeredExpressionMapping mapping))
                return;

            if (!HasLayeredPortraitImages())
                return;

            ApplySpriteToImage(portraitEyebrowImage, GetEyebrowOpenSprite(mapping));
            ApplySpriteToImage(portraitEyeImage, mapping.eyeOpenSprite);
        }

        private bool HasActiveLayeredPortraitBlinkImages()
        {
            return IsActiveImage(portraitEyebrowImage) && IsActiveImage(portraitEyeImage);
        }

        private bool IsActiveImage(Image image)
        {
            return image != null && image.gameObject != null && image.gameObject.activeInHierarchy;
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

        private void StopLayeredPortraitBlink(string characterId = null, bool restoreOpenFrame = false)
        {
            if (!string.IsNullOrWhiteSpace(characterId)
                && !string.Equals(portraitBlinkCharacterId, characterId, System.StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string stoppedCharacterId = portraitBlinkCharacterId;
            string stoppedExpressionId = portraitBlinkExpressionId;

            if (portraitBlinkCoroutine != null)
            {
                StopCoroutine(portraitBlinkCoroutine);
                portraitBlinkCoroutine = null;
            }

            portraitBlinkCharacterId = null;
            portraitBlinkExpressionId = null;

            if (restoreOpenFrame)
                RestoreLayeredPortraitBlinkOpenFrame(stoppedCharacterId, stoppedExpressionId);
        }

        private bool CanApplyLayeredPortrait(string characterId, string expressionId)
        {
            return HasLayeredPortraitImages() && TryGetLayeredExpressionMapping(characterId, expressionId, out _);
        }

        private bool CanApplyLayeredCharacter(string position, string characterId, string expressionId)
        {
            return TryGetLayeredCharacterSlot(position, out VNLayeredCharacterSlot slot)
                && HasLayeredCharacterImages(slot)
                && TryGetLayeredExpressionMapping(characterId, expressionId, out _);
        }

        private bool IsLayeredPortraitState(VNCharacterState state)
        {
            if (state == null || NormalizePosition(state.position) != PortraitPosition)
                return false;

            return TryGetCharacterDefinition(state.characterId, out VNCharacterDefinition definition)
                && definition.renderMode == VNCharacterRenderMode.LayeredPortrait;
        }

        private bool IsLayeredCharacterState(VNCharacterState state)
        {
            if (state == null || NormalizePosition(state.position) == PortraitPosition)
                return false;

            return TryGetCharacterDefinition(state.characterId, out VNCharacterDefinition definition)
                && definition.renderMode == VNCharacterRenderMode.LayeredCharacter;
        }

        private bool HasLayeredPortraitImages()
        {
            return portraitBaseImage != null
                && portraitEyebrowImage != null
                && portraitEyeImage != null
                && portraitMouthImage != null;
        }

        private bool TryGetLayeredCharacterSlot(string position, out VNLayeredCharacterSlot slot)
        {
            slot = null;
            string normalizedPosition = NormalizePosition(position);
            if (normalizedPosition == PortraitPosition || layeredCharacterSlots == null)
                return false;

            for (int i = 0; i < layeredCharacterSlots.Count; i++)
            {
                VNLayeredCharacterSlot candidate = layeredCharacterSlots[i];
                if (candidate == null)
                    continue;

                if (NormalizePosition(candidate.position) != normalizedPosition)
                    continue;

                slot = candidate;
                return true;
            }

            return false;
        }

        private bool HasLayeredCharacterImages(VNLayeredCharacterSlot slot)
        {
            return slot != null
                && slot.baseImage != null
                && slot.eyebrowImage != null
                && slot.eyeImage != null
                && slot.mouthImage != null;
        }

        private void ApplySpriteToImage(Image image, Sprite sprite)
        {
            if (image == null)
                return;

            image.sprite = sprite;
            image.enabled = sprite != null;
        }

        

        private void ClearLayeredCharacterSlotImages(string position)
        {
            string normalizedPosition = NormalizePosition(position);
            StopLayeredCharacterMouth(normalizedPosition, applyClosed: false, clearRequest: true);
            StopLayeredCharacterBlink(normalizedPosition, restoreOpenFrame: false);
            StopLayeredCharacterFade(normalizedPosition);

            if (!TryGetLayeredCharacterSlot(normalizedPosition, out VNLayeredCharacterSlot slot))
                return;

            ApplySpriteToImage(slot.baseImage, null);
            ApplySpriteToImage(slot.eyebrowImage, null);
            ApplySpriteToImage(slot.eyeImage, null);
            ApplySpriteToImage(slot.mouthImage, null);
            SetLayeredCharacterAlpha(slot, 1f);
        }

        private void ClearImage(Image image)
        {
            if (image == null)
                return;

            image.sprite = null;
            image.enabled = false;
        }

        private void ClearLayeredPortraitImages()
        {
            ClearImage(portraitBaseImage);
            ClearImage(portraitEyebrowImage);
            ClearImage(portraitEyeImage);
            ClearImage(portraitMouthImage);
            ClearImage(portraitImage);
        }

        private bool HasVisibleLayeredCharacterSlot(string position)
        {
            return TryGetLayeredCharacterSlot(position, out VNLayeredCharacterSlot slot)
                && (HasVisibleSprite(slot.baseImage)
                    || HasVisibleSprite(slot.eyebrowImage)
                    || HasVisibleSprite(slot.eyeImage)
                    || HasVisibleSprite(slot.mouthImage));
        }

        private bool HasVisibleSprite(Image image)
        {
            return image != null && image.enabled && image.sprite != null;
        }

        private void SetLayeredCharacterActive(VNLayeredCharacterSlot slot, bool active)
        {
            if (slot == null)
                return;

            SetImageObjectActive(slot.baseImage, active);
            SetImageObjectActive(slot.eyebrowImage, active);
            SetImageObjectActive(slot.eyeImage, active);
            SetImageObjectActive(slot.mouthImage, active);
        }

        private void SetImageObjectActive(Image image, bool active)
        {
            if (image != null && image.gameObject != null)
                image.gameObject.SetActive(active);
        }

        private void SetLayeredCharacterAlpha(VNLayeredCharacterSlot slot, float alpha)
        {
            if (slot == null)
                return;

            SetAlpha(slot.baseImage, alpha);
            SetAlpha(slot.eyebrowImage, alpha);
            SetAlpha(slot.eyeImage, alpha);
            SetAlpha(slot.mouthImage, alpha);
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
            pendingShows.Remove(normalizedPosition);
            fadingOutPositions.Remove(normalizedPosition);

            if (slot != null)
            {
                StopSlotFade(slot);
                slot.sprite = null;
                slot.enabled = false;
                SetAlpha(slot, 1f);
            }

            if (normalizedPosition != PortraitPosition)
                ClearLayeredCharacterSlotImages(normalizedPosition);
        }

        private void FadeOutAndClearSlot(string position)
        {
            string normalizedPosition = NormalizePosition(position);
            Image slot = GetSlotImage(normalizedPosition);

            if (fadingOutPositions.Contains(normalizedPosition))
                return;

            if (HasVisibleLayeredCharacterSlot(normalizedPosition)
                && TryGetLayeredCharacterSlot(normalizedPosition, out VNLayeredCharacterSlot layeredSlot))
            {
                StopLayeredCharacterMouth(normalizedPosition, applyClosed: true, clearRequest: true);
                StopLayeredCharacterBlink(normalizedPosition, restoreOpenFrame: true);
                if (slot != null)
                {
                    StopSlotFade(slot);
                    slot.sprite = null;
                    slot.enabled = false;
                    SetAlpha(slot, 1f);
                }

                StopLayeredCharacterFade(normalizedPosition);

                if (fadeDuration <= 0f)
                {
                    ClearLayeredCharacterSlotImages(normalizedPosition);
                    fadingOutPositions.Remove(normalizedPosition);
                    ApplyPendingShow(normalizedPosition);
                    return;
                }

                fadingOutPositions.Add(normalizedPosition);
                SetLayeredCharacterActive(layeredSlot, true);
                float fromAlpha = GetLayeredCharacterAlpha(layeredSlot);
                LogFadeDebug($"Start layered character fade out: position={normalizedPosition}, from alpha={fromAlpha:0.###}, to alpha=0, duration={fadeDuration:0.###}");
                layeredCharacterFadeCoroutines[normalizedPosition] = StartCoroutine(FadeLayeredCharacterSlot(layeredSlot, normalizedPosition, fromAlpha, 0f, fadeDuration, clearOnComplete: true));
                return;
            }

            if (slot == null)
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

        private IEnumerator FadeLayeredCharacterSlot(VNLayeredCharacterSlot slot, string normalizedPosition, float fromAlpha, float toAlpha, float duration, bool clearOnComplete)
        {
            if (slot == null)
                yield break;

            if (duration <= 0f)
            {
                SetLayeredCharacterAlpha(slot, toAlpha);
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
                    SetLayeredCharacterAlpha(slot, Mathf.Lerp(fromAlpha, toAlpha, t));
                    yield return null;
                }

                SetLayeredCharacterAlpha(slot, toAlpha);
            }

            layeredCharacterFadeCoroutines.Remove(normalizedPosition);
            LogFadeDebug($"Layered character fade complete: position={normalizedPosition}, final alpha={toAlpha:0.###}");

            if (clearOnComplete)
            {
                ClearLayeredCharacterSlotImages(normalizedPosition);
                fadingOutPositions.Remove(normalizedPosition);
                ApplyPendingShow(normalizedPosition);
            }
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

        private void StopLayeredCharacterFade(string position)
        {
            string normalizedPosition = NormalizePosition(position);
            if (!layeredCharacterFadeCoroutines.TryGetValue(normalizedPosition, out Coroutine coroutine) || coroutine == null)
                return;

            StopCoroutine(coroutine);
            layeredCharacterFadeCoroutines.Remove(normalizedPosition);
        }

        private void StopAllSlotFades()
        {
            foreach (Coroutine coroutine in fadeCoroutines.Values)
            {
                if (coroutine != null)
                    StopCoroutine(coroutine);
            }

            fadeCoroutines.Clear();

            foreach (Coroutine coroutine in layeredCharacterFadeCoroutines.Values)
            {
                if (coroutine != null)
                    StopCoroutine(coroutine);
            }

            layeredCharacterFadeCoroutines.Clear();
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

        private static float GetLayeredCharacterAlpha(VNLayeredCharacterSlot slot)
        {
            if (slot == null)
                return 1f;

            if (slot.baseImage != null)
                return slot.baseImage.color.a;
            if (slot.eyebrowImage != null)
                return slot.eyebrowImage.color.a;
            if (slot.eyeImage != null)
                return slot.eyeImage.color.a;
            if (slot.mouthImage != null)
                return slot.mouthImage.color.a;

            return 1f;
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
