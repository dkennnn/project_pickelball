using UnityEngine;
using UnityEngine.Events;

namespace Pickleball
{
    /// <summary>
    /// Đặt trên GameObject có Animator để nhận Animation Event và chuyển tiếp lên
    /// <see cref="BasePlayerController"/> (state đang chạy sẽ lắng nghe các event tương ứng).
    /// <para>
    /// Tên hàm phải khớp CHÍNH XÁC với tên Animation Event đặt trong clip:
    /// <c>OnThrowBall</c> (khung tung bóng của cú giao), <c>OnShotHit</c> (khung vợt chạm bóng),
    /// <c>OnPaddleAligned</c> (khung vợt đã giơ xong).
    /// </para>
    /// </summary>
    public class PlayerAnimationEventReceiver : MonoBehaviour
    {
        /// <summary>Controller nhận các event này; để trống thì tự tìm ở object cha.</summary>
        public BasePlayerController owner;

        /// <summary>Hook phụ cho VFX/SFX ở khung tung bóng.</summary>
        public UnityEvent onThrowBall;

        /// <summary>Hook phụ cho VFX/SFX ở khung vợt chạm bóng.</summary>
        public UnityEvent onShotHit;

        /// <summary>Hook phụ cho VFX/SFX ở khung vợt đã khớp hướng.</summary>
        public UnityEvent onPaddleAligned;

        private void Awake()
        {
            if (owner == null) owner = GetComponentInParent<BasePlayerController>();
        }

        /// <summary>Animation Event: bóng rời tay người giao.</summary>
        public void OnThrowBall()
        {
            if (owner != null) owner.RaiseAnimationThrowBall();
            onThrowBall?.Invoke();
        }

        /// <summary>Animation Event: vợt chạm bóng.</summary>
        public void OnShotHit()
        {
            if (owner != null) owner.RaiseAnimationShotHit();
            onShotHit?.Invoke();
        }

        /// <summary>Animation Event: vợt đã giơ/khớp hướng xong, cho phép thực hiện cú đánh.</summary>
        public void OnPaddleAligned()
        {
            if (owner != null) owner.RaiseAnimationPaddleAligned();
            onPaddleAligned?.Invoke();
        }
    }
}
