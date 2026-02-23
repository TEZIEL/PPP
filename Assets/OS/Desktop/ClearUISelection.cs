using UnityEngine;
using UnityEngine.EventSystems;

public class ClearUISelection : MonoBehaviour
{
    void Update()
    {
        // Space/Enter 같은 Submit이 버튼에 전달되기 전에 "선택"을 끊어버림
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            EventSystem.current?.SetSelectedGameObject(null);
    }

    void LateUpdate()
    {
        // 클릭으로 버튼이 선택돼도 다음 프레임에 바로 선택 해제
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
            EventSystem.current.SetSelectedGameObject(null);
    }
}