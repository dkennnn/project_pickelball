using System;
using System.Collections.Generic;
using StarterKit.Utilities;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Nguồn sự thật duy nhất cho trạng thái theme sự kiện (Halloween, Giáng sinh…).
    /// Mọi <see cref="ThemeObject"/> nghe <see cref="OnThemeChanged"/> để tự đổi mặt.
    /// <para>
    /// LƯU Ý: bản gốc đọc cờ từ <c>GameData</c> / RemoteConfig, nhưng <c>Data/GameData.cs</c> ở bản
    /// dựng lại KHÔNG có field <c>isEventOngoing</c> và không được phép sửa. Vì vậy manager tự giữ
    /// cờ <see cref="isEventOngoing"/>. Khi RemoteConfig được dựng lại, chỉ cần gọi
    /// <see cref="SetEventOngoing"/> từ nơi nạp config là toàn bộ ThemeObject cập nhật theo.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public class ThemeManager : Singleton<ThemeManager>
    {
        [Tooltip("Sự kiện có đang diễn ra không. Đổi lúc runtime nên gọi SetEventOngoing để phát event.")]
        public bool isEventOngoing;

        [Tooltip("Định danh theme đang bật (ví dụ \"halloween\"). Chỉ mang tính thông tin.")]
        public string currentThemeId = string.Empty;

        /// <summary>
        /// Phát mỗi khi trạng thái theme đổi. Static để <see cref="ThemeObject"/> đăng ký được
        /// ngay cả khi chưa có manager trong scene.
        /// </summary>
        public static event Action<bool> OnThemeChanged;

        /// <summary>Theme sự kiện có đang bật không.</summary>
        public bool IsThemeEnabled => isEventOngoing;

        /// <summary>Các ThemeObject đã đăng ký thủ công (dùng khi cần ép cập nhật đồng loạt).</summary>
        private readonly HashSet<ThemeObject> registeredThemeObjects = new HashSet<ThemeObject>();

        /// <inheritdoc/>
        protected override void OnAwake()
        {
            base.OnAwake();
            // Phát ngay trạng thái khởi tạo cho mọi listener đã đăng ký trước đó.
            OnThemeChanged?.Invoke(isEventOngoing);
        }

        /// <summary>
        /// Đặt trạng thái sự kiện và phát <see cref="OnThemeChanged"/> nếu có thay đổi.
        /// </summary>
        /// <param name="ongoing">True = sự kiện đang diễn ra.</param>
        /// <param name="force">True = phát event kể cả khi trạng thái không đổi.</param>
        public void SetEventOngoing(bool ongoing, bool force = false)
        {
            if (!force && isEventOngoing == ongoing) return;

            isEventOngoing = ongoing;
            OnThemeChanged?.Invoke(isEventOngoing);
            UpdateAllThemeObjects();
        }

        /// <summary>Phát lại trạng thái hiện tại cho mọi listener (dùng sau khi load scene mới).</summary>
        public void RefreshThemeState()
        {
            OnThemeChanged?.Invoke(isEventOngoing);
            UpdateAllThemeObjects();
        }

        /// <summary>Đăng ký một ThemeObject để manager cập nhật trực tiếp (không bắt buộc).</summary>
        /// <param name="themeObject">Object cần đăng ký; null thì bỏ qua.</param>
        public void RegisterThemeObject(ThemeObject themeObject)
        {
            if (themeObject == null) return;
            if (!registeredThemeObjects.Add(themeObject)) return;
            themeObject.ApplyTheme(isEventOngoing);
        }

        /// <summary>Huỷ đăng ký một ThemeObject.</summary>
        /// <param name="themeObject">Object cần huỷ; null thì bỏ qua.</param>
        public void UnregisterThemeObject(ThemeObject themeObject)
        {
            if (themeObject == null) return;
            registeredThemeObjects.Remove(themeObject);
        }

        private void UpdateAllThemeObjects()
        {
            registeredThemeObjects.RemoveWhere(o => o == null);
            foreach (ThemeObject themeObject in registeredThemeObjects)
            {
                themeObject.ApplyTheme(isEventOngoing);
            }
        }

        /// <inheritdoc/>
        protected override void OnDestroy()
        {
            registeredThemeObjects.Clear();
            base.OnDestroy();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Bật/tắt cờ ngay trên Inspector lúc Play Mode vẫn phát được event.
            if (!Application.isPlaying) return;
            OnThemeChanged?.Invoke(isEventOngoing);
        }
#endif
    }
}
