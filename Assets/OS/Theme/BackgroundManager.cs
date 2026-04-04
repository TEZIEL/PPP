using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BackgroundManager : MonoBehaviour
{
    [SerializeField] private Image skyImage;
    [SerializeField] private Image buildingImage;
    [SerializeField] private Image highlightImage;
    [SerializeField] private BackgroundThemeData backgroundData;

    public static BackgroundManager Instance { get; private set; }

    private int currentSky;
    private int currentBuilding;
    private int currentHighlight;

    private int pendingSky;
    private int pendingBuilding;
    private int pendingHighlight;

    public int PendingSky => pendingSky;
    public int PendingBuilding => pendingBuilding;
    public int PendingHighlight => pendingHighlight;
    public int CurrentSky => currentSky;
    public int CurrentBuilding => currentBuilding;
    public int CurrentHighlight => currentHighlight;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResetPendingToCurrent();
        ApplyPending();
    }

    public void InitializeDropdown(TMP_Dropdown dropdown, string prefix, Sprite[] options)
    {
        if (dropdown == null)
            return;

        dropdown.ClearOptions();

        int count = options == null ? 0 : options.Length;
        var names = new System.Collections.Generic.List<string>(count);

        for (int i = 0; i < count; i++)
        {
            string optionName = options[i] != null ? options[i].name : string.Empty;
            names.Add(string.IsNullOrWhiteSpace(optionName) ? $"{prefix} {i + 1:00}" : optionName);
        }

        dropdown.AddOptions(names);
    }

    public void SetSky(int index)
    {
        pendingSky = ClampSky(index);
        ApplyPending();
    }

    public void SetBuilding(int index)
    {
        pendingBuilding = ClampBuilding(index);
        ApplyPending();
    }

    public void SetHighlight(int index)
    {
        pendingHighlight = ClampHighlight(index);
        ApplyPending();
    }

    public void OnOpen()
    {
        ResetPendingToCurrent();
        ApplyPending();
    }

    public void Apply()
    {
        currentSky = pendingSky;
        currentBuilding = pendingBuilding;
        currentHighlight = pendingHighlight;
        ApplyPending();
    }

    public void Cancel()
    {
        ResetPendingToCurrent();
        ApplyPending();
    }

    public void SetAppliedState(int skyIndex, int buildingIndex, int highlightIndex)
    {
        currentSky = ClampSky(skyIndex);
        currentBuilding = ClampBuilding(buildingIndex);
        currentHighlight = ClampHighlight(highlightIndex);

        pendingSky = currentSky;
        pendingBuilding = currentBuilding;
        pendingHighlight = currentHighlight;

        ApplyPending();
    }

    public void ResetToDefault()
    {
        SetAppliedState(0, 0, 0);
    }

    public Sprite[] GetSkyOptions()
    {
        return backgroundData == null ? null : backgroundData.skyOptions;
    }

    public Sprite[] GetBuildingOptions()
    {
        return backgroundData == null ? null : backgroundData.buildingOptions;
    }

    public Sprite[] GetHighlightOptions()
    {
        return backgroundData == null ? null : backgroundData.highlightOptions;
    }

    private void ResetPendingToCurrent()
    {
        currentSky = ClampSky(currentSky);
        currentBuilding = ClampBuilding(currentBuilding);
        currentHighlight = ClampHighlight(currentHighlight);

        pendingSky = currentSky;
        pendingBuilding = currentBuilding;
        pendingHighlight = currentHighlight;
    }

    private void ApplyPending()
    {
        Apply(skyImage, GetSkyOptions(), pendingSky);
        Apply(buildingImage, GetBuildingOptions(), pendingBuilding);
        Apply(highlightImage, GetHighlightOptions(), pendingHighlight);
    }

    private static void Apply(Image target, Sprite[] options, int index)
    {
        if (target == null || options == null || options.Length == 0)
            return;

        if (index < 0 || index >= options.Length)
            index = 0;

        target.sprite = options[index];
    }

    private int ClampSky(int index)
    {
        return ClampIndex(index, GetSkyOptions());
    }

    private int ClampBuilding(int index)
    {
        return ClampIndex(index, GetBuildingOptions());
    }

    private int ClampHighlight(int index)
    {
        return ClampIndex(index, GetHighlightOptions());
    }

    private static int ClampIndex(int index, Sprite[] options)
    {
        if (options == null || options.Length == 0)
            return 0;

        return Mathf.Clamp(index, 0, options.Length - 1);
    }
}
