using System;
using System.Collections.Generic;
using UnityEngine;

namespace PPP.BLUE.VN
{
    public sealed class VNRunner : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool logToConsole = true;

        public event Action<string, string, string> OnSay; // speakerId, text, lineId
        public event Action OnEnd;

        private VNScript script;
        private int pointer = 0;


        // 테스트용(나중에 DrinkSave/변수 dict로 교체)
        private int greatCount = 1;
        private int failCount = 0;
        private int successCount = 1;
        private string lastResult = "great"; // "fail"/"success"/"great"

        // 1단계: 하드코딩 테스트용
        private void Start()
        {
            script = BuildTestScript();
            pointer = 0;

            if (logToConsole)
                Debug.Log("[VNRunner] Ready. Press Space to Next().");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
                Next();
        }

        public void Next()
        {
            if (script == null || script.nodes == null)
            {
                Debug.LogError("[VNRunner] No script loaded.");
                return;
            }

            // 끝까지 갔으면 End 취급
            if (pointer < 0 || pointer >= script.nodes.Count)
            {
                Finish();
                return;
            }

            var node = script.nodes[pointer];
            if (node == null)
            {
                pointer++;
                return;
            }

            switch (node.type)
            {
                case VNNodeType.Say:
                    EmitSay(node);
                    pointer++;
                    break;

                case VNNodeType.Branch:
                    DoBranch(node);
                    break;

                case VNNodeType.Label:
                    // Label은 실행 효과 없음. 그냥 다음으로.
                    if (logToConsole) Debug.Log($"[VN] Label: {node.label} (idx {pointer})");
                    pointer++;
                    break;

                case VNNodeType.Jump:
                    DoJump(node.label);
                    break;

                case VNNodeType.End:
                    Finish();
                    break;

                default:
                    Debug.LogWarning($"[VNRunner] Unknown node type: {node.type}");
                    pointer++;
                    break;
            }
        }

        private void EmitSay(VNNode node)
        {
            if (logToConsole)
                Debug.Log($"[VN] {node.speakerId}: {node.text} (id={node.id})");

            OnSay?.Invoke(node.speakerId, node.text, node.id);
        }

        private bool EvaluateExpr(string expr)
        {
            if (string.IsNullOrEmpty(expr)) return false;

            // 공백 제거
            expr = expr.Replace(" ", "").ToLowerInvariant();

            // last==great 같은 패턴
            if (expr.StartsWith("last=="))
            {
                var v = expr.Substring("last==".Length);
                return lastResult == v;
            }

            // great>=2 같은 패턴
            if (TryParseCompare(expr, "great", greatCount, out var ok1)) return ok1;
            if (TryParseCompare(expr, "fail", failCount, out var ok2)) return ok2;
            if (TryParseCompare(expr, "success", successCount, out var ok3)) return ok3;

            Debug.LogWarning($"[VNRunner] Unknown expr: {expr}");
            return false;
        }

        private bool TryParseCompare(string expr, string key, int value, out bool result)
        {
            result = false;

            if (!expr.StartsWith(key)) return false;

            // 지원 연산자: >=, <=, ==, >, <
            string op = null;
            if (expr.Contains(">=")) op = ">=";
            else if (expr.Contains("<=")) op = "<=";
            else if (expr.Contains("==")) op = "==";
            else if (expr.Contains(">")) op = ">";
            else if (expr.Contains("<")) op = "<";
            else return false;

            var parts = expr.Split(new[] { op }, StringSplitOptions.None);
            if (parts.Length != 2) return false;

            // parts[0] = key, parts[1] = number
            if (!int.TryParse(parts[1], out var rhs)) return false;

            result = op switch
            {
                ">=" => value >= rhs,
                "<=" => value <= rhs,
                "==" => value == rhs,
                ">" => value > rhs,
                "<" => value < rhs,
                _ => false
            };

            return true;
        }

        private void DoBranch(VNNode node)
        {
            if (node.branches == null || node.branches.Length == 0)
            {
                Debug.LogError("[VNRunner] Branch has no rules.");
                pointer++;
                return;
            }

            foreach (var r in node.branches)
            {
                if (r == null) continue;

                if (r.expr == "else" || EvaluateExpr(r.expr))
                {
                    if (!string.IsNullOrEmpty(r.jumpLabel))
                    {
                        Debug.Log($"[VN] Branch -> {r.jumpLabel} (expr={r.expr})");
                        DoJump(r.jumpLabel);
                        return;
                    }
                }
            }
                        // 아무 조건도 못 맞추면 그냥 다음
            pointer++;
        }



        private void DoJump(string label)
        {
            if (string.IsNullOrEmpty(label))
            {
                Debug.LogError("[VNRunner] Jump label is empty.");
                pointer++;
                return;
            }

            if (!script.TryGetLabelIndex(label, out var idx))
            {
                Debug.LogError($"[VNRunner] Jump target not found: {label}");
                pointer++;
                return;
            }

            if (logToConsole)
                Debug.Log($"[VN] Jump -> {label} (idx {idx})");

            // 보통 라벨 다음 줄부터 실행하고 싶으니 +1
            pointer = idx + 1;
        }

        private void Finish()
        {
            if (logToConsole)
                Debug.Log("[VN] End");

            OnEnd?.Invoke();
            enabled = false; // 1단계에서는 그냥 멈춤
        }

        private VNScript BuildTestScript()
        {
            // 네 “하루 시작 -> 뉴스 -> 대화 -> 음료” 중 ‘대화’ 최소 느낌만 테스트
            
            
            var nodes = new List<VNNode>
            {
                new VNNode { id="t.001", type=VNNodeType.Say, speakerId="sys", text="(분기 테스트)" },

                new VNNode {
                    id="t.002",
                    type=VNNodeType.Branch,
                    branches = new[]
             {
                new VNNode.BranchRule{ expr="great>=2", jumpLabel="route2" },
                new VNNode.BranchRule{ expr="fail>=2",  jumpLabel="route3" },
                new VNNode.BranchRule{ expr="else",     jumpLabel="route1" },
                }
            },

                new VNNode { id="t.r1", type=VNNodeType.Label, label="route1" },
                new VNNode { id="t.r1s", type=VNNodeType.Say, speakerId="sys", text="루트1로 진입" },
                new VNNode { id="t.end1", type=VNNodeType.End },

                new VNNode { id="t.r2", type=VNNodeType.Label, label="route2" },
                new VNNode { id="t.r2s", type=VNNodeType.Say, speakerId="sys", text="루트2(대성공 2회 이상) 진입" },
                new VNNode { id="t.end2", type=VNNodeType.End },

                new VNNode { id="t.r3", type=VNNodeType.Label, label="route3" },
                new VNNode { id="t.r3s", type=VNNodeType.Say, speakerId="sys", text="루트3(실패 2회 이상) 진입" },
                new VNNode { id="t.end3", type=VNNodeType.End },



            };

            return new VNScript("test", nodes);
        }
    }
}