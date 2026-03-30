using System;
using UnityEngine;
using UnityEngine.UI;

namespace PPP.BLUE.VN
{
    public sealed class VNPinVisualBindings : MonoBehaviour
    {
        [Serializable]
        private struct PinVisualBinding
        {
            public string label;
            public Button button;
            public Image targetImage;
            public UIDragMoveClamped stateSource;
            public Sprite activeSprite;
            public Sprite inactiveSprite;
        }

        [Header("VN Pin Bindings")]
        [SerializeField] private PinVisualBinding[] pinBindings = Array.Empty<PinVisualBinding>();

        private void Awake()
        {
            ApplyCurrentTheme();
            RefreshAll();
        }

        private void OnEnable()
        {
            var manager = AppUIThemeManager.Instance;
            if (manager != null)
                manager.OnThemeChanged += HandleThemeChanged;

            BindStateEvents();
            ApplyCurrentTheme();
            RefreshAll();
        }

        private void OnDisable()
        {
            var manager = AppUIThemeManager.Instance;
            if (manager != null)
                manager.OnThemeChanged -= HandleThemeChanged;

            UnbindStateEvents();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ApplyCurrentTheme();
            RefreshAll();
        }
#endif

        private void HandleThemeChanged()
        {
            ApplyCurrentTheme();
            RefreshAll();
        }

        private void BindStateEvents()
        {
            if (pinBindings == null)
                return;

            for (int i = 0; i < pinBindings.Length; i++)
            {
                var source = pinBindings[i].stateSource;
                if (source != null)
                    source.PinnedStateChanged += HandlePinnedStateChanged;
            }
        }

        private void UnbindStateEvents()
        {
            if (pinBindings == null)
                return;

            for (int i = 0; i < pinBindings.Length; i++)
            {
                var source = pinBindings[i].stateSource;
                if (source != null)
                    source.PinnedStateChanged -= HandlePinnedStateChanged;
            }
        }

        private void HandlePinnedStateChanged(UIDragMoveClamped source, bool _)
        {
            RefreshBindingForSource(source);
        }

        private void ApplyCurrentTheme()
        {
            var themeManager = AppUIThemeManager.Instance;
            if (themeManager == null || themeManager.CurrentTheme == null || pinBindings == null)
                return;

            var vnTheme = themeManager.CurrentTheme.vn;
            for (int i = 0; i < pinBindings.Length; i++)
            {
                var binding = pinBindings[i];

                if (vnTheme.pinActiveSprite != null)
                    binding.activeSprite = vnTheme.pinActiveSprite;
                if (vnTheme.pinInactiveSprite != null)
                    binding.inactiveSprite = vnTheme.pinInactiveSprite;

                pinBindings[i] = binding;
            }
        }

        private void RefreshAll()
        {
            if (pinBindings == null)
                return;

            for (int i = 0; i < pinBindings.Length; i++)
                RefreshBinding(i);
        }

        private void RefreshBindingForSource(UIDragMoveClamped source)
        {
            if (source == null || pinBindings == null)
                return;

            for (int i = 0; i < pinBindings.Length; i++)
            {
                if (pinBindings[i].stateSource != source)
                    continue;

                RefreshBinding(i);
            }
        }

        private void RefreshBinding(int index)
        {
            if (index < 0 || pinBindings == null || index >= pinBindings.Length)
                return;

            var binding = pinBindings[index];
            var targetImage = binding.targetImage;
            if (targetImage == null && binding.button != null)
                targetImage = binding.button.targetGraphic as Image;

            if (targetImage == null)
                return;

            bool isPinned = binding.stateSource != null && binding.stateSource.IsPinned;
            Sprite targetSprite = isPinned ? binding.activeSprite : binding.inactiveSprite;
            if (targetSprite != null)
                targetImage.sprite = targetSprite;
        }
    }
}
