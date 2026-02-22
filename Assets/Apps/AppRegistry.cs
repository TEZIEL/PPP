using System.Collections.Generic;
using UnityEngine;

public class AppRegistry : MonoBehaviour
{
    [SerializeField] private List<AppDefinition> apps = new();

    private Dictionary<string, AppDefinition> map;

    private void Awake()
    {
        Build();
    }

    private void OnValidate()
    {
        // 에디터에서 목록 바꿀 때도 갱신
        Build();
    }

    private void Build()
    {
        map = new Dictionary<string, AppDefinition>();
        foreach (var a in apps)
        {
            if (a == null) continue;
            if (string.IsNullOrWhiteSpace(a.AppId)) continue;

            // 중복 appId면 마지막이 덮어씀(원하면 경고 넣어도 됨)
            map[a.AppId] = a;
        }
    }

    public bool TryGet(string appId, out AppDefinition def)
    {
        def = null;
        if (map == null) Build();
        return !string.IsNullOrEmpty(appId) && map.TryGetValue(appId, out def) && def != null;
    }
}