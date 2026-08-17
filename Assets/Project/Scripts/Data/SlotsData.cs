using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Locker: các ô chứa túi đồ người chơi nhận được sau trận.
    /// Mỗi túi bỏ vào ô sẽ chạy đồng hồ mở khoá; hết giờ mới mở được,
    /// hoặc trả gem để bỏ qua đồng hồ. Ô ngoài số ô mặc định phải mở khoá bằng gem.
    /// </summary>
    [CreateAssetMenu(fileName = "SlotsData", menuName = "ScriptableObjects/SlotsData")]
    public class SlotsData : ScriptableObject
    {
        /// <summary>Dữ liệu người chơi dùng để trừ gem khi mở khoá/bỏ qua đồng hồ.</summary>
        public GameData gameData;

        /// <summary>Tổng số ô trong locker.</summary>
        public int maxSlots = 8;

        /// <summary>Số ô được mở sẵn từ đầu; các ô còn lại phải mở bằng gem.</summary>
        public int defaultUnlockedSlots = 4;

        /// <summary>Bắn ra mỗi khi trạng thái locker đổi để UI vẽ lại.</summary>
        public event Action OnSlotsChanged;

        /// <summary>Thời gian mở khoá và giá bỏ qua đồng hồ theo từng bậc túi.</summary>
        [Header("Kitbag Settings")]
        public List<KitbagSettings> kitbagSettings;

        /// <summary>Giá gem để mở vĩnh viễn một ô đang khoá.</summary>
        [Header("Slot Settings")]
        public int gemsToUnlockSlot = 150;

        /// <summary>Trạng thái từng ô trong locker.</summary>
        public SlotData[] slots;

        /// <summary>Số trận thắng liên tiếp để được nhận túi bậc cao hơn.</summary>
        public const int winsForHigherKitbag = 5;

        /// <summary>Cấu hình đồng hồ mở khoá cho một bậc túi.</summary>
        [Serializable]
        public class KitbagSettings
        {
            /// <summary>Bậc túi áp dụng.</summary>
            public KitbagType kitbagType;

            /// <summary>Thời gian chờ mở khoá, tính bằng giây.</summary>
            public float unlockTimeInSeconds;

            /// <summary>Số gem để bỏ qua đồng hồ chờ.</summary>
            public int gemsToSkipTimer;
        }

        /// <summary>Trạng thái của một ô trong locker.</summary>
        [Serializable]
        public class SlotData
        {
            /// <summary>Túi đang nằm trong ô; <see cref="KitbagType.None"/> nghĩa là ô rỗng.</summary>
            public KitbagType kitbagType = KitbagType.None;

            /// <summary>True khi ô chưa được mở khoá (cần trả gem).</summary>
            public bool isSlotLocked;

            /// <summary>
            /// Mốc thời gian UTC túi trong ô sẵn sàng mở.
            /// Unity không serialize được <see cref="DateTime"/> nên giá trị thật lưu ở
            /// <see cref="unlockTimeString"/>.
            /// </summary>
            [NonSerialized]
            public DateTime unlockTime;

            /// <summary>Bản chuỗi ISO round-trip ("o") của <see cref="unlockTime"/> để lưu/đồng bộ.</summary>
            public string unlockTimeString;

            /// <summary>Ghi <see cref="unlockTime"/> xuống chuỗi lưu trữ.</summary>
            public void UpdateTimeToStored()
            {
                unlockTimeString = unlockTime.ToString("o", CultureInfo.InvariantCulture);
            }

            /// <summary>Đọc <see cref="unlockTime"/> lên từ chuỗi lưu trữ.</summary>
            public void UpdateTimeFromStored()
            {
                if (string.IsNullOrEmpty(unlockTimeString))
                {
                    unlockTime = DateTime.MinValue;
                    return;
                }

                DateTime parsed;
                bool ok = DateTime.TryParse(
                    unlockTimeString,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out parsed);

                unlockTime = ok ? parsed : DateTime.MinValue;
            }
        }

        private void OnEnable()
        {
            EnsureSlots();
            RestoreTimes();
        }

        /// <summary>Xoá sạch locker: mọi ô rỗng, chỉ mở sẵn <see cref="defaultUnlockedSlots"/> ô đầu.</summary>
        public void ResetSlots()
        {
            if (maxSlots < 1) maxSlots = 1;
            slots = new SlotData[maxSlots];

            for (int i = 0; i < maxSlots; i++)
            {
                SlotData slot = new SlotData();
                slot.kitbagType = KitbagType.None;
                slot.isSlotLocked = i >= defaultUnlockedSlots;
                slot.unlockTime = DateTime.MinValue;
                slot.UpdateTimeToStored();
                slots[i] = slot;
            }

            NotifySlotsChanged();
        }

        /// <summary>
        /// Bỏ một túi vào ô trống đã mở khoá đầu tiên và bắt đầu đồng hồ chờ.
        /// Trả về <c>false</c> khi locker đã đầy hoặc bậc túi không hợp lệ.
        /// </summary>
        /// <param name="type">Bậc túi cần cất.</param>
        public bool FillSlot(KitbagType type)
        {
            if (type == KitbagType.None) return false;

            EnsureSlots();

            int index = GetEmptyUnlockedSlot();
            if (index < 0) return false;

            SlotData slot = slots[index];
            slot.kitbagType = type;

            // TODO: đổi sang server time khi có backend
            slot.unlockTime = DateTime.UtcNow.AddSeconds(GetUnlockTimeForKitbag(type));
            slot.UpdateTimeToStored();

            NotifySlotsChanged();
            return true;
        }

        /// <summary>
        /// Mở túi trong ô nếu đồng hồ đã hết và trả về bậc túi vừa mở, đồng thời làm rỗng ô.
        /// Trả về <see cref="KitbagType.None"/> nếu ô rỗng, đang khoá hoặc chưa hết giờ.
        /// </summary>
        /// <param name="index">Chỉ số ô trong locker.</param>
        public KitbagType OpenSlot(int index)
        {
            if (!IsValidIndex(index)) return KitbagType.None;

            SlotData slot = slots[index];
            if (slot.isSlotLocked || slot.kitbagType == KitbagType.None) return KitbagType.None;
            if (GetRemainingUnlockTime(index) > TimeSpan.Zero) return KitbagType.None;

            KitbagType opened = slot.kitbagType;
            slot.kitbagType = KitbagType.None;
            slot.unlockTime = DateTime.MinValue;
            slot.UpdateTimeToStored();

            NotifySlotsChanged();
            return opened;
        }

        /// <summary>
        /// Mở vĩnh viễn một ô đang khoá bằng gem.
        /// Trả về <c>false</c> khi ô không khoá hoặc không đủ gem.
        /// </summary>
        /// <param name="index">Chỉ số ô trong locker.</param>
        public bool UnlockSlotWithGems(int index)
        {
            if (!IsValidIndex(index)) return false;

            SlotData slot = slots[index];
            if (!slot.isSlotLocked) return false;
            if (gameData == null || !gameData.TrySpendGems(gemsToUnlockSlot)) return false;

            slot.isSlotLocked = false;
            NotifySlotsChanged();
            return true;
        }

        /// <summary>
        /// Trả gem để bỏ qua đồng hồ chờ của túi trong ô.
        /// Trả về <c>false</c> khi ô rỗng, đã sẵn sàng hoặc không đủ gem.
        /// </summary>
        /// <param name="index">Chỉ số ô trong locker.</param>
        public bool SkipUnlockTimerWithGems(int index)
        {
            if (!IsValidIndex(index)) return false;

            SlotData slot = slots[index];
            if (slot.isSlotLocked || slot.kitbagType == KitbagType.None) return false;
            if (GetRemainingUnlockTime(index) <= TimeSpan.Zero) return false;

            int cost = GetGemsToSkipTimer(slot.kitbagType);
            if (gameData == null || !gameData.TrySpendGems(cost)) return false;

            // TODO: đổi sang server time khi có backend
            slot.unlockTime = DateTime.UtcNow;
            slot.UpdateTimeToStored();

            NotifySlotsChanged();
            return true;
        }

        /// <summary>
        /// Thời gian còn lại trước khi túi trong ô mở được.
        /// Trả về <see cref="TimeSpan.Zero"/> khi ô rỗng hoặc đã sẵn sàng.
        /// </summary>
        /// <param name="index">Chỉ số ô trong locker.</param>
        public TimeSpan GetRemainingUnlockTime(int index)
        {
            if (!IsValidIndex(index)) return TimeSpan.Zero;

            SlotData slot = slots[index];
            if (slot.kitbagType == KitbagType.None) return TimeSpan.Zero;

            // TODO: đổi sang server time khi có backend
            TimeSpan remaining = slot.unlockTime - DateTime.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }

        /// <summary>Giá gem để bỏ qua đồng hồ của một bậc túi. Trả về 0 nếu chưa cấu hình.</summary>
        /// <param name="kitbagType">Bậc túi cần tra cứu.</param>
        public int GetGemsToSkipTimer(KitbagType kitbagType)
        {
            KitbagSettings settings = FindSettings(kitbagType);
            return settings != null ? settings.gemsToSkipTimer : 0;
        }

        /// <summary>True khi còn ít nhất một ô đã mở khoá đang rỗng.</summary>
        public bool IsSlotEmpty()
        {
            return GetEmptyUnlockedSlot() >= 0;
        }

        /// <summary>True khi có ít nhất một túi đã hết giờ chờ và sẵn sàng mở.</summary>
        public bool HasReadyToOpenBags()
        {
            EnsureSlots();

            for (int i = 0; i < slots.Length; i++)
            {
                SlotData slot = slots[i];
                if (slot == null || slot.isSlotLocked) continue;
                if (slot.kitbagType == KitbagType.None) continue;
                if (GetRemainingUnlockTime(i) <= TimeSpan.Zero) return true;
            }
            return false;
        }

        /// <summary>
        /// Nạp trạng thái locker từ dữ liệu đã lưu. Mảng nạp vào được cắt/đệm
        /// cho khớp <see cref="maxSlots"/> và mốc thời gian được khôi phục từ chuỗi ISO.
        /// </summary>
        /// <param name="slotsData">Trạng thái các ô đã lưu.</param>
        public void ApplySlotsData(SlotData[] slotsData)
        {
            if (slotsData == null)
            {
                ResetSlots();
                return;
            }

            if (maxSlots < 1) maxSlots = 1;
            SlotData[] applied = new SlotData[maxSlots];

            for (int i = 0; i < maxSlots; i++)
            {
                SlotData source = i < slotsData.Length ? slotsData[i] : null;
                if (source == null)
                {
                    source = new SlotData();
                    source.kitbagType = KitbagType.None;
                    source.isSlotLocked = i >= defaultUnlockedSlots;
                    source.UpdateTimeToStored();
                }

                source.UpdateTimeFromStored();
                applied[i] = source;
            }

            slots = applied;
            NotifySlotsChanged();
        }

        /// <summary>Tạo mảng ô nếu chưa có hoặc sai kích thước.</summary>
        private void EnsureSlots()
        {
            if (maxSlots < 1) maxSlots = 1;

            if (slots == null || slots.Length != maxSlots)
            {
                ResetSlots();
                return;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) slots[i] = new SlotData();
            }
        }

        /// <summary>Khôi phục mốc thời gian của mọi ô từ chuỗi lưu trữ.</summary>
        private void RestoreTimes()
        {
            if (slots == null) return;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null) slots[i].UpdateTimeFromStored();
            }
        }

        /// <summary>Chỉ số ô đã mở khoá và đang rỗng đầu tiên; -1 nếu không còn.</summary>
        private int GetEmptyUnlockedSlot()
        {
            EnsureSlots();

            for (int i = 0; i < slots.Length; i++)
            {
                SlotData slot = slots[i];
                if (slot == null || slot.isSlotLocked) continue;
                if (slot.kitbagType == KitbagType.None) return i;
            }
            return -1;
        }

        /// <summary>Thời gian chờ (giây) của một bậc túi. Trả về 0 nếu chưa cấu hình.</summary>
        private float GetUnlockTimeForKitbag(KitbagType kitbagType)
        {
            KitbagSettings settings = FindSettings(kitbagType);
            return settings != null ? settings.unlockTimeInSeconds : 0f;
        }

        private KitbagSettings FindSettings(KitbagType kitbagType)
        {
            if (kitbagSettings == null) return null;

            for (int i = 0; i < kitbagSettings.Count; i++)
            {
                KitbagSettings settings = kitbagSettings[i];
                if (settings != null && settings.kitbagType == kitbagType) return settings;
            }
            return null;
        }

        private bool IsValidIndex(int index)
        {
            EnsureSlots();
            return index >= 0 && index < slots.Length && slots[index] != null;
        }

        private void NotifySlotsChanged()
        {
            Action handler = OnSlotsChanged;
            if (handler != null) handler.Invoke();
        }
    }
}
