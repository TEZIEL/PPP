using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PPP.BLUE.VN
{
    public static class VNScriptLoader
    {
        public static VNScript LoadDay(string dayId)
        {
            return LoadFromStreamingAssets(dayId);
        }

        public static VNScript LoadFromStreamingAssets(string fileNameNoExt)
        {
            var path = Application.streamingAssetsPath + "/VN/" + fileNameNoExt + ".json";
            string json;

            try
            {
                json = File.ReadAllText(path);
            }
            catch (Exception e)
            {
                Debug.LogError($"[VNScriptLoader] Failed to read JSON file: {path}\n{e}");
                return null;
            }

            VNScriptDTO dto;
            try
            {
                dto = JsonUtility.FromJson<VNScriptDTO>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[VNScriptLoader] Failed to parse JSON: {path}\n{e}");
                return null;
            }

            if (dto == null)
            {
                Debug.LogError($"[VNScriptLoader] Parsed DTO is null: {path}");
                return null;
            }

            var nodes = new List<VNNode>();
            if (dto.nodes != null)
            {
                for (int i = 0; i < dto.nodes.Length; i++)
                {
                    var nodeDto = dto.nodes[i];
                    if (nodeDto == null)
                    {
                        nodes.Add(null);
                        continue;
                    }

                    if (!Enum.TryParse(nodeDto.type, true, out VNNodeType t))
                    {
                        Debug.LogError($"[VNScriptLoader] Invalid node type '{nodeDto.type}' at index {i} in {path}");
                        return null;
                    }

                    var node = new VNNode
                    {
                        id = nodeDto.id,
                        type = t,
                        speakerId = nodeDto.speakerId ?? string.Empty,
                        text = nodeDto.text ?? string.Empty,
                        label = nodeDto.label ?? string.Empty,
                        callTarget = nodeDto.arg1 ?? string.Empty,
                        callArg = nodeDto.arg2 ?? string.Empty,
                        branches = ConvertBranches(nodeDto.branches),
                        choices = ConvertChoices(nodeDto.choices),
                    };

                    nodes.Add(node);
                }
            }

            var scriptId = string.IsNullOrWhiteSpace(dto.scriptId) ? (fileNameNoExt ?? string.Empty) : dto.scriptId;
            var script = new VNScript(scriptId, nodes);
            Debug.Log($"[VN] Loaded scriptId={script.ScriptId} nodes={script.nodes.Count} labels={script.LabelCount}");
            return script;
        }

        private static VNNode.BranchRule[] ConvertBranches(VNBranchRuleDTO[] branchDtos)
        {
            if (branchDtos == null)
            {
                return null;
            }

            var branches = new VNNode.BranchRule[branchDtos.Length];
            for (int i = 0; i < branchDtos.Length; i++)
            {
                var branchDto = branchDtos[i];
                if (branchDto == null)
                {
                    continue;
                }

                branches[i] = new VNNode.BranchRule
                {
                    expr = branchDto.expr ?? string.Empty,
                    jumpLabel = branchDto.jumpLabel ?? string.Empty,
                    choiceText = branchDto.choiceText ?? string.Empty,
                };
            }

            return branches;
        }

        private static VNNode.ChoiceOption[] ConvertChoices(VNChoiceOptionDTO[] choiceDtos)
        {
            if (choiceDtos == null)
            {
                return null;
            }

            var choices = new VNNode.ChoiceOption[choiceDtos.Length];
            for (int i = 0; i < choiceDtos.Length; i++)
            {
                var choiceDto = choiceDtos[i];
                if (choiceDto == null)
                {
                    continue;
                }

                choices[i] = new VNNode.ChoiceOption
                {
                    choiceText = choiceDto.choiceText ?? string.Empty,
                    jumpLabel = choiceDto.jumpLabel ?? string.Empty,
                };
            }

            return choices;
        }
    }
}
