using System;

namespace Pickleball
{
    /// <summary>
    /// Trạng thái đã lưu của một ô locker: túi đang nằm trong ô, ô có bị khoá không
    /// và mốc thời gian mở khoá dạng chuỗi ISO round-trip ("o").
    /// <para>
    /// Bản sao phẳng của <see cref="SlotsData.SlotData"/> — cố tình không lưu trực tiếp
    /// lớp nested đó để định dạng file không phụ thuộc vào chi tiết nội bộ của asset locker.
    /// </para>
    /// </summary>
    [Serializable]
    public class SlotSaveData
    {
        /// <summary>Túi đang nằm trong ô; <see cref="KitbagType.None"/> nghĩa là ô rỗng.</summary>
        public KitbagType kitbagType = KitbagType.None;

        /// <summary>True khi ô chưa được mở khoá (cần trả gem).</summary>
        public bool isSlotLocked;

        /// <summary>Mốc thời gian mở khoá dạng ISO round-trip; rỗng nghĩa là chưa chạy đồng hồ.</summary>
        public string unlockTimeString;

        /// <summary>Khởi tạo rỗng phục vụ serialize.</summary>
        public SlotSaveData() { }

        /// <summary>Khởi tạo đầy đủ trạng thái lưu của một ô locker.</summary>
        /// <param name="kitbagType">Túi trong ô.</param>
        /// <param name="isSlotLocked">Ô có đang khoá hay không.</param>
        /// <param name="unlockTimeString">Mốc mở khoá dạng ISO round-trip.</param>
        public SlotSaveData(KitbagType kitbagType, bool isSlotLocked, string unlockTimeString)
        {
            this.kitbagType = kitbagType;
            this.isSlotLocked = isSlotLocked;
            this.unlockTimeString = unlockTimeString;
        }

        /// <summary>Chụp lại trạng thái một ô locker đang chạy. Trả về <c>null</c> nếu ô rỗng.</summary>
        /// <param name="slot">Ô locker runtime.</param>
        public static SlotSaveData From(SlotsData.SlotData slot)
        {
            if (slot == null) return null;
            return new SlotSaveData(slot.kitbagType, slot.isSlotLocked, slot.unlockTimeString);
        }

        /// <summary>Dựng lại ô locker runtime từ dữ liệu đã lưu, đã khôi phục mốc thời gian.</summary>
        public SlotsData.SlotData ToSlotData()
        {
            SlotsData.SlotData slot = new SlotsData.SlotData
            {
                kitbagType = kitbagType,
                isSlotLocked = isSlotLocked,
                unlockTimeString = unlockTimeString
            };
            slot.UpdateTimeFromStored();
            return slot;
        }
    }
}
