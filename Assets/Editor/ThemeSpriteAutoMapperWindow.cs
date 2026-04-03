using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public class ThemeSpriteAutoMapperWindow : EditorWindow
{
    private ScriptableObject sourceAsset;
    private ScriptableObject targetAsset;
    private string sourceSuffix = "01";
    private string targetSuffix = "02";
    private bool overwriteExistingValues = true;
    private bool dryRun = false;
    private Vector2 reportScroll;
    private string latestReport = string.Empty;

    [MenuItem("Tools/Theme/Sprite Auto Mapper")]
    private static void Open()
    {
        GetWindow<ThemeSpriteAutoMapperWindow>("Theme Sprite Auto Mapper");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Theme Sprite Auto Mapper", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Use a completed 01 theme asset as the source map. The tool reads source sprite references field-by-field and remaps by sprite name suffix (e.g., 01 -> 02).",
            MessageType.Info);

        sourceAsset = (ScriptableObject)EditorGUILayout.ObjectField("Source Theme Asset", sourceAsset, typeof(ScriptableObject), false);
        targetAsset = (ScriptableObject)EditorGUILayout.ObjectField("Target Theme Asset", targetAsset, typeof(ScriptableObject), false);

        using (new EditorGUILayout.HorizontalScope())
        {
            sourceSuffix = EditorGUILayout.TextField("Source Suffix", sourceSuffix);
            targetSuffix = EditorGUILayout.TextField("Target Suffix", targetSuffix);
        }

        overwriteExistingValues = EditorGUILayout.Toggle("Overwrite Existing", overwriteExistingValues);
        dryRun = EditorGUILayout.Toggle("Dry Run (Preview)", dryRun);

        GUI.enabled = CanExecute();
        if (GUILayout.Button(dryRun ? "Preview Auto Remap" : "Auto Remap Sprites"))
        {
            RunRemap();
        }

        GUI.enabled = true;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Report", EditorStyles.boldLabel);
        reportScroll = EditorGUILayout.BeginScrollView(reportScroll, GUILayout.ExpandHeight(true));
        EditorGUILayout.TextArea(latestReport, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    private bool CanExecute()
    {
        return sourceAsset != null && targetAsset != null;
    }

    private void RunRemap()
    {
        var validationError = ValidateAssets();
        if (!string.IsNullOrEmpty(validationError))
        {
            latestReport = validationError;
            Debug.LogError(validationError, this);
            return;
        }

        var sourceSerializedObject = new SerializedObject(sourceAsset);
        var targetSerializedObject = new SerializedObject(targetAsset);

        var spriteIndex = BuildSpriteIndex();
        var missingRecords = new List<string>();
        var warningRecords = new List<string>();

        var mappedCount = 0;
        var skippedCount = 0;
        var scannedSpriteFields = 0;

        var iterator = sourceSerializedObject.GetIterator();
        var enterChildren = true;

        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = true;

            if (iterator.propertyType != SerializedPropertyType.ObjectReference)
            {
                continue;
            }

            if (iterator.objectReferenceValue != null && iterator.objectReferenceValue is not Sprite sourceSprite)
            {
                continue;
            }

            var targetProperty = targetSerializedObject.FindProperty(iterator.propertyPath);
            if (targetProperty == null || targetProperty.propertyType != SerializedPropertyType.ObjectReference)
            {
                continue;
            }

            scannedSpriteFields++;

            if (!overwriteExistingValues && targetProperty.objectReferenceValue != null)
            {
                skippedCount++;
                continue;
            }

            if (sourceSprite == null)
            {
                if (!dryRun)
                {
                    targetProperty.objectReferenceValue = null;
                }

                continue;
            }

            var expectedSpriteName = ReplaceSuffix(sourceSprite.name, sourceSuffix, targetSuffix);
            if (string.Equals(expectedSpriteName, sourceSprite.name, StringComparison.Ordinal))
            {
                warningRecords.Add($"[UnchangedName] {iterator.propertyPath}: '{sourceSprite.name}' did not change with suffix '{sourceSuffix}' -> '{targetSuffix}'.");
            }

            if (!spriteIndex.TryGetValue(expectedSpriteName, out var candidates) || candidates.Count == 0)
            {
                missingRecords.Add($"[Missing] {iterator.propertyPath}: source '{sourceSprite.name}' -> expected '{expectedSpriteName}'.");
                continue;
            }

            var mappedSprite = candidates[0];
            if (candidates.Count > 1)
            {
                warningRecords.Add($"[Ambiguous] {iterator.propertyPath}: '{expectedSpriteName}' has {candidates.Count} candidates. Using '{AssetDatabase.GetAssetPath(mappedSprite)}'.");
            }

            if (!dryRun)
            {
                targetProperty.objectReferenceValue = mappedSprite;
            }

            mappedCount++;
        }

        if (!dryRun)
        {
            targetSerializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(targetAsset);
            AssetDatabase.SaveAssets();
        }

        latestReport = BuildReport(
            dryRun,
            sourceAsset,
            targetAsset,
            scannedSpriteFields,
            mappedCount,
            skippedCount,
            warningRecords,
            missingRecords);

        if (missingRecords.Count > 0)
        {
            Debug.LogWarning(latestReport, targetAsset);
        }
        else
        {
            Debug.Log(latestReport, targetAsset);
        }
    }

    private string ValidateAssets()
    {
        if (sourceAsset == null || targetAsset == null)
        {
            return "Source/Target asset must both be assigned.";
        }

        if (sourceAsset.GetType() != targetAsset.GetType())
        {
            return $"Source type ({sourceAsset.GetType().Name}) and target type ({targetAsset.GetType().Name}) must match.";
        }

        var supported = sourceAsset is ThemeData || sourceAsset is AppUIThemeData;
        if (!supported)
        {
            return "Only ThemeData and AppUIThemeData are supported.";
        }

        return string.Empty;
    }

    private static Dictionary<string, List<Sprite>> BuildSpriteIndex()
    {
        var index = new Dictionary<string, List<Sprite>>(StringComparer.Ordinal);
        var spriteGuids = AssetDatabase.FindAssets("t:Sprite");

        foreach (var guid in spriteGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                continue;
            }

            if (!index.TryGetValue(sprite.name, out var bucket))
            {
                bucket = new List<Sprite>();
                index.Add(sprite.name, bucket);
            }

            bucket.Add(sprite);
        }

        return index;
    }

    private static string ReplaceSuffix(string originalName, string source, string target)
    {
        if (string.IsNullOrEmpty(originalName) || string.IsNullOrEmpty(source))
        {
            return originalName;
        }

        var sourceWithUnderscore = "_" + source;
        var targetWithUnderscore = "_" + target;

        if (originalName.EndsWith(sourceWithUnderscore, StringComparison.Ordinal))
        {
            return originalName[..^sourceWithUnderscore.Length] + targetWithUnderscore;
        }

        if (originalName.EndsWith(source, StringComparison.Ordinal))
        {
            return originalName[..^source.Length] + target;
        }

        if (originalName.Contains(sourceWithUnderscore, StringComparison.Ordinal))
        {
            return originalName.Replace(sourceWithUnderscore, targetWithUnderscore, StringComparison.Ordinal);
        }

        return originalName.Replace(source, target, StringComparison.Ordinal);
    }

    private static string BuildReport(
        bool isDryRun,
        ScriptableObject source,
        ScriptableObject target,
        int scanned,
        int mapped,
        int skipped,
        List<string> warnings,
        List<string> missing)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"[Theme Sprite Auto Mapper] {(isDryRun ? "Dry Run" : "Apply")}");
        builder.AppendLine($"Source: {AssetDatabase.GetAssetPath(source)}");
        builder.AppendLine($"Target: {AssetDatabase.GetAssetPath(target)}");
        builder.AppendLine($"Scanned Sprite Fields: {scanned}");
        builder.AppendLine($"Mapped: {mapped}");
        builder.AppendLine($"Skipped (overwrite off): {skipped}");
        builder.AppendLine($"Missing: {missing.Count}");
        builder.AppendLine($"Warnings: {warnings.Count}");

        if (warnings.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("--- Warnings ---");
            foreach (var item in warnings)
            {
                builder.AppendLine(item);
            }
        }

        if (missing.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("--- Missing Sprites ---");
            foreach (var item in missing)
            {
                builder.AppendLine(item);
            }
        }

        return builder.ToString();
    }
}
