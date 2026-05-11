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
                return;
            }

            warnedNoBackgrounds = false;
            ClampCurrentIndex();
            Sprite current = backgrounds[currentBackgroundIndex];

            ApplyToTarget(titleBackgroundImage, current);
            ApplyToTarget(inGameBackgroundImage, current);
            ApplyToTarget(titleWindowBackgroundImage, current);

            if (additionalTargets == null)
                return;

            for (int i = 0; i < additionalTargets.Length; i++)
                ApplyToTarget(additionalTargets[i], current);
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

        private static void ApplyToTarget(Image target, Sprite sprite)
        {
            if (target == null)
                return;

            target.sprite = sprite;
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
