using System.Collections.Generic;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Vật phẩm shop có thể mua và nâng cấp (nhân vật, cán vợt, vợt, bài tập).
    /// <para>
    /// <see cref="currentLevel"/> chạy từ 0 tới <see cref="maxLevel"/> (mặc định 4 → 5 bậc).
    /// Các mảng <see cref="tazosRequired"/>, <see cref="gemsRequired"/>, <see cref="coinsRequired"/>
    /// và <see cref="PropertyLevelValues.value"/> đều dài 5, index bằng <see cref="currentLevel"/>:
    /// phần tử thứ <c>currentLevel</c> là chi phí để lên bậc kế tiếp.
    /// </para>
    /// <para>
    /// Chỉ số của item nằm trên thang 0..100 và được nhân với
    /// <see cref="propertiesEffectiveness"/> khi <see cref="PlayerLoadout"/> cộng dồn
    /// (Character 0.5, Paddle 0.2, Workout 0.2, Grip 0.1 → tổng 1.0).
    /// </para>
    /// </summary>
    public class Item : ScriptableObject
    {
        /// <summary>Loại vật phẩm, quyết định kho tazo và ô loadout tương ứng.</summary>
        public ShopItemType itemType;

        /// <summary>Tên hiển thị của vật phẩm.</summary>
        public string itemName;

        /// <summary>Ảnh hiển thị trong shop.</summary>
        public Sprite itemSprite;

        /// <summary>Bậc nâng cấp hiện tại (0..<see cref="maxLevel"/>).</summary>
        public int currentLevel;

        /// <summary>Bậc nâng cấp cao nhất (bản gốc luôn là 4).</summary>
        public int maxLevel;

        /// <summary>Người chơi đã sở hữu vật phẩm này hay chưa.</summary>
        public bool isPurchased;

        /// <summary>Vật phẩm còn được bày bán/sử dụng hay không.</summary>
        public bool isActive;

        /// <summary>Giá mua bằng coin (0 nghĩa là vật phẩm mặc định, có sẵn).</summary>
        public int purchaseCoins;

        /// <summary>Trọng số đóng góp của vật phẩm vào chỉ số tổng của người chơi (0..1).</summary>
        [Range(0f, 1f)]
        public float propertiesEffectiveness;

        /// <summary>Số tazo cần cho từng bậc nâng cấp.</summary>
        public int[] tazosRequired;

        /// <summary>Số gem cần cho từng bậc nâng cấp.</summary>
        public int[] gemsRequired;

        /// <summary>Số coin cần cho từng bậc nâng cấp.</summary>
        public int[] coinsRequired;

        /// <summary>Bảng chỉ số của vật phẩm theo từng bậc nâng cấp.</summary>
        public List<PropertyLevelValues> propertyValues;

        /// <summary>Đã sở hữu và chưa đạt bậc tối đa, tức về nguyên tắc còn nâng cấp được.</summary>
        /// <param name="gameData">Ví tiền và kho tazo của người chơi (không bắt buộc dùng ở đây).</param>
        public bool CanBeUpgrade(GameData gameData)
        {
            return isPurchased && currentLevel < maxLevel;
        }

        /// <summary>Có đủ tazo trong kho để nâng cấp thêm một bậc hay không.</summary>
        /// <param name="gameData">Nguồn dữ liệu chứa <see cref="TazoData"/>.</param>
        public bool CanUpgradeWithTazos(GameData gameData)
        {
            if (gameData == null || gameData.tazoData == null) return false;
            if (!CanBeUpgrade(gameData)) return false;

            int required = GetCost(tazosRequired);
            return required > 0 && gameData.tazoData.CanUpgradeWithTazos(this, required);
        }

        /// <summary>Có đủ gem để nâng cấp thêm một bậc hay không.</summary>
        /// <param name="gameData">Nguồn dữ liệu chứa số gem hiện có.</param>
        public bool CanUpgradeWithGems(GameData gameData)
        {
            if (gameData == null) return false;
            if (!CanBeUpgrade(gameData)) return false;

            int required = GetCost(gemsRequired);
            return required > 0 && gameData.totalGems >= required;
        }

        /// <summary>Có đủ coin để nâng cấp thêm một bậc hay không.</summary>
        /// <param name="gameData">Nguồn dữ liệu chứa số coin hiện có.</param>
        public bool CanUpgradeWithCoins(GameData gameData)
        {
            if (gameData == null) return false;
            if (!CanBeUpgrade(gameData)) return false;

            int required = GetCost(coinsRequired);
            return required > 0 && gameData.totalCoins >= required;
        }

        /// <summary>Chưa sở hữu và đủ coin để mua vật phẩm.</summary>
        /// <param name="gameData">Nguồn dữ liệu chứa số coin hiện có.</param>
        public bool CanPurchase(GameData gameData)
        {
            if (gameData == null || isPurchased) return false;
            return gameData.totalCoins >= purchaseCoins;
        }

        /// <summary>
        /// Nâng cấp một bậc bằng tazo: trừ tazo trong kho rồi tăng <see cref="currentLevel"/>.
        /// Không làm gì nếu điều kiện chưa thoả.
        /// </summary>
        /// <param name="gameData">Nguồn dữ liệu chứa kho tazo.</param>
        public virtual void UpgradeWithTazos(GameData gameData)
        {
            if (!CanUpgradeWithTazos(gameData)) return;

            gameData.tazoData.UseTazos(this, GetCost(tazosRequired));
            currentLevel++;
        }

        /// <summary>
        /// Nâng cấp một bậc bằng gem: trừ gem rồi tăng <see cref="currentLevel"/>.
        /// Không làm gì nếu điều kiện chưa thoả.
        /// </summary>
        /// <param name="gameData">Nguồn dữ liệu chứa số gem.</param>
        public virtual void UpgradeWithGems(GameData gameData)
        {
            if (!CanUpgradeWithGems(gameData)) return;

            gameData.ReduceGems(GetCost(gemsRequired));
            currentLevel++;
        }

        /// <summary>Giá trị của một chỉ số tại bậc hiện tại (thang 0..100, chưa nhân trọng số).</summary>
        /// <param name="propertyType">Loại chỉ số cần lấy.</param>
        public virtual float GetCurrentPropertyValue(PropertyType propertyType)
        {
            return GetCurrentLevelValue(GetProperty(propertyType));
        }

        /// <summary>Tìm bảng giá trị của một chỉ số. Trả về <c>null</c> nếu item không có chỉ số đó.</summary>
        /// <param name="propertyType">Loại chỉ số cần tìm.</param>
        public PropertyLevelValues GetProperty(PropertyType propertyType)
        {
            if (propertyValues == null) return null;

            for (int i = 0; i < propertyValues.Count; i++)
            {
                PropertyLevelValues data = propertyValues[i];
                if (data != null && data.propertyType == propertyType) return data;
            }
            return null;
        }

        /// <summary>Đọc giá trị ứng với bậc hiện tại trong một bảng chỉ số. Trả về 0 nếu bảng rỗng.</summary>
        /// <param name="property">Bảng giá trị theo bậc.</param>
        public float GetCurrentLevelValue(PropertyLevelValues property)
        {
            return GetLevelValue(property, currentLevel);
        }

        /// <summary>
        /// Xuất toàn bộ chỉ số của item cho UI: giá trị hiện tại, giá trị bậc kế tiếp và giá trị trần.
        /// Thứ tự theo <see cref="PropertiesData.properties"/> nếu <paramref name="gameData"/> có
        /// cấu hình, ngược lại theo thứ tự khai báo trong <see cref="propertyValues"/>.
        /// </summary>
        /// <param name="gameData">Nguồn dữ liệu chứa <see cref="PropertiesData"/> để quyết định thứ tự.</param>
        public List<ProfileProperty> GetPropertyValues(GameData gameData)
        {
            List<ProfileProperty> result = new List<ProfileProperty>();
            if (propertyValues == null) return result;

            PropertiesData propertiesData = gameData != null ? gameData.propertiesData : null;
            if (propertiesData != null && propertiesData.properties != null)
            {
                for (int i = 0; i < propertiesData.properties.Count; i++)
                {
                    PropertyData data = propertiesData.properties[i];
                    if (data == null) continue;

                    PropertyLevelValues values = GetProperty(data.propertyType);
                    if (values != null) result.Add(MakeProfileProperty(values));
                }
                return result;
            }

            for (int i = 0; i < propertyValues.Count; i++)
            {
                PropertyLevelValues values = propertyValues[i];
                if (values != null) result.Add(MakeProfileProperty(values));
            }
            return result;
        }

        /// <summary>Chi phí nâng cấp bậc kế tiếp trong một mảng chi phí; 0 nếu thiếu dữ liệu.</summary>
        private int GetCost(int[] costs)
        {
            if (costs == null || costs.Length == 0) return 0;
            if (currentLevel < 0 || currentLevel >= costs.Length) return 0;
            return costs[currentLevel];
        }

        /// <summary>Đọc giá trị chỉ số tại một bậc bất kỳ, có kẹp biên. Trả về 0 nếu bảng rỗng.</summary>
        private static float GetLevelValue(PropertyLevelValues property, int level)
        {
            if (property == null || property.value == null || property.value.Length == 0) return 0f;
            return property.value[Mathf.Clamp(level, 0, property.value.Length - 1)];
        }

        private ProfileProperty MakeProfileProperty(PropertyLevelValues values)
        {
            float current = GetLevelValue(values, currentLevel);
            float next = currentLevel < maxLevel ? GetLevelValue(values, currentLevel + 1) : current;
            float max = values.value != null && values.value.Length > 0
                ? values.value[values.value.Length - 1]
                : 0f;

            return new ProfileProperty(values.propertyType, current, next, max);
        }
    }
}
