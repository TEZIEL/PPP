using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TaskbarManager : MonoBehaviour
{
    [SerializeField] private WindowManager windowManager;
    [SerializeField] private TaskbarButtonController buttonPrefab;
    [SerializeField] private RectTransform buttonRoot;

    private readonly Dictionary<string, TaskbarButtonController> buttons = new();
    private readonly HashSet<string> minimizedApps = new();

    // ✅ 동시성 경계용
    private bool _isMutating;
    public bool IsMutating => _isMutating;

    private void Awake()
    {
        if (buttonRoot == null) buttonRoot = (RectTransform)transform;
    }

    public void Add(string appId, string displayName, WindowController window, Sprite appIcon = null)
    {
        if (_isMutating) return;
        if (string.IsNullOrEmpty(appId)) return;
        if (buttons.ContainsKey(appId) || buttonPrefab == null) return;

        _isMutating = true;

        minimizedApps.Remove(appId);

        Transform root = buttonRoot != null ? buttonRoot : transform;
        var button = Instantiate(buttonPrefab, root);

        button.Initialize(appId, displayName, windowManager, window, appIcon);

        buttons.Add(appId, button);
        SetState(appId, isActive: false, isMinimized: false);

        ForceRebuild();

        _isMutating = false;
    }

    public void Remove(string appId)
    {
        if (_isMutating) return;
        if (string.IsNullOrEmpty(appId)) return;

        minimizedApps.Remove(appId);

        if (!buttons.TryGetValue(appId, out var removed) || removed == null) return;

        _isMutating = true;

        // ✅ 딕셔너리에서 먼저 제거(외부에서 GetButtonRect 등으로 잡히지 않게)
        buttons.Remove(appId);

        // ✅ 레이아웃 즉시 계산(시작 프레임에 한 번)
        ForceRebuild();

        // ✅ 버튼 자체가 폭을 0으로 줄이며 사라짐 -> LayoutGroup이 알아서 “밀림” 처리
        removed.PlayCloseReflow(() =>
        {
            if (removed != null) Destroy(removed.gameObject);

            // ✅ 마지막에 한 번 더 정리(끝 프레임)
            ForceRebuild();
            _isMutating = false;
        });
    }

    // -------------------------
    // Busy 락 적용
    // -------------------------
    public void SetActive(string appId, bool isActive)
    {
        if (_isMutating) return;
        SetState(appId, isActive, minimizedApps.Contains(appId));
    }

    public void SetMinimized(string appId, bool minimized)
    {
        if (_isMutating) return;

        if (minimized) minimizedApps.Add(appId);
        else minimizedApps.Remove(appId);

        if (!buttons.TryGetValue(appId, out var btn) || btn == null) return;
        btn.SetMinimizedVisual(minimized);
    }

    private void SetState(string appId, bool isActive, bool isMinimized)
    {
        if (!buttons.TryGetValue(appId, out var button) || button == null) return;
        button.SetMinimizedVisual(isMinimized);
        // isActive 시각은 필요하면 여기서 추가
    }

    public RectTransform GetButtonRect(string appId)
    {
        if (_isMutating) return null;
        if (buttons.TryGetValue(appId, out var b) && b != null) return b.Rect;
        return null;
    }

    private void ForceRebuild()
    {
        if (buttonRoot == null) return;
        LayoutRebuilder.ForceRebuildLayoutImmediate(buttonRoot);
    }
}