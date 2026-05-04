using UnityEngine;

public class OptionsModal : MonoBehaviour
{
    private PPP.BLUE.VN.VNPolicyController policy;
    private bool isModalPushed;

    private void Awake()
    {
        policy = FindFirstObjectByType<PPP.BLUE.VN.VNPolicyController>(FindObjectsInactive.Include);
        gameObject.SetActive(false);
    }

    // 🔥 모달 열기
    public void Open()
    {
        gameObject.SetActive(true);
        Debug.Log("[OS_MODAL] Options open blocking=True");

        // 🔥 UI 최신 상태로 갱신
        if (OptionManager.Instance != null)
        {
            OptionManager.Instance.OnOpen();
        }
    }

    // 🔥 닫기 (단순 닫기)
    public void Close()
    {
        gameObject.SetActive(false);
        Debug.Log("[OS_MODAL] Options close blocking=False");
    }

    // 🔥 OK 버튼 (적용 + 닫기)
    public void OnClickOK()
    {
        if (OptionManager.Instance != null)
        {
            OptionManager.Instance.Apply();
        }

        gameObject.SetActive(false);
        Debug.Log("[OS_MODAL] Options close blocking=False");
    }

    // 🔥 Apply 버튼 (적용만)
    public void OnClickApply()
    {
        if (OptionManager.Instance != null)
        {
            OptionManager.Instance.Apply();
        }
    }

    // 🔥 Cancel 버튼 (되돌림 + 닫기)
    public void OnClickCancel()
    {
        if (OptionManager.Instance != null)
        {
            OptionManager.Instance.Cancel();
        }

        gameObject.SetActive(false);
        Debug.Log("[OS_MODAL] Options close blocking=False");
    }
}
