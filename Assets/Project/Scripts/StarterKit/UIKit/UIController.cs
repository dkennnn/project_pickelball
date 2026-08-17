using System;
using System.Collections;
using System.Collections.Generic;
using Pickleball;
using StarterKit.Utilities;
using UnityEngine;

namespace StarterKit.UIKit
{
    /// <summary>
    /// Bộ điều phối UI toàn cục: giữ sổ đăng ký màn hình, quản lý ngăn xếp hiển thị và
    /// xử lý phím Back/Escape.
    /// <para>
    /// Quy tắc ngăn xếp: màn hình thường thay thế màn hình thường đang đứng trên cùng
    /// (màn cũ bị ẩn), còn popup (<see cref="UIScreenBase.IsPopup"/> = true) chồng lên
    /// mà KHÔNG ẩn màn hình phía dưới.
    /// </para>
    /// </summary>
    public class UIController : IndestructibleSingleton<UIController>
    {
        /// <summary>Màn hình mở đầu tiên sau khi mọi màn hình đã đăng ký xong.</summary>
        [SerializeField] private ScreenType startScreen = ScreenType.MainMenu;

        /// <summary>Cấu hình popup/back cho từng màn hình; không bắt buộc.</summary>
        [SerializeField] private UIConfigration configuration;

        /// <summary>Bật xử lý phím Escape / nút Back của Android.</summary>
        [SerializeField] private bool handleBackKey = true;

        /// <summary>Phát sau khi một màn hình được hiện.</summary>
        public static event Action<ScreenType> OnScreenShown;

        /// <summary>Phát sau khi một màn hình bị ẩn.</summary>
        public static event Action<ScreenType> OnScreenHidden;

        /// <summary>Màn hình đang ở trên cùng ngăn xếp; <see cref="ScreenType.None"/> nếu trống.</summary>
        public ScreenType CurrentScreen
        {
            get { return stack.Count > 0 ? stack[stack.Count - 1] : ScreenType.None; }
        }

        /// <summary>Cấu hình popup/back đang dùng (có thể null).</summary>
        public UIConfigration Configuration => configuration;

        private readonly Dictionary<ScreenType, UIScreenBase> screens = new Dictionary<ScreenType, UIScreenBase>();
        private readonly List<ScreenType> stack = new List<ScreenType>();

        private bool startScreenShown;

        protected override void OnAwake()
        {
            base.OnAwake();
            StartCoroutine(ShowStartScreenRoutine());
        }

        protected override void OnDestroy()
        {
            screens.Clear();
            stack.Clear();
            base.OnDestroy();
        }

        private void Update()
        {
            if (!handleBackKey) return;

            // activeInputHandler = 0 (Input Manager cũ): Escape cũng chính là nút Back của Android.
            if (Input.GetKeyDown(KeyCode.Escape)) Back();
        }

        // ------------------------------------------------------------------
        // Đăng ký
        // ------------------------------------------------------------------

        /// <summary>Ghi một màn hình vào sổ đăng ký. Màn hình tự gọi hàm này trong Awake.</summary>
        /// <param name="screen">Màn hình cần đăng ký; bỏ qua nếu null.</param>
        public void Register(UIScreenBase screen)
        {
            if (screen == null) return;

            ApplyConfiguration(screen);

            if (screens.TryGetValue(screen.screenType, out UIScreenBase existing) && existing != null && existing != screen)
            {
                Debug.LogWarning(
                    $"[UIController] Đã có màn hình '{existing.name}' giữ ScreenType.{screen.screenType}; " +
                    $"'{screen.name}' sẽ thay thế.", screen);
            }

            screens[screen.screenType] = screen;
        }

        /// <summary>Bỏ một màn hình khỏi sổ đăng ký.</summary>
        /// <param name="screen">Màn hình cần gỡ; bỏ qua nếu null.</param>
        public void Unregister(UIScreenBase screen)
        {
            if (screen == null) return;
            if (screens.TryGetValue(screen.screenType, out UIScreenBase existing) && existing == screen)
            {
                screens.Remove(screen.screenType);
            }
            stack.Remove(screen.screenType);
        }

        // ------------------------------------------------------------------
        // Hiện / ẩn
        // ------------------------------------------------------------------

        /// <summary>
        /// Hiện một màn hình. Nếu là popup thì chồng lên; nếu là màn thường thì ẩn màn
        /// thường đang hiển thị (và mọi popup của nó).
        /// </summary>
        /// <param name="screenType">Màn hình cần hiện.</param>
        /// <param name="data">Dữ liệu tuỳ ý chuyển cho <see cref="UIScreenBase.OnShow"/>.</param>
        public void Show(ScreenType screenType, object data = null)
        {
            UIScreenBase screen = Get(screenType);
            if (screen == null)
            {
                Debug.LogWarning($"[UIController] Chưa có màn hình nào đăng ký ScreenType.{screenType}.");
                return;
            }

            if (!screen.IsPopup)
            {
                // Đóng mọi popup rồi ẩn màn thường hiện tại.
                for (int i = stack.Count - 1; i >= 0; i--)
                {
                    ScreenType type = stack[i];
                    if (type == screenType) continue;

                    UIScreenBase other = Get(type);
                    if (other == null) continue;

                    other.Hide();
                    OnScreenHidden?.Invoke(type);
                }
                stack.Clear();
            }

            stack.Remove(screenType);
            stack.Add(screenType);

            screen.Show(data);
            OnScreenShown?.Invoke(screenType);
        }

        /// <summary>Ẩn một màn hình và gỡ nó khỏi ngăn xếp.</summary>
        /// <param name="screenType">Màn hình cần ẩn.</param>
        public void Hide(ScreenType screenType)
        {
            UIScreenBase screen = Get(screenType);
            stack.Remove(screenType);

            if (screen == null) return;
            if (!screen.IsVisible) return;

            screen.Hide();
            OnScreenHidden?.Invoke(screenType);
        }

        /// <summary>Ẩn toàn bộ màn hình đã đăng ký và làm rỗng ngăn xếp.</summary>
        public void HideAll()
        {
            foreach (KeyValuePair<ScreenType, UIScreenBase> pair in screens)
            {
                UIScreenBase screen = pair.Value;
                if (screen == null || !screen.IsVisible) continue;

                screen.Hide();
                OnScreenHidden?.Invoke(pair.Key);
            }
            stack.Clear();
        }

        /// <summary>True nếu màn hình đang hiển thị.</summary>
        /// <param name="screenType">Màn hình cần kiểm tra.</param>
        public bool IsShown(ScreenType screenType)
        {
            UIScreenBase screen = Get(screenType);
            return screen != null && screen.IsVisible;
        }

        /// <summary>Lấy màn hình theo kiểu C#; trả về null nếu chưa đăng ký.</summary>
        /// <typeparam name="T">Kiểu màn hình cần lấy.</typeparam>
        public T Get<T>() where T : UIScreenBase
        {
            foreach (KeyValuePair<ScreenType, UIScreenBase> pair in screens)
            {
                if (pair.Value is T typed) return typed;
            }
            return null;
        }

        /// <summary>Lấy màn hình theo <see cref="ScreenType"/>; trả về null nếu chưa đăng ký.</summary>
        /// <param name="screenType">Định danh màn hình.</param>
        public UIScreenBase Get(ScreenType screenType)
        {
            return screens.TryGetValue(screenType, out UIScreenBase screen) ? screen : null;
        }

        /// <summary>
        /// Xử lý Back: chuyển cho màn hình trên cùng có <see cref="UIScreenBase.CanGoBack"/> = true.
        /// Không làm gì nếu ngăn xếp trống hoặc màn hình trên cùng chặn Back.
        /// </summary>
        public void Back()
        {
            for (int i = stack.Count - 1; i >= 0; i--)
            {
                UIScreenBase screen = Get(stack[i]);
                if (screen == null || !screen.IsVisible) continue;

                if (!screen.CanGoBack) return;

                try { screen.OnBackPressed(); }
                catch (Exception e) { Debug.LogException(e, screen); }
                return;
            }
        }

        // ------------------------------------------------------------------
        // Nội bộ
        // ------------------------------------------------------------------

        /// <summary>Chờ một khung hình để mọi màn hình kịp đăng ký rồi mở màn hình mở đầu.</summary>
        private IEnumerator ShowStartScreenRoutine()
        {
            yield return null;
            yield return null;

            if (startScreenShown) yield break;
            startScreenShown = true;

            if (startScreen == ScreenType.None) yield break;
            if (Get(startScreen) == null) yield break;

            Show(startScreen);
        }

        /// <summary>Ghi đè cờ popup/back của màn hình từ <see cref="UIConfigration"/> nếu có.</summary>
        private void ApplyConfiguration(UIScreenBase screen)
        {
            if (configuration == null || screen == null) return;

            UIConfigration.ScreenEntry entry = configuration.Get(screen.screenType);
            if (entry == null) return;

            // Cờ chỉ mang tính tham khảo: IsPopup/CanGoBack là virtual property của từng class,
            // nên ở đây chỉ cảnh báo khi cấu hình lệch với code để phát hiện sai sót sớm.
            if (entry.isPopup != screen.IsPopup)
            {
                Debug.LogWarning(
                    $"[UIController] UIConfigration khai báo ScreenType.{screen.screenType} isPopup={entry.isPopup} " +
                    $"nhưng '{screen.GetType().Name}' trả về {screen.IsPopup}.", screen);
            }
        }
    }
}
