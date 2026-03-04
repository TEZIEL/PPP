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

                var raw = lines[i].TrimStart();
                if (raw.StartsWith("#")) continue;

                var row = ParseCsvLine(lines[i]);
                string id = GetValueAny(row, map, "id");
                string type = GetValueAny(row, map, "type");
                string speaker = GetValueAny(row, map, "speaker", "speakerId");
                string text = GetValueAny(row, map, "text");
                string label = GetValueAny(row, map, "label");
                string target = GetValueAny(row, map, "target", "arg2");
                string arg = GetValueAny(row, map, "arg", "arg1");
                string cond = GetValueAny(row, map, "cond", "arg1");
                string next = GetValueAny(row, map, "next", "jumpLabel");


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
                        expr = cond ?? string.Empty,
                        cond = cond ?? string.Empty,
                        jumpLabel = next ?? string.Empty,
                        next = next ?? string.Empty,
                        choiceText = text ?? string.Empty,
                    });

                    continue;
                }

                FlushBranchNode(nodes, ref branchAccumulator, branchRules);

                if (normalizedType.Equals(nameof(VNNodeType.Choice), StringComparison.OrdinalIgnoreCase))
                    {
                     // 새 Choice 블록 시작(같은 id면 누적 유지)
                        if (choiceAccumulator == null || !string.Equals(choiceAccumulator.id, id, StringComparison.Ordinal))
                     {
                     FlushChoiceNode(nodes, ref choiceAccumulator, choiceOptions);
                     choiceAccumulator = new VNNodeDTO
                     {
                           id = id,
                           type = nameof(VNNodeType.Choice)
                      };
                   }

                    // 최소 스키마(Choice 행 자체에 text/next를 두는 형태)도 허용
                    if (!string.IsNullOrEmpty(text) || !string.IsNullOrEmpty(next))
                    {
                        choiceOptions.Add(new VNChoiceOptionDTO
                        {
                            choiceText = !string.IsNullOrEmpty(text) ? text : arg,
                            jumpLabel = next ?? string.Empty,
                            next = next ?? string.Empty,
                        });
                    }

                     continue;
                }
                
                if (normalizedType.Equals("ChoiceOption", StringComparison.OrdinalIgnoreCase))
{
    if (choiceAccumulator == null)
    {
        Debug.LogWarning($"[VNScriptImporter] ChoiceOption without active Choice at line {i + 1} in {csvFilePath} (id={id})");
        continue;
    }

    // A안 규칙:
    // - arg1 = 표시 텍스트 (너 지금 JSON에서도 arg1에 들어오고 있음)
    // - jumpLabel 컬럼 = 점프 라벨
    var ct = !string.IsNullOrEmpty(arg) ? arg : text;

    choiceOptions.Add(new VNChoiceOptionDTO
    {
        choiceText = ct ?? string.Empty,
        jumpLabel = next ?? string.Empty,
        next = next ?? string.Empty,
    });

    // ✅ nodes.Add(...) 절대 하지 말고 누적만 하고 끝
    continue;
}
                

                    

                FlushBranchNode(nodes, ref branchAccumulator, branchRules);
                FlushChoiceNode(nodes, ref choiceAccumulator, choiceOptions);

                var node = new VNNodeDTO
                {
                    id = id,
                    type = NormalizeType(normalizedType),
                    speakerId = speaker,
                    speaker = speaker,
                    text = text,
                    label = ResolveLabel(normalizedType, label, next),
                    target = target,
                    arg = arg,
                    cond = cond,
                    next = next,
                    arg1 = arg,
                    arg2 = target,
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

            if (string.Equals(type, "ChoiceOption", StringComparison.OrdinalIgnoreCase))
                return "ChoiceOption";

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
            string[] required = { "id", "type" };
            foreach (var col in required)
            {
                if (!map.ContainsKey(col))
                {
                    throw new InvalidOperationException($"[VNScriptImporter] Missing required column '{col}' in {csvPath}");
                }
            }
        }

        private static string GetValueAny(List<string> row, Dictionary<string, int> map, params string[] columns)
        {
            if (columns == null) return string.Empty;

            for (int i = 0; i < columns.Length; i++)
            {
                var v = GetValue(row, map, columns[i]);
                if (!string.IsNullOrEmpty(v)) return v;
            }

            return string.Empty;
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
