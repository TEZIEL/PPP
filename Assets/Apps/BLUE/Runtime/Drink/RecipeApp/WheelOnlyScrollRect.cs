using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PPP.BLUE.VN.RecipeApp
{
    /// <summary>
    /// 留곗  ㅽщ·留 ⑺怨, 留곗 洹 ㅽщ· 留 ScrollRect.
    /// </summary>
    public sealed class WheelOnlyScrollRect : ScrollRect
    {
        public override void OnBeginDrag(PointerEventData eventData)
        {
            // 洹  臾댁
        }

        public override void OnDrag(PointerEventData eventData)
        {
            // 洹 대 臾댁
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            // 洹 醫

        }
    }
}
