using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PPP.BLUE.VN
{
    public sealed class VNChoicePanel : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private GameObject root;          // UI_ChoicePanel (자기 자신이어도 됨)
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

        public void Open(VNNode.BranchRule[] rules)
        {
            Debug.Log($"[VNChoicePanel] Open called on instanceID={GetInstanceID()} rules={rules?.Length ?? -1}");

            if (root == null || ChoicesList == null || choiceButtonPrefab == null || runner == null)
            {
                Debug.LogError("[VNChoicePanel] Missing refs. Check inspector wiring.");
                return;
            }

            Debug.Log($"[VNChoicePanel] root before activeSelf={root.activeSelf}, activeInHierarchy={root.activeInHierarchy}");
            ClearButtons();

           
            // 모달 락(스페이스 진행 막기)
            policy?.SetModalOpen(true);

            root.SetActive(true);

            for (int i = 0; i < rules.Length; i++)
            {
                var r = rules[i];
                if (r == null) continue;

                // choiceText 없는 건 선택지로 안 보여준다(자동분기용)
                if (string.IsNullOrEmpty(r.choiceText)) continue;

                
                var btn = Instantiate(choiceButtonPrefab, ChoicesList, worldPositionStays: false);
                spawned.Add(btn);


                // 라벨 텍스트 설정 (TMP 우선)
                var tmp = btn.GetComponentInChildren<TMP_Text>(true);
                if (tmp != null) tmp.text = r.choiceText;

                string jump = r.jumpLabel; // 클로저 캡처 안전
                btn.onClick.AddListener(() =>
                {
                    Close();
                    runner.Choose(jump);
                });

            }

            // 아무 버튼도 안 만들어졌으면 그냥 닫고 진행
            if (spawned.Count == 0)
            {
                Debug.LogWarning("[VNChoicePanel] No choices to show. Closing and continuing.");
                Close();
                runner.Next();
            }

            // 선택 포커스 제거 (Space/Enter가 버튼에 먹지 않게)
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        }

        public void Close()
        {
            ClearButtons();
            if (root != null) root.SetActive(false);

            policy?.SetModalOpen(false);
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