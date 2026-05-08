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

        private readonly Dictionary<string, VNCharacterSpriteMapping> spriteLookup = new();
        private readonly Dictionary<string, VNCharacterState> activeStates = new(System.StringComparer.OrdinalIgnoreCase);

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

            activeStates[characterId] = state;
            ApplyStateToSlot(state);
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
                ClearSlot(normalizedPosition);

            activeStates.Remove(characterId);
        }

        public void ClearAll()
        {
            activeStates.Clear();
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

            slot.sprite = GetSprite(state.characterId, state.expressionId);
            slot.enabled = slot.sprite != null;

            if (normalizedPosition != PortraitPosition)
                slot.gameObject.SetActive(true);
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

            slot.sprite = sprite;
            slot.enabled = sprite != null;

            if (normalizedPosition != PortraitPosition)
                slot.gameObject.SetActive(true);
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

            slot.sprite = null;
            slot.enabled = false;
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
