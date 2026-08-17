using System;
using System.Collections.Generic;
using StarterKit.Utilities;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Đầu não của hệ hướng dẫn: dựng 11 state, nạp tiến độ từ save data, chạy máy trạng thái
    /// <see cref="TutorialStateContext"/> và ghi lại từng bước đã hoàn thành.
    /// <para>
    /// Sống xuyên scene (<see cref="IndestructibleSingleton{T}"/>) vì chuỗi hướng dẫn bắc cầu
    /// giữa scene menu và scene sân đấu.
    /// </para>
    /// </summary>
    public class TutorialManager : IndestructibleSingleton<TutorialManager>
    {
        /// <summary>Bảng cấu hình chuỗi hướng dẫn (asset TutorialConfiguration).</summary>
        [Header("Tutorial Configuration")]
        public TutorialConfiguration configuration;

        /// <summary>Dữ liệu người chơi, dùng khi trao thưởng tutorial.</summary>
        public GameData gameData;

        /// <summary>Túi thưởng "TutorialKitbag"; để trống thì bước thưởng sẽ tự bỏ qua.</summary>
        [SerializeField] private DynamicKitbag tutorialKitbag;

        /// <summary>Màn hình hướng dẫn; để trống thì tự dò trong scene lúc chạy.</summary>
        [SerializeField] private TutorialUI tutorialUI;

        /// <summary>Bắt đầu chuỗi ngay trong Start nếu người chơi chưa hoàn thành hướng dẫn.</summary>
        [SerializeField] private bool startAutomatically = true;

        /// <summary>
        /// Tự mở màn hình bắt buộc khi một bước đang phải chờ nó. Tắt cờ này nếu muốn
        /// người chơi tự điều hướng tới đúng màn hình.
        /// </summary>
        [SerializeField] private bool autoOpenRequiredScreen = true;

        /// <summary>Ngữ cảnh dùng chung + máy trạng thái.</summary>
        [Header("Tutorial Context")]
        [SerializeField] private TutorialStateContext context = new TutorialStateContext();

        /// <summary>Phát mỗi khi bước hướng dẫn hiện tại đổi (kể cả khi chuỗi kết thúc).</summary>
        public static event Action<TutorialType> OnTutorialStepChanged;

        /// <summary>True khi chuỗi hướng dẫn đang chạy.</summary>
        public bool IsRunning { get; private set; }

        /// <summary>Ngữ cảnh dùng chung, để các hệ thống khác gán tham chiếu scene.</summary>
        public TutorialStateContext Context
        {
            get { return context; }
        }

        /// <summary>Bước hướng dẫn đang chạy; <see cref="TutorialType.None"/> nếu không có.</summary>
        public TutorialType CurrentTutorialType
        {
            get { return context != null ? context.CurrentType : TutorialType.None; }
        }

        private bool statesBuilt;

        // ------------------------------------------------------------------
        // Vòng đời
        // ------------------------------------------------------------------

        /// <inheritdoc/>
        protected override void OnAwake()
        {
            base.OnAwake();
            BuildContext();
        }

        private void Start()
        {
            if (startAutomatically) StartTutorialIfNeeded();
        }

        private void Update()
        {
            if (!IsRunning || context == null) return;

            if (autoOpenRequiredScreen) OpenRequiredScreenIfWaiting();
            context.Tick();
        }

        /// <summary>
        /// Khi một bước đang chờ đúng màn hình UI mà màn hình đó chưa hiện, tự mở giúp
        /// để chuỗi không kẹt (ví dụ bước ép nâng cấp grip cần DressingRoomUI).
        /// </summary>
        private void OpenRequiredScreenIfWaiting()
        {
            if (context.CurrentState != null) return;
            if (context.PendingType == TutorialType.None) return;
            if (configuration == null) return;
            if (!StarterKit.UIKit.UIController.HasInstance) return;

            TutorialConfiguration.TutorialStepConfig step = configuration.Get(context.PendingType);
            if (step == null || step.requiredUIScreen == ScreenType.None) return;

            StarterKit.UIKit.UIController controller = StarterKit.UIKit.UIController.Instance;
            if (controller.IsShown(step.requiredUIScreen)) return;
            if (controller.Get(step.requiredUIScreen) == null) return;

            controller.Show(step.requiredUIScreen);
        }

        /// <inheritdoc/>
        protected override void OnDestroy()
        {
            if (context != null)
            {
                context.OnStepStarted -= HandleStepStarted;
                context.OnStepCompleted -= HandleStepCompleted;
                context.OnTutorialFinished -= HandleTutorialFinished;
                context.Stop();
            }

            IsRunning = false;
            base.OnDestroy();
        }

        // ------------------------------------------------------------------
        // API công khai
        // ------------------------------------------------------------------

        /// <summary>
        /// Bắt đầu chuỗi nếu hệ hướng dẫn đang bật và người chơi chưa hoàn thành.
        /// Gọi lại nhiều lần an toàn.
        /// </summary>
        public void StartTutorialIfNeeded()
        {
            if (IsRunning) return;
            if (configuration == null)
            {
                Debug.LogWarning("[TutorialManager] Chưa gán TutorialConfiguration, bỏ qua hướng dẫn.");
                return;
            }
            if (!configuration.enableTutorialSystem) return;

            LoadProgress();

            SavedDataHandler saved = SavedDataHandler.HasInstance ? SavedDataHandler.Instance : null;
            if (saved != null && saved.GetTutorialCompletion()) return;

            BuildContext();

            IsRunning = true;
            context.Begin(configuration.startingTutorial);
        }

        /// <summary>Chạy chuỗi từ một bước cụ thể, bất kể bước đó đã hoàn thành hay chưa.</summary>
        /// <param name="tutorialType">Bước muốn ép chạy.</param>
        public void ForceStart(TutorialType tutorialType)
        {
            BuildContext();
            LoadProgress();

            if (configuration != null) configuration.SetCompleted(tutorialType, false);

            IsRunning = true;
            context.Begin(tutorialType);
        }

        /// <summary>Bỏ qua toàn bộ hướng dẫn và ghi nhận đã hoàn thành vào save data.</summary>
        public void SkipAll()
        {
            BuildContext();

            context.Skip();

            if (configuration != null && configuration.tutorialSteps != null)
            {
                for (int i = 0; i < configuration.tutorialSteps.Count; i++)
                {
                    TutorialConfiguration.TutorialStepConfig step = configuration.tutorialSteps[i];
                    if (step != null && step.isEnabled) SaveStep(step.tutorialType);
                }
            }

            SaveStep(TutorialType.TutorialCompleted);
            IsRunning = false;
        }

        /// <summary>True khi bước đã hoàn thành (ưu tiên cấu hình runtime, dự phòng là save data).</summary>
        /// <param name="tutorialType">Bước cần kiểm tra.</param>
        public bool IsStepCompleted(TutorialType tutorialType)
        {
            if (configuration != null)
            {
                TutorialConfiguration.TutorialStepConfig step = configuration.Get(tutorialType);
                if (step != null) return step.isCompleted;
            }

            SavedDataHandler saved = SavedDataHandler.HasInstance ? SavedDataHandler.Instance : null;
            if (saved == null) return false;

            List<TutorialType> completed = saved.GetCompletedTutorialSteps();
            return completed != null && completed.Contains(tutorialType);
        }

        /// <summary>Ghi nhận một bước đã hoàn thành vào cấu hình runtime và save data.</summary>
        /// <param name="tutorialType">Bước vừa xong.</param>
        public void MarkStepCompleted(TutorialType tutorialType)
        {
            if (tutorialType == TutorialType.None) return;

            if (configuration != null) configuration.SetCompleted(tutorialType, true);
            SaveStep(tutorialType);
        }

        /// <summary>
        /// Dò lại các tham chiếu của scene gameplay (người chơi, bóng, sân, input).
        /// Gọi sau khi scene sân đấu đã nạp xong.
        /// </summary>
        public void RefreshSceneReferences()
        {
            if (context == null) return;

            if (context.tutorialUI == null)
            {
                context.tutorialUI = tutorialUI != null
                    ? tutorialUI
                    : FindAnyObjectByType<TutorialUI>(FindObjectsInactive.Include);
            }

            if (context.inputManager == null && InputManager.HasInstance)
            {
                context.inputManager = InputManager.Instance;
            }

            if (context.gameManager == null && GameManager.HasInstance)
            {
                context.gameManager = GameManager.Instance;
            }

            if (context.ball == null && BallController.HasInstance)
            {
                context.ball = BallController.Instance;
            }

            if (context.court == null)
            {
                context.court = context.gameManager != null && context.gameManager.court != null
                    ? context.gameManager.court
                    : FindAnyObjectByType<Court>(FindObjectsInactive.Include);
            }

            if (context.tutorialPlayer == null || context.coachPlayer == null)
            {
                BasePlayerController[] players =
                    FindObjectsByType<BasePlayerController>(FindObjectsSortMode.None);

                for (int i = 0; i < players.Length; i++)
                {
                    BasePlayerController p = players[i];
                    if (p == null) continue;

                    bool isAI = p is PickleballAIController;
                    if (isAI && context.coachPlayer == null) context.coachPlayer = p;
                    else if (!isAI && context.tutorialPlayer == null) context.tutorialPlayer = p;
                }
            }
        }

        // ------------------------------------------------------------------
        // Nội bộ
        // ------------------------------------------------------------------

        /// <summary>Dựng context + đăng ký 11 state đúng một lần.</summary>
        private void BuildContext()
        {
            if (context == null) context = new TutorialStateContext();

            context.configuration = configuration;
            context.tutorialManager = this;
            context.gameData = gameData;
            if (context.tutorialKitbag == null) context.tutorialKitbag = tutorialKitbag;
            if (context.tutorialUI == null) context.tutorialUI = tutorialUI;

            if (statesBuilt) return;
            statesBuilt = true;

            context.Register(new MainMenuForcedPlayTutorialState(context));
            context.Register(new WelcomeMessageTutorialState(context));
            context.Register(new CourtSidesAndKitchenInfoTutorialState(context));
            context.Register(new ReceiveInfoTutorialState(context));
            context.Register(new CrossServeInfoTutorialState(context));
            context.Register(new BasicHitTutorialState(context));
            context.Register(new TargetedHitTutorialState(context));
            context.Register(new KitchenHitTutorialState(context));
            context.Register(new TwoPointEasyBotMatchTutorialState(context));
            context.Register(new TutorialKitbagRewardTutorialState(context));
            context.Register(new ForcedGripUpgradeTutorialState(context));

            context.OnStepStarted += HandleStepStarted;
            context.OnStepCompleted += HandleStepCompleted;
            context.OnTutorialFinished += HandleTutorialFinished;
        }

        /// <summary>Nạp tiến độ đã lưu vào cấu hình runtime.</summary>
        private void LoadProgress()
        {
            if (configuration == null) return;

            SavedDataHandler saved = SavedDataHandler.HasInstance ? SavedDataHandler.Instance : null;
            if (saved == null) return;

            configuration.ApplyCompletedSteps(saved.GetCompletedTutorialSteps());
        }

        /// <summary>Ghi một bước vào save data; im lặng bỏ qua nếu chưa có handler.</summary>
        private static void SaveStep(TutorialType tutorialType)
        {
            SavedDataHandler saved = SavedDataHandler.HasInstance ? SavedDataHandler.Instance : null;
            if (saved == null) return;

            saved.SetTutorialStepCompletion(tutorialType);
            saved.RequestSave();
        }

        private void HandleStepStarted(TutorialType tutorialType)
        {
            RefreshSceneReferences();

            Action<TutorialType> changed = OnTutorialStepChanged;
            if (changed != null) changed.Invoke(tutorialType);
        }

        private void HandleStepCompleted(TutorialType tutorialType)
        {
            MarkStepCompleted(tutorialType);
        }

        private void HandleTutorialFinished()
        {
            IsRunning = false;
            MarkStepCompleted(TutorialType.TutorialCompleted);

            Action<TutorialType> changed = OnTutorialStepChanged;
            if (changed != null) changed.Invoke(TutorialType.TutorialCompleted);
        }
    }
}
