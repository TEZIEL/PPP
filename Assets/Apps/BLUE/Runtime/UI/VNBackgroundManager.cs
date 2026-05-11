using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PPP.BLUE.VN
{
    public sealed class VNBackgroundManager : MonoBehaviour
    {
        [Header("Backgrounds")]
        [SerializeField] private List<Sprite> backgrounds = new();
        [SerializeField, Min(0)] private int currentBackgroundIndex;

        [Header("Synced Image Targets")]
        [SerializeField] private Image titleBackgroundImage;
        [SerializeField] private Image inGameBackgroundImage;
        [SerializeField] private Image titleWindowBackgroundImage;
        [SerializeField] private Image[] additionalTargets = System.Array.Empty<Image>();

        [Header("Background Tint")]
        [SerializeField]
        private Color[] backgroundTints =
        {
            Color.white,
            new Color(200f / 255f, 200f / 255f, 220f / 255f, 1f),
            new Color(255f / 255f, 205f / 255f, 220f / 255f, 1f),
        };

        [Header("Tint Targets")]
        [SerializeField] private Image[] tintTargetImages = System.Array.Empty<Image>();
        [SerializeField] private Transform[] tintTargetRoots = System.Array.Empty<Transform>();
        [SerializeField] private bool includeInactiveTintTargets = true;

        private bool warnedNoBackgrounds;

        public int CurrentBackgroundIndex => currentBackgroundIndex;
        public int BackgroundCount => backgrounds != null ? backgrounds.Count : 0;

        private void Awake()
        {
            ResolveNamedTargets();
        }

        private void OnEnable()
        {
            ApplyCurrentBackground();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ClampCurrentIndex();
            NormalizeTintAlpha();

            if (isActiveAndEnabled)
                ApplyCurrentBackground();
        }
#endif

        public void ApplyCurrentBackground()
        {
            if (backgrounds == null || backgrounds.Count == 0)
            {
                if (!warnedNoBackgrounds)
                {
                    Debug.LogWarning("[VN_BACKGROUND] No VN backgrounds configured.", this);
                    warnedNoBackgrounds = true;
                }

                ApplyCurrentTint();
                return;
            }

            warnedNoBackgrounds = false;
            ClampCurrentIndex();
            Sprite current = backgrounds[currentBackgroundIndex];

            ApplyToTarget(titleBackgroundImage, current);
            ApplyToTarget(inGameBackgroundImage, current);
            ApplyToTarget(titleWindowBackgroundImage, current);

            if (additionalTargets != null)
            {
                for (int i = 0; i < additionalTargets.Length; i++)
                    ApplyToTarget(additionalTargets[i], current);
            }

            ApplyCurrentTint();
        }

        public void ApplyCurrentTint()
        {
            Color currentTint = GetCurrentTint();
            currentTint.a = 1f;

            if (tintTargetImages != null)
            {
                for (int i = 0; i < tintTargetImages.Length; i++)
                    ApplyTintToImage(tintTargetImages[i], currentTint);
            }

            if (tintTargetRoots == null)
                return;

            for (int i = 0; i < tintTargetRoots.Length; i++)
                ApplyTintToRoot(tintTargetRoots[i], currentTint);
        }

        public Color GetCurrentTint()
        {
            if (backgroundTints == null || backgroundTints.Length == 0)
                return Color.white;

            int tintIndex = Mathf.Clamp(currentBackgroundIndex, 0, backgroundTints.Length - 1);
            Color tint = backgroundTints[tintIndex];
            tint.a = 1f;
            return tint;
        }

        public void SetTintForCurrentBackground(Color color)
        {
            EnsureTintArraySize(currentBackgroundIndex + 1);
            color.a = 1f;
            backgroundTints[currentBackgroundIndex] = color;
            ApplyCurrentTint();
        }

        public void SetBackgroundByIndex(int index)
        {
            if (backgrounds == null || backgrounds.Count == 0)
            {
                ApplyCurrentBackground();
                return;
            }

            currentBackgroundIndex = Mathf.Clamp(index, 0, backgrounds.Count - 1);
            ApplyCurrentBackground();
        }

        public void NextBackground()
        {
            if (backgrounds == null || backgrounds.Count == 0)
            {
                ApplyCurrentBackground();
                return;
            }

            currentBackgroundIndex = (currentBackgroundIndex + 1) % backgrounds.Count;
            ApplyCurrentBackground();
        }

        public void PreviousBackground()
        {
            if (backgrounds == null || backgrounds.Count == 0)
            {
                ApplyCurrentBackground();
                return;
            }

            currentBackgroundIndex--;
            if (currentBackgroundIndex < 0)
                currentBackgroundIndex = backgrounds.Count - 1;

            ApplyCurrentBackground();
        }

        public int GetCurrentBackgroundIndex()
        {
            return currentBackgroundIndex;
        }

        public Sprite GetCurrentBackgroundSprite()
        {
            if (backgrounds == null || backgrounds.Count == 0)
                return null;

            ClampCurrentIndex();
            return backgrounds[currentBackgroundIndex];
        }

        private void ClampCurrentIndex()
        {
            if (backgrounds == null || backgrounds.Count == 0)
            {
                currentBackgroundIndex = 0;
                return;
            }

            currentBackgroundIndex = Mathf.Clamp(currentBackgroundIndex, 0, backgrounds.Count - 1);
        }

        private void NormalizeTintAlpha()
        {
            if (backgroundTints == null)
                return;

            for (int i = 0; i < backgroundTints.Length; i++)
            {
                Color tint = backgroundTints[i];
                tint.a = 1f;
                backgroundTints[i] = tint;
            }
        }

        private void EnsureTintArraySize(int minSize)
        {
            if (minSize <= 0)
                return;

            if (backgroundTints != null && backgroundTints.Length >= minSize)
                return;

            int oldLength = backgroundTints != null ? backgroundTints.Length : 0;
            var next = new Color[minSize];
            for (int i = 0; i < next.Length; i++)
                next[i] = i < oldLength ? backgroundTints[i] : Color.white;

            backgroundTints = next;
        }

        private static void ApplyToTarget(Image target, Sprite sprite)
        {
            if (target == null)
                return;

            target.sprite = sprite;
        }

        private void ApplyTintToRoot(Transform root, Color tint)
        {
            if (root == null)
                return;

            var images = root.GetComponentsInChildren<Image>(includeInactiveTintTargets);
            for (int i = 0; i < images.Length; i++)
                ApplyTintToImage(images[i], tint);
        }

        private void ApplyTintToImage(Image target, Color tint)
        {
            if (target == null || IsTintExcluded(target))
                return;

            target.color = tint;
        }

        private bool IsTintExcluded(Image target)
        {
            if (target == null)
                return true;

            if (target == titleBackgroundImage || target == inGameBackgroundImage || target == titleWindowBackgroundImage)
                return true;

            if (additionalTargets != null)
            {
                for (int i = 0; i < additionalTargets.Length; i++)
                {
                    if (target == additionalTargets[i])
                        return true;
                }
            }

            string objectName = target.gameObject.name;
            if (objectName == "TitleBackground"
                || objectName == "InGameBackground"
                || objectName == "TitleWindowBackground"
                || objectName == "VNTitleDarkOverlay"
                || objectName == "TitleDarkOverlay")
                return true;

            return HasMelionInHierarchy(target.transform);
        }

        private static bool HasMelionInHierarchy(Transform current)
        {
            while (current != null)
            {
                if (current.name.IndexOf("Melion", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;

                current = current.parent;
            }

            return false;
        }

        private void ResolveNamedTargets()
        {
            var images = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                var image = images[i];
                if (image == null)
                    continue;

                string objectName = image.gameObject.name;
                if (titleBackgroundImage == null && objectName == "TitleBackground")
                {
                    titleBackgroundImage = image;
                    continue;
                }

                if (inGameBackgroundImage == null && objectName == "InGameBackground")
                {
                    inGameBackgroundImage = image;
                    continue;
                }

                if (titleWindowBackgroundImage == null && objectName == "TitleWindowBackground")
                    titleWindowBackgroundImage = image;
            }
        }
    }
}
