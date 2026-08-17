using System;
using System.Collections;
using System.Collections.Generic;
using Pickleball;
using UnityEngine;

namespace StarterKit.UIKit
{
    /// <summary>
    /// Lớp cha của mọi màn hình/popup. Mỗi màn hình là một <see cref="Canvas"/> riêng;
    /// ẩn/hiện bằng cách bật tắt Canvas (KHÔNG tắt GameObject) để màn hình vẫn nghe được
    /// sự kiện dữ liệu và giữ nguyên coroutine khi đang ẩn.
    /// <para>
    /// Màn hình tự đăng ký với <see cref="UIController"/> trong Awake; nếu controller chưa
    /// tồn tại thì chờ bằng coroutine.
    /// </para>
    /// </summary>
    public abstract class UIScreenBase : MonoBehaviour
    {
        /// <summary>Định danh màn hình; <see cref="UIController"/> dùng khoá này để tra cứu.</summary>
        public ScreenType screenType;

        /// <summary>Canvas của màn hình; tự lấy trên chính GameObject nếu để trống.</summary>
        [SerializeField] protected Canvas canvas;

        /// <summary>True khi màn hình đang hiển thị.</summary>
        public bool IsVisible { get; private set; }

        /// <summary>True nếu màn hình là popup (chồng lên màn hình dưới thay vì thay thế).</summary>
        public virtual bool IsPopup => false;

        /// <summary>False nếu màn hình không cho phép bấm Back/Escape để thoát.</summary>
        public virtual bool CanGoBack => true;

        /// <summary>
        /// Giá trị <see cref="screenType"/> mà lớp con muốn dùng khi prefab chưa gán.
        /// Nhờ vậy màn hình vẫn đăng ký đúng chỗ dù công cụ sinh prefab chỉ gắn component
        /// mà quên set trường trên Inspector.
        /// </summary>
        public virtual ScreenType DefaultScreenType => ScreenType.None;

        private readonly List<Animatable> animatables = new List<Animatable>();
        private Behaviour[] raycasters;
        private bool initialized;
        private Coroutine registerRoutine;

        protected virtual void Awake()
        {
            if (screenType == ScreenType.None) screenType = DefaultScreenType;

            CacheReferences();
            ApplyVisibility(false);
            registerRoutine = StartCoroutine(RegisterRoutine());
        }

        protected virtual void OnDestroy()
        {
            if (registerRoutine != null)
            {
                StopCoroutine(registerRoutine);
                registerRoutine = null;
            }
        }

        /// <summary>
        /// Hiện màn hình: bật canvas, chạy hiệu ứng vào của mọi <see cref="Animatable"/> con,
        /// rồi gọi <see cref="OnShow"/>.
        /// </summary>
        /// <param name="data">Dữ liệu tuỳ ý truyền cho màn hình; có thể null.</param>
        public void Show(object data = null)
        {
            CacheReferences();

            if (!initialized)
            {
                initialized = true;
                try { OnInit(); }
                catch (Exception e) { Debug.LogException(e, this); }
            }

            ApplyVisibility(true);
            IsVisible = true;

            try { OnShow(data); }
            catch (Exception e) { Debug.LogException(e, this); }

            for (int i = 0; i < animatables.Count; i++)
            {
                Animatable a = animatables[i];
                if (a == null) continue;
                try { a.PlayIn(); }
                catch (Exception e) { Debug.LogException(e, this); }
            }
        }

        /// <summary>
        /// Ẩn màn hình: chạy hiệu ứng ra của mọi <see cref="Animatable"/> con, tắt canvas
        /// rồi gọi <see cref="OnHide"/>.
        /// </summary>
        public void Hide()
        {
            if (!IsVisible)
            {
                ApplyVisibility(false);
                return;
            }

            CacheReferences();
            IsVisible = false;

            float wait = 0f;
            for (int i = 0; i < animatables.Count; i++)
            {
                Animatable a = animatables[i];
                if (a == null) continue;
                wait = Mathf.Max(wait, a.delay + a.duration);
                try { a.PlayOut(); }
                catch (Exception e) { Debug.LogException(e, this); }
            }

            try { OnHide(); }
            catch (Exception e) { Debug.LogException(e, this); }

            if (wait > 0f && isActiveAndEnabled) StartCoroutine(HideAfter(wait));
            else ApplyVisibility(false);
        }

        /// <summary>Khởi tạo một lần duy nhất, ngay trước lần hiện đầu tiên.</summary>
        public virtual void OnInit() { }

        /// <summary>Được gọi mỗi lần màn hình hiện; nơi cập nhật widget theo dữ liệu.</summary>
        /// <param name="data">Dữ liệu truyền vào từ <see cref="UIController.Show"/>; có thể null.</param>
        public virtual void OnShow(object data) { }

        /// <summary>Được gọi mỗi lần màn hình ẩn; nơi huỷ đăng ký sự kiện.</summary>
        public virtual void OnHide() { }

        /// <summary>Xử lý phím Back/Escape. Mặc định là đóng chính màn hình này.</summary>
        public virtual void OnBackPressed()
        {
            if (UIController.HasInstance) UIController.Instance.Hide(screenType);
            else Hide();
        }

        /// <summary>Gom canvas, raycaster và danh sách <see cref="Animatable"/> con. An toàn khi gọi lại.</summary>
        protected void CacheReferences()
        {
            if (canvas == null) canvas = GetComponent<Canvas>();
            if (canvas == null) canvas = GetComponentInChildren<Canvas>(true);

            if (raycasters == null)
            {
                raycasters = GetComponents<UnityEngine.EventSystems.BaseRaycaster>();
            }

            if (animatables.Count == 0)
            {
                GetComponentsInChildren(true, animatables);
                for (int i = 0; i < animatables.Count; i++)
                {
                    if (animatables[i] != null) animatables[i].Initialize();
                }
            }
        }

        private void ApplyVisibility(bool visible)
        {
            if (canvas != null) canvas.enabled = visible;

            if (raycasters != null)
            {
                for (int i = 0; i < raycasters.Length; i++)
                {
                    if (raycasters[i] != null) raycasters[i].enabled = visible;
                }
            }
        }

        private IEnumerator HideAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            if (!IsVisible) ApplyVisibility(false);
        }

        private IEnumerator RegisterRoutine()
        {
            // UIController có thể được Awake sau màn hình này, nên chờ tối đa vài giây.
            float timeout = 5f;
            while (!UIController.HasInstance && timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }

            if (UIController.HasInstance) UIController.Instance.Register(this);
            else Debug.LogWarning($"[UIScreenBase] Không tìm thấy UIController để đăng ký '{name}'.", this);

            registerRoutine = null;
        }
    }
}
