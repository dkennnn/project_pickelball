using System;
using StarterKit.Utilities;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Điểm khởi động của game: chạy trước mọi manager khác, nối các asset dữ liệu lại với nhau,
    /// dựng trang bị của người chơi và tính chỉ số lần đầu để mọi màn hình đọc được dữ liệu đúng.
    /// <para>
    /// Đặt <see cref="DefaultExecutionOrderAttribute"/> = -500 để chắc chắn chạy trước
    /// <c>GameManager</c>, UI và các handler khác.
    /// </para>
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public class GameBootstrap : IndestructibleSingleton<GameBootstrap>
    {
        /// <summary>Hub dữ liệu trung tâm của game.</summary>
        public GameData gameData;

        /// <summary>Asset cửa hàng, sẽ được gắn vào <see cref="GameData.shopData"/> nếu chưa có.</summary>
        public Shop shop;

        /// <summary>Cấu hình trận đấu, được áp dụng tham số theo chế độ đang chọn.</summary>
        public GameSettings gameSettings;

        /// <summary>Phát ngay sau khi khởi động xong; UI nên chờ sự kiện này trước khi vẽ.</summary>
        public static event Action OnBootstrapped;

        /// <summary>True khi quá trình khởi động đã hoàn tất.</summary>
        public bool IsReady { get; private set; }

        /// <inheritdoc/>
        protected override void OnAwake()
        {
            base.OnAwake();

            // TODO P08: nạp SaveData tại đây trước khi UpdateProfile

            if (gameData != null && gameData.shopData == null) gameData.shopData = shop;

            if (shop != null)
            {
                if (shop.gameData == null) shop.gameData = gameData;
                shop.SetupLoadout();
            }
            else
            {
                Debug.LogWarning("[GameBootstrap] Chưa gán Shop — bỏ qua bước dựng trang bị từ cửa hàng.");
            }

            ReapplyLoadout();

            if (gameSettings != null) gameSettings.ApplySelectedModeSettings();
            else Debug.LogWarning("[GameBootstrap] Chưa gán GameSettings — tham số trận đấu giữ nguyên giá trị cũ.");

            IsReady = true;

            Action handler = OnBootstrapped;
            if (handler != null) handler.Invoke();
        }

        /// <summary>
        /// Tính lại chỉ số nhân vật từ trang bị hiện tại. Gọi sau khi mua/nâng cấp/đổi trang bị
        /// ở ngoài luồng <see cref="Shop"/>, hoặc sau khi nạp dữ liệu lưu.
        /// </summary>
        public void ReapplyLoadout()
        {
            PlayerLoadout loadout = gameData != null ? gameData.playerLoadout : null;
            if (loadout == null) loadout = shop != null ? shop.playerLoadout : null;

            if (loadout == null)
            {
                Debug.LogWarning("[GameBootstrap] Không tìm thấy PlayerLoadout — chỉ số nhân vật không được tính lại.");
                return;
            }

            loadout.UpdateProfile();
        }
    }
}
