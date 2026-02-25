using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PPP.BLUE.VN
{
    public sealed class VNChoicePanel : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private GameObject root;          // UI_ChoicePanel root object
        [SerializeField] private Transform ChoicesList;
        [SerializeField] private Button choiceButtonPrefab; // ChoiceButton prefab

        [SerializeField] private VNRunner runner;
        [SerializeField] private VNPolicyController policy;

        private readonly List<Button> spawned = new();


        private static string GetPath(Transform t)
        {
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }

        private void Awake()
        {
            if (root == null) root = gameObject;

            if (runner == null) runner = GetComponentInParent<VNRunner>(true);
            if (policy == null) policy = GetComponentInParent<VNPolicyController>(true);

            root.SetActive(false);

            Debug.Log($"[VNChoicePanel] bind runner={(runner ? runner.name : "NULL")} policy={(policy ? policy.name : "NULL")}");
        }

        public void Open(VNNode.ChoiceOption[] choices)
        {
            Debug.Log($"[VNChoicePanel] Open called on instanceID={GetInstanceID()} choices={choices?.Length ?? -1}");

            if (root == null || ChoicesList == null || choiceButtonPrefab == null || runner == null || choices == null)
            {
                Debug.LogError("[VNChoicePanel] Missing refs. Check inspector wiring.");
                return;
            }

            Debug.Log($"[VNChoicePanel] root before activeSelf={root.activeSelf}, activeInHierarchy={root.activeInHierarchy}");
            ClearButtons();


            // Auto pause + modal lock while choice UI is open.
            runner?.ForceAutoOff("ChoicePanel Open");
            policy?.PushModal("ChoicePanel");

            root.SetActive(true);

            for (int i = 0; i < choices.Length; i++)
            {
                var choice = choices[i];
                if (choice == null) continue;

                // Create only entries with visible text.
                if (string.IsNullOrEmpty(choice.choiceText)) continue;


                var btn = Instantiate(choiceButtonPrefab, ChoicesList, worldPositionStays: false);
                spawned.Add(btn);


                // Set button text (TMP first).
                var tmp = btn.GetComponentInChildren<TMP_Text>(true);
                if (tmp != null) tmp.text = choice.choiceText;

                string jump = choice.jumpLabel; // capture for closure
                btn.onClick.AddListener(() =>
                {
                    Close();
                    runner.ForceAutoOff("Choice Selected"); // ✅ 선택은 유저 개입 → Auto OFF
                    runner.Choose(jump);
                });

            }

            // No valid buttons: close and continue safely.
            if (spawned.Count == 0)
            {
                Debug.LogWarning("[VNChoicePanel] No choices to show. Closing and continuing.");
                Close();
                runner.Next();
            }

            // Clear focus to avoid accidental keyboard click.
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        }

        public void Close()
        {
            ClearButtons();
            if (root != null) root.SetActive(false);

            policy?.PopModal("ChoicePanel");
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        }

        private void ClearButtons()
        {
            for (int i = 0; i < spawned.Count; i++)
            {
                if (spawned[i] != null) Destroy(spawned[i].gameObject);
            }
            spawned.Clear();
        }
    }
}
