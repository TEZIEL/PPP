using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using PPP.BLUE.VN;
using System.Text.RegularExpressions;
using UnityEngine.UI;

namespace PPP.BLUE.VN
{
    public sealed class VNRunner : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool logToConsole = false;
        [SerializeField] private VNScript testScript;
        [SerializeField] private bool loadFromJsonOnStart;
        [SerializeField] private string startDayId = "day01";
        [SerializeField] private VNOSBridge bridge;
        public bool SaveAllowed { get; private set; }
        [SerializeField] private float typeSpeed = 30f;

        [SerializeField] private VNRunner runner;
        private VNState state;
        private Coroutine autoCo;
        [SerializeField] private VNPolicyController policy;
        private VNDialogueView dialogueView;
        // --- Auto suspend/resume by modal/drink ---
        private bool lastBlocked;                 // 지난 프레임에 입력/오토가 막혀있었나?
        private bool autoSuspendedByBlock;        // 막힘 때문에 Auto를 멈췄었나?
        private bool lastMinimized;
        private int waitPointer = -1;   // 화면에 보여주고 '기다리는' 노드 인덱스
        private bool isWaiting;         // 지금 입력/선택 대기 상태인지
        public bool IsWaiting => isWaiting;
        public bool IsHoldSkipInputActive => holdSkipInputActive;

        private List<VNNode> nodes = new List<VNNode>();
        public bool IsSkipMode => skipMode;

        private const string SAVE_KEY = "vn.state";
        private const string VN_STATE_KEY = "vn.state";
        private const string VN_STATE_KEY_DBG = "vn.state.dbg";
        [Header("Timing")]
        [SerializeField, Min(0.05f)] private float autoPlayDelaySeconds = 0.35f;
        private int lastShownPointer = -1;
        private int lastStopIndex = 0; // 마지막으로 '멈춘' 노드(Say/Choice)의 인덱스
        private Dictionary<string, int> flags = new Dictionary<string, int>();
        private Dictionary<string, int> labels = new Dictionary<string, int>();

        public System.Action<string> OnEnterDrink;
        public bool skipMode = false;

       

        public int CallStackCount
        {
            get { return callStack != null ? callStack.Count : 0; }
        }

        // Legacy compatibility fields: kept to prevent compile breaks on partial merges
        // that still reference old hold-skip variables.
        [SerializeField] private float holdSkipStepInterval = 0.1f;
        [SerializeField, Min(1)] private int skipBurstPerFrame = 20;
        private float nextHoldSkipAllowedTime;
        private bool wasHoldSkipHeld;
        private bool uiSkipHeld;
        private bool holdSkipInputActive;

        private string lastDrinkResult = "";

        [Header("External Call")]
        [SerializeField] private string[] allowedExternalCallTargets = { "Drink" };
        [Header("Window Focus Linked Images (VN App Only)")]
        [SerializeField] private bool syncFocusLinkedImages = true;

        private readonly HashSet<string> externalCallTargetSet = new(StringComparer.OrdinalIgnoreCase);
        [SerializeField] private FocusLinkedImage[] focusLinkedImages = Array.Empty<FocusLinkedImage>();
        private bool? lastWindowFocusedVisualState;


        private struct InlineCommand
        {
            public string name;
            public string arg;
            public int index;
        }

        [Serializable]
        private struct FocusLinkedImage
        {
            public Image target;
            public Sprite activeSprite;
            public Sprite inactiveSprite;
        }

        [System.Serializable]
        public class VNCallFrame
        {
            public int returnPointer;
            public string target;
            public string arg;

            
        }

        [System.Serializable]
        public class VNRuntimeState
        {
            public int pointer;
            public List<int> callStack = new List<int>();
        }

        public void DebugJump(string label)
        {
            DoJump(label);
        }

        public void SetFlag(string key, int value)
        {
            flags[key] = value;
        }

        public void InjectBridge(VNOSBridge b)
        {
            bridge = b;
            lastWindowFocusedVisualState = null;
        }

        private void Awake()
        {
            TryResolveBridge(silent: true);

            BindPolicy("Awake");
            if (dialogueView == null) dialogueView = GetComponentInChildren<VNDialogueView>(true);
            skipMode = false; // 인스펙터 값과 무관하게 런타임 기본 OFF
            

            // ✅ 1프레임 뒤 재시도 (WindowManager가 AttachContent 후 wiring 끝난 뒤)
            StartCoroutine(CoBindPolicyNextFrame());

            RebuildExternalCallTargetSet();
        }


        public void MarkSaveBlocked() => SaveAllowed = false;
        public void MarkSaveAllowed(bool allowed, string reason)
        {
            bool gate = policy != null && policy.CanSaveDialogueState();
            bool callActive = callStack.Count > 0;
            SaveAllowed = allowed && gate && !callActive;

            VNLog($"[VN] SaveAllowed {(SaveAllowed ? "TRUE" : "FALSE")} ({reason})");

            // 자동 저장 제거: SaveAllowed 상태 계산/AutoTimer 제어만 유지
            if (SaveAllowed && !skipMode)
                TryStartAutoTimer();
            else
                StopAutoTimer();
        }

        // ✅ 기존 호출 유지용(대부분 DialogueView가 runner.MarkSaveAllowed()로 호출하니까)
        public void MarkSaveAllowed()
        {
            MarkSaveAllowed(true, "Typing End");
        }

        private void RefreshSaveAllowedFromPolicy(string reason)
        {
            if (policy == null) return;

            bool gate = policy.CanSaveDialogueState();
            bool callActive = callStack.Count > 0;
            bool nextAllowed = gate && !callActive;

            if (SaveAllowed == nextAllowed) return;

            SaveAllowed = nextAllowed;

            VNLog($"[VN] SaveAllowed {(SaveAllowed ? "TRUE" : "FALSE")} ({reason})");

            if (SaveAllowed)
            {
                if (settings.auto)
                    TryStartAutoTimer();
            }
            else
            {
                StopAutoTimer();
            }
        }


        public event Action<string, string, string> OnSay; // speakerId, text, lineId
        public event Action OnEnd;

        public bool HasScript => script != null;
        public bool IsAutoPlayEnabled => settings.auto;
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
        private bool dispatchingRestoredCall;
        private bool isRestoringFromLoad = false;
        public bool IsDispatchingRestoredCall => dispatchingRestoredCall;
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
            VNLog($"[VN] Begin() id={GetInstanceID()} go={gameObject.name} started={started}");

            if (LoadState())
                GetComponentInChildren<VNDialogueView>(true)?.LockInputFrames(1);

            // ✅ 시작 시 Auto는 항상 OFF로 강제(저장 상태가 ON이어도 시작 직후 자동 진행 방지)
            if (settings.auto)
            {
                settings.auto = false;
                StopAutoTimer();
                CancelAuto();

                VNLog("[VN] Auto forced OFF (Begin)");
            }

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

        public event Action<string, string> OnCall; // callTarget, callArg

        private void Start()
        {
            TryResolveBridge(silent: false);

            if (bridge != null)               

            StartCoroutine(CoBindPolicyNextFrame());

            RebuildExternalCallTargetSet();

            if (isRestoringFromLoad)
            {
                Debug.Log("[VN] Skip Start initialization (Load Restore)");
                return;
            }

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

            SyncFocusLinkedImages();

            if (policy != null && !VNInputGate.CanRouteInput(policy))
                return;
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

                    VNLog("[VN] Auto forced OFF (Window minimized)");
                }
            }
            lastMinimized = nowMin;

            // ----------------------------
            // 1) Policy 기반 Auto 강제 OFF (modal/drink/minimized)
            // ----------------------------
            bool blocked = IsBlockingModalState(nowMin);
            bool wasBlocked = lastBlocked;

            // 막힘이 "켜지는 순간" -> Auto 강제 OFF
            if (blocked && !wasBlocked)
            {
                ForceAutoOff("Blocked by modal/drink/minimized");
            }

            // ✅ 막힘이 풀리는 순간 Save gate를 재평가해서 Auto 재시작 가능 상태를 복구
            if (!blocked && wasBlocked)
            {
                RefreshSaveAllowedFromPolicy("Unblocked");
            }

            lastBlocked = blocked;


            bool keyboardSkipHeld = Input.GetKey(KeyCode.F1);
            bool holdSkip = keyboardSkipHeld ^ uiSkipHeld; // 동시 입력은 무시 (둘 중 하나만 허용)
            holdSkipInputActive = holdSkip;

            if (holdSkip && !wasHoldSkipHeld)
            {
                ForceAutoOff("Hold Skip");
            }

            wasHoldSkipHeld = holdSkip;

            if (holdSkip && VNInputGate.CanUseSkipOrAuto(policy))
            {
                if (Time.time >= nextHoldSkipAllowedTime)
                {
                    SkipStep();
                    nextHoldSkipAllowedTime = Time.time + holdSkipStepInterval;
                }
            }

            if (Input.GetKeyDown(KeyCode.F2))
            {
                ToggleAutoFromInput("Hotkey F2");
            }
            // ----------------------------
            // 4) Safety: 조건 깨졌으면 코루틴 Auto 즉시 정지
            // ----------------------------
            if (autoCo != null && !CanAutoAdvance())
                StopAutoTimer();

            // (여기 아래에 네 기존 autoPending 방식 로직이 있으면 그대로 두면 됨)
        }

        private void SyncFocusLinkedImages()
        {
            if (!syncFocusLinkedImages || focusLinkedImages == null || focusLinkedImages.Length == 0)
                return;

            bool isFocused = false;
            if (bridge != null)
            {
                var state = bridge.GetWindowState();
                isFocused = state.IsFocused && !state.IsMinimized;
            }

            if (lastWindowFocusedVisualState.HasValue && lastWindowFocusedVisualState.Value == isFocused)
                return;

            lastWindowFocusedVisualState = isFocused;

            for (int i = 0; i < focusLinkedImages.Length; i++)
            {
                var entry = focusLinkedImages[i];
                if (entry.target == null)
                    continue;

                Sprite next = isFocused ? entry.activeSprite : entry.inactiveSprite;
                if (next != null)
                    entry.target.sprite = next;
            }
        }

        public void NotifyLineTypedEnd()
        {
            if (!settings.auto) return;

            TryStartAutoTimer();

            VNLog("[VN] AutoTimer Start (TypingEnd)");
        }


        public void CancelAuto()
        {
            StopAutoTimer();
        }

        private bool IsInteractionNodeForSkip(VNNode node)
        {
            if (node == null) return false;


           
            return false;
        }

        private string GetSkipStopReason(VNNode node)
        {
            if (node == null) return "Interaction Node";
            if (node.type == VNNodeType.Call)
            {
                var target = node.callTarget ?? string.Empty;
                if (target.IndexOf("drink", StringComparison.OrdinalIgnoreCase) >= 0)
                    return "Drink Call";

                return "Call Node";
            }

            if (node.type == VNNodeType.Choice || node.type == VNNodeType.Branch)
                return "Choice Node";

            return "Interaction Node";
        }

        private bool CanRunSkipStep()
        {
         
            if (!HasScript) return false;
            if (policy == null) return false;
            return VNInputGate.CanUseSkipOrAuto(policy);
        }

        private void ForceSkipOff(string reason)
        {
            if (!skipMode) return;

            skipMode = false;

            //VNLog($"[VN] SkipMode forced OFF ({reason})");
        }

        bool isAdvancing;

        public void Next()
        {
            if (isAdvancing)
                return;

            isAdvancing = true;

            try
            {
                NextInternal();
            }
            finally
            {
                isAdvancing = false;
            }
        }

        public void NextInternal()
        {


            if (!started)
            {
                VNLog("[VN] Next ignored (not started).");
                return;
            }

            // 🔴 External Call 중 VN 진행 차단
            if (callStack != null && callStack.Count > 0 && pointer == waitPointer)
            {
                VNLog("[VN] Next blocked (ExternalCall waiting)");
                return;
            }

            if (TryResumePendingCallAfterLoad()) return;

            isWaiting = false;
            waitPointer = -1;

            if (policy != null && policy.GetWindowState().IsMinimized)
            {
                VNLog("[VN] Next ignored (minimized).");
                return;
            }

            VNLog($"[VN] Next() id={GetInstanceID()} go={gameObject.name} pointer={pointer} started={started}");
            VNLog($"[VN] Next() pointer={pointer} nodes={script?.nodes?.Count ?? -1} started={started}");
            VNLog("[VN] SaveAllowed FALSE (Next)");
            MarkSaveBlocked();

            if (script == null || script.nodes == null)
            {
                Debug.LogError("[VNRunner] No script loaded.");
                return;
            }

            int safety = 0;

            while (true)
            {
                if (++safety > 1000)
                {
                    Debug.LogError("[VN] Safety break triggered. Possible infinite loop.");
                    Finish();
                    return;
                }

                int previousPointer = pointer;

                if (pointer < 0 || pointer >= script.nodes.Count)
                {
                    Finish();
                    return;
                }

                var node = script.nodes[pointer];
                VNLog($"[VN] node@{pointer} type={node?.type} label={node?.label}");

                if (node == null)
                {
                    pointer++;
                    if (pointer == previousPointer)
                    {
                        pointer++;
                    }

                    continue;
                }

                if (skipMode && IsInteractionNodeForSkip(node))
                {
                    lastStopIndex = pointer;
                    waitPointer = pointer;
                    isWaiting = true;
                    MarkSaveAllowed(true, "Skip Interaction Stop");
                    return;
                }

                switch (node.type)
                {
                    case VNNodeType.Say:
                        {
                            lastShownPointer = pointer;

                            EmitSay(node);
                            pointer++;

                            if (!skipMode)
                            {
                                waitPointer = pointer - 1;
                                isWaiting = true;
                                return;
                            }

                            return;
                        }

                    case VNNodeType.Choice:
                        {
                            Debug.LogWarning("[VN] Choice node encountered but Choice system is disabled.");
                            pointer++;
                            continue;
                        }

                    case VNNodeType.Branch:
                        {
                            ResolveBranchNode(node);
                            VNLog("[VN] Branch auto resolved -> pointer=" + pointer);
                            if (pointer == previousPointer)
                            {
                                pointer++;
                            }

                            continue;
                        }

                    case VNNodeType.Switch:
                        {
                            ResolveSwitchNode(node);
                            if (pointer == previousPointer)
                            {
                                pointer++;
                            }

                            continue;
                        }

                    case VNNodeType.Label:
                        {
                            VNLog($"[VN] Label: {node.label} (idx {pointer})");
                            pointer++;
                            if (pointer == previousPointer)
                            {
                                pointer++;
                            }

                            continue;
                        }

                    case VNNodeType.Jump:
                        {
                            if (!DoJump(node.label))
                            {
                                VNLog("[VN] Jump failed -> pointer++");
                                pointer++;
                            }

                            if (pointer == previousPointer)
                            {
                                pointer++;
                            }

                            continue;
                        }

                    case VNNodeType.Call:
                        {
                            string target = node.callTarget ?? string.Empty;
                            string arg = node.callArg ?? string.Empty;
                            Debug.Log($"[CALL DEBUG] target={target} arg={arg}");

                            if (!StartExternalCall(target, arg))
                            {
                                pointer++;
                                if (pointer == previousPointer)
                                {
                                    pointer++;
                                }

                                continue;
                            }

                            pointer++;

                            return;
                        }

                    case VNNodeType.Return:
                        {
                            if (callStack.Count == 0)
                            {
                                Debug.LogError("Return without callStack");
                                pointer++;
                                if (pointer == previousPointer)
                                {
                                    pointer++;
                                }

                                continue;
                            }

                            ReturnFromCall(node.callArg ?? string.Empty);
                            if (pointer == previousPointer)
                            {
                                pointer++;
                            }

                            continue;
                        }

                    case VNNodeType.End:
                        {
                            Finish();
                            return;
                        }

                    default:
                        {
                            Debug.LogWarning($"[VNRunner] Unknown node type: {node.type}");
                            pointer++;
                            if (pointer == previousPointer)
                            {
                                pointer++;
                            }

                            continue;
                        }
                }
            }
        }



        private void ResolveSwitchNode(VNNode node)
        {
            if (node == null)
            {
                pointer++;
                return;
            }

            string value = GetVar(node.switchVar, 0).ToString();

            if (node.switchCases != null && node.switchCases.TryGetValue(value, out var next) && !string.IsNullOrEmpty(next))
            {
                if (!DoJump(next))
                {
                    VNLog("[VN] Switch jump failed -> pointer++");
                    pointer++;
                }
                return;
            }

            if (!string.IsNullOrEmpty(node.switchDefault))
            {
                if (!DoJump(node.switchDefault))
                {
                    VNLog("[VN] Switch jump failed -> pointer++");
                    pointer++;
                }
                return;
            }

            pointer++;
        }

        private void ResolveBranchNode(VNNode node)
        {
            if (node == null)
            {
                pointer++;
                return;
            }

            int lastDrink = GetVar("lastDrink", 0);
            Debug.Log($"[BRANCH CHECK] lastDrink={lastDrink}, great={greatCount}");

            if (node.branches != null && node.branches.Length > 0)
            {
                for (int i = 0; i < node.branches.Length; i++)
                {
                    var rule = node.branches[i];
                    if (rule == null)
                        continue;

                    if (string.Equals(rule.expr, "else", StringComparison.OrdinalIgnoreCase))
                    {
                        VNLog("[VN_TEST] Branch resolved route=" + rule.jumpLabel);
                        DoJump(rule.jumpLabel);
                        return;
                    }

                    if (EvaluateExpr(rule.expr))
                    {
                        VNLog("[VN_TEST] Branch resolved route=" + rule.jumpLabel);
                        DoJump(rule.jumpLabel);
                        return;
                    }
                }

                pointer++;
                return;
            }

            VNLog("[VN_TEST] Branch resolved route=" + node.label);
            DoBranch(node.label);
        }

        private bool StartExternalCall(string target, string arg)
        {
            Debug.Log($"[CALL EXECUTE] inputRequestId={arg}");

            if (skipMode)
            {
                VNLog("[VN] Skip disabled due to ExternalCall");
                skipMode = false;
            }

            Debug.Log("[VN_TEST] ExternalCall target=" + target + " arg=" + arg);
            if (string.IsNullOrWhiteSpace(target) || !IsExternalCallTargetAllowed(target))
            {
                Debug.LogError($"[VN] Unknown external call target: '{target}'");
                return false;
            }

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
            return true;
        }

        private void RebuildExternalCallTargetSet()
        {
            externalCallTargetSet.Clear();
            if (allowedExternalCallTargets == null)
                return;

            for (int i = 0; i < allowedExternalCallTargets.Length; i++)
            {
                var target = allowedExternalCallTargets[i];
                if (!string.IsNullOrWhiteSpace(target))
                    externalCallTargetSet.Add(target.Trim());
            }

          
        }

        private bool IsExternalCallTargetAllowed(string target)
        {
            if (string.IsNullOrWhiteSpace(target))
                return false;

            if (externalCallTargetSet.Count == 0)
                RebuildExternalCallTargetSet();

            return externalCallTargetSet.Contains(target.Trim());
        }

        public void OnAdvance()
        {
            
        }


        private void VNLog(string msg)
        {
            if (!logToConsole) return;
            if (skipMode) return;
            if (!Application.isEditor)
                return;

            Debug.Log(msg);
        }


        public void SetScript(VNScript s)
        {
            script = s;
            pointer = 0;
            started = false;
        }

        private void EmitSay(VNNode node)
        {
            string text = node?.text ?? string.Empty;
            var commands = ParseInlineCommands(text);

            string cleanText = RemoveInlineCommands(text);

            VNLog($"[VN] {node.speakerId}: {cleanText} (id={node.id})");

            OnSay?.Invoke(node.speakerId, cleanText, node.id);

            foreach (var cmd in commands)
                ExecuteInline(cmd);
        }

        private IEnumerator RunInlineCommands(List<InlineCommand> cmds)
        {
            foreach (var cmd in cmds)
            {
                yield return ExecuteInlineCommand(cmd);
            }
        }

        public void MarkSeen(string lineId)
        {
            if (string.IsNullOrEmpty(lineId)) return;

            if (seenLineIds.Add(lineId))
            {
                VNLog($"[VN] Seen + {lineId} (total={seenLineIds.Count})");
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

        private void DoBranch(string condition)
        {
            string[] parts = condition.Split(':');

            if (parts.Length < 2)
            {
                pointer++;
                return;
            }

            string expected = parts[0];
            string target = parts[1];

            if (lastDrinkResult == expected)
            {
                DoJump(target);
            }
            else
            {
                pointer++;
            }
        }



        private bool DoJump(string label)
        {
            if (string.IsNullOrEmpty(label))
            {
                Debug.LogError($"[VN] Jump label is empty. nodeId={script?.nodes?[pointer]?.id} idx={pointer}");
                return false;
            }

            if (!script.TryGetLabelIndex(label, out var idx))
            {
                // ✅ 1회 자동 복구: 라벨 인덱스 재빌드 후 재시도
                script.RebuildLabelIndex();

                if (!script.TryGetLabelIndex(label, out idx))
                {
                    Debug.LogError($"[VN] Label not found: '{label}' nodeId={script?.nodes?[pointer]?.id} curIdx={pointer} labels={script.LabelCount} dump={script.DumpLabels()}");
                    pointer++;
                    return false;
                }
            }

            VNLog($"[VN] Jump -> {label} (idx {idx}) from nodeId={script?.nodes?[pointer]?.id} curIdx={pointer}");

            pointer = idx;
            // 🔴 Jump 루프 감지 가드 (로그만)
            if (pointer == idx)
            {
                Debug.LogWarning($"[VN] Jump loop detected. label={label} idx={idx}");
            }
            return true;
        }

        private void Finish()
        {
            Debug.Log("[VN_TEST] VN Finished script=" + (script?.ScriptId ?? string.Empty));
            VNLog("[VN] End");

            OnEnd?.Invoke();
            started = false;
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

            st.callStack = new List<VNRunner.VNCallFrame>(callStack.Count);
            foreach (var frame in callStack)
            {
                if (frame == null) continue;

                st.callStack.Add(new VNRunner.VNCallFrame
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
                speed = settings.speed
            };

            st.vars = new List<VNIntVar>();

            foreach (var kv in vars)
            {
                st.vars.Add(new VNIntVar
                {
                    key = kv.Key,
                    value = kv.Value
                });
            }

            st.greatCount = greatCount;
            st.successCount = successCount;
            st.failCount = failCount;
            st.lastResult = lastResult;

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
        }

        public void DebugForceSave(string reason)
        {
            VNLog($"[VNDBG] ForceSave (debug slot) reason={reason}");
            SaveStateToKey(VN_STATE_KEY_DBG, ignoreSaveAllowed: true);
        }

        public bool TrySaveNow(string reason = "manual")
        {
            bool canPersist = CanPersistState();
            if (!canPersist && logToConsole)
                Debug.Log($"[VN] Save requested ({reason}) but blocked; delegating to SaveStateToKey for detailed reason.");

            SaveStateToKey(VN_STATE_KEY, ignoreSaveAllowed: false);
            return canPersist;
        }

        public bool TryLoadNow(string reason = "manual")
        {
            if (callStack.Count > 0)
            {
                VNLog("[VN] Load blocked (external call active)");
                return false;
            }


            bool ok = LoadStateFromKey(VN_STATE_KEY);
            VNLog($"[VN] Load {(ok ? "ok" : "miss")} ({reason})");
            return ok;
        }



        private bool LoadState()
        {
            return LoadStateFromKey(VN_STATE_KEY);
        }

        public void DebugForceLoad(string reason)
        {
            VNLog($"[VNDBG] ForceLoad (debug slot) reason={reason}");

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

            VNLog($"[VN] TestSpeed25 -> speed={settings.speed}");
        }

        public void SetSpeed(float v)
        {
            // 안전장치
            if (settings == null)
                settings = VNSettings.Default();

            settings.speed = v;
            VNLog($"[VN] SetSpeed -> {settings.speed}");
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

            VNLog($"[VN] ResumePendingCall -> OnCall {frame.target} arg={frame.arg}");

            if (string.Equals(frame.target, "Drink", StringComparison.OrdinalIgnoreCase))
                VNLog($"[VN] Drink restore requested target={frame.target} arg={frame.arg}");

            dispatchingRestoredCall = true;
            OnCall?.Invoke(frame.target ?? string.Empty, frame.arg ?? string.Empty);
            dispatchingRestoredCall = false;

            if (string.Equals(frame.target, "Drink", StringComparison.OrdinalIgnoreCase))
                VNLog($"[VN] Drink restore opened target={frame.target} arg={frame.arg}");

            return true;
        }


        public void ReturnFromCall(string result)
        {
            Debug.Log("[VN] ReturnFromCall result=" + result);

            if (callStack == null)
            {
                Debug.LogError("[VN] callStack null");
                return;
            }

            if (callStack.Count == 0)
            {
                Debug.LogError("[VN] CallStack desync detected");

                // 🔴 waiting 상태 복구
                isWaiting = false;
                waitPointer = -1;

                return;
            }

            var frame = callStack.Pop();
            ApplyCallResult(result);

            pointer = frame.returnPointer;
            isWaiting = false;
            waitPointer = -1;

            // 🔴 ExternalCall 복귀 후 자동 진행
            Next();

            Debug.Log("[VN_TEST] ReturnFromCall result=" + result + " pointer=" + pointer);

            VNLog($"[VN] ReturnFromCall target={frame.target} arg={frame.arg} result={result} -> pointer={pointer}");
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

        public void ApplyDrinkResult(string result)
        {
            Debug.Log($"[BEFORE APPLY] great={greatCount}");
            lastResult = result;

            if (result == "great")
            {
                greatCount++;
                successCount++;
            }
            else if (result == "success")
            {
                successCount++;
            }
            else
            {
                failCount++;
            }

            // 🔥 추가 (핵심)
            SetVar("greatCount", greatCount);
            SetVar("successCount", successCount);
            SetVar("failCount", failCount);

            VNLog($"[VN] DrinkResult = {result} (great={greatCount}, success={successCount}, fail={failCount})");
        }


        // AutoNext가 "절대" 나가면 안 되는 조건들을 한 군데로 묶는다.
        private bool CanAutoAdvance()
        {
            if (!settings.auto) return false;
            if (!started) return false;
            if (policy == null) return false;

            return VNInputGate.CanAutoAdvanceInBackground(policy) && SaveAllowed;
        }

        private bool CanToggleAuto()
        {
            if (policy == null) return false;
            return VNInputGate.CanUseSkipOrAuto(policy);
        }

        private void ToggleAutoFromInput(string source)
        {
            if (!CanToggleAuto())
            {
                if (logToConsole) VNLog("[VN] Auto toggle ignored (blocked).");
                return;
            }

            bool turnOn = !settings.auto;
            settings.auto = turnOn;
            autoSuspendedByBlock = false;

            if (turnOn)
            {
                // ✅ 모달/팝업 해제 직후 SaveAllowed가 stale FALSE로 남은 경우 복구
                RefreshSaveAllowedFromPolicy("Auto Toggle On");

                skipMode = false;
                StopAutoTimer();
                TryStartAutoTimer();
            }
            else
            {
                StopAutoTimer();
                CancelAuto();
            }

            VNLog($"[VN] Auto={(settings.auto ? "On" : "Off")} ({source})");
        }

        private bool IsBlockingModalState(bool nowMin)
        {
            if (policy == null) return nowMin;
            if (nowMin) return true;
            return policy.IsBlockingModalState();
        }

        public void StopAutoExternal(string reason)
        {
            StopAutoTimer();
            if (logToConsole) VNLog($"[VN] AutoTimer Stop ({reason})");
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

                VNLog("[VN] AutoTimer Stopped");
            }
        }

        private IEnumerator CoAutoNext()
        {
            if (logToConsole) VNLog($"[VN] AutoTimer Start ({autoPlayDelaySeconds:0.00}s)");

            yield return new WaitForSeconds(autoPlayDelaySeconds);

            autoCo = null;

            // ✅ 발사 직전 재검사 (SaveAllowed FALSE, 모달 ON, 드링크 ON이면 여기서 차단)
            if (!CanAutoAdvance()) yield break;

            if (logToConsole) VNLog("[VN] AutoNext");
            Next();
        }

        public void ForceAutoOff(string reason)
        {
            if (!settings.auto && autoCo == null) return; // 이미 꺼져있으면 스킵(선택)

            settings.auto = false;          // 유저 상태 자체 OFF
            autoSuspendedByBlock = false;   // unblock 자동재개 플래그 제거

            StopAutoTimer();
            CancelAuto();

            if (logToConsole) VNLog($"[VN] Auto forced OFF ({reason})");
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
                if (logToConsole) VNLog($"[VN] policy bind=OK({policy.name}) from={from}");
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

            VNLog($"[VN] policy bind={(policy != null ? $"OK({policy.name})" : "NULL")} from={from}");
        }


        private void SaveStateToKey(string key, bool ignoreSaveAllowed)
        {
            if (!ignoreSaveAllowed && !CanPersistState())
            {
                VNLog($"[VN] SaveState skipped key={key} SaveAllowed={SaveAllowed} policyOk={(policy != null && policy.CanSaveDialogueState())}");
                return;
            }
            if (script == null)
            {
                Debug.LogError("[VN] Save blocked: script null");
                return;
            }

            if (pointer < 0)
            {
                Debug.LogError("[VN] Save blocked: pointer invalid");
                return;
            }

            int lastDrink = GetVar("lastDrink", 0);
            Debug.Log(
            $"[SAVE CHECK] pointer={pointer}, lastDrink={lastDrink}, " +
            $"great={greatCount}, success={successCount}, fail={failCount}, " +
            $"varsCount={(vars != null ? vars.Count : 0)}"
            );

            var st = BuildState();

            if (logToConsole)
            {
                var safe = st.settings ?? VNSettings.Default();
                var top = callStack.Count > 0 ? callStack.Peek() : null;
                VNLog($"[VN] SaveState seen={st.seen?.Count ?? 0} auto={safe.auto} speed={safe.speed} pointer={st.pointer} callStack={callStack.Count} top={top?.target ?? "-"}/{top?.arg ?? "-"}");
            }

            VNFileSaveSystem.Save(key, st);

            VNLog($"[VN] SaveState -> key={key} scriptId={st.scriptId} pointer={st.pointer} vars={st.vars?.Count ?? 0}");
        }

        private bool CanPersistState()
        {
            if (!SaveAllowed) return false;
            if (policy == null) return false;
            if (policy.IsDrinkModeActive()) return false;
            if (callStack.Count > 0) return false;
            return policy.CanSaveDialogueState();
        }

        private bool LoadStateFromKey(string key)
        {
            if (script == null)
                return false;

            var st = VNFileSaveSystem.Load(key);
            if (st == null)
                return false;

            if (!string.Equals(st.scriptId, script.ScriptId, StringComparison.Ordinal))
                return false;

            isRestoringFromLoad = true;
            try
            {
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
                lastResult = string.IsNullOrEmpty(st.lastResult) ? "success" : st.lastResult;

                Debug.Log($"[LOAD STATE] great={greatCount}, success={successCount}, fail={failCount}");

                callStack.Clear();
                pendingCallResumeFrame = null;

                if (st.callStack != null && st.callStack.Count > 0)
                {
                    for (int i = st.callStack.Count - 1; i >= 0; i--)
                    {
                        var frame = st.callStack[i];
                        if (frame == null) continue;

                        callStack.Push(new VNRunner.VNCallFrame
                        {
                            returnPointer = frame.returnPointer,
                            target = frame.target,
                            arg = frame.arg
                        });
                    }

                    pendingCallResumeFrame = callStack.Peek();
                    isWaiting = true;
                    waitPointer = Mathf.Max(0, pendingCallResumeFrame.returnPointer - 1);

                    VNLog("[VN] Restoring pending external call");
                }

                if (logToConsole)
                {
                    VNLog($"[VN] LoadState <- key={key} pointer={pointer} callStack={callStack.Count} target={pendingCallResumeFrame?.target ?? "-"} arg={pendingCallResumeFrame?.arg ?? "-"}");
                }

                int lastDrink = GetVar("lastDrink", 0);
                Debug.Log(
                $"[LOAD CHECK] pointer={pointer}, lastDrink={lastDrink}, " +
                $"great={greatCount}, success={successCount}, fail={failCount}, " +
                $"varsCount={(vars != null ? vars.Count : 0)}"
                );

                EmitCurrent();
                return true;
            }
            finally
            {
                isRestoringFromLoad = false;
            }

        }

        public VNRuntimeState CaptureState()
        {
            VNRuntimeState state = new VNRuntimeState();

            state.pointer = pointer;

            foreach (var f in callStack)
                state.callStack.Add(f.returnPointer);

            return state;
        }

        public void RestoreState(VNRuntimeState state)
        {
            pointer = state.pointer;

            callStack.Clear();

            foreach (var p in state.callStack)
            {
                callStack.Push(new VNRunner.VNCallFrame
                {
                    returnPointer = p
                });
            }

            VNLog($"[VN] Restore pointer={pointer} stack={callStack.Count}");
        }


        private void PushCall(string label, string arg = null)
        {
            var frame = new VNCallFrame
            {
                returnPointer = pointer + 1,
                target = label,
                arg = null
            };

            callStack.Push(frame);

            VNLog($"[VN] Call push -> {label} return={frame.returnPointer}");

            pointer = labels[label];
        }

        private void PopReturn()
        {
            if (callStack.Count == 0)
            {
                Debug.LogError("[VN] Return but stack empty");
                return;
            }

            var frame = callStack.Pop();

            pointer = frame.returnPointer;

            VNLog($"[VN] Return -> {pointer}");
        }

        private void ExecuteInline(InlineCommand cmd)
        {
            switch (cmd.name)
            {
                case "wait":

                    float t = 1f;

                    if (cmd.arg != null)
                        float.TryParse(cmd.arg, out t);

                    StartCoroutine(WaitCommand(t));
                    break;

                case "speed":

                    if (cmd.arg != null)
                        float.TryParse(cmd.arg, out typeSpeed);

                    break;

                case "shake":

                    VNLog("[VN] shake");

                    break;

                case "sfx":

                    VNLog("[VN] sfx " + cmd.arg);

                    break;
            }
        }

        void ExecuteInlineCommand(string name, string arg)
        {
            switch (name)
            {
                case "wait":

                    VNLog("[VN CMD] wait");
                    StartCoroutine(WaitRoutine(0.5f));
                    break;

                case "speed":

                    if (int.TryParse(arg, out int v))
                    {
                        typeSpeed = v;
                        VNLog("[VN CMD] speed=" + v);
                    }
                    break;

                case "sfx":

                    VNLog("[VN CMD] sfx=" + arg);
                    // TODO: AudioManager.Play(arg);
                    break;

                case "shake":

                    VNLog("[VN CMD] shake");
                    // TODO: CameraShake.Trigger();
                    break;
            }
        }

        System.Collections.IEnumerator WaitRoutine(float t)
        {
            yield return new WaitForSeconds(t);
        }


        private void DoCall(string label)
        {
            if (label.StartsWith("drink"))
            {
                EnterDrinkMode(label);
                return;
            }

            if (!labels.TryGetValue(label, out int target))
            {
                Debug.LogError($"[VN] Call label not found: {label}");
                return;
            }

            VNCallFrame frame = new VNCallFrame
            {
                returnPointer = pointer + 1
            };

            callStack.Push(frame);

            VNLog($"[VN] Call -> {label} return={frame.returnPointer}");

            pointer = target;
        }


        public void ReturnFromDrink(string result)
        {
            VNLog("[VN] Drink Result: " + result);

            lastResult = result;

            DoReturn();
        }



        private void DoReturn()
        {
            if (callStack.Count == 0)
            {
                Debug.LogError("[VN] Return called but stack empty");
                return;
            }

            var frame = callStack.Pop();

            pointer = frame.returnPointer;

            VNLog($"[VN] Return -> {pointer}");
        }


        private List<InlineCommand> ParseInlineCommands(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new List<InlineCommand>();

            List<InlineCommand> cmds = new List<InlineCommand>();

            int i = 0;

            while (i < text.Length)
            {
                if (text[i] == '[')
                {
                    int end = text.IndexOf(']', i);
                    if (end > i)
                    {
                        string content = text.Substring(i + 1, end - i - 1);

                        string[] parts = content.Split(' ');

                        InlineCommand cmd = new InlineCommand
                        {
                            name = parts[0],
                            arg = parts.Length > 1 ? parts[1] : null,
                            index = i
                        };

                        cmds.Add(cmd);

                        i = end + 1;
                        continue;
                    }
                }

                i++;
            }

            return cmds;
        }

        private IEnumerator ExecuteInlineCommand(InlineCommand cmd)
        {
            switch (cmd.name)
            {
                case "wait":

                    if (float.TryParse(cmd.arg, out float t))
                        yield return new WaitForSeconds(t);

                    break;

                case "sfx":

                    VNLog("[VN] SFX " + cmd.arg);

                    break;

                case "speed":

                    if (float.TryParse(cmd.arg, out float sp))
                        typeSpeed = sp;

                    break;

                case "shake":

                    VNLog("[VN] Shake");

                    break;
            }
        }
        public VNState ExportState()
        {
            VNState state = new VNState();

            state.pointer = pointer;

            // vars
            state.vars.Clear();
            foreach (var v in vars)
            {
                state.vars.Add(new VNIntVar
                {
                    key = v.Key,
                    value = v.Value
                });
            }

            // callstack
            state.callStack.Clear();

            foreach (var f in callStack)
            {
                state.callStack.Add(new VNRunner.VNCallFrame
                {
                    returnPointer = f.returnPointer,
                    target = f.target,
                    arg = f.arg
                });
            }

            return state;
        }


        public void ImportState(VNState state)
        {
            if (state == null)
                return;

            pointer = state.pointer;

            vars.Clear();

            foreach (var v in state.vars)
            {
                vars[v.key] = v.value;
            }

            callStack.Clear();

            foreach (var f in state.callStack)
            {
                callStack.Push(new VNRunner.VNCallFrame
                {
                    returnPointer = f.returnPointer,
                    target = f.target,
                    arg = f.arg
                });
            }
        }


       



        private void EnterDrinkMode(string label)
        {
            VNLog("[VN] Enter Drink Mode: " + label);

            OnEnterDrink?.Invoke(label);
        }

        private void ExecuteInlineCommand(string cmd)
        {
            string[] parts = cmd.Split(' ');

            string name = parts[0];

            switch (name)
            {
                case "wait":
                    {
                        float t = 1f;

                        if (parts.Length > 1)
                            float.TryParse(parts[1], out t);

                        StartCoroutine(WaitCommand(t));
                        break;
                    }

                case "speed":
                    {
                        float s = 1f;

                        if (parts.Length > 1)
                            float.TryParse(parts[1], out s);

                        typeSpeed = s;
                        break;
                    }

                case "shake":
                    {
                        VNLog("[VN] shake command");
                        break;
                    }

                case "sfx":
                    {
                        if (parts.Length > 1)
                            VNLog("[VN] play sfx: " + parts[1]);
                        break;
                    }
            }
        }

        private string RemoveInlineCommands(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            return Regex.Replace(text, @"\[[^\]]+\]", "");
        }



        private IEnumerator WaitCommand(float t)
        {
            yield return new WaitForSeconds(t);
        }

        public void OnDrinkGreat()
        {
            runner.ReturnFromCall("great");
            
        }

        public void OnDrinkSuccess()
        {
            runner.ReturnFromCall("success");
            
        }

        public void OnDrinkFail()
        {
            runner.ReturnFromCall("fail");
            
        }


        public void OnSkipButton()
        {
            runner.ToggleSkip();
        }

        public void OnAutoButton()
        {
            runner.ToggleAutoFromInput("UI Button");
        }

        public void ToggleSkip(string source = "UI Button")
        {
            if (!HasScript || policy == null || !VNInputGate.CanUseSkipOrAuto(policy))
            {
                VNLog($"[VN] SkipMode toggle ignored (blocked) source={source}");
                return;
            }

            ForceAutoOff("Skip Step (UI)");
            SkipStep();

            VNLog($"[VN] SkipStep triggered source={source}");
        }

        public void RequestSkipStep(string source = "UI Button")
        {
            ToggleSkip(source);
        }

        public void SetUiSkipHeld(bool held, string source = "UI Hold Skip")
        {
            if (uiSkipHeld == held)
                return;

            uiSkipHeld = held;
            if (held)
            {
                ForceAutoOff(source);
            }
        }

        public void ToggleAuto(string source = "UI Button")
        {
            ToggleAutoFromInput(source);
        }

        public void SetAutoPlay(bool value, string source = "UI Button")
        {
            if (settings.auto == value)
                return;

            ToggleAutoFromInput(source);
        }

        private void SkipStep()
        {
            if (!CanRunSkipStep())
                return;

            if (dialogueView == null)
                dialogueView = GetComponentInChildren<VNDialogueView>(true);

            skipMode = true;

            // 타이핑 중이면 문장 완성
            if (dialogueView?.TryCompleteCurrentLineForSkip() == true)
            {
                skipMode = false;
                return;
            }

            if (script != null && script.nodes != null &&
                pointer >= 0 && pointer < script.nodes.Count)
            {
                var nextNode = script.nodes[pointer];

                if (IsInteractionNodeForSkip(nextNode))
                {
                    ForceSkipOff(GetSkipStopReason(nextNode));
                    skipMode = false;
                    return;
                }
            }

            try
            {
                Next();
            }
            finally
            {
                skipMode = false;
            }
        }


        private VNScript BuildTestScript()
        {
            var nodes = new List<VNNode>
            {
                new VNNode { id="t.001", type=VNNodeType.Say, speakerId="sys", text="(Test) Dialogue -> Drink -> Branch. Press SPACE to continue." },

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
                new VNNode { id="t.r1s", type=VNNodeType.Say, speakerId="sys", text="Route 1: Normal result (not enough Great, not enough Fail)." },
                new VNNode { id="t.end1", type=VNNodeType.End },

                new VNNode { id="t.r2", type=VNNodeType.Label, label="route2" },
                new VNNode { id="t.r2s", type=VNNodeType.Say, speakerId="sys", text="Route 2: Great >= 2 (Excellent performance)." },
                new VNNode { id="t.end2", type=VNNodeType.End },

                new VNNode { id="t.r3", type=VNNodeType.Label, label="route3" },
                new VNNode { id="t.r3s", type=VNNodeType.Say, speakerId="sys", text="Route 3: Fail >= 2 (Too many mistakes)." },
                new VNNode { id="t.end3", type=VNNodeType.End },
            };

            return new VNScript("test", nodes);
        }
    }

}
