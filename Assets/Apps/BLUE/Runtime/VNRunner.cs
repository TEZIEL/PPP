using System;
using System.Collections.Generic;
using UnityEngine;

namespace PPP.BLUE.VN
{
    public sealed class VNRunner : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool logToConsole = true;
        [SerializeField] private VNScript testScript;
        [SerializeField] private VNOSBridge bridge;
        public bool SaveAllowed { get; private set; }

        public void MarkSaveBlocked() => SaveAllowed = false;
        public void MarkSaveAllowed() => SaveAllowed = true;

        public event Action<string, string, string> OnSay; // speakerId, text, lineId
        public event Action OnEnd;

        public bool HasScript => script != null;
        private VNScript script;
        private int pointer = 0;
        private bool started;

        // 테스트용(나중에 DrinkSave/변수 dict로 교체)
        private int greatCount = 0;
        private int failCount = 0;
        private int successCount = 0;
        private string lastResult = "great"; // "fail"/"success"/"great"

        // VNRunner 필드
        private readonly Dictionary<string, int> vars = new();

        // VNRunner 메서드
        public void SetVar(string key, int value)
        {
            if (string.IsNullOrEmpty(key)) return;
            vars[key] = value;
        }

        public int GetVar(string key, int defaultValue = 0)
        {
            if (string.IsNullOrEmpty(key)) return defaultValue;
            return vars.TryGetValue(key, out var v) ? v : defaultValue;
        }

        // 1단계: 하드코딩 테스트용
        public void Begin()
        {
            if (started) return;
            if (script == null)
            {
                Debug.LogError("[VNRunner] No script loaded.");
                return;
            }
            started = true;
            Next();
        }

        public event Action<VNNode.BranchRule[]> OnChoice;

        private void Start()
        {
            SetVar("lastDrink", 0); // 테스트

            var loaded = VNScriptLoader.LoadFromStreamingAssets("day01");
            SetScript(loaded);

            if (bridge != null) bridge.RequestBlockClose(true);
        }

        private void Update()
        {

            if (!HasScript) return;

        }

        public void Choose(string jumpLabel)
        {
            DoJump(jumpLabel);
            Next();
        }

        private bool HasAnyChoiceText(VNNode.BranchRule[] rules)
        {
            foreach (var r in rules)
                if (r != null && !string.IsNullOrEmpty(r.choiceText))
                    return true;
            return false;
        }

        public void Next()
        {
            Debug.Log("[VN] SaveAllowed FALSE (Next)");
            MarkSaveBlocked();
            if (script == null || script.nodes == null)
            {
                Debug.LogError("[VNRunner] No script loaded.");
                return;
            }

            // 무한루프 방지용 안전장치
            const int MAX_STEPS = 1000;
            int steps = 0;

            while (steps++ < MAX_STEPS)
            {
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
                    continue;
                }

                switch (node.type)
                {
                    case VNNodeType.Say:
                        EmitSay(node);
                        pointer++;
                        return; // ✅ Say는 "멈춤" 포인트 (화면에 보여주고 기다림)

                    case VNNodeType.Branch:
                        if (node.branches != null && node.branches.Length > 0 &&
                            HasAnyChoiceText(node.branches))
                        {
                            OnChoice?.Invoke(node.branches);
                            pointer++;        // Branch 자체는 소비
                            return;           // ✅ 여기서 멈춤(선택 기다림)
                        }
                        else
                        {
                            DoBranch(node);   // 기존 자동 분기
                            continue;
                        }

                    case VNNodeType.Label:
                        if (logToConsole) Debug.Log($"[VN] Label: {node.label} (idx {pointer})");
                        pointer++;
                        continue; // ✅ 출력 없음 → 계속 진행

                    case VNNodeType.Jump:
                        DoJump(node.label);
                        continue; // ✅ 출력 없음 → 계속 진행

                    case VNNodeType.End:
                        Finish();
                        return;

                    default:
                        Debug.LogWarning($"[VNRunner] Unknown node type: {node.type}");
                        pointer++;
                        continue;
                }
            }
            Debug.LogError($"[VNRunner] MAX_STEPS exceeded at pointer={pointer}. Possible infinite loop.");
            
            Finish();
        }

        public void OnAdvance()
        {
            
        }


        

        

        public void SetScript(VNScript s)
        {
            script = s;
            pointer = 0;
            started = false;

            if (script == null)
            {
                Debug.LogError("[VNRunner] Script is null.");
                return;
            }

            Begin(); // ✅ 스크립트가 확실히 있을 때만 시작
        }

        private void EmitSay(VNNode node)
        {
            if (logToConsole)
                Debug.Log($"[VN] {node.speakerId}: {node.text} (id={node.id})");

            OnSay?.Invoke(node.speakerId, node.text, node.id);
        }

        private bool EvaluateExpr(string expr)
        {
            if (string.IsNullOrWhiteSpace(expr)) return false;

            expr = expr.Replace(" ", "").Trim();

            // else는 DoBranch에서 이미 처리하니까 여기서는 false
            if (expr.Equals("else", StringComparison.OrdinalIgnoreCase))
                return false;

            // 지원 연산자: >= <= == > <
            string op = null;
            if (expr.Contains(">=")) op = ">=";
            else if (expr.Contains("<=")) op = "<=";
            else if (expr.Contains("==")) op = "==";
            else if (expr.Contains(">")) op = ">";
            else if (expr.Contains("<")) op = "<";
            else
            {
                Debug.LogWarning($"[VNRunner] Unknown expr(no operator): {expr}");
                return false;
            }

            var parts = expr.Split(new[] { op }, StringSplitOptions.None);
            if (parts.Length != 2)
            {
                Debug.LogWarning($"[VNRunner] Bad expr(split): {expr}");
                return false;
            }

            var key = parts[0];
            if (!int.TryParse(parts[1], out var rhs))
            {
                Debug.LogWarning($"[VNRunner] Bad expr(rhs not int): {expr}");
                return false;
            }

            var lhs = GetVar(key, 0); // ✅ vars(int)에서 가져옴

            return op switch
            {
                ">=" => lhs >= rhs,
                "<=" => lhs <= rhs,
                "==" => lhs == rhs,
                ">" => lhs > rhs,
                "<" => lhs < rhs,
                _ => false
            };
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
                Debug.LogWarning("[VNRunner] Branch has no rules. Skip.");
                pointer++;
                return;
            }

            foreach (var r in node.branches)
            {
                if (r == null) continue;

                if (string.Equals(r.expr, "else", StringComparison.OrdinalIgnoreCase) || EvaluateExpr(r.expr))
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
                Debug.LogError($"[VNRunner] Jump label is empty. nodeId={script?.nodes?[pointer]?.id} idx={pointer}");
                pointer++;
                return;
            }

            if (!script.TryGetLabelIndex(label, out var idx))
            {
                Debug.LogError($"[VNRunner] Jump target not found: '{label}'. nodeId={script?.nodes?[pointer]?.id} idx={pointer} scriptId={script?.scriptId}");
                pointer++;
                return;
            }

            if (logToConsole)
                Debug.Log($"[VN] Jump -> {label} (idx {idx}) from nodeId={script?.nodes?[pointer]?.id} curIdx={pointer}");

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


        public void ApplyDrinkResult(string result) // "fail" / "success" / "great"
        {
            lastResult = result;

            if (result == "great")
            {
                greatCount++;
                successCount++; // great는 success 포함 규칙
            }
            else if (result == "success")
            {
                successCount++;
            }
            else
            {
                failCount++;
            }

            if (logToConsole)
                Debug.Log($"[VN] DrinkResult = {result} (great={greatCount}, success={successCount}, fail={failCount})");
        }

        private VNScript BuildTestScript()
        {
            var nodes = new List<VNNode>
    {
            new VNNode { id="t.001", type=VNNodeType.Say, speakerId="sys",
            text="(Test) Dialogue -> Drink -> Branch. Press SPACE to continue." },

        // ✅ 이 라인을 만나면 VNDialogueView에서 drinkTestPanel.Open()을 호출하도록 할 것
            new VNNode { id="t.drink", type=VNNodeType.Say, speakerId="sys",
            text="(Drink) Please make a drink now." },

        // 결과 버튼을 누르면 runner.ApplyDrinkResult(...)가 호출되고
        // 그 직후 runner.Next()로 여기 Branch로 진입하게 됨
            new VNNode {
                id="t.branch",
                type=VNNodeType.Branch,
                branches = new[]
                {
                // great는 success 포함 규칙이라 great>=2면 항상 route2로 가게 됨
                new VNNode.BranchRule{ expr="great>=2", jumpLabel="route2" },
                new VNNode.BranchRule{ expr="fail>=2",  jumpLabel="route3" },
                new VNNode.BranchRule{ expr="else",     jumpLabel="route1" },
                }
            },

            new VNNode { id="t.r1", type=VNNodeType.Label, label="route1" },
            new VNNode { id="t.r1s", type=VNNodeType.Say, speakerId="sys",
                text="Route 1: Normal result (not enough Great, not enough Fail)." },
            new VNNode { id="t.end1", type=VNNodeType.End },

            new VNNode { id="t.r2", type=VNNodeType.Label, label="route2" },
            new VNNode { id="t.r2s", type=VNNodeType.Say, speakerId="sys",
                text="Route 2: Great >= 2 (Excellent performance)." },
            new VNNode { id="t.end2", type=VNNodeType.End },

            new VNNode { id="t.r3", type=VNNodeType.Label, label="route3" },
            new VNNode { id="t.r3s", type=VNNodeType.Say, speakerId="sys",
                text="Route 3: Fail >= 2 (Too many mistakes)." },
            new VNNode { id="t.end3", type=VNNodeType.End },
            };

            return new VNScript("test", nodes);
        }
    }
}