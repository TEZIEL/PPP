#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PPP.BLUE.VN.Editor
{
    public static class VNScriptImporter
    {
        private const string CsvInputPath = "Assets/Apps/BLUE/Data/VN";
        private const string JsonOutputPath = "Assets/StreamingAssets/VN";

        [MenuItem("PPP/BLUE/VN/Import CSV -> JSON")]
        public static void ImportCsvToJson()
        {
            if (!Directory.Exists(CsvInputPath))
            {
                Debug.LogError($"[VNScriptImporter] CSV folder not found: {CsvInputPath}");
                return;
            }

            Directory.CreateDirectory(JsonOutputPath);

            var csvFiles = Directory.GetFiles(CsvInputPath, "*.csv", SearchOption.TopDirectoryOnly);
            foreach (var csvFile in csvFiles)
            {
                ImportSingleCsv(csvFile);
            }

            AssetDatabase.Refresh();
            Debug.Log($"[VNScriptImporter] Imported {csvFiles.Length} csv file(s) to JSON.");
        }

        private static void ImportSingleCsv(string csvFilePath)
        {
            var lines = File.ReadAllLines(csvFilePath);
            if (lines.Length == 0)
            {
                Debug.LogWarning($"[VNScriptImporter] Empty CSV: {csvFilePath}");
                return;
            }

            var header = ParseCsvLine(lines[0]);
            var map = BuildHeaderMap(header);
            ValidateRequiredColumns(map, csvFilePath);

            var nodes = new List<VNNodeDTO>();
            VNNodeDTO branchAccumulator = null;
            var branchRules = new List<VNBranchRuleDTO>();
            VNNodeDTO choiceAccumulator = null;
            var choiceOptions = new List<VNChoiceOptionDTO>();

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                {
                    continue;
                }

                var row = ParseCsvLine(lines[i]);
                string id = GetValue(row, map, "id");
                string type = GetValue(row, map, "type");
                string speakerId = GetValue(row, map, "speakerId");
                string text = GetValue(row, map, "text");
                string label = GetValue(row, map, "label");
                string jumpLabel = GetValue(row, map, "jumpLabel");
                string arg1 = GetValue(row, map, "arg1");
                string arg2 = GetValue(row, map, "arg2");

                if (string.IsNullOrWhiteSpace(type))
                {
                    continue;
                }

                var normalizedType = type.Trim();
                if (normalizedType.Equals(nameof(VNNodeType.Branch), StringComparison.OrdinalIgnoreCase))
                {
                    if (branchAccumulator == null || !string.Equals(branchAccumulator.id, id, StringComparison.Ordinal))
                    {
                        FlushBranchNode(nodes, ref branchAccumulator, branchRules);
                        branchAccumulator = new VNNodeDTO
                        {
                            id = id,
                            type = nameof(VNNodeType.Branch)
                        };
                    }

                    branchRules.Add(new VNBranchRuleDTO
                    {
                        expr = arg1 ?? string.Empty,
                        jumpLabel = jumpLabel ?? string.Empty,
                        choiceText = arg2 ?? string.Empty,
                    });

                    continue;
                }

                FlushBranchNode(nodes, ref branchAccumulator, branchRules);

                if (normalizedType.Equals(nameof(VNNodeType.Choice), StringComparison.OrdinalIgnoreCase))
{
    if (choiceAccumulator == null || !string.Equals(choiceAccumulator.id, id, StringComparison.Ordinal))
    {
        FlushChoiceNode(nodes, ref choiceAccumulator, choiceOptions);
        choiceAccumulator = new VNNodeDTO
        {
            id = id,
            type = nameof(VNNodeType.Choice)
        };
    }

    choiceOptions.Add(new VNChoiceOptionDTO
    {
        jumpLabel = jumpLabel ?? string.Empty,
        choiceText = arg2 ?? string.Empty,   // ✅ 버튼 텍스트는 arg2로 고정
    });

    continue;
}
                

                

                    

                FlushBranchNode(nodes, ref branchAccumulator, branchRules);
                FlushChoiceNode(nodes, ref choiceAccumulator, choiceOptions);

                var node = new VNNodeDTO
                {
                    id = id,
                    type = NormalizeType(normalizedType),
                    speakerId = speakerId,
                    text = text,
                    label = ResolveLabel(normalizedType, label, jumpLabel),
                };

                nodes.Add(node);
            }

            FlushBranchNode(nodes, ref branchAccumulator, branchRules);
            FlushChoiceNode(nodes, ref choiceAccumulator, choiceOptions);

            var scriptId = Path.GetFileNameWithoutExtension(csvFilePath);
            var dto = new VNScriptDTO
            {
                scriptId = scriptId,
                nodes = nodes.ToArray(),
            };

            var outputPath = Path.Combine(JsonOutputPath, scriptId + ".json");
            File.WriteAllText(outputPath, JsonUtility.ToJson(dto, true));
            Debug.Log($"[VNScriptImporter] Wrote {outputPath} (nodes={nodes.Count})");
        }

        private static string ResolveLabel(string type, string label, string jumpLabel)
        {
            if (type.Equals(nameof(VNNodeType.Label), StringComparison.OrdinalIgnoreCase))
            {
                return string.IsNullOrWhiteSpace(label) ? jumpLabel : label;
            }

            if (type.Equals(nameof(VNNodeType.Jump), StringComparison.OrdinalIgnoreCase))
            {
                return string.IsNullOrWhiteSpace(jumpLabel) ? label : jumpLabel;
            }

            return label;
        }

        private static string NormalizeType(string type)
        {
            foreach (VNNodeType t in Enum.GetValues(typeof(VNNodeType)))
            {
                if (string.Equals(type, t.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return t.ToString();
                }
            }

            return type;
        }

        private static void FlushBranchNode(List<VNNodeDTO> nodes, ref VNNodeDTO branchAccumulator, List<VNBranchRuleDTO> branchRules)
        {
            if (branchAccumulator == null)
            {
                return;
            }

            branchAccumulator.branches = branchRules.ToArray();
            nodes.Add(branchAccumulator);
            branchAccumulator = null;
            branchRules.Clear();
        }

        private static void FlushChoiceNode(List<VNNodeDTO> nodes, ref VNNodeDTO choiceAccumulator, List<VNChoiceOptionDTO> choiceOptions)
        {
            if (choiceAccumulator == null)
            {
                return;
            }

            choiceAccumulator.choices = choiceOptions.ToArray();
            nodes.Add(choiceAccumulator);
            choiceAccumulator = null;
            choiceOptions.Clear();
        }

        private static Dictionary<string, int> BuildHeaderMap(List<string> header)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < header.Count; i++)
            {
                var key = header[i]?.Trim();
                if (!string.IsNullOrEmpty(key))
                {
                    map[key] = i;
                }
            }

            return map;
        }

        private static void ValidateRequiredColumns(Dictionary<string, int> map, string csvPath)
        {
            string[] required = { "id", "type", "speakerId", "text", "label", "jumpLabel", "arg1", "arg2" };
            foreach (var col in required)
            {
                if (!map.ContainsKey(col))
                {
                    throw new InvalidOperationException($"[VNScriptImporter] Missing required column '{col}' in {csvPath}");
                }
            }
        }

        private static string GetValue(List<string> row, Dictionary<string, int> map, string column)
        {
            if (!map.TryGetValue(column, out var index))
            {
                return string.Empty;
            }

            return index >= 0 && index < row.Count ? row[index] : string.Empty;
        }

        private static List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            if (line == null)
            {
                return result;
            }

            bool inQuotes = false;
            var current = new System.Text.StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }

                    continue;
                }

                if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Length = 0;
                    continue;
                }

                current.Append(c);
            }

            result.Add(current.ToString());
            return result;
        }
    }
}
#endif
