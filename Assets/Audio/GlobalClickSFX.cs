using UnityEngine;
using UnityEngine.EventSystems;

public class GlobalClickSFX : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // UI 위에서만 재생
            if (EventSystem.current.IsPointerOverGameObject())
            {
                SoundManager.Instance.PlayOS(OSSoundEvent.Click);
            }
        }
    }
}