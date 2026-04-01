using UnityEngine;

public class OptionsModal : MonoBehaviour
{
    private void Awake()
    {
        gameObject.SetActive(false);
    }

    // 🔥 모달 열기
    public void Open()
    {
        gameObject.SetActive(true);

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
    }

    // 🔥 OK 버튼 (적용 + 닫기)
    public void OnClickOK()
    {
        if (OptionManager.Instance != null)
        {
            OptionManager.Instance.Apply();
        }

        gameObject.SetActive(false);
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
    }
}