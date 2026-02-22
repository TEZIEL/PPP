using UnityEngine;

[CreateAssetMenu(menuName = "Apps/App Definition", fileName = "AppDefinition")]
public class AppDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string appId = "app.vn";
    [SerializeField] private string displayName = "VN";

    [Header("Prefabs")]
    [SerializeField] private WindowController windowPrefab;
    [SerializeField] private GameObject contentPrefab;

    [Header("Defaults")]
    [SerializeField] private Vector2 defaultPos = new Vector2(200, -120);
    [SerializeField] private Vector2 defaultSize = new Vector2(640, 480);

    [Header("Desktop Icon")]
    [SerializeField] private Sprite iconSprite;

    // ✅ 외부는 무조건 프로퍼티로 접근
    public string AppId => appId;
    public string DisplayName => displayName;

    public WindowController WindowPrefab => windowPrefab;
    public GameObject ContentPrefab => contentPrefab;

    public Vector2 DefaultPos => defaultPos;
    public Vector2 DefaultSize => defaultSize;

    public Sprite IconSprite => iconSprite;
}