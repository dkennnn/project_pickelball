using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Trang bị người chơi đang chọn (nhân vật, cán vợt, vợt, bài tập) và chỉ số tổng hợp sinh ra từ đó.
    /// <para>
    /// Công thức chỉ số: mỗi vật phẩm góp <c>GetCurrentPropertyValue(p) * propertiesEffectiveness</c>
    /// (Character 0.5, Paddle 0.2, Workout 0.2, Grip 0.1 → tổng 1.0), cho ra giá trị thô thang 0..100.
    /// Giá trị thô sau đó được <see cref="PlayerProfileLimits.Denormalize"/> quy đổi sang đơn vị vật lý
    /// thật của gameplay.
    /// </para>
    /// <para>
    /// Các thông số không đến từ trang bị (tầm volley, tầm đánh, chiều cao, hồi/tiêu hao thể lực)
    /// lấy trực tiếp từ <see cref="Character"/> đang chọn.
    /// </para>
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerLoadout", menuName = "ScriptableObjects/PlayerLoadout")]
    public class PlayerLoadout : ScriptableObject
    {
        /// <summary>Phát sau mỗi lần chỉ số tổng được tính lại và ghi vào <see cref="profile"/>.</summary>
        public static event Action OnPlayerLoadoutUpdated;

        /// <summary>
        /// Danh mục vật phẩm theo từng loại kèm vật phẩm đang trang bị.
        /// Dùng List thay Dictionary vì Unity không serialize được Dictionary.
        /// </summary>
        public List<ShopCategoryData> categories;

        /// <summary>Hub dữ liệu để tra ví tiền và kho tazo khi mua/nâng cấp.</summary>
        [Header("References")]
        public GameData gameData;

        /// <summary>Dải min/max để quy đổi chỉ số thang 0..100 sang giá trị thật.</summary>
        public PlayerProfileLimits profileLimits;

        /// <summary>Asset chỉ số mà kết quả tổng hợp được ghi vào; gameplay đọc từ đây.</summary>
        public PlayerProfile profile;

        /// <summary>Nhân vật dự phòng khi chưa chọn nhân vật nào.</summary>
        public Character defaultCharacter;

        /// <summary>Thể lực tối đa sau quy đổi.</summary>
        [Header("Computed Stats (read-only)")]
        public float totalStamina;

        /// <summary>Độ nhanh nhẹn sau quy đổi.</summary>
        public float totalAgility;

        /// <summary>Tầm volley lấy từ nhân vật đang chọn (mét).</summary>
        public float totalVolley;

        /// <summary>Khả năng vung vợt sau quy đổi.</summary>
        public float totalSwingAbility;

        /// <summary>Khả năng tạo xoáy sau quy đổi.</summary>
        public float totalSpinAbility;

        /// <summary>Độ chính xác bám bóng sau quy đổi.</summary>
        public float totalAccuracy;

        /// <summary>Lực đánh sau quy đổi.</summary>
        public float totalShotPower;

        /// <summary>Tốc độ di chuyển sau quy đổi (m/s).</summary>
        public float totalMovementSpeed;

        /// <summary>Chiều cao lấy từ nhân vật đang chọn (mét).</summary>
        public float finalHeight;

        /// <summary>Tầm đánh lấy từ nhân vật đang chọn (mét).</summary>
        public float totalShotRange;

        /// <summary>Tốc độ hồi thể lực lấy từ nhân vật đang chọn.</summary>
        public float totalStaminaRechargeRate;

        /// <summary>Tốc độ tiêu hao thể lực lấy từ nhân vật đang chọn.</summary>
        public float totalStaminaDepletionRate;

        /// <summary>
        /// Tính lại toàn bộ chỉ số tổng từ trang bị đang chọn, ghi vào <see cref="profile"/>
        /// rồi phát <see cref="OnPlayerLoadoutUpdated"/>.
        /// Gọi sau mỗi lần đổi trang bị, mua hoặc nâng cấp vật phẩm.
        /// </summary>
        public void UpdateProfile()
        {
            totalAgility = Denormalize(PropertyType.Agility);
            totalAccuracy = Denormalize(PropertyType.Accuracy);
            totalShotPower = Denormalize(PropertyType.Power);
            totalSwingAbility = Denormalize(PropertyType.Swing);
            totalSpinAbility = Denormalize(PropertyType.Spin);
            totalMovementSpeed = Denormalize(PropertyType.Speed);
            totalStamina = Denormalize(PropertyType.Stamina);

            Character character = GetSelectedCharacter();
            if (character != null)
            {
                totalVolley = character.GetCurrentVolley();
                totalShotRange = character.GetCurrentShotRange();
                finalHeight = character.GetCurrentHeight();
                totalStaminaRechargeRate = character.GetCurrentStaminaRechargeRate();
                totalStaminaDepletionRate = character.GetCurrentStaminaDepletionRate();
            }

            if (profile != null)
            {
                profile.Agility = totalAgility;
                profile.TrackingAccuracy = totalAccuracy;
                profile.shotPower = totalShotPower;
                profile.swingAbility = totalSwingAbility;
                profile.spinAbility = totalSpinAbility;
                profile.MovementSpeed = totalMovementSpeed;
                profile.maxStamina = totalStamina;

                profile.volleyRange = totalVolley;
                profile.shotRange = totalShotRange;
                profile.height = finalHeight;
                profile.staminaRechargeRate = totalStaminaRechargeRate;
                profile.staminaDepletionRate = totalStaminaDepletionRate;
            }

            OnPlayerLoadoutUpdated?.Invoke();
        }

        /// <summary>Vật phẩm đang trang bị của một loại. Trả về <c>null</c> nếu chưa chọn.</summary>
        /// <param name="itemType">Loại vật phẩm cần lấy.</param>
        public Item GetSelectedItemByType(ShopItemType itemType)
        {
            ShopCategoryData category = GetCategory(itemType);
            return category != null ? category.selectedItem : null;
        }

        /// <summary>
        /// Trang bị một vật phẩm vào ô tương ứng rồi tính lại chỉ số.
        /// Bỏ qua nếu vật phẩm rỗng, chưa mua, hoặc loại của nó chưa có trong <see cref="categories"/>.
        /// </summary>
        /// <param name="item">Vật phẩm muốn trang bị.</param>
        public void SetSelectedItem(Item item)
        {
            if (item == null || !item.isPurchased) return;

            ShopCategoryData category = GetCategory(item.itemType);
            if (category == null) return;

            category.selectedItem = item;
            UpdateProfile();
        }

        /// <summary>
        /// Giá trị thô của một chỉ số (thang 0..100) sau khi cộng dồn 4 vật phẩm đang trang bị
        /// theo trọng số <see cref="Item.propertiesEffectiveness"/>. Chưa quy đổi qua
        /// <see cref="PlayerProfileLimits"/>.
        /// </summary>
        /// <param name="propertyType">Loại chỉ số cần tính.</param>
        public virtual float GetCurrentPropertyValue(PropertyType propertyType)
        {
            if (categories == null) return 0f;

            float total = 0f;
            for (int i = 0; i < categories.Count; i++)
            {
                ShopCategoryData category = categories[i];
                Item item = category != null ? category.selectedItem : null;
                if (item == null) continue;

                total += item.GetCurrentPropertyValue(propertyType) * item.propertiesEffectiveness;
            }
            return total;
        }

        /// <summary>
        /// Xuất chỉ số tổng cho UI (thang 0..100): giá trị hiện tại, giá trị nếu nâng toàn bộ
        /// trang bị thêm một bậc, và giá trị trần khi mọi vật phẩm đạt bậc cao nhất.
        /// Thứ tự theo <see cref="PropertiesData.properties"/> nếu có, ngược lại theo thứ tự enum.
        /// </summary>
        public List<ProfileProperty> GetPropertyValues()
        {
            List<ProfileProperty> result = new List<ProfileProperty>();

            PropertiesData propertiesData = gameData != null ? gameData.propertiesData : null;
            if (propertiesData != null && propertiesData.properties != null)
            {
                for (int i = 0; i < propertiesData.properties.Count; i++)
                {
                    PropertyData data = propertiesData.properties[i];
                    if (data == null || data.propertyType == PropertyType.None) continue;
                    result.Add(MakeProfileProperty(data.propertyType));
                }
                return result;
            }

            foreach (PropertyType propertyType in (PropertyType[])Enum.GetValues(typeof(PropertyType)))
            {
                if (propertyType == PropertyType.None) continue;
                result.Add(MakeProfileProperty(propertyType));
            }
            return result;
        }

        /// <summary>Nhân vật đang trang bị, lùi về <see cref="defaultCharacter"/> nếu chưa chọn.</summary>
        private Character GetSelectedCharacter()
        {
            Character character = GetSelectedItemByType(ShopItemType.Character) as Character;
            return character != null ? character : defaultCharacter;
        }

        /// <summary>Quy đổi chỉ số thô sang giá trị thật; trả về giá trị thô nếu thiếu bảng giới hạn.</summary>
        private float Denormalize(PropertyType propertyType)
        {
            float raw = GetCurrentPropertyValue(propertyType);
            return profileLimits != null ? profileLimits.Denormalize(propertyType, raw) : raw;
        }

        /// <summary>Tổng hợp giá trị hiện tại / bậc kế tiếp / bậc tối đa của một chỉ số.</summary>
        private ProfileProperty MakeProfileProperty(PropertyType propertyType)
        {
            float current = 0f;
            float next = 0f;
            float max = 0f;

            if (categories != null)
            {
                for (int i = 0; i < categories.Count; i++)
                {
                    ShopCategoryData category = categories[i];
                    Item item = category != null ? category.selectedItem : null;
                    if (item == null) continue;

                    PropertyLevelValues values = item.GetProperty(propertyType);
                    if (values == null || values.value == null || values.value.Length == 0) continue;

                    float effectiveness = item.propertiesEffectiveness;
                    int lastIndex = values.value.Length - 1;
                    int levelIndex = Mathf.Clamp(item.currentLevel, 0, lastIndex);
                    int nextIndex = Mathf.Clamp(item.currentLevel + 1, 0, lastIndex);

                    current += values.value[levelIndex] * effectiveness;
                    next += values.value[nextIndex] * effectiveness;
                    max += values.value[lastIndex] * effectiveness;
                }
            }

            return new ProfileProperty(propertyType, current, next, max);
        }

        private ShopCategoryData GetCategory(ShopItemType itemType)
        {
            if (categories == null) return null;

            for (int i = 0; i < categories.Count; i++)
            {
                ShopCategoryData category = categories[i];
                if (category != null && category.categoryType == itemType) return category;
            }
            return null;
        }
    }
}
