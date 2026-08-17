using System.Collections.Generic;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Kho tazo (mảnh nâng cấp) của người chơi, tách riêng theo từng loại vật phẩm.
    /// Tazo là nguồn nâng cấp "miễn phí" rơi ra từ kitbag/level-up, song song với đường nâng cấp bằng gem.
    /// </summary>
    [CreateAssetMenu(fileName = "TazoData", menuName = "ScriptableObjects/TazoData")]
    public class TazoData : ScriptableObject
    {
        /// <summary>Số tazo hiện có theo từng loại vật phẩm.</summary>
        public List<TazoCountData> tazos;

        /// <summary>
        /// Quy đổi loại phần thưởng tazo sang loại vật phẩm tương ứng.
        /// Trả về <see cref="ShopItemType.None"/> nếu phần thưởng không phải tazo.
        /// </summary>
        /// <param name="rewardType">Loại phần thưởng cần quy đổi.</param>
        public static ShopItemType FromRewardType(RewardType rewardType)
        {
            switch (rewardType)
            {
                case RewardType.GripTazos: return ShopItemType.Grip;
                case RewardType.PaddleTazos: return ShopItemType.Paddle;
                case RewardType.WorkoutTazos: return ShopItemType.Workout;
                case RewardType.CharacterTazos: return ShopItemType.Character;
                default: return ShopItemType.None;
            }
        }

        /// <summary>Kho tazo của loại vật phẩm này có đủ số lượng yêu cầu hay không.</summary>
        /// <param name="item">Vật phẩm muốn nâng cấp.</param>
        /// <param name="requiredTazos">Số tazo cần cho bậc nâng cấp kế tiếp.</param>
        public bool CanUpgradeWithTazos(Item item, int requiredTazos)
        {
            if (item == null || requiredTazos <= 0) return false;
            return GetTazoCounts(item.itemType) >= requiredTazos;
        }

        /// <summary>Trừ tazo khi nâng cấp. Không làm gì nếu kho không đủ.</summary>
        /// <param name="item">Vật phẩm được nâng cấp.</param>
        /// <param name="requiredTazos">Số tazo tiêu tốn.</param>
        public void UseTazos(Item item, int requiredTazos)
        {
            if (!CanUpgradeWithTazos(item, requiredTazos)) return;
            RemoveTazos(item.itemType, requiredTazos);
        }

        /// <summary>Cộng tazo vào kho; tự tạo mục mới nếu loại đó chưa có.</summary>
        /// <param name="itemType">Loại vật phẩm.</param>
        /// <param name="amount">Số tazo cộng thêm (bỏ qua nếu &lt;= 0).</param>
        public void AddTazos(ShopItemType itemType, int amount)
        {
            if (itemType == ShopItemType.None || amount <= 0) return;

            TazoCountData entry = FindEntry(itemType);
            if (entry == null)
            {
                if (tazos == null) tazos = new List<TazoCountData>();
                entry = new TazoCountData(itemType, 0);
                tazos.Add(entry);
            }

            entry.count += amount;
        }

        /// <summary>Trừ tazo khỏi kho, không cho xuống dưới 0.</summary>
        /// <param name="itemType">Loại vật phẩm.</param>
        /// <param name="amount">Số tazo bị trừ (bỏ qua nếu &lt;= 0).</param>
        public void RemoveTazos(ShopItemType itemType, int amount)
        {
            if (amount <= 0) return;

            TazoCountData entry = FindEntry(itemType);
            if (entry == null) return;

            entry.count = Mathf.Max(0, entry.count - amount);
        }

        /// <summary>
        /// Ghi đè toàn bộ kho tazo từ dữ liệu lưu trữ (dùng khi nạp save).
        /// Loại không có trong danh sách nạp vào sẽ được đưa về 0.
        /// </summary>
        /// <param name="tazoCounts">Danh sách số tazo theo loại.</param>
        public void ApplyTazoCounts(List<TazoCountData> tazoCounts)
        {
            if (tazos == null) tazos = new List<TazoCountData>();

            for (int i = 0; i < tazos.Count; i++)
            {
                if (tazos[i] != null) tazos[i].count = 0;
            }

            if (tazoCounts == null) return;

            for (int i = 0; i < tazoCounts.Count; i++)
            {
                TazoCountData saved = tazoCounts[i];
                if (saved == null || saved.ShopItemType == ShopItemType.None) continue;

                TazoCountData entry = FindEntry(saved.ShopItemType);
                if (entry == null)
                {
                    tazos.Add(new TazoCountData(saved.ShopItemType, Mathf.Max(0, saved.count)));
                    continue;
                }

                entry.count = Mathf.Max(0, saved.count);
            }
        }

        /// <summary>Số tazo đang có của một loại vật phẩm; 0 nếu loại đó chưa khai báo.</summary>
        /// <param name="itemType">Loại vật phẩm cần tra cứu.</param>
        public int GetTazoCounts(ShopItemType itemType)
        {
            TazoCountData entry = FindEntry(itemType);
            return entry != null ? entry.count : 0;
        }

        private TazoCountData FindEntry(ShopItemType itemType)
        {
            if (tazos == null) return null;

            for (int i = 0; i < tazos.Count; i++)
            {
                TazoCountData data = tazos[i];
                if (data != null && data.ShopItemType == itemType) return data;
            }
            return null;
        }
    }
}
