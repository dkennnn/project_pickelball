using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Hợp đồng của một "bộ não chiến thuật" cắm vào <see cref="PickleballAIController"/>.
    /// <para>
    /// Mỗi bậc độ khó có một implement riêng (<see cref="NewbieStrategy"/>, <see cref="AmateurStrategy"/>,
    /// <see cref="CompetitorStrategy"/>, <see cref="ProStrategy"/>, <see cref="MasterStrategy"/>).
    /// Strategy chỉ trả lời hai câu hỏi: <b>đánh vào đâu</b> và <b>đánh kiểu gì</b>;
    /// mọi thứ còn lại (thời gian bay, xoáy, sai số, animation) do controller lo.
    /// </para>
    /// <para><b>Quy ước toạ độ</b> (theo <see cref="Court"/>): sân trên mặt phẳng XZ, lưới tại <c>z = 0</c>.</para>
    /// </summary>
    public interface IAIStrategy
    {
        /// <summary>
        /// Chọn điểm rơi cho cú đánh sắp tới.
        /// </summary>
        /// <param name="playerPosition">Vị trí hiện tại của ĐỐI THỦ (world space).</param>
        /// <param name="courtWidth">Bề ngang TOÀN SÂN theo trục X (mét).</param>
        /// <param name="courtDepth">Chiều sâu của MỘT NỬA sân theo trục Z (mét) — tức <c>Court.HalfLength</c>.</param>
        /// <param name="isPositiveCourt">Nửa sân ĐÍCH: true = nhắm vào nửa sân <c>z &gt; 0</c>.</param>
        /// <returns>Điểm rơi mong muốn (world space, <c>y = 0</c>).</returns>
        Vector3 CalculateShotDestination(Vector3 playerPosition, float courtWidth, float courtDepth,
                                         bool isPositiveCourt);

        /// <summary>
        /// Chọn loại cú đánh phù hợp với tình huống.
        /// </summary>
        /// <param name="aiPosition">Vị trí hiện tại của AI (world space).</param>
        /// <param name="destination">Điểm rơi vừa chọn ở <see cref="CalculateShotDestination"/>.</param>
        /// <param name="ballController">Quả bóng của trận (có thể <c>null</c> khi chạy test).</param>
        /// <param name="netHeight">Chiều cao lưới (mét) — mốc so sánh độ cao bóng.</param>
        /// <param name="profile">Chỉ số runtime của AI (có thể <c>null</c>).</param>
        ShotType SelectShotType(Vector3 aiPosition, Vector3 destination, BallController ballController,
                                float netHeight, PlayerProfileProperties profile);
    }
}
