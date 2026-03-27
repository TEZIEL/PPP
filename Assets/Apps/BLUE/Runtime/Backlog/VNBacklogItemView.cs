using TMPro;
using UnityEngine;

namespace PPP.BLUE.VN
{
    public sealed class VNBacklogItemView : MonoBehaviour
    {
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private TMP_Text bodyText;

        public void SetupRuntimeTexts(TMP_Text speaker, TMP_Text body)
        {
            speakerText = speaker;
            bodyText = body;
        }

        public void Bind(VNBacklogEntry entry)
        {
            if (entry == null)
                return;

            if (speakerText != null)
                speakerText.text = entry.speaker ?? string.Empty;

            if (bodyText != null)
                bodyText.text = entry.text ?? string.Empty;
        }
    }
}
