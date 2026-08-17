using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pickleball
{
    /// <summary>Một mục âm thanh trong <see cref="AudioData"/>: id để tra + clip + tham số phát.</summary>
    [Serializable]
    public class AudioClipData
    {
        [Tooltip("Định danh dùng khi gọi AudioManager.PlaySFX/PlayUI/PlayMusic.")]
        public string id;

        [Tooltip("Clip sẽ phát. Bỏ trống thì mục này bị bỏ qua.")]
        public AudioClip clip;

        [Tooltip("Âm lượng riêng của clip, nhân với âm lượng kênh.")]
        [Range(0f, 1f)]
        public float volume = 1f;

        [Tooltip("Phát lặp (chỉ có ý nghĩa với nhạc nền).")]
        public bool loop;
    }

    /// <summary>
    /// Bảng tra âm thanh toàn game. Tạo asset qua menu
    /// <c>Assets → Create → ScriptableObjects → AudioData</c> rồi gán vào
    /// <see cref="AudioManager"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioData", menuName = "ScriptableObjects/AudioData")]
    public class AudioData : ScriptableObject
    {
        [Tooltip("Toàn bộ âm thanh của game, tra theo id.")]
        public List<AudioClipData> clips = new List<AudioClipData>();

        /// <summary>Chỉ mục id → mục âm thanh, dựng lười ở lần tra đầu tiên.</summary>
        private Dictionary<string, AudioClipData> lookup;

        /// <summary>
        /// Tra một mục âm thanh theo id.
        /// </summary>
        /// <param name="id">Định danh cần tra.</param>
        /// <returns>Mục tương ứng, hoặc null nếu id rỗng / không tồn tại / clip chưa gán.</returns>
        public AudioClipData Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            EnsureLookup();
            return lookup.TryGetValue(id, out AudioClipData data) ? data : null;
        }

        /// <summary>Có mục âm thanh với id này không.</summary>
        /// <param name="id">Định danh cần kiểm tra.</param>
        public bool Contains(string id)
        {
            return Get(id) != null;
        }

        /// <summary>Xoá chỉ mục để lần tra sau dựng lại (gọi sau khi sửa <see cref="clips"/> lúc runtime).</summary>
        public void RebuildIndex()
        {
            lookup = null;
        }

        private void EnsureLookup()
        {
            if (lookup != null) return;

            lookup = new Dictionary<string, AudioClipData>(StringComparer.Ordinal);
            if (clips == null) return;

            for (int i = 0; i < clips.Count; i++)
            {
                AudioClipData data = clips[i];
                if (data == null || string.IsNullOrEmpty(data.id) || data.clip == null) continue;
                // Trùng id thì mục đầu tiên thắng — tránh nuốt lỗi cấu hình một cách âm thầm.
                if (lookup.ContainsKey(data.id)) continue;
                lookup[data.id] = data;
            }
        }

        private void OnDisable()
        {
            lookup = null;
        }
    }
}
