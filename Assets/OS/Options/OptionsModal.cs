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
        PushOptionsModalIfNeeded();

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
        PopOptionsModalIfNeeded();
    }

    // 🔥 OK 버튼 (적용 + 닫기)
    public void OnClickOK()
    {
        if (OptionManager.Instance != null)
        {
            OptionManager.Instance.Apply();
        }

        gameObject.SetActive(false);
        PopOptionsModalIfNeeded();
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
        PopOptionsModalIfNeeded();
    }

    private void PushOptionsModalIfNeeded()
    {
        if (isModalPushed)
            return;

        policy?.PushModal("Options");
        isModalPushed = true;
    }

    private void PopOptionsModalIfNeeded()
    {
        if (!isModalPushed)
            return;

        policy?.PopModal("Options");
        isModalPushed = false;
    }

    private void OnDisable()
    {
        PopOptionsModalIfNeeded();
    }
}
