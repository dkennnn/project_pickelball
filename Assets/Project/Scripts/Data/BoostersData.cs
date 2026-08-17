using System.Collections.Generic;
using UnityEngine;

namespace Pickleball.Data
{
    /// <summary>
    /// Kho booster của người chơi kèm ảnh biểu tượng cho UI.
    /// Chỉ quản lý số lượng; hiệu ứng in-match do BoosterManager xử lý.
    /// </summary>
    [CreateAssetMenu(fileName = "BoostersData", menuName = "ScriptableObjects/BoostersData")]
    public class BoostersData : ScriptableObject
    {
        /// <summary>Số lượng booster đang sở hữu theo từng loại.</summary>
        public List<BoosterCountData> boosters;

        /// <summary>Ảnh biểu tượng UI theo từng loại booster.</summary>
        public List<BoosterImageData> boosterImages;

        /// <summary>Lấy số lượng booster đang có. Trả về 0 nếu loại đó chưa khai báo.</summary>
        /// <param name="boosterType">Loại booster cần tra cứu.</param>
        public int GetCount(BoosterType boosterType)
        {
            BoosterCountData entry = FindEntry(boosterType);
            return entry != null ? entry.count : 0;
        }

        /// <summary>
        /// Cộng thêm booster vào kho. Nếu loại đó chưa có trong danh sách sẽ được tạo mới.
        /// Số lượng không bao giờ xuống dưới 0.
        /// </summary>
        /// <param name="boosterType">Loại booster.</param>
        /// <param name="amount">Số lượng cộng thêm (có thể âm để trừ).</param>
        public void Add(BoosterType boosterType, int amount)
        {
            if (boosterType == BoosterType.None) return;

            BoosterCountData entry = FindEntry(boosterType);
            if (entry == null)
            {
                if (boosters == null) boosters = new List<BoosterCountData>();
                entry = new BoosterCountData { boosterType = boosterType, count = 0 };
                boosters.Add(entry);
            }

            entry.count = Mathf.Max(0, entry.count + amount);
        }

        /// <summary>
        /// Ghi đè toàn bộ kho booster bằng dữ liệu đã lưu.
        /// Đối xứng với <see cref="TazoData.ApplyTazoCounts"/>; hệ save dùng hàm này khi nạp file.
        /// </summary>
        /// <param name="savedCounts">Danh sách đã lưu; <c>null</c> thì giữ nguyên kho hiện tại.</param>
        public void ApplyBoosterCounts(List<BoosterCountData> savedCounts)
        {
            if (savedCounts == null) return;

            if (boosters == null) boosters = new List<BoosterCountData>();
            for (int i = 0; i < boosters.Count; i++) boosters[i].count = 0;

            for (int i = 0; i < savedCounts.Count; i++)
            {
                BoosterCountData saved = savedCounts[i];
                if (saved == null || saved.boosterType == BoosterType.None) continue;

                BoosterCountData entry = FindEntry(saved.boosterType);
                if (entry == null)
                {
                    entry = new BoosterCountData { boosterType = saved.boosterType };
                    boosters.Add(entry);
                }
                entry.count = Mathf.Max(0, saved.count);
            }
        }

        /// <summary>
        /// Tiêu thụ booster nếu đủ số lượng.
        /// Trả về <c>true</c> và trừ kho khi thành công, <c>false</c> khi không đủ.
        /// </summary>
        /// <param name="boosterType">Loại booster.</param>
        /// <param name="amount">Số lượng cần dùng, mặc định 1.</param>
        public bool TryConsume(BoosterType boosterType, int amount = 1)
        {
            if (amount <= 0) return false;

            BoosterCountData entry = FindEntry(boosterType);
            if (entry == null || entry.count < amount) return false;

            entry.count -= amount;
            return true;
        }

        /// <summary>Lấy ảnh biểu tượng của một loại booster. Trả về <c>null</c> nếu chưa gán.</summary>
        /// <param name="boosterType">Loại booster cần lấy ảnh.</param>
        public Sprite GetImage(BoosterType boosterType)
        {
            if (boosterImages == null) return null;

            for (int i = 0; i < boosterImages.Count; i++)
            {
                BoosterImageData data = boosterImages[i];
                if (data != null && data.boosterType == boosterType) return data.image;
            }
            return null;
        }

        private BoosterCountData FindEntry(BoosterType boosterType)
        {
            if (boosters == null) return null;

            for (int i = 0; i < boosters.Count; i++)
            {
                BoosterCountData data = boosters[i];
                if (data != null && data.boosterType == boosterType) return data;
            }
            return null;
        }
    }
}
