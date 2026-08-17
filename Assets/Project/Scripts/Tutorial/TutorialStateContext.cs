using System;
using System.Collections.Generic;
using StarterKit.UIKit;
using StarterKit.Utilities;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Máy trạng thái của hệ hướng dẫn kiêm túi đựng tham chiếu dùng chung cho mọi bước.
    /// <para>
    /// Đây là POCO có <c>[Serializable]</c> để <see cref="TutorialManager"/> phơi ra Inspector,
    /// đồng thời chạy được trong EditMode test (không cần scene, không cần MonoBehaviour).
    /// </para>
    /// <para>
    /// Luồng: <see cref="Begin"/> → bước đầu tiên chạy được → <see cref="Tick"/> mỗi khung hình →
    /// khi state báo xong (hoặc gọi <see cref="CompleteCurrent"/>) thì chuyển sang
    /// <see cref="TutorialConfiguration.TutorialStepConfig.nextTutorial"/> sau
    /// <see cref="TutorialConfiguration.TutorialStepConfig.nextTutorialStartAfterDelay"/> giây.
    /// Bước đã tắt hoặc đã hoàn thành bị nhảy qua, bước có
    /// <see cref="TutorialConfiguration.TutorialStepConfig.requiredUIScreen"/> phải chờ đúng
    /// màn hình mới được vào.
    /// </para>
    /// </summary>
    [Serializable]
    public class TutorialStateContext
    {
        /// <summary>Số lần dò tối đa khi nhảy qua các bước tắt/đã xong, chặn cấu hình vòng lặp.</summary>
        private const int MaxResolveHops = 64;

        // ------------------------------------------------------------------
        // Cấu hình + tham chiếu scene
        // ------------------------------------------------------------------

        /// <summary>Bảng cấu hình chuỗi hướng dẫn.</summary>
        public TutorialConfiguration configuration;

        /// <summary>Màn hình hướng dẫn (thông điệp, highlight, nút Skip).</summary>
        [Header("Menu Scene References")]
        public TutorialUI tutorialUI;

        /// <summary>Bộ quản lý hướng dẫn đang sở hữu context này.</summary>
        public TutorialManager tutorialManager;

        /// <summary>Dữ liệu người chơi, dùng khi trao thưởng và kiểm tra nâng cấp.</summary>
        public GameData gameData;

        /// <summary>Túi thưởng "TutorialKitbag" trao ở bước <see cref="TutorialType.TutorialKitbagReward"/>.</summary>
        public DynamicKitbag tutorialKitbag;

        /// <summary>Nút Play trên MainMenu; để trống thì bước ép bấm Play sẽ tự dò tìm.</summary>
        public UnityEngine.UI.Button mainMenuPlayButton;

        /// <summary>Người chơi thật trong trận hướng dẫn.</summary>
        [Header("Gameplay Scene References")]
        public BasePlayerController tutorialPlayer;

        /// <summary>Huấn luyện viên / bot phía đối diện.</summary>
        public BasePlayerController coachPlayer;

        /// <summary>Quả bóng của trận.</summary>
        public BallController ball;

        /// <summary>Trọng tài trận đấu.</summary>
        public GameManager gameManager;

        /// <summary>Bộ nhận input vuốt/chạm.</summary>
        public InputManager inputManager;

        /// <summary>Hình học sân.</summary>
        public Court court;

        /// <summary>Khối highlight nửa sân phía z &gt; 0.</summary>
        [Header("Highlight Objects (optional)")]
        public GameObject positiveCourtHighlight;

        /// <summary>Khối highlight nửa sân phía z &lt; 0.</summary>
        public GameObject negativeCourtHighlight;

        /// <summary>Khối highlight vùng kitchen.</summary>
        public GameObject kitchenHighlight;

        /// <summary>Vòng tròn đích cho bước đánh có mục tiêu.</summary>
        public Transform targetIndicator;

        // ------------------------------------------------------------------
        // Trạng thái máy
        // ------------------------------------------------------------------

        /// <summary>State đang chạy; null khi chưa bắt đầu hoặc đang chờ điều kiện.</summary>
        public ITutorialState CurrentState { get; private set; }

        /// <summary>Bước đang chạy; <see cref="TutorialType.None"/> khi không có.</summary>
        public TutorialType CurrentType { get; private set; }

        /// <summary>Bước đang chờ vào (chờ độ trễ hoặc chờ đúng màn hình UI).</summary>
        public TutorialType PendingType { get; private set; }

        /// <summary>True khi chuỗi đã chạy hết hoặc đã bị dừng.</summary>
        public bool IsFinished { get; private set; }

        /// <summary>Phát khi một bước bắt đầu.</summary>
        public event Action<TutorialType> OnStepStarted;

        /// <summary>Phát khi một bước hoàn thành.</summary>
        public event Action<TutorialType> OnStepCompleted;

        /// <summary>Phát khi toàn bộ chuỗi kết thúc (hoặc bị bỏ qua).</summary>
        public event Action OnTutorialFinished;

        private readonly Dictionary<TutorialType, ITutorialState> states =
            new Dictionary<TutorialType, ITutorialState>();

        private Coroutine delayRoutine;

        // ------------------------------------------------------------------
        // API
        // ------------------------------------------------------------------

        /// <summary>Đăng ký một state vào máy; state trùng <see cref="ITutorialState.Type"/> sẽ ghi đè.</summary>
        /// <param name="state">State cần đăng ký; bỏ qua nếu null.</param>
        public void Register(ITutorialState state)
        {
            if (state == null) return;
            states[state.Type] = state;
        }

        /// <summary>Danh sách state đã đăng ký, chỉ đọc.</summary>
        public IEnumerable<ITutorialState> RegisteredStates
        {
            get { return states.Values; }
        }

        /// <summary>Bắt đầu chuỗi từ một bước cho trước.</summary>
        /// <param name="tutorialType">Bước xuất phát; bước tắt/đã xong sẽ tự nhảy qua.</param>
        public void Begin(TutorialType tutorialType)
        {
            CancelDelay();
            ExitCurrent();

            IsFinished = false;
            QueueStep(tutorialType);
        }

        /// <summary>
        /// Nhịp chạy mỗi khung hình: đưa state vào (nếu đang chờ), cập nhật state hiện tại
        /// và chuyển bước khi state báo xong.
        /// </summary>
        public void Tick()
        {
            if (IsFinished) return;

            if (CurrentState == null)
            {
                if (PendingType != TutorialType.None) TryEnterPending();
                return;
            }

            CurrentState.Update();

            if (CurrentState != null && CurrentState.IsCompleted) CompleteCurrent();
        }

        /// <summary>
        /// Kết thúc bước hiện tại: thoát state, ghi cờ hoàn thành, phát
        /// <see cref="OnStepCompleted"/> rồi hẹn giờ vào bước kế tiếp.
        /// </summary>
        public void CompleteCurrent()
        {
            if (CurrentState == null) return;

            TutorialType finished = CurrentType;

            ExitCurrent();

            if (configuration != null) configuration.SetCompleted(finished, true);

            Action<TutorialType> completed = OnStepCompleted;
            if (completed != null) completed.Invoke(finished);

            TutorialType next = configuration != null
                ? configuration.GetNext(finished)
                : TutorialType.None;

            float delay = 0f;
            if (configuration != null)
            {
                TutorialConfiguration.TutorialStepConfig step = configuration.Get(finished);
                if (step != null) delay = Mathf.Max(0f, step.nextTutorialStartAfterDelay);
            }

            ScheduleStep(next, delay);
        }

        /// <summary>
        /// Bỏ qua toàn bộ hướng dẫn: thoát bước hiện tại, đánh dấu mọi bước đang bật là xong
        /// rồi phát <see cref="OnTutorialFinished"/>.
        /// </summary>
        public void Skip()
        {
            CancelDelay();
            ExitCurrent();

            if (configuration != null && configuration.tutorialSteps != null)
            {
                for (int i = 0; i < configuration.tutorialSteps.Count; i++)
                {
                    TutorialConfiguration.TutorialStepConfig step = configuration.tutorialSteps[i];
                    if (step != null && step.isEnabled) step.isCompleted = true;
                }
            }

            Finish();
        }

        /// <summary>Dừng máy trạng thái ngay lập tức mà KHÔNG phát <see cref="OnTutorialFinished"/>.</summary>
        public void Stop()
        {
            CancelDelay();
            ExitCurrent();

            PendingType = TutorialType.None;
            IsFinished = true;
        }

        // ------------------------------------------------------------------
        // Nội bộ
        // ------------------------------------------------------------------

        /// <summary>Hẹn giờ vào bước kế tiếp. Ngoài Play mode thì vào ngay để test tất định.</summary>
        private void ScheduleStep(TutorialType next, float delay)
        {
            CancelDelay();

            if (delay > 0f && Application.isPlaying)
            {
                delayRoutine = DelayedAction.Run(delay, () =>
                {
                    delayRoutine = null;
                    QueueStep(next);
                });
                return;
            }

            QueueStep(next);
        }

        /// <summary>Giải chuỗi bước tắt/đã xong rồi thử vào bước tìm được.</summary>
        private void QueueStep(TutorialType tutorialType)
        {
            PendingType = ResolveRunnable(tutorialType);
            TryEnterPending();
        }

        /// <summary>
        /// Đi theo <c>nextTutorial</c> cho tới khi gặp bước đang bật và chưa hoàn thành,
        /// hoặc gặp điểm kết thúc chuỗi.
        /// </summary>
        private TutorialType ResolveRunnable(TutorialType tutorialType)
        {
            TutorialType type = tutorialType;

            for (int hop = 0; hop < MaxResolveHops; hop++)
            {
                if (type == TutorialType.None || type == TutorialType.TutorialCompleted) return type;
                if (configuration == null) return type;

                TutorialConfiguration.TutorialStepConfig step = configuration.Get(type);
                if (step == null) return TutorialType.None;

                if (step.isEnabled && !step.isCompleted) return type;

                type = step.nextTutorial;
            }

            Debug.LogWarning("[TutorialStateContext] Cấu hình bước hướng dẫn có vòng lặp; dừng chuỗi.");
            return TutorialType.None;
        }

        /// <summary>Vào bước đang chờ nếu điều kiện màn hình đã thoả.</summary>
        private void TryEnterPending()
        {
            if (PendingType == TutorialType.None || PendingType == TutorialType.TutorialCompleted)
            {
                PendingType = TutorialType.None;
                Finish();
                return;
            }

            TutorialConfiguration.TutorialStepConfig step =
                configuration != null ? configuration.Get(PendingType) : null;

            if (step != null && !IsScreenReady(step.requiredUIScreen)) return;

            ITutorialState state;
            if (!states.TryGetValue(PendingType, out state) || state == null)
            {
                // Chưa cài đặt state cho bước này: coi như xong để chuỗi không kẹt.
                TutorialType skipped = PendingType;
                PendingType = TutorialType.None;

                if (configuration != null) configuration.SetCompleted(skipped, true);

                Action<TutorialType> completed = OnStepCompleted;
                if (completed != null) completed.Invoke(skipped);

                TutorialType next = configuration != null
                    ? configuration.GetNext(skipped)
                    : TutorialType.None;

                ScheduleStep(next, 0f);
                return;
            }

            CurrentType = PendingType;
            PendingType = TutorialType.None;
            CurrentState = state;

            state.Enter();

            Action<TutorialType> started = OnStepStarted;
            if (started != null) started.Invoke(CurrentType);
        }

        /// <summary>
        /// True khi màn hình bắt buộc đang hiển thị. Không có ràng buộc, hoặc chưa có
        /// <see cref="UIController"/> trong scene, đều coi là thoả.
        /// </summary>
        private bool IsScreenReady(ScreenType requiredScreen)
        {
            if (requiredScreen == ScreenType.None) return true;
            if (!UIController.HasInstance) return true;

            return UIController.Instance.IsShown(requiredScreen);
        }

        /// <summary>Thoát state hiện tại (nếu có) và xoá con trỏ.</summary>
        private void ExitCurrent()
        {
            if (CurrentState == null)
            {
                CurrentType = TutorialType.None;
                return;
            }

            ITutorialState state = CurrentState;
            CurrentState = null;
            CurrentType = TutorialType.None;

            try { state.Exit(); }
            catch (Exception e) { Debug.LogException(e); }
        }

        /// <summary>Đóng chuỗi và phát <see cref="OnTutorialFinished"/> đúng một lần.</summary>
        private void Finish()
        {
            if (IsFinished) return;

            CancelDelay();
            ExitCurrent();

            PendingType = TutorialType.None;
            IsFinished = true;

            Action finished = OnTutorialFinished;
            if (finished != null) finished.Invoke();
        }

        /// <summary>Huỷ coroutine hẹn giờ chuyển bước nếu đang chạy.</summary>
        private void CancelDelay()
        {
            if (delayRoutine == null) return;

            DelayedAction.Cancel(delayRoutine);
            delayRoutine = null;
        }
    }
}
