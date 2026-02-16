using System;
using UnityEngine;
using UnityEngine.UI;

public class DesktopContextMenuView : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private Button alignIconsButton;
    [SerializeField] private Button toggleLayoutButton;
    [SerializeField] private Button resetWindowsButton;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button createFileButton;

    public event Action OnAlignIcons;
    public event Action OnToggleLayout;
    public event Action OnResetWindows;
    public event Action OnRefresh;
    public event Action OnCreateFile;

    private void Awake()
    {
        if (alignIconsButton != null) alignIconsButton.onClick.AddListener(() => OnAlignIcons?.Invoke());
        if (toggleLayoutButton != null) toggleLayoutButton.onClick.AddListener(() => OnToggleLayout?.Invoke());
        if (resetWindowsButton != null) resetWindowsButton.onClick.AddListener(() => OnResetWindows?.Invoke());
        if (refreshButton != null) refreshButton.onClick.AddListener(() => OnRefresh?.Invoke());
        if (createFileButton != null) createFileButton.onClick.AddListener(() => OnCreateFile?.Invoke());
    }
}
