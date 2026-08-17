using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Cầu nối giữa logic đánh bóng và Animator của nhân vật.
    /// <para>
    /// Mỗi cú đánh trong bản gốc gồm một cặp clip: <b>Pre-shot</b> (giơ vợt) rồi <b>Hit</b> (vung vợt),
    /// khớp với cơ chế <see cref="BasePlayerController.AnimationPaddleAligned"/> —
    /// vợt phải xoay khớp hướng xong thì cú đánh mới được tính.
    /// </para>
    /// <para>
    /// Toàn bộ tham số Animator được cache bằng <see cref="Animator.StringToHash"/>.
    /// Nếu <see cref="animator"/> chưa được gán (giai đoạn chưa có rig) thì MỌI hàm đều
    /// không làm gì cả và không ném lỗi.
    /// </para>
    /// </summary>
    public class BasePlayerAnimationController : MonoBehaviour
    {
        /// <summary>Animator của nhân vật; để trống thì mọi hàm trở thành no-op an toàn.</summary>
        public Animator animator;

        /// <summary>Tỉ lệ chiều cao vai so với chiều cao nhân vật — bóng cao hơn mức này là volley trên đầu.</summary>
        [Range(0.5f, 1f)] public float shoulderHeightRatio = 0.8f;

        /// <summary>Tỉ lệ chiều cao hông — bóng thấp hơn mức này phải dùng cú đánh cúi người.</summary>
        [Range(0.1f, 0.8f)] public float hipHeightRatio = 0.45f;

        private static readonly int XSpeedHash = Animator.StringToHash(StringConstants.AnimXSpeed);
        private static readonly int ZSpeedHash = Animator.StringToHash(StringConstants.AnimZSpeed);
        private static readonly int ShotTriggerHash = Animator.StringToHash(StringConstants.AnimShotTrigger);
        private static readonly int PreShotTriggerHash = Animator.StringToHash(StringConstants.AnimPreShotTrigger);
        private static readonly int MissTriggerHash = Animator.StringToHash(StringConstants.AnimMissTrigger);
        private static readonly int ResetTriggerHash = Animator.StringToHash(StringConstants.AnimResetTrigger);
        private static readonly int WinTriggerHash = Animator.StringToHash(StringConstants.AnimWinTrigger);
        private static readonly int LoseTriggerHash = Animator.StringToHash(StringConstants.AnimLoseTrigger);
        private static readonly int ShotTypeIndexHash = Animator.StringToHash(StringConstants.AnimShotTypeIndex);
        private static readonly int IsRightSideHash = Animator.StringToHash(StringConstants.AnimIsRightSide);
        private static readonly int IsGameplayActiveHash = Animator.StringToHash(StringConstants.AnimIsGameplayActive);
        private static readonly int MirrorHandSideHash = Animator.StringToHash(StringConstants.AnimMirrorHandSide);

        /// <summary>True khi đã có Animator hợp lệ để điều khiển.</summary>
        public bool HasAnimator => animator != null;

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
        }

        /// <summary>
        /// Chọn clip đánh bóng theo độ cao bóng so với người và bên đánh.
        /// <list type="bullet">
        /// <item><description>Bóng cao hơn vai → <see cref="ShotAnimationType.HeadUpVolley"/> (giữa),
        /// <see cref="ShotAnimationType.RightVolley"/> / <see cref="ShotAnimationType.LeftVolley"/>.</description></item>
        /// <item><description>Bóng thấp hơn hông → <see cref="ShotAnimationType.DownRightSideShot"/> /
        /// <see cref="ShotAnimationType.DownLeftSideShot"/>.</description></item>
        /// <item><description>Còn lại → <see cref="ShotAnimationType.RightSideShot"/> /
        /// <see cref="ShotAnimationType.LeftSideShot"/>.</description></item>
        /// </list>
        /// </summary>
        /// <param name="shotType">Loại cú đánh (Serve được ưu tiên trả về clip giao bóng).</param>
        /// <param name="shotSide">Bóng ở bên phải / trái / chính giữa thân người.</param>
        /// <param name="ballHeight">Độ cao của bóng tại điểm chạm vợt (mét).</param>
        /// <param name="playerHeight">Chiều cao nhân vật (mét).</param>
        public ShotAnimationType DetermineShotAnimation(ShotType shotType, ShotSide shotSide, float ballHeight,
                                                        float playerHeight)
        {
            if (shotType == ShotType.Serve) return ShotAnimationType.Serve;

            if (playerHeight <= 0f) playerHeight = 1.7f;

            float shoulderHeight = playerHeight * shoulderHeightRatio;
            float hipHeight = playerHeight * hipHeightRatio;

            if (ballHeight > shoulderHeight)
            {
                switch (shotSide)
                {
                    case ShotSide.Left: return ShotAnimationType.LeftVolley;
                    case ShotSide.Right: return ShotAnimationType.RightVolley;
                    default: return ShotAnimationType.HeadUpVolley;
                }
            }

            if (ballHeight < hipHeight)
            {
                return shotSide == ShotSide.Left
                    ? ShotAnimationType.DownLeftSideShot
                    : ShotAnimationType.DownRightSideShot;
            }

            return shotSide == ShotSide.Left ? ShotAnimationType.LeftSideShot : ShotAnimationType.RightSideShot;
        }

        /// <summary>Phát clip giơ vợt (Pre-shot) tương ứng với kiểu cú đánh.</summary>
        /// <param name="shotAnimationType">Kiểu cú đánh sắp thực hiện.</param>
        public void PlayPreShot(ShotAnimationType shotAnimationType)
        {
            if (animator == null) return;

            animator.SetInteger(ShotTypeIndexHash, (int)shotAnimationType);
            animator.SetTrigger(PreShotTriggerHash);
        }

        /// <summary>Phát clip vung vợt đánh bóng.</summary>
        /// <param name="shotAnimationType">Kiểu cú đánh đang thực hiện.</param>
        public void PlayShot(ShotAnimationType shotAnimationType)
        {
            if (animator == null) return;

            animator.SetInteger(ShotTypeIndexHash, (int)shotAnimationType);
            animator.SetTrigger(ShotTriggerHash);
        }

        /// <summary>Phát clip giao bóng.</summary>
        public void PlayServe()
        {
            if (animator == null) return;

            // Controller gốc KHÔNG có trigger "Serve" riêng. Giao bóng = HitType 4 + trigger Hit,
            // còn bool IsRightSide quyết định state "Serve Right" hay "Serve Left".
            animator.SetInteger(ShotTypeIndexHash, (int)ShotAnimationType.Serve);
            animator.SetTrigger(ShotTriggerHash);
        }

        /// <summary>Phát clip ăn mừng chiến thắng.</summary>
        public void PlayVictory()
        {
            if (animator == null) return;
            animator.SetTrigger(WinTriggerHash);
        }

        /// <summary>
        /// Cập nhật tốc độ di chuyển cho blend tree.
        /// <para>Controller gốc dùng blend tree <b>2D</b> trên hai trục XSpeed/ZSpeed chứ không phải
        /// một tham số "Speed" duy nhất, nên phải truyền vector chứ không phải độ lớn.</para>
        /// </summary>
        /// <param name="normalizedSpeed">Tốc độ 0..1 (giữ lại cho tương thích ngược, map vào trục Z).</param>
        public void SetMoveSpeed(float normalizedSpeed)
        {
            SetMoveSpeed(new Vector2(0f, Mathf.Clamp01(normalizedSpeed)));
        }

        /// <summary>Cập nhật blend tree 2D di chuyển bằng vận tốc cục bộ đã chuẩn hoá.</summary>
        /// <param name="normalizedLocalVelocity">x = ngang, y = dọc, mỗi trục trong [-1, 1].</param>
        public void SetMoveSpeed(Vector2 normalizedLocalVelocity)
        {
            if (animator == null) return;
            animator.SetFloat(XSpeedHash, Mathf.Clamp(normalizedLocalVelocity.x, -1f, 1f));
            animator.SetFloat(ZSpeedHash, Mathf.Clamp(normalizedLocalVelocity.y, -1f, 1f));
        }

        /// <summary>Đặt bên sân đang đứng — quyết định state giao bóng trái/phải của controller gốc.</summary>
        public void SetRightSide(bool isRightSide)
        {
            if (animator == null) return;
            animator.SetBool(IsRightSideHash, isRightSide);
        }

        /// <summary>Bật/tắt lớp animation trong trận.</summary>
        public void SetGameplayActive(bool active)
        {
            if (animator == null) return;
            animator.SetBool(IsGameplayActiveHash, active);
        }

        /// <summary>Lật animation cho tay thuận trái.</summary>
        public void SetHandSide(HandSide handSide)
        {
            if (animator == null) return;
            animator.SetBool(MirrorHandSideHash, handSide == HandSide.Left);
        }

        /// <summary>Phát clip đánh hụt.</summary>
        public void PlayMiss()
        {
            if (animator == null) return;
            animator.SetTrigger(MissTriggerHash);
        }

        /// <summary>Phát clip thua trận.</summary>
        public void PlayLose()
        {
            if (animator == null) return;
            animator.SetTrigger(LoseTriggerHash);
        }

        /// <summary>Xoá mọi trigger đang treo (gọi khi reset pha bóng để tránh clip chạy trễ).</summary>
        public void ResetTriggers()
        {
            if (animator == null) return;

            animator.ResetTrigger(ShotTriggerHash);
            animator.ResetTrigger(PreShotTriggerHash);
            animator.ResetTrigger(MissTriggerHash);
            animator.ResetTrigger(WinTriggerHash);
            animator.ResetTrigger(LoseTriggerHash);
            animator.SetTrigger(ResetTriggerHash);
        }
    }
}
