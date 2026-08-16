using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Trạng thái đuổi bóng của AI: chạy tới điểm đứng đã được
    /// <see cref="BasePlayerController.FindOptimalTargetPositionForTrackingBall"/> tính sẵn và đếm ngược
    /// tới thời điểm chạm bóng.
    /// <para>
    /// Khi chỉ còn <c>preHitShotTime</c> giây nữa là bóng tới, state chuyển sang
    /// <see cref="PlayerState.TakeShot"/> để kịp giơ vợt. AI nhanh nhẹn (Agility cao) cần ít thời gian
    /// chuẩn bị hơn nên vào pha đánh muộn hơn — đúng cảm giác "phản xạ tốt".
    /// </para>
    /// <para>
    /// State cũng theo dõi <c>isBallInKitchen</c> để HUỶ ý định volley khi cả bóng lẫn AI đều nằm trong
    /// kitchen — nếu không AI sẽ tự phạm luật cấm volley.
    /// </para>
    /// </summary>
    public class AITrackBallState : IPlayerState
    {
        private readonly PickleballAIController aiController;

        private bool isReadyToHit;
        private bool isBallInKitchen;
        private float timeToHitBall;
        private float preHitShotTime;

        /// <summary>Thời gian chuẩn bị (giây) của AI chậm nhất.</summary>
        private const float MaxPreShotTime = 0.45f;

        /// <summary>Thời gian chuẩn bị (giây) của AI nhanh nhẹn nhất.</summary>
        private const float MinPreShotTime = 0.22f;

        /// <summary>Chỉ số Agility tương ứng với <see cref="MinPreShotTime"/>.</summary>
        private const float MaxAgility = 3.5f;

        /// <summary>Tạo trạng thái đuổi bóng cho một AI.</summary>
        /// <param name="controller">Controller sở hữu trạng thái này.</param>
        public AITrackBallState(PickleballAIController controller)
        {
            aiController = controller;
        }

        /// <summary>Ghi nhận thời điểm chạm bóng dự kiến và chạy tới điểm đứng đánh bóng.</summary>
        public void Enter()
        {
            if (aiController == null) return;

            isReadyToHit = false;
            isBallInKitchen = false;

            timeToHitBall = aiController.TargetBallHitTime;

            float agility = aiController.profile != null ? aiController.profile.Agility : 1f;
            preHitShotTime = Mathf.Lerp(MaxPreShotTime, MinPreShotTime, Mathf.Clamp01(agility / MaxAgility));

            if (timeToHitBall > 0f) aiController.SetTargetPosition(aiController.TargetHitStandPoint);

            if (aiController.animationController != null) aiController.animationController.SetMoveSpeed(1f);
        }

        /// <summary>Trả tốc độ animation di chuyển về 0 khi rời trạng thái.</summary>
        public void Exit()
        {
            if (aiController == null) return;

            if (aiController.animationController != null) aiController.animationController.SetMoveSpeed(0f);
        }

        /// <summary>Đếm ngược tới cú đánh; không có điểm đón bóng hợp lệ thì về trạng thái chờ.</summary>
        public void Update()
        {
            if (aiController == null) return;

            if (timeToHitBall <= 0f)
            {
                aiController.ChangeState(PlayerState.Idle);
                return;
            }

            UpdateVolleyIntent();

            float remaining = timeToHitBall - Time.time;

            if (remaining > preHitShotTime) return;

            isReadyToHit = true;
            aiController.ChangeState(PlayerState.TakeShot);
        }

        /// <summary>Huỷ ý định volley nếu điểm đón bóng buộc AI phải đứng trong kitchen (phạm luật).</summary>
        private void UpdateVolleyIntent()
        {
            Court court = aiController.MatchCourt;
            if (court == null || !aiController.isAttemptingVolley) return;

            isBallInKitchen = aiController.ballController != null
                              && court.IsInKitchen(aiController.ballController.transform.position);

            if (isBallInKitchen && court.IsInKitchen(aiController.transform.position))
            {
                aiController.SetAttemptingVolley(false);
            }
        }

        /// <summary>True khi đã tới lúc chuyển sang pha đánh bóng.</summary>
        public bool IsReadyToHit => isReadyToHit;

        /// <summary>True khi quả bóng đang nằm trong vùng kitchen.</summary>
        public bool IsBallInKitchen => isBallInKitchen;
    }
}
