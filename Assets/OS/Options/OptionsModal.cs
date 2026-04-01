using UnityEngine;

public class OptionsModal : MonoBehaviour
{
    private void Awake()
    {
        gameObject.SetActive(false);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }
}
