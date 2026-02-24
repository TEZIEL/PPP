using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PPP.BLUE.VN
{
    public static class VNScriptLoader
    {
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

                    if (!Enum.TryParse<VNNodeType>(nodeDto.type, true, out var t))
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
                        branches = ConvertBranches(nodeDto.branches),
                    };

                    nodes.Add(node);
                }
            }

            return new VNScript(dto.scriptId ?? string.Empty, nodes);
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
    }
}
