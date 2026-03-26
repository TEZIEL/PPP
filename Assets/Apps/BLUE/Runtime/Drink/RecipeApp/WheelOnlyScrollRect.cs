using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PPP.BLUE.VN.RecipeApp
{
    /// <summary>
    /// 마우스 휠 스크롤만 허용하고, 마우스 드래그 스크롤은 막는 ScrollRect.
    /// </summary>
    public sealed class WheelOnlyScrollRect : ScrollRect
    {
        protected override void Awake()
        {
            base.Awake();
            ApplyNonSlidingSettings();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            ApplyNonSlidingSettings();
        }

        public override void OnBeginDrag(PointerEventData eventData)
        {
            // 드래그 시작 무시
        }

        public override void OnDrag(PointerEventData eventData)
        {
            // 드래그 이동 무시
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            // 드래그 종료 무시
        }

        public override void OnScroll(PointerEventData data)
        {
            // 휠 스크롤은 허용하되 관성(미끌림)은 제거한다.
            base.OnScroll(data);
            velocity = UnityEngine.Vector2.zero;
        }

        private void ApplyNonSlidingSettings()
        {
            inertia = false;
            movementType = MovementType.Clamped;
            elasticity = 0f;
            decelerationRate = 0f;
        }
    }
}
