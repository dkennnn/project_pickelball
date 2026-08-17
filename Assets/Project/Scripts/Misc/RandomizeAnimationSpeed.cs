using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Đặt <see cref="Animator.speed"/> ngẫu nhiên trong khoảng [<see cref="minSpeed"/>,
    /// <see cref="maxSpeed"/>] ở <c>Start</c>. Dùng cho đám đông khán giả / cây cối để hàng chục
    /// object cùng clip không nhấp nháy đồng pha.
    /// <para>Không có <see cref="Animator"/> thì component im lặng không làm gì.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public class RandomizeAnimationSpeed : MonoBehaviour
    {
        [Tooltip("Animator cần đổi tốc độ. Bỏ trống thì tự tìm trên GameObject này hoặc trong con.")]
        public Animator targetAnimator;

        [Tooltip("Tốc độ phát nhỏ nhất.")]
        public float minSpeed = 0.85f;

        [Tooltip("Tốc độ phát lớn nhất.")]
        public float maxSpeed = 1.15f;

        [Tooltip("Bù thêm một offset ngẫu nhiên vào thời điểm bắt đầu clip để lệch pha triệt để hơn.")]
        public bool randomizeStartOffset = true;

        private void Start()
        {
            Randomize();
        }

        /// <summary>Bốc lại tốc độ (và offset) ngẫu nhiên ngay lập tức.</summary>
        public void Randomize()
        {
            if (targetAnimator == null) targetAnimator = GetComponent<Animator>();
            if (targetAnimator == null) targetAnimator = GetComponentInChildren<Animator>(true);
            if (targetAnimator == null) return;

            float lo = Mathf.Min(minSpeed, maxSpeed);
            float hi = Mathf.Max(minSpeed, maxSpeed);
            targetAnimator.speed = Random.Range(lo, hi);

            if (!randomizeStartOffset) return;
            if (targetAnimator.runtimeAnimatorController == null) return;

            // Nhảy tới một thời điểm ngẫu nhiên trong state hiện tại của layer 0.
            AnimatorStateInfo state = targetAnimator.GetCurrentAnimatorStateInfo(0);
            targetAnimator.Play(state.fullPathHash, 0, Random.value);
        }
    }
}
