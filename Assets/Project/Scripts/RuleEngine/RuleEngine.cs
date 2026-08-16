using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Bộ luật pickleball thuần logic (POCO, KHÔNG phải MonoBehaviour) — chỉ dựa vào hình học
    /// của <see cref="Court"/> nên có thể unit-test độc lập, không cần scene.
    /// <para>
    /// Ba luật đặc trưng được cài ở đây:
    /// <br/>• <b>Giao chéo sân</b>: bóng giao phải rơi vào ô giao chéo, ngoài kitchen.
    /// <br/>• <b>Two-bounce rule</b>: sau khi giao, mỗi bên phải để bóng nảy đúng một lần
    /// trước khi được phép volley (đánh bóng khi chưa chạm đất).
    /// <br/>• <b>Kitchen / non-volley zone</b>: không được volley khi đang đứng trong kitchen.
    /// </para>
    /// </summary>
    public class RuleEngine
    {
        /// <summary>Sân dùng để tra cứu hình học. Không bao giờ null trong sử dụng bình thường.</summary>
        public readonly Court court;

        /// <summary>Tạo bộ luật gắn với một sân cụ thể.</summary>
        /// <param name="court">Sân đang thi đấu.</param>
        public RuleEngine(Court court)
        {
            this.court = court;
        }

        /// <summary>
        /// Cú giao có hợp lệ không (rơi đúng ô giao chéo sân, ngoài kitchen, trong biên).
        /// </summary>
        /// <param name="landingPosition">Điểm chạm đất đầu tiên của bóng giao.</param>
        /// <param name="isServerPositiveCourt">True nếu người giao ở nửa sân dương (z &gt; 0).</param>
        /// <param name="serveSide">Ô giao mà người giao đang đứng.</param>
        public bool CheckServeValidity(Vector3 landingPosition, bool isServerPositiveCourt, ServeSide serveSide)
        {
            if (court == null) return false;
            return court.IsServeValid(landingPosition, isServerPositiveCourt, serveSide);
        }

        /// <summary>
        /// Người chơi có được phép volley (đánh bóng khi bóng CHƯA chạm đất) tại vị trí này không.
        /// <para>
        /// Hai điều kiện: (1) <paramref name="hasBallBounced"/> phải true — tức yêu cầu
        /// two-bounce rule đã được thoả (cả hai bên đã để bóng nảy một lần); (2) người chơi không
        /// đứng trong kitchen.
        /// </para>
        /// </summary>
        /// <param name="playerPosition">Vị trí người chơi lúc chạm bóng.</param>
        /// <param name="hasBallBounced">True khi two-bounce rule đã được thoả, cho phép volley.</param>
        public bool CheckVolleyValidity(Vector3 playerPosition, bool hasBallBounced)
        {
            if (!hasBallBounced) return false;
            return !IsVolleyInKitchen(playerPosition);
        }

        /// <summary>Điểm nảy có nằm trong biên và đúng nửa sân được chỉ định không.</summary>
        public bool CheckBounceInCorrectArea(Vector3 bouncePosition, bool isPositiveCourt)
        {
            if (court == null) return false;
            return court.IsBounceInCorrectArea(bouncePosition, isPositiveCourt);
        }

        /// <summary>True nếu người chơi đang đứng trong kitchen (volley ở đây là lỗi).</summary>
        public bool IsVolleyInKitchen(Vector3 playerPosition)
        {
            if (court == null) return false;
            return court.IsInKitchen(playerPosition);
        }

        /// <summary>
        /// Two-bounce rule bị vi phạm khi một trong hai bên chưa để bóng nảy đủ một lần
        /// trong hai lượt đánh đầu tiên của pha bóng.
        /// </summary>
        /// <param name="serverBounceCount">Số lần bóng nảy trên nửa sân của người giao.</param>
        /// <param name="receiverBounceCount">Số lần bóng nảy trên nửa sân của người nhận.</param>
        public bool IsDoubleBounceRuleViolated(int serverBounceCount, int receiverBounceCount)
        {
            return serverBounceCount < 1 || receiverBounceCount < 1;
        }

        /// <summary>True nếu bóng đã nảy từ 2 lần trở lên trên cùng một nửa sân (bên đó thua điểm).</summary>
        public bool IsDoubleBounceOnGround(int bounceCount)
        {
            return bounceCount >= 2;
        }

        /// <summary>
        /// Đánh giá tổng hợp một lần bóng chạm đất trong pha bóng (không phải cú giao).
        /// Trả về <see cref="RuleType.None"/> nếu hợp lệ.
        /// </summary>
        /// <param name="bouncePosition">Vị trí bóng chạm đất.</param>
        /// <param name="isPositiveCourtOfHitter">
        /// Nửa sân của NGƯỜI VỪA ĐÁNH. Bóng hợp lệ phải rơi ở nửa sân đối diện.
        /// </param>
        /// <param name="bounceCountOnThisSide">
        /// Số lần bóng đã nảy liên tiếp trên nửa sân vừa nhận (đã bao gồm lần nảy này).
        /// </param>
        /// <returns>
        /// <see cref="RuleType.DoubleBounceOnSide"/> nếu bên nhận để bóng nảy hai lần;
        /// <see cref="RuleType.BounceOutOfCourt"/> nếu bóng rơi ngoài biên hoặc sai nửa sân;
        /// ngược lại <see cref="RuleType.None"/>.
        /// </returns>
        public RuleType EvaluateBounce(Vector3 bouncePosition, bool isPositiveCourtOfHitter, int bounceCountOnThisSide)
        {
            // Nảy lần thứ hai trên cùng một bên => bên đó không kịp trả bóng.
            if (IsDoubleBounceOnGround(bounceCountOnThisSide)) return RuleType.DoubleBounceOnSide;

            // Lần nảy đầu tiên phải nằm trong nửa sân đối diện người vừa đánh.
            if (!CheckBounceInCorrectArea(bouncePosition, !isPositiveCourtOfHitter)) return RuleType.BounceOutOfCourt;

            return RuleType.None;
        }
    }
}
