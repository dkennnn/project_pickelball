using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Lớp cha của mọi bước hướng dẫn. Đây là POCO (không phải MonoBehaviour) nên
    /// <see cref="TutorialManager"/> có thể dựng đủ 11 bước bằng <c>new</c> mà không cần
    /// GameObject riêng cho từng bước như bản gốc.
    /// <para>
    /// Toàn bộ helper bên dưới đều null-guard: state vẫn chạy được (và vẫn hoàn thành được)
    /// khi thiếu <see cref="TutorialUI"/>, thiếu <see cref="InputManager"/> hay thiếu
    /// tham chiếu scene — chỉ là không có hiệu ứng hiển thị.
    /// </para>
    /// </summary>
    public abstract class BaseTutorialState : ITutorialState
    {
        /// <summary>Một giây — dùng cho các bước hẹn giờ ngắn.</summary>
        protected const float OneSecond = 1f;

        /// <summary>Hai giây.</summary>
        protected const float TwoSeconds = 2f;

        /// <summary>Ba giây — thời lượng mặc định của các bước chỉ hiện thông điệp.</summary>
        protected const float ThreeSeconds = 3f;

        /// <summary>Năm giây.</summary>
        protected const float FiveSeconds = 5f;

        /// <summary>Ngữ cảnh dùng chung: tham chiếu scene, UI và cấu hình hướng dẫn.</summary>
        protected readonly TutorialStateContext context;

        /// <summary>Các <see cref="Selectable"/> vừa bị <see cref="BlockInputExcept"/> tắt, để khôi phục sau.</summary>
        private readonly List<Selectable> blockedSelectables = new List<Selectable>();

        /// <summary>Các GameObject highlight vừa được bật, để tắt lại khi <see cref="ClearHighlight"/>.</summary>
        private readonly List<GameObject> activatedHighlights = new List<GameObject>();

        private bool inputBlocked;

        /// <summary>Khởi tạo bước với ngữ cảnh dùng chung; <paramref name="context"/> có thể null.</summary>
        /// <param name="context">Ngữ cảnh do <see cref="TutorialManager"/> cấp.</param>
        protected BaseTutorialState(TutorialStateContext context)
        {
            this.context = context;
        }

        /// <inheritdoc/>
        public abstract TutorialType Type { get; }

        /// <inheritdoc/>
        public bool IsCompleted { get; private set; }

        /// <summary>Số giây đã trôi qua kể từ khi bước bắt đầu (cộng dồn trong <see cref="Update"/>).</summary>
        protected float ElapsedTime { get; private set; }

        /// <inheritdoc/>
        public void Enter()
        {
            IsCompleted = false;
            ElapsedTime = 0f;

            try { OnEnter(); }
            catch (System.Exception e) { Debug.LogException(e); }
        }

        /// <inheritdoc/>
        public void Update()
        {
            if (IsCompleted) return;

            ElapsedTime += Time.deltaTime;

            try { OnUpdate(); }
            catch (System.Exception e) { Debug.LogException(e); }
        }

        /// <inheritdoc/>
        public void Exit()
        {
            try { OnExit(); }
            catch (System.Exception e) { Debug.LogException(e); }

            ClearHighlight();
            HideMessage();
            AllowAllInput();
        }

        // ------------------------------------------------------------------
        // Hook cho lớp con
        // ------------------------------------------------------------------

        /// <summary>Lớp con cài đặt phần bắt đầu bước.</summary>
        protected virtual void OnEnter() { }

        /// <summary>Lớp con cài đặt phần chạy mỗi khung hình.</summary>
        protected virtual void OnUpdate() { }

        /// <summary>Lớp con cài đặt phần dọn dẹp (huỷ đăng ký sự kiện...).</summary>
        protected virtual void OnExit() { }

        // ------------------------------------------------------------------
        // Helper dùng chung
        // ------------------------------------------------------------------

        /// <summary>Hiện thông điệp hướng dẫn trên <see cref="TutorialUI"/>.</summary>
        /// <param name="text">Nội dung cần hiện; bỏ qua nếu rỗng.</param>
        protected void ShowMessage(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            TutorialUI ui = TutorialUIRef;
            if (ui == null) return;

            ui.ShowMessage(text);
        }

        /// <summary>Ẩn bảng thông điệp hướng dẫn.</summary>
        protected void HideMessage()
        {
            TutorialUI ui = TutorialUIRef;
            if (ui == null) return;

            ui.HideMessage();
        }

        /// <summary>
        /// Làm nổi bật một đối tượng. Với <see cref="RectTransform"/> thì dùng khung sáng của
        /// <see cref="TutorialUI"/>; với transform trong không gian 3D thì chỉ bật GameObject
        /// highlight tương ứng lên (và tắt lại ở <see cref="ClearHighlight"/>).
        /// </summary>
        /// <param name="target">Đối tượng cần làm nổi bật; bỏ qua nếu null.</param>
        protected void HighlightObject(Transform target)
        {
            if (target == null) return;

            RectTransform rect = target as RectTransform;
            if (rect != null)
            {
                TutorialUI ui = TutorialUIRef;
                if (ui != null) ui.SetHighlight(rect);
                return;
            }

            GameObject go = target.gameObject;
            if (go.activeSelf) return;

            go.SetActive(true);
            activatedHighlights.Add(go);
        }

        /// <summary>Tắt mọi highlight mà bước này đã bật.</summary>
        protected void ClearHighlight()
        {
            TutorialUI ui = TutorialUIRef;
            if (ui != null) ui.ClearHighlight();

            for (int i = 0; i < activatedHighlights.Count; i++)
            {
                GameObject go = activatedHighlights[i];
                if (go != null) go.SetActive(false);
            }
            activatedHighlights.Clear();
        }

        /// <summary>
        /// Chặn mọi tương tác trừ nhánh cây của <paramref name="allowed"/>: tắt input gameplay
        /// và tắt <c>interactable</c> của mọi <see cref="Selectable"/> nằm ngoài nhánh đó.
        /// </summary>
        /// <param name="allowed">Nhánh UI duy nhất còn bấm được; null nghĩa là chặn hết.</param>
        protected void BlockInputExcept(Transform allowed)
        {
            AllowAllInput();

            InputManager input = InputManagerRef;
            if (input != null) input.InputEnabled = false;
            inputBlocked = true;

            Selectable[] all = Object.FindObjectsByType<Selectable>(FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                Selectable s = all[i];
                if (s == null || !s.interactable) continue;
                if (allowed != null && s.transform.IsChildOf(allowed)) continue;

                s.interactable = false;
                blockedSelectables.Add(s);
            }
        }

        /// <summary>Mở lại toàn bộ tương tác đã bị <see cref="BlockInputExcept"/> chặn.</summary>
        protected void AllowAllInput()
        {
            for (int i = 0; i < blockedSelectables.Count; i++)
            {
                Selectable s = blockedSelectables[i];
                if (s != null) s.interactable = true;
            }
            blockedSelectables.Clear();

            if (!inputBlocked) return;
            inputBlocked = false;

            InputManager input = InputManagerRef;
            if (input != null) input.InputEnabled = true;
        }

        /// <summary>Đánh dấu bước đã hoàn thành; context sẽ chuyển bước ở lần Tick kế tiếp.</summary>
        protected void Complete()
        {
            IsCompleted = true;
        }

        // ------------------------------------------------------------------
        // Truy cập tham chiếu có null-guard
        // ------------------------------------------------------------------

        /// <summary>Màn hình hướng dẫn; null nếu chưa có trong scene.</summary>
        protected TutorialUI TutorialUIRef
        {
            get { return context != null ? context.tutorialUI : null; }
        }

        /// <summary>Bộ nhận input; null nếu chưa có trong scene.</summary>
        protected InputManager InputManagerRef
        {
            get
            {
                if (context != null && context.inputManager != null) return context.inputManager;
                return InputManager.HasInstance ? InputManager.Instance : null;
            }
        }

        /// <summary>Trọng tài trận đấu; null nếu đang ở scene menu.</summary>
        protected GameManager GameManagerRef
        {
            get
            {
                if (context != null && context.gameManager != null) return context.gameManager;
                return GameManager.HasInstance ? GameManager.Instance : null;
            }
        }

        /// <summary>Quả bóng; null nếu đang ở scene menu.</summary>
        protected BallController BallRef
        {
            get
            {
                if (context != null && context.ball != null) return context.ball;
                return BallController.HasInstance ? BallController.Instance : null;
            }
        }

        /// <summary>Hình học sân; null nếu đang ở scene menu.</summary>
        protected Court CourtRef
        {
            get
            {
                if (context != null && context.court != null) return context.court;

                GameManager gm = GameManagerRef;
                return gm != null ? gm.court : null;
            }
        }

        /// <summary>teamID của người chơi thật; mặc định "P1" nếu chưa dựng được tham chiếu.</summary>
        protected string PlayerTeamID
        {
            get
            {
                if (context != null && context.tutorialPlayer != null &&
                    !string.IsNullOrEmpty(context.tutorialPlayer.teamID))
                {
                    return context.tutorialPlayer.teamID;
                }

                GameManager gm = GameManagerRef;
                return gm != null ? gm.player1TeamID : "P1";
            }
        }

        /// <summary>teamID của huấn luyện viên / AI; mặc định "P2".</summary>
        protected string CoachTeamID
        {
            get
            {
                if (context != null && context.coachPlayer != null &&
                    !string.IsNullOrEmpty(context.coachPlayer.teamID))
                {
                    return context.coachPlayer.teamID;
                }

                GameManager gm = GameManagerRef;
                return gm != null ? gm.player2TeamID : "P2";
            }
        }

        /// <summary>Mở một màn hình qua <see cref="StarterKit.UIKit.UIController"/> nếu có.</summary>
        /// <param name="screen">Màn hình cần mở; bỏ qua nếu <see cref="ScreenType.None"/>.</param>
        /// <param name="data">Dữ liệu tuỳ ý gửi kèm.</param>
        protected static void ShowScreen(ScreenType screen, object data = null)
        {
            if (screen == ScreenType.None) return;
            if (!StarterKit.UIKit.UIController.HasInstance) return;

            StarterKit.UIKit.UIController.Instance.Show(screen, data);
        }
    }
}
