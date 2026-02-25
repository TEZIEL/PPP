using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using PPP.BLUE.VN;

namespace PPP.BLUE.VN
{
    public sealed class VNRunner : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool logToConsole = true;
        [SerializeField] private VNScript testScript;
        [SerializeField] private bool loadFromJsonOnStart;
        [SerializeField] private string startDayId = "day01";
        [SerializeField] private VNOSBridge bridge;
        public bool SaveAllowed { get; private set; }

        [SerializeField] private VNRunner runner;
        private VNState state;
        private Coroutine autoCo;
        [SerializeField] private VNPolicyController policy;
        // --- Auto suspend/resume by modal/drink ---
        private bool lastBlocked;                 // 지난 프레임에 입력/오토가 막혀있었나?
        private bool autoSuspendedByBlock;        // 막힘 때문에 Auto를 멈췄었나?
        private bool lastMinimized;
        private int waitPointer = -1;   // 화면에 보여주고 '기다리는' 노드 인덱스
        private bool isWaiting;         // 지금 입력/선택 대기 상태인지

        private const string SAVE_KEY = "vn.state";
        private const string VN_STATE_KEY = "vn.state";
        private const string VN_STATE_KEY_DBG = "vn.state.dbg";
        private const float AutoDelaySeconds = 0.35f;
        private int lastShownPointer = -1;
        private int lastStopIndex = 0; // 마지막으로 '멈춘' 노드(Say/Choice)의 인덱스
        


        public void InjectBridge(VNOSBridge b)
        {
            bridge = b;
        }

        private void Awake()
        {
            TryResolveBridge(silent: true);

            BindPolicy("Awake");

            // ✅ 1프레임 뒤 재시도 (WindowManager가 AttachContent 후 wiring 끝난 뒤)
            StartCoroutine(CoBindPolicyNextFrame());
        }


        public void MarkSaveBlocked() => SaveAllowed = false;
        public void MarkSaveAllowed(bool allowed, string reason)
        {
            SaveAllowed = allowed;

            if (logToConsole)
                Debug.Log($"[VN] SaveAllowed {(SaveAllowed ? "TRUE" : "FALSE")} ({reason})");

            if (SaveAllowed)
            {
                SaveState();
                TryStartAutoTimer();
            }
            else
                StopAutoTimer();
        }

        // ✅ 기존 호출 유지용(대부분 DialogueView가 runner.MarkSaveAllowed()로 호출하니까)
        public void MarkSaveAllowed()
        {
            MarkSaveAllowed(true, "Typing End");
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
        private readonly HashSet<string> seenLineIds = new();
        private readonly Stack<VNCallFrame> callStack = new();
        private VNCallFrame pendingCallResumeFrame;
        private VNSettings settings = VNSettings.Default();

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

            if (LoadState())
                GetComponentInChildren<VNDialogueView>(true)?.LockInputFrames(1);

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

        public event Action<VNNode.ChoiceOption[]> OnChoice;
        public event Action<string, string> OnCall; // callTarget, callArg

        private void Start()
        {
            TryResolveBridge(silent: false);

            if (bridge != null)
                bridge.RequestBlockClose(true);

            StartCoroutine(CoBindPolicyNextFrame());

            if (testScript != null)
                SetScript(testScript);

            if (loadFromJsonOnStart)
            {
                var loaded = VNScriptLoader.LoadDay(startDayId);
                if (loaded == null)
                {
                    Debug.LogError($"[VNRunner] Failed to load script '{startDayId}'.");
                }
                else
                {
                    SetScript(loaded);
                }
            }

            if (!HasScript)
            {
                Debug.LogError("[VNRunner] No script configured to start.");
                return;
            }

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

            // ----------------------------
            // 0) Window state (minimized)
            // ----------------------------
            bool nowMin = (policy != null) && policy.GetWindowState().IsMinimized;

            // ✅ "방금 최소화됨" 순간 -> Auto 강제 OFF
            if (nowMin && !lastMinimized)
            {
                if (settings.auto)
                {
                    settings.auto = false;        // ✅ 유저 상태 자체를 OFF로
                    autoSuspendedByBlock = false; // ✅ 자동재개 플래그 리셋

                    StopAutoTimer();
                    CancelAuto();

                    if (logToConsole)
                        Debug.Log("[VN] Auto forced OFF (Window minimized)");
                }
            }
            lastMinimized = nowMin;

            // ----------------------------
            // 1) Policy 기반 Auto suspend/resume (modal/drink/minimized)
            // ----------------------------
            bool blocked = (policy != null) && (policy.IsModalOpen || policy.IsInDrinkMode || nowMin);

            // 막힘이 "켜지는 순간" -> Auto 일시정지
            if (blocked && !lastBlocked)
            {
                if (settings.auto) autoSuspendedByBlock = true;

                StopAutoTimer();
                CancelAuto();

                if (logToConsole) Debug.Log("[VN] Auto suspended by modal/drink/minimized");
            }

            // 막힘이 "꺼지는 순간" -> Auto 재개 (유저 의도가 ON일 때만)
            if (!blocked && lastBlocked)
            {
                if (settings.auto && autoSuspendedByBlock)
                {
                    autoSuspendedByBlock = false;
                    TryStartAutoTimer();

                    if (logToConsole) Debug.Log("[VN] Auto resumed after modal/drink");
                }
            }

            lastBlocked = blocked;

            // ----------------------------
            // 2) Hotkeys: blocked 중에는 무시
            // ----------------------------
            if (Input.GetKeyDown(KeyCode.F1))
            {
                if (blocked)
                {
                    if (logToConsole) Debug.Log("[VN] Skip toggle ignored (blocked).");
                }
                else
                {
                    settings.skipMode =
                        settings.skipMode == VNSkipMode.Off
                            ? VNSkipMode.SeenOnly
                            : VNSkipMode.Off;

                    Debug.Log($"[VN] SkipMode={settings.skipMode}");
                }
            }

            if (Input.GetKeyDown(KeyCode.F2))
            {
                if (blocked)
                {
                    if (logToConsole) Debug.Log("[VN] Auto toggle ignored (blocked).");
                }
                else
                {
                    settings.auto = !settings.auto;
                    Debug.Log($"[VN] Auto={(settings.auto ? "On" : "Off")}");

                    autoSuspendedByBlock = false; // 유저가 직접 토글했으니 자동재개 플래그 리셋

                    if (settings.auto)
                        TryStartAutoTimer();
                    else
                    {
                        StopAutoTimer();
                        CancelAuto();
                    }
                }
            }

            // ----------------------------
            // 3) Safety: 조건 깨졌으면 코루틴 Auto 즉시 정지
            // ----------------------------
            if (autoCo != null && !CanAutoAdvance())
                StopAutoTimer();

            // (여기 아래에 네 기존 autoPending 방식 로직이 있으면 그대로 두면 됨)
        }

        public void NotifyLineTypedEnd()
        {
            if (!settings.auto) return;

            TryStartAutoTimer();

            if (logToConsole)
                Debug.Log("[VN] AutoTimer Start (TypingEnd)");
        }


        public void CancelAuto()
        {
            StopAutoTimer();
        }

        public void Choose(string jumpLabel)
        {
            DoJump(jumpLabel);
            Next();
        }

        private static bool HasChoiceText(VNNode.ChoiceOption[] choices)
        {
            if (choices == null || choices.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < choices.Length; i++)
            {
                var choice = choices[i];
                if (choice != null && !string.IsNullOrEmpty(choice.choiceText))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasAnyChoiceText(VNNode.BranchRule[] rules)
        {
            foreach (var r in rules)
                if (r != null && !string.IsNullOrEmpty(r.choiceText))
                    return true;
            return false;
        }

        private static VNNode.ChoiceOption[] ConvertChoiceRules(VNNode.BranchRule[] rules)
        {
            if (rules == null || rules.Length == 0)
            {
                return Array.Empty<VNNode.ChoiceOption>();
            }

            var choices = new VNNode.ChoiceOption[rules.Length];
            for (int i = 0; i < rules.Length; i++)
            {
            
                var rule = rules[i];
                if (rule == null)
                {
                    continue;
                }

                choices[i] = new VNNode.ChoiceOption
                {
                    choiceText = rule.choiceText,
                    jumpLabel = rule.jumpLabel,
                };
            }

            return choices;
        }

        public void Next()
        {
            if (TryResumePendingCallAfterLoad()) return;

            isWaiting = false;
            waitPointer = -1;

            if (policy != null && policy.GetWindowState().IsMinimized)
            {
                if (logToConsole) Debug.Log("[VN] Next ignored (minimized).");
                return;
            }

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
                            if (settings.skipMode == VNSkipMode.SeenOnly
                                && !string.IsNullOrEmpty(node.id)
                                && seenLineIds.Contains(node.id))
                            {
                                Debug.Log($"[VN] Skip: {node.id}");
                                pointer++;
                                continue;
                            }

                            waitPointer = pointer;
                            isWaiting = true;
                            lastShownPointer = pointer;

                            EmitSay(node);
                            pointer++;
                            return;

                        case VNNodeType.Choice:
                            {
                                Debug.Log($"[VN] Choice hit id={node.id} choices={(node.choices == null ? -1 : node.choices.Length)} hasText={HasChoiceText(node.choices)}");

                                if (node.choices != null && HasChoiceText(node.choices))
                                {
                                    lastStopIndex = pointer;
                                    waitPointer = pointer;
                                    isWaiting = true;

                                    MarkSaveAllowed(true, "Choice Open");
                                    Debug.Log("[VN] OnChoice invoke");
                                    OnChoice?.Invoke(node.choices);
                                    return;
                                }

                                Debug.LogWarning("[VN] Choice skipped (no options or no text).");
                                pointer++; // (너 코드에 이미 있을 가능성 높음)
                                continue;
                            }

                            pointer++;
                            continue;

                        case VNNodeType.Branch:
                            if (node.branches != null && node.branches.Length > 0 &&
                                HasAnyChoiceText(node.branches))
                            {
                                lastStopIndex = pointer;
                                waitPointer = pointer;  // ✅ “이 Branch에서 멈춤”
                                isWaiting = true;

                                MarkSaveAllowed(true, "Choice Open");
                                OnChoice?.Invoke(ConvertChoiceRules(node.branches));
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

                        case VNNodeType.Call:
                            {
                                string target = node.callTarget ?? string.Empty;
                                string arg = node.callArg ?? string.Empty;

                                StopAutoExternal("Call:" + target);
                                isWaiting = true;
                                waitPointer = pointer;
                                lastStopIndex = pointer;

                                callStack.Push(new VNCallFrame
                                {
                                    returnPointer = pointer + 1,
                                    target = target,
                                    arg = arg,
                                });

                                OnCall?.Invoke(target, arg);
                                return;
                            }

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
        }
        public void OnAdvance()
        {
            
        }






        public void SetScript(VNScript s)
        {
            script = s;
            pointer = 0;
            started = false;
        }

        private void EmitSay(VNNode node)
        {
            if (logToConsole)
                Debug.Log($"[VN] {node.speakerId}: {node.text} (id={node.id})");

            OnSay?.Invoke(node.speakerId, node.text, node.id);
        }

        public void MarkSeen(string lineId)
        {
            if (string.IsNullOrEmpty(lineId)) return;

            if (seenLineIds.Add(lineId))
            {
                Debug.Log($"[VN] Seen + {lineId} (total={seenLineIds.Count})");
            }
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
                return;
            }

            if (!script.TryGetLabelIndex(label, out var idx))
            {
                // ✅ 1회 자동 복구: 라벨 인덱스 재빌드 후 재시도
                script.RebuildLabelIndex();

                if (!script.TryGetLabelIndex(label, out idx))
                {
                    Debug.LogError($"[VN] Label not found: '{label}' nodeId={script?.nodes?[pointer]?.id} curIdx={pointer} labels={script.LabelCount} dump={script.DumpLabels()}");
                    return;
                }
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
            st.scriptId = script.ScriptId;

            int savePointer = pointer;

            // ✅ 타이핑 완료로 SaveAllowed TRUE가 되는 ‘발할라식’ 타이밍이면
            // 실제로 화면에 떠있는 라인은 lastShownPointer다.
            if (lastShownPointer >= 0 && script != null && lastShownPointer < script.nodes.Count)
                savePointer = lastShownPointer;

            if (callStack.Count > 0 && waitPointer >= 0)
                savePointer = waitPointer;

            st.pointer = savePointer;

            st.callStack = new List<VNCallFrame>(callStack.Count);
            foreach (var frame in callStack)
            {
                if (frame == null) continue;

                st.callStack.Add(new VNCallFrame
                {
                    returnPointer = frame.returnPointer,
                    target = frame.target,
                    arg = frame.arg,
                });
            }

            // seen 복사
            st.seen = new List<string>(seenLineIds);

            // settings 복사
            st.settings = new VNSettings
            {
                auto = settings.auto,
                
                skipMode = settings.skipMode,
                speed = settings.speed
            };

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
            SaveState(ignoreSaveAllowed: false);
        }

        private void SaveState(bool ignoreSaveAllowed)
        {
            SaveStateToKey(VN_STATE_KEY, ignoreSaveAllowed);
            if (!ignoreSaveAllowed && !SaveAllowed) return;
            if (bridge == null || script == null) return;

            var st = BuildState(); // CaptureState/BuildState 중 하나로 통일 추천

            if (logToConsole)
            {
                var safe = st.settings ?? VNSettings.Default();
                Debug.Log($"[VN] SaveState seen={st.seen?.Count ?? 0} auto={safe.auto} skipMode={safe.skipMode} speed={safe.speed}");
            }

            bridge.SaveVN(VN_STATE_KEY, st);

            if (logToConsole)
                Debug.Log($"[VN] SaveState -> key={VN_STATE_KEY} scriptId={st.scriptId} pointer={st.pointer} vars={st.vars?.Count ?? 0}");
        }

        public void DebugForceSave(string reason)
        {
            Debug.Log($"[VNDBG] ForceSave (debug slot) reason={reason}");
            SaveStateToKey(VN_STATE_KEY_DBG, ignoreSaveAllowed: true);
        }



        private bool LoadState()
        {
            return LoadStateFromKey(VN_STATE_KEY);

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

            st.seen ??= new List<string>();
            st.settings ??= VNSettings.Default();

            seenLineIds.Clear();
            foreach (var lineId in st.seen)
            {
                if (string.IsNullOrEmpty(lineId)) continue;
                seenLineIds.Add(lineId);
            }

            settings = st.settings;

            greatCount = st.greatCount;
            successCount = st.successCount;
            failCount = st.failCount;
            lastResult = st.lastResult;

            callStack.Clear();
            pendingCallResumeFrame = null;
            if (st.callStack != null && st.callStack.Count > 0)
            {
                for (int i = st.callStack.Count - 1; i >= 0; i--)
                {
                    var frame = st.callStack[i];
                    if (frame == null) continue;

                    var restored = new VNCallFrame
                    {
                        returnPointer = frame.returnPointer,
                        target = frame.target,
                        arg = frame.arg,
                    };

                    callStack.Push(restored);
                }

                pendingCallResumeFrame = callStack.Peek();
                isWaiting = true;
                waitPointer = Mathf.Max(0, pendingCallResumeFrame.returnPointer - 1);
            }

            if (logToConsole)
            {
                Debug.Log($"[VN] LoadState pointer={pointer} callStack={callStack.Count} target={pendingCallResumeFrame?.target ?? "-"} arg={pendingCallResumeFrame?.arg ?? "-"}");
                Debug.Log($"[VN] LoadState seen={seenLineIds.Count} auto={settings.auto} skipMode={settings.skipMode} speed={settings.speed}");
            }

            return true;
        }

        public void DebugForceLoad(string reason)
        {
            Debug.Log($"[VNDBG] ForceLoad (debug slot) reason={reason}");

            if (LoadStateFromKey(VN_STATE_KEY_DBG))
                GetComponentInChildren<VNDialogueView>(true)?.LockInputFrames(1);
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




        public void TestSpeed25()
        {
            if (settings == null)
                settings = VNSettings.Default();

            settings.speed = 2.5f;

            Debug.Log($"[VN] TestSpeed25 -> speed={settings.speed}");
        }

        public void SetSpeed(float v)
        {
            // 안전장치
            if (settings == null)
                settings = VNSettings.Default();

            settings.speed = v;
            Debug.Log($"[VN] SetSpeed -> {settings.speed}");
        }

        private bool TryResumePendingCallAfterLoad()
        {
            if (pendingCallResumeFrame == null)
                return false;

            var frame = pendingCallResumeFrame;
            pendingCallResumeFrame = null;

            StopAutoExternal("ResumePendingCall");
            isWaiting = true;
            waitPointer = Mathf.Max(0, frame.returnPointer - 1);

            if (logToConsole)
                Debug.Log($"[VN] ResumePendingCall -> OnCall {frame.target} arg={frame.arg}");

            OnCall?.Invoke(frame.target ?? string.Empty, frame.arg ?? string.Empty);
            return true;
        }


        public void ReturnFromCall(string result)
        {
            if (callStack.Count == 0)
            {
                Debug.LogWarning("[VN] ReturnFromCall ignored (callStack empty)");
                return;
            }

            var frame = callStack.Pop();
            ApplyCallResult(result);

            pointer = frame.returnPointer;
            isWaiting = false;
            waitPointer = -1;

            if (logToConsole)
                Debug.Log($"[VN] ReturnFromCall target={frame.target} arg={frame.arg} result={result} -> pointer={pointer}");

            Next();
        }

        private void ApplyCallResult(string result)
        {
            if (string.IsNullOrEmpty(result))
                return;

            if (string.Equals(result, "great", StringComparison.OrdinalIgnoreCase))
            {
                SetVar("lastDrink", 1);
            }
            else if (string.Equals(result, "success", StringComparison.OrdinalIgnoreCase))
            {
                SetVar("lastDrink", 2);
            }
            else if (string.Equals(result, "fail", StringComparison.OrdinalIgnoreCase))
            {
                SetVar("lastDrink", 3);
            }

            ApplyDrinkResult(result.ToLowerInvariant());
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


        // AutoNext가 "절대" 나가면 안 되는 조건들을 한 군데로 묶는다.
        private bool CanAutoAdvance()
        {
            if (!settings.auto) return false;
            if (!SaveAllowed) return false;
            if (!started) return false;
            if (policy == null) return false;
            if (policy.IsModalOpen) return false;
            if (policy.IsInDrinkMode) return false;
            var ws = policy.GetWindowState(); // bridge 통해 받아옴
            if (ws.IsMinimized) return false;
            return true;
        }

        public void StopAutoExternal(string reason)
        {
            StopAutoTimer();
            if (logToConsole) Debug.Log($"[VN] AutoTimer Stop ({reason})");
        }

        private void TryStartAutoTimer()
        {
            if (autoCo != null) return;
            if (!CanAutoAdvance()) return;

            autoCo = StartCoroutine(CoAutoNext());
        }

        private void StopAutoTimer()
        {
            if (autoCo != null)
            {
                StopCoroutine(autoCo);
                autoCo = null;

                if (logToConsole)
                    Debug.Log("[VN] AutoTimer Stopped");
            }
        }

        private IEnumerator CoAutoNext()
        {
            if (logToConsole) Debug.Log($"[VN] AutoTimer Start ({AutoDelaySeconds:0.00}s)");

            yield return new WaitForSeconds(AutoDelaySeconds);

            autoCo = null;

            // ✅ 발사 직전 재검사 (SaveAllowed FALSE, 모달 ON, 드링크 ON이면 여기서 차단)
            if (!CanAutoAdvance()) yield break;

            if (logToConsole) Debug.Log("[VN] AutoNext");
            Next();
        }

        public void ForceAutoOff(string reason)
        {
            if (!settings.auto && autoCo == null) return; // 이미 꺼져있으면 스킵(선택)

            settings.auto = false;          // 유저 상태 자체 OFF
            autoSuspendedByBlock = false;   // unblock 자동재개 플래그 제거

            StopAutoTimer();
            CancelAuto();

            if (logToConsole) Debug.Log($"[VN] Auto forced OFF ({reason})");
        }


        private IEnumerator CoBindPolicyNextFrame()
        {
            yield return null;
            BindPolicy("Start+1frame");
        }


        private void BindPolicy(string from)
        {
            if (policy != null)
            {
                if (logToConsole) Debug.Log($"[VN] policy bind=OK({policy.name}) from={from}");
                return;
            }

            // 1) 내 자식/부모
            policy = GetComponentInChildren<VNPolicyController>(true);
            if (policy == null) policy = GetComponentInParent<VNPolicyController>(true);

            // 2) 그래도 없으면: "VN_APP 루트"를 잡아서 그 안에서 찾기
            if (policy == null)
            {
                // VN_Runner의 부모가 VN_APP라는 전제(너 Hierarchy 기준)
                var appRoot = transform.parent; // VN_APP
                if (appRoot != null)
                    policy = appRoot.GetComponentInChildren<VNPolicyController>(true);
            }

            if (logToConsole)
                Debug.Log($"[VN] policy bind={(policy != null ? $"OK({policy.name})" : "NULL")} from={from}");
        }


        private void SaveStateToKey(string key, bool ignoreSaveAllowed)
        {
            if (!ignoreSaveAllowed && !SaveAllowed) return;
            if (bridge == null || script == null) return;

            var st = BuildState();

            if (logToConsole)
            {
                var safe = st.settings ?? VNSettings.Default();
                Debug.Log($"[VN] SaveState seen={st.seen?.Count ?? 0} auto={safe.auto} skipMode={safe.skipMode} speed={safe.speed}");
            }

            bridge.SaveVN(key, st);

            if (logToConsole)
                Debug.Log($"[VN] SaveState -> key={key} scriptId={st.scriptId} pointer={st.pointer} vars={st.vars?.Count ?? 0}");
        }

        private bool LoadStateFromKey(string key)
        {
            if (bridge == null || script == null)
                return false;

            var st = bridge.LoadVN<VNState>(key);
            if (st == null)
                return false;

            if (!string.Equals(st.scriptId, script.ScriptId, StringComparison.Ordinal))
                return false;

            pointer = Mathf.Clamp(st.pointer, 0, script.nodes.Count - 1);
            lastShownPointer = pointer;
            lastStopIndex = pointer;

            vars.Clear();
            if (st.vars != null)
            {
                foreach (var kv in st.vars)
                    if (!string.IsNullOrEmpty(kv.key))
                        vars[kv.key] = kv.value;
            }

            st.seen ??= new List<string>();
            st.settings ??= VNSettings.Default();

            seenLineIds.Clear();
            foreach (var lineId in st.seen)
                if (!string.IsNullOrEmpty(lineId))
                    seenLineIds.Add(lineId);

            settings = st.settings;

            greatCount = st.greatCount;
            successCount = st.successCount;
            failCount = st.failCount;
            lastResult = st.lastResult;

            callStack.Clear();
            pendingCallResumeFrame = null;

            if (st.callStack != null && st.callStack.Count > 0)
            {
                for (int i = st.callStack.Count - 1; i >= 0; i--)
                {
                    var frame = st.callStack[i];
                    if (frame == null) continue;

                    callStack.Push(new VNCallFrame
                    {
                        returnPointer = frame.returnPointer,
                        target = frame.target,
                        arg = frame.arg
                    });
                }

                pendingCallResumeFrame = callStack.Peek();
                isWaiting = true;
                waitPointer = Mathf.Max(0, pendingCallResumeFrame.returnPointer - 1);
            }

            if (logToConsole)
            {
                Debug.Log($"[VN] LoadState <- key={key} pointer={pointer} callStack={callStack.Count} target={pendingCallResumeFrame?.target ?? "-"} arg={pendingCallResumeFrame?.arg ?? "-"}");
            }

            return true;
        }

        private VNScript BuildTestScript()
        {
            var nodes = new List<VNNode>
    {
            new VNNode { id="t.001", type=VNNodeType.Say, speakerId="sys",
            text="(Test) Dialogue -> Drink -> Branch. Press SPACE to continue." },

        // ✅ Call 노드로 외부 Drink 패널을 연다.
            new VNNode { id="t.call", type=VNNodeType.Call, callTarget="Drink", callArg="order001" },

        // 결과 버튼을 누르면 runner.ReturnFromCall(...)이 호출되고
        // 그 직후 여기 Branch로 복귀한다.
            new VNNode {
                id="t.branch",
                type=VNNodeType.Branch,
                branches = new[]
                {
                // great는 success 포함 규칙이라 great>=2면 항상 route2로 가게 됨
                new VNNode.BranchRule{ expr="lastDrink==1", jumpLabel="route2" },
                new VNNode.BranchRule{ expr="lastDrink==3", jumpLabel="route3" },
                new VNNode.BranchRule{ expr="else",         jumpLabel="route1" },
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
