using System;
using System.Collections.Generic;
using Pickleball.Data;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Hub dữ liệu trung tâm của game: ví tiền, kho vật phẩm, hồ sơ người chơi và mọi bảng cấu hình
    /// mà UI cùng gameplay cần tra cứu. Mọi thay đổi tiền tệ đều phải đi qua asset này để UI
    /// nhận được sự kiện cập nhật.
    /// </summary>
    [CreateAssetMenu(fileName = "GameData", menuName = "ScriptableObjects/GameData")]
    public class GameData : ScriptableObject
    {
        /// <summary>Hồ sơ tiến trình của người chơi (tên, thành tích, trophy, level).</summary>
        [Header("Player")]
        public PlayerProfileData playerProfileData;

        /// <summary>Bảng mốc level và quà theo level.</summary>
        public PlayerLevels playerLevels;

        /// <summary>Số coin đang có.</summary>
        [Header("Currencies")]
        public int totalCoins;

        /// <summary>Số gem đang có.</summary>
        public int totalGems;

        /// <summary>Kho tazo dùng để nâng cấp vật phẩm.</summary>
        [Header("Inventory")]
        public TazoData tazoData;

        /// <summary>Kho booster dùng trong trận.</summary>
        public BoostersData boostersData;

        /// <summary>Trang bị đang chọn và chỉ số tổng hợp của người chơi.</summary>
        public PlayerLoadout playerLoadout;

        // TODO P10b/P11: Shop shopData, SlotsData slotsData, TournamentsData tournamentsData
        // sẽ được bổ sung ở bước Shop/Locker/Tournament (agent khác phụ trách).

        /// <summary>Kho tên hiển thị cho đối thủ AI.</summary>
        [Header("Content")]
        public AINamesData namesData;

        /// <summary>Bộ ảnh đại diện chọn trong hồ sơ.</summary>
        public List<Sprite> avatarSprites;

        /// <summary>Bộ ảnh đại diện dùng ở màn hình kết quả.</summary>
        public List<Sprite> resultAvatarSprites;

        /// <summary>Ngưỡng tự chọn độ khó AI khi ở chế độ Auto.</summary>
        [Header("AI")]
        public AIDifficultySettings difficultySettings;

        /// <summary>Bảng tra profile AI theo bậc độ khó.</summary>
        public PlayerProfileConfigs AIProfileConfigs;

        /// <summary>Thông tin hiển thị của 7 chỉ số.</summary>
        [Header("Configs")]
        public PropertiesData propertiesData;

        /// <summary>Danh mục sân đấu theo level.</summary>
        public StadiumsData stadiumsData;

        /// <summary>Chế độ chọn độ khó AI người chơi đang đặt.</summary>
        [Header("Session")]
        public AIDifficultyMode aiDifficultyMode;

        /// <summary>Chế độ chơi của phiên hiện tại.</summary>
        public GameMode gameMode;

        /// <summary>Tay thuận người chơi đã chọn.</summary>
        public HandSide handSide;

        /// <summary>Phát khi số coin thay đổi, tham số là tổng coin mới.</summary>
        public event Action<int> OnTotalCoinsUpdated;

        /// <summary>Phát khi số gem thay đổi, tham số là tổng gem mới.</summary>
        public event Action<int> OnTotalGemsUpdated;

        /// <summary>Trừ coin (không xuống dưới 0) và phát sự kiện cập nhật.</summary>
        /// <param name="amount">Số coin bị trừ; bỏ qua nếu &lt;= 0.</param>
        public void ReduceCoins(int amount)
        {
            if (amount <= 0) return;

            totalCoins = Mathf.Max(0, totalCoins - amount);
            OnTotalCoinsUpdated?.Invoke(totalCoins);
        }

        /// <summary>Cộng coin và phát sự kiện cập nhật.</summary>
        /// <param name="amount">Số coin cộng thêm; bỏ qua nếu &lt;= 0.</param>
        public void IncreaseCoins(int amount)
        {
            if (amount <= 0) return;

            totalCoins += amount;
            OnTotalCoinsUpdated?.Invoke(totalCoins);
        }

        /// <summary>
        /// Tiêu coin nếu đủ. Trả về <c>true</c> và trừ tiền khi thành công,
        /// <c>false</c> khi không đủ (không thay đổi gì).
        /// </summary>
        /// <param name="amount">Số coin cần tiêu.</param>
        public bool TrySpendCoins(int amount)
        {
            if (amount <= 0) return true;
            if (totalCoins < amount) return false;

            ReduceCoins(amount);
            return true;
        }

        /// <summary>Trừ gem (không xuống dưới 0) và phát sự kiện cập nhật.</summary>
        /// <param name="amount">Số gem bị trừ; bỏ qua nếu &lt;= 0.</param>
        public void ReduceGems(int amount)
        {
            if (amount <= 0) return;

            totalGems = Mathf.Max(0, totalGems - amount);
            OnTotalGemsUpdated?.Invoke(totalGems);
        }

        /// <summary>Cộng gem và phát sự kiện cập nhật.</summary>
        /// <param name="amount">Số gem cộng thêm; bỏ qua nếu &lt;= 0.</param>
        public void IncreaseGems(int amount)
        {
            if (amount <= 0) return;

            totalGems += amount;
            OnTotalGemsUpdated?.Invoke(totalGems);
        }

        /// <summary>
        /// Tiêu gem nếu đủ. Trả về <c>true</c> và trừ gem khi thành công,
        /// <c>false</c> khi không đủ (không thay đổi gì).
        /// </summary>
        /// <param name="amount">Số gem cần tiêu.</param>
        public bool TrySpendGems(int amount)
        {
            if (amount <= 0) return true;
            if (totalGems < amount) return false;

            ReduceGems(amount);
            return true;
        }

        /// <summary>
        /// Cộng một phần thưởng vào tài khoản, tự định tuyến sang ví tiền, kho tazo hoặc kho booster.
        /// Phần thưởng dạng hình ảnh (ball/racket/stadium visual) hiện chỉ ghi log chờ hệ shop visual.
        /// </summary>
        /// <param name="type">Loại phần thưởng.</param>
        /// <param name="amount">Số lượng; bỏ qua nếu &lt;= 0.</param>
        public void GrantReward(RewardType type, int amount)
        {
            if (amount <= 0) return;

            switch (type)
            {
                case RewardType.Coins:
                    IncreaseCoins(amount);
                    return;

                case RewardType.Gems:
                    IncreaseGems(amount);
                    return;

                case RewardType.GripTazos:
                case RewardType.PaddleTazos:
                case RewardType.WorkoutTazos:
                case RewardType.CharacterTazos:
                    if (tazoData != null) tazoData.AddTazos(TazoData.FromRewardType(type), amount);
                    return;

                case RewardType.StaminaBooster:
                    AddBooster(BoosterType.Stamina, amount);
                    return;

                case RewardType.SpeedBooster:
                    AddBooster(BoosterType.Speed, amount);
                    return;

                case RewardType.SpinBooster:
                    AddBooster(BoosterType.Spin, amount);
                    return;

                case RewardType.SwingBooster:
                    AddBooster(BoosterType.Swing, amount);
                    return;

                case RewardType.PowerBooster:
                    AddBooster(BoosterType.Power, amount);
                    return;

                default:
                    // TODO P10b/P11: phần thưởng hình ảnh cần hệ shop visual (ball/racket/stadium).
                    Debug.Log($"[GameData] TODO: chưa xử lý phần thưởng {type} x{amount}.");
                    return;
            }
        }

        private void AddBooster(BoosterType boosterType, int amount)
        {
            if (boostersData == null) return;
            boostersData.Add(boosterType, amount);
        }
    }
}
