using UnityEngine;
using TMPro;
using System.Collections.Generic;
using PPP.BLUE.VN;

public class VNDebugConsole : MonoBehaviour
{
    [SerializeField] TMP_InputField commandInput;
    [SerializeField] TMP_Text logText;
    [SerializeField] TMP_Text hudText;
    [SerializeField] VNRunner runner;
    [SerializeField] private VNSaveManager saveManager;

    Dictionary<string, string> vars = new Dictionary<string, string>();

    List<string> history = new List<string>();
    List<string> timeline = new List<string>();
    List<string> dump = new List<string>();
    List<string> labels = new List<string>();
    HashSet<string> watchVars = new HashSet<string>();
   

    string breakpointNode = null;

    bool autoMode = false;
    int autoCount = 0;

    const int historyMax = 40;
    const int timelineMax = 60;

    void Start()
    {
        commandInput.onEndEdit.AddListener(OnSubmit);
        commandInput.ActivateInputField();

        Print("VN DEBUG CONSOLE READY");
        Print("type 'help'");
    }

    void Update()
    {
        UpdateHUD();

        if (autoMode && autoCount > 0)
        {
            runner?.Next();
            AddTimeline("auto-next");
            autoCount--;

            if (autoCount <= 0)
                autoMode = false;
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            runner?.Next();
            AddTimeline("hotkey next");
        }

        if (Input.GetKeyDown(KeyCode.J))
        {
            for (int i = 0; i < 5; i++)
                runner?.Next();

            AddTimeline("hotkey skip5");
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            Print("Timeline:");

            foreach (var t in timeline)
                Print(t);
        }
    }

    void OnSubmit(string text)
    {
        if (!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter))
            return;

        if (string.IsNullOrWhiteSpace(text))
            return;

        Print("> " + text);

        Execute(text);

        commandInput.text = "";
        commandInput.ActivateInputField();
    }

    void Execute(string input)
    {
        string[] args = input.Split(' ');
        string cmd = args[0].ToLower();

        switch (cmd)
        {
            case "next":

                runner?.Next();
                AddHistory("next()");
                AddTimeline("next");

                break;

            case "jump":

                if (args.Length > 1)
                {
                    runner?.DebugJump(args[1]);

                    AddHistory("jump " + args[1]);
                    AddTimeline("jump -> " + args[1]);
                }
                else
                    Print("Usage: jump NODE");

                break;

            case "skip":

                int count = 10;

                if (args.Length > 1)
                    int.TryParse(args[1], out count);

                for (int i = 0; i < count; i++)
                    runner?.Next();

                AddHistory("skip " + count);
                AddTimeline("skip x" + count);

                break;

            case "rand":

                int r = Random.Range(0, 3);

                for (int i = 0; i < r + 1; i++)
                    runner?.Next();

                AddHistory("rand " + r);
                AddTimeline("rand -> branch " + r);

                break;

            case "set":

                if (args.Length > 2)
                {
                    vars[args[1]] = args[2];
                    Print("Var set: " + args[1] + " = " + args[2]);
                }
                else
                    Print("Usage: set NAME VALUE");

                break;

            case "vars":

                Print("Variables:");

                foreach (var v in vars)
                    Print(v.Key + " = " + v.Value);

                break;

            case "history":

                Print("History:");

                foreach (var h in history)
                    Print(h);

                break;

            case "timeline":

                Print("Timeline:");

                foreach (var t in timeline)
                    Print(t);

                break;

            case "dump":

                Print("Script Dump:");

                foreach (var d in dump)
                    Print(d);

                break;

            case "node":

                if (timeline.Count > 0)
                    Print("Current Node: " + timeline[timeline.Count - 1]);
                else
                    Print("No node yet");

                break;

            case "break":

                if (args.Length > 1)
                {
                    breakpointNode = args[1];
                    Print("Breakpoint set: " + breakpointNode);
                }
                else
                    Print("Usage: break NODE");

                break;

            case "auto":

                if (args.Length > 1)
                {
                    int.TryParse(args[1], out autoCount);
                    autoMode = true;

                    Print("Auto next: " + autoCount);
                }

                break;

            case "labels":

                Print("Labels:");

                foreach (var l in labels)
                    Print(l);

                break;

            case "clear":

                logText.text = "";

                break;

            case "reset":

                history.Clear();
                timeline.Clear();
                dump.Clear();
                vars.Clear();

                Print("Debug state reset");

                break;

            case "watch":

                if (args.Length > 1)
                {
                    watchVars.Add(args[1]);
                    Print("Watching: " + args[1]);
                }

                break;



            case "save":

                if (saveManager != null)
                {
                    saveManager.SaveGame();
                    Print("Game Saved");
                }
                else
                {
                    Print("SaveManager missing");
                }

                break;


            case "load":

                if (saveManager != null)
                {
                    saveManager.LoadGame();
                    Print("Game Loaded");
                }
                else
                {
                    Print("SaveManager missing");
                }

                break;



            case "help":

                Print("Commands:");
                Print("next");
                Print("jump NODE");
                Print("skip [count]");
                Print("rand");
                Print("set NAME VALUE");
                Print("vars");
                Print("history");
                Print("timeline");
                Print("dump");
                Print("node");
                Print("break NODE");
                Print("auto N");
                Print("labels");
                Print("clear");
                Print("reset");
                Print("save");
                Print("load");

                break;

            default:

                Print("Unknown command: " + cmd);

                break;
        }
    }

    void UpdateHUD()
    {
        if (hudText == null) return;

        string hud = "DEBUG HUD\n\n";

        hud += "Vars\n";

        foreach (var v in vars)
            hud += v.Key + ": " + v.Value + "\n";

        hud += "\nWatch\n";

        foreach (var w in watchVars)
        {
            if (vars.ContainsKey(w))
                hud += w + ": " + vars[w] + "\n";
        }

        hud += "\nTimeline\n";

        int start = Mathf.Max(0, timeline.Count - 6);

        for (int i = start; i < timeline.Count; i++)
            hud += timeline[i] + "\n";

        hudText.text = hud;
    }

    void AddHistory(string msg)
    {
        history.Add(msg);

        if (history.Count > historyMax)
            history.RemoveAt(0);
    }

    void AddTimeline(string msg)
    {
        timeline.Add(msg);
        dump.Add(msg);

        if (timeline.Count > timelineMax)
            timeline.RemoveAt(0);

        if (breakpointNode != null && msg.Contains(breakpointNode))
        {
            Print("BREAKPOINT HIT: " + breakpointNode);
            autoMode = false;
        }
    }

    void Print(string msg)
    {
        logText.text += "\n" + msg;
    }
}