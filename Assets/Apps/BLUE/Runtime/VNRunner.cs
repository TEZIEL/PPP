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

        private const string SAVE_KEY = "vn.state";
        private const string VN_STATE_KEY = "vn.state";
        private int lastShownPointer = -1;
        private int lastStopIndex = 0; // 마지막으로 '멈춘' 노드(Say/Choice)의 인덱스

        public void InjectBridge(VNOSBridge b)
        {
            bridge = b;
        }

        private void Awake()
        {
            TryResolveBridge(silent: true);
        }


        public void MarkSaveBlocked() => SaveAllowed = false;
        public void MarkSaveAllowed() 
        { 
                SaveAllowed = true;
                SaveState(); // ✅ 여기 한 줄로 “자동 저장” 완성
            }


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


        public void Begin()
        {
            if (started) return;
            if (script == null) { Debug.LogError("[VNRunner] No script loaded."); return; }
            Debug.Log($"[VN] Begin() id={GetInstanceID()} go={gameObject.name} started={started}");

            LoadState();   // 있으면 복원, 없으면 false
            started = true;
            Next();
        }

        private void EmitCurrent()
        {
            if (script == null || script.nodes == null) return;
            if (pointer < 0 || pointer >= script.nodes.Count)
            {
                Debug.LogWarning($"[VNRunner] EmitCurrent out of range pointer={pointer}");
                return;
            }

            // “현재 포인터 노드를 한번 재생”하는 함수
            // 보통 Next() 안에서 switch(node.type) 하는 로직이 있을 텐데,
            // 그걸 분리해서 "RunNode(script.nodes[pointer], advance:false)" 같은 형태로 만드는 게 베스트.
            // 일단 간단히는 Next() 구조를 조금 바꿔야 함.
        }

        public event Action<VNNode.BranchRule[]> OnChoice;

        private void Start()
        {
            TryResolveBridge(silent: false);

            if (bridge != null)
                bridge.RequestBlockClose(true);

            var loaded = VNScriptLoader.LoadFromStreamingAssets("day01");
            SetScript(loaded);

            Begin();
        }

        private void TryResolveBridge(bool silent)
        {
            if (bridge != null) return;

            // 같은 프리팹 루트(VN_APP) 아래에서 브릿지 찾기
            bridge = GetComponentInParent<Transform>()
                ?.GetComponentInChildren<VNOSBridge>(true);

            if (!silent && bridge == null)
                Debug.LogError("[VNRunner] bridge not found (VNOSBridge).");
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
            Debug.Log($"[VN] Next() id={GetInstanceID()} go={gameObject.name} pointer={pointer} started={started}");
            Debug.Log($"[VN] Next() pointer={pointer} nodes={script?.nodes?.Count ?? -1} started={started}");
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
                Debug.Log($"[VN] node@{pointer} type={node?.type} label={node?.label}");
                if (node == null)
                {
                    pointer++;
                    continue;
                }

                switch (node.type)
                {
                    case VNNodeType.Say:
                        lastShownPointer = pointer; // ✅ 이 줄이 핵심
                        EmitSay(node);
                        pointer++;
                        return; // ✅ Say는 "멈춤" 포인트 (화면에 보여주고 기다림)

                    case VNNodeType.Branch:
                        if (node.branches != null && node.branches.Length > 0 &&
                            HasAnyChoiceText(node.branches))
                        {
                            lastStopIndex = pointer;
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
            lastStopIndex = 0;
            started = false;
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
                Debug.LogError($"[VN] Jump label is empty. nodeId={script?.nodes?[pointer]?.id} idx={pointer}");
                // 안전: 여기서는 진행하지 말자
                return;
            }

            if (!script.TryGetLabelIndex(label, out var idx))
            {
                Debug.LogError($"[VN] Label not found: '{label}' nodeId={script?.nodes?[pointer]?.id} curIdx={pointer}");
                // 안전: 여기서는 진행하지 말자 (엔진 상태를 망치지 않게)
                return;
            }

            if (logToConsole)
                Debug.Log($"[VN] Jump -> {label} (idx {idx}) from nodeId={script?.nodes?[pointer]?.id} curIdx={pointer}");

            pointer = idx + 1;
        }

        private void Finish()
        {
            if (logToConsole)
                Debug.Log("[VN] End");

            OnEnd?.Invoke();
            enabled = false; // 1단계에서는 그냥 멈춤
        }


        private VNState BuildState()
        {
            var st = new VNState();
            st.scriptId = script != null ? script.ScriptId : "";

            // ✅ 마지막으로 화면에 보여준 노드부터 다시 시작
            st.pointer = (lastShownPointer >= 0) ? lastShownPointer : pointer;

            st.vars.Clear();
            foreach (var kv in vars)
                st.vars.Add(new VNIntVar { key = kv.Key, value = kv.Value });

            st.greatCount = greatCount;
            st.successCount = successCount;
            st.failCount = failCount;
            st.lastResult = lastResult ?? "";

            return st;
        }


        private void ApplyState(VNState st)
        {
            if (st == null) return;

            // scriptId가 다르면 다른 스크립트 세이브라서 무시
            if (script == null || !string.Equals(st.scriptId, script.ScriptId, StringComparison.Ordinal))
                return;

            // pointer 안전 클램프
            pointer = Mathf.Clamp(st.pointer, 0, script.nodes != null ? script.nodes.Count : 0);

            // vars 복원
            vars.Clear();
            if (st.vars != null)
            {
                for (int i = 0; i < st.vars.Count; i++)
                {
                    var v = st.vars[i];
                    if (v == null || string.IsNullOrEmpty(v.key)) continue;
                    vars[v.key] = v.value;
                }
            }

            // drink counters 복원
            greatCount = st.greatCount;
            successCount = st.successCount;
            failCount = st.failCount;
            lastResult = string.IsNullOrEmpty(st.lastResult) ? "success" : st.lastResult;
        }


        private void SaveState()
        {
            if (!SaveAllowed) return;
            if (bridge == null || script == null) return;

            var st = BuildState(); // CaptureState/BuildState 중 하나로 통일 추천
            bridge.SaveVN(VN_STATE_KEY, st);

            if (logToConsole)
                Debug.Log($"[VN] SaveState -> key={VN_STATE_KEY} scriptId={st.scriptId} pointer={st.pointer} vars={st.vars?.Count ?? 0}");
        }



        private bool LoadState()
        {
            if (bridge == null || script == null)
                return false;

            var st = bridge.LoadVN<VNState>(VN_STATE_KEY);
            if (st == null)
                return false;

            if (!string.Equals(st.scriptId, script.ScriptId, StringComparison.Ordinal))
                return false;

            pointer = Mathf.Clamp(st.pointer, 0, script.nodes.Count - 1);
            lastShownPointer = pointer;
            lastStopIndex = pointer; // 같이 맞춰두면 안전

            vars.Clear();
            if (st.vars != null)
            {
                foreach (var kv in st.vars)
                {
                    if (!string.IsNullOrEmpty(kv.key))
                        vars[kv.key] = kv.value;
                }
            }

            greatCount = st.greatCount;
            successCount = st.successCount;
            failCount = st.failCount;
            lastResult = st.lastResult;

            return true;
        }



        public bool RestoreState(VNState dto)
        {
            if (dto == null) return false;
            if (script == null || script.nodes == null) return false;

            // 스크립트가 다르면 로드 실패 (원하면 여기서 리셋로직)
            if (!string.Equals(dto.scriptId, script.ScriptId, StringComparison.Ordinal))
                return false;

            // pointer 범위 보정
            pointer = Mathf.Clamp(dto.pointer, 0, script.nodes.Count);

            vars.Clear();
            

            greatCount = dto.greatCount;
            successCount = dto.successCount;
            failCount = dto.failCount;
            lastResult = dto.lastResult;

            return true;
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