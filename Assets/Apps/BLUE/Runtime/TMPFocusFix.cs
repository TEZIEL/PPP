using UnityEngine;
using TMPro;

public class TMPFocusFix : MonoBehaviour
{
    public TMP_InputField input;

    void Start()
    {
        input.ActivateInputField();
    }
}