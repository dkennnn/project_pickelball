using System.Collections.Generic;
using NUnit.Framework;
using Pickleball;
using UnityEngine;

namespace Pickleball.Tests
{
    /// <summary>
    /// Kiểm chứng <see cref="TutorialConfiguration"/> và máy trạng thái
    /// <see cref="TutorialStateContext"/> ở mức POCO thuần — không cần scene, không cần
    /// MonoBehaviour, nên chạy được trong EditMode.
    ///
    /// <para>Ngoài Play mode, <see cref="TutorialStateContext"/> bỏ qua độ trễ chuyển bước
    /// (không có coroutine để chạy), nên chuỗi tiến ngay khi gọi
    /// <see cref="TutorialStateContext.CompleteCurrent"/> — đúng thứ các test dưới đây cần.</para>
    /// </summary>
    public class TutorialTests
    {
        /// <summary>State giả: chỉ ghi lại số lần vào/ra, không đụng tới scene.</summary>
        private sealed class FakeTutorialState : ITutorialState
        {
            private readonly TutorialType type;

            public FakeTutorialState(TutorialType type)
            {
                this.type = type;
            }

            public TutorialType Type => type;
            public bool IsCompleted { get; private set; }
            public int EnterCount { get; private set; }
            public int ExitCount { get; private set; }
            public int UpdateCount { get; private set; }

            public void Enter()
            {
                IsCompleted = false;
                EnterCount++;
            }

            public void Exit()
            {
                ExitCount++;
            }

            public void Update()
            {
                UpdateCount++;
            }

            /// <summary>Bật cờ hoàn thành để context tự chuyển bước ở lần Tick kế tiếp.</summary>
            public void MarkCompleted()
            {
                IsCompleted = true;
            }
        }

        private TutorialConfiguration configuration;

        /// <summary>Chuỗi 4 bước rút gọn có đủ 2 bước gameplay để test.</summary>
        private static readonly TutorialType[] Chain =
        {
            TutorialType.MainMenuForcedPlay,
            TutorialType.WelcomeMessage,
            TutorialType.BasicHit,
            TutorialType.KitchenHit
        };

        [SetUp]
        public void SetUp()
        {
            configuration = ScriptableObject.CreateInstance<TutorialConfiguration>();
            configuration.enableTutorialSystem = true;
            configuration.startingTutorial = TutorialType.MainMenuForcedPlay;
            configuration.tutorialSteps = new List<TutorialConfiguration.TutorialStepConfig>
            {
                Step(TutorialType.MainMenuForcedPlay, TutorialType.WelcomeMessage, false),
                Step(TutorialType.WelcomeMessage, TutorialType.BasicHit, false),
                Step(TutorialType.BasicHit, TutorialType.KitchenHit, true),
                Step(TutorialType.KitchenHit, TutorialType.TutorialCompleted, true)
            };
        }

        [TearDown]
        public void TearDown()
        {
            if (configuration != null) Object.DestroyImmediate(configuration);
            configuration = null;
        }

        private static TutorialConfiguration.TutorialStepConfig Step(
            TutorialType type, TutorialType next, bool isGameplay)
        {
            return new TutorialConfiguration.TutorialStepConfig
            {
                tutorialType = type,
                isEnabled = true,
                isCompleted = false,
                nextTutorial = next,
                nextTutorialStartAfterDelay = 0f,
                isGameplayTutorial = isGameplay,
                requiredUIScreen = ScreenType.None
            };
        }

        // ------------------------------------------------------------------
        // TutorialConfiguration
        // ------------------------------------------------------------------

        [Test]
        public void GetNext_TraDungBuocKeTiepTheoChuoi()
        {
            Assert.AreEqual(TutorialType.WelcomeMessage,
                configuration.GetNext(TutorialType.MainMenuForcedPlay));
            Assert.AreEqual(TutorialType.BasicHit,
                configuration.GetNext(TutorialType.WelcomeMessage));
            Assert.AreEqual(TutorialType.KitchenHit,
                configuration.GetNext(TutorialType.BasicHit));
            Assert.AreEqual(TutorialType.TutorialCompleted,
                configuration.GetNext(TutorialType.KitchenHit));
        }

        [Test]
        public void GetNext_BuocKhongCoTrongBang_TraVeNone()
        {
            Assert.AreEqual(TutorialType.None, configuration.GetNext(TutorialType.SkillsTutorial));
        }

        [Test]
        public void ApplyCompletedSteps_DanhDauDungCacBuocDaXong()
        {
            configuration.ApplyCompletedSteps(new List<TutorialType>
            {
                TutorialType.MainMenuForcedPlay,
                TutorialType.BasicHit
            });

            Assert.IsTrue(configuration.Get(TutorialType.MainMenuForcedPlay).isCompleted);
            Assert.IsFalse(configuration.Get(TutorialType.WelcomeMessage).isCompleted);
            Assert.IsTrue(configuration.Get(TutorialType.BasicHit).isCompleted);
            Assert.IsFalse(configuration.Get(TutorialType.KitchenHit).isCompleted);
        }

        [Test]
        public void ApplyCompletedSteps_BuocKhongCoTrongDanhSach_BiDatLaiThanhChuaXong()
        {
            configuration.Get(TutorialType.WelcomeMessage).isCompleted = true;

            configuration.ApplyCompletedSteps(new List<TutorialType> { TutorialType.BasicHit });

            Assert.IsFalse(configuration.Get(TutorialType.WelcomeMessage).isCompleted,
                "ApplyCompletedSteps phải đồng bộ hai chiều với save data.");
        }

        [Test]
        public void AreAllGameplayStepsCompleted_ChiTrueKhiDuMoiBuocGameplay()
        {
            Assert.IsFalse(configuration.AreAllGameplayStepsCompleted());

            configuration.SetCompleted(TutorialType.BasicHit, true);
            Assert.IsFalse(configuration.AreAllGameplayStepsCompleted(),
                "Mới xong 1/2 bước gameplay thì chưa được tính là đủ.");

            configuration.SetCompleted(TutorialType.KitchenHit, true);
            Assert.IsTrue(configuration.AreAllGameplayStepsCompleted());
        }

        [Test]
        public void AreAllGameplayStepsCompleted_KhongTinhBuocKhongPhaiGameplay()
        {
            configuration.SetCompleted(TutorialType.MainMenuForcedPlay, true);
            configuration.SetCompleted(TutorialType.WelcomeMessage, true);

            Assert.IsFalse(configuration.AreAllGameplayStepsCompleted(),
                "Bước menu không được tính vào điều kiện hoàn thành gameplay.");
        }

        [Test]
        public void AreAllGameplayStepsCompleted_BangDayDu11Buoc_CanDu4BuocGameplay()
        {
            TutorialConfiguration full = BuildFullConfiguration();
            try
            {
                TutorialType[] gameplay =
                {
                    TutorialType.BasicHit,
                    TutorialType.TargetedHit,
                    TutorialType.KitchenHit,
                    TutorialType.TwoPointEasyBotMatch
                };

                for (int i = 0; i < gameplay.Length; i++)
                {
                    Assert.IsFalse(full.AreAllGameplayStepsCompleted(),
                        "Chưa xong đủ 4 bước gameplay mà đã báo hoàn thành ở vòng " + i);
                    full.SetCompleted(gameplay[i], true);
                }

                Assert.IsTrue(full.AreAllGameplayStepsCompleted());
            }
            finally
            {
                Object.DestroyImmediate(full);
            }
        }

        // ------------------------------------------------------------------
        // TutorialStateContext
        // ------------------------------------------------------------------

        [Test]
        public void Begin_VaoDungBuocDauTien()
        {
            Dictionary<TutorialType, FakeTutorialState> states;
            TutorialStateContext context = BuildContext(out states);

            List<TutorialType> started = new List<TutorialType>();
            context.OnStepStarted += started.Add;

            context.Begin(TutorialType.MainMenuForcedPlay);

            Assert.AreEqual(TutorialType.MainMenuForcedPlay, context.CurrentType);
            Assert.AreSame(states[TutorialType.MainMenuForcedPlay], context.CurrentState);
            Assert.AreEqual(1, states[TutorialType.MainMenuForcedPlay].EnterCount);
            CollectionAssert.AreEqual(new[] { TutorialType.MainMenuForcedPlay }, started);
        }

        [Test]
        public void CompleteCurrent_DiHetChuoiVaPhatOnTutorialFinished()
        {
            Dictionary<TutorialType, FakeTutorialState> states;
            TutorialStateContext context = BuildContext(out states);

            List<TutorialType> started = new List<TutorialType>();
            List<TutorialType> completed = new List<TutorialType>();
            int finishedCount = 0;

            context.OnStepStarted += started.Add;
            context.OnStepCompleted += completed.Add;
            context.OnTutorialFinished += () => finishedCount++;

            context.Begin(TutorialType.MainMenuForcedPlay);

            for (int i = 0; i < Chain.Length; i++) context.CompleteCurrent();

            CollectionAssert.AreEqual(Chain, started);
            CollectionAssert.AreEqual(Chain, completed);
            Assert.AreEqual(1, finishedCount, "OnTutorialFinished phải phát đúng một lần.");
            Assert.IsTrue(context.IsFinished);
            Assert.IsNull(context.CurrentState);
            Assert.AreEqual(TutorialType.None, context.CurrentType);

            for (int i = 0; i < Chain.Length; i++)
            {
                Assert.AreEqual(1, states[Chain[i]].ExitCount, "Mỗi state phải được Exit đúng 1 lần.");
                Assert.IsTrue(configuration.Get(Chain[i]).isCompleted);
            }
        }

        [Test]
        public void Tick_StateTuBaoXong_ChuyenSangBuocKeTiep()
        {
            Dictionary<TutorialType, FakeTutorialState> states;
            TutorialStateContext context = BuildContext(out states);

            context.Begin(TutorialType.MainMenuForcedPlay);

            context.Tick();
            Assert.AreEqual(TutorialType.MainMenuForcedPlay, context.CurrentType);
            Assert.AreEqual(1, states[TutorialType.MainMenuForcedPlay].UpdateCount);

            states[TutorialType.MainMenuForcedPlay].MarkCompleted();
            context.Tick();

            Assert.AreEqual(TutorialType.WelcomeMessage, context.CurrentType);
        }

        [Test]
        public void Begin_BuocBiTat_BiBoQua()
        {
            configuration.Get(TutorialType.WelcomeMessage).isEnabled = false;

            Dictionary<TutorialType, FakeTutorialState> states;
            TutorialStateContext context = BuildContext(out states);

            List<TutorialType> started = new List<TutorialType>();
            context.OnStepStarted += started.Add;

            context.Begin(TutorialType.MainMenuForcedPlay);
            context.CompleteCurrent();

            Assert.AreEqual(TutorialType.BasicHit, context.CurrentType,
                "Bước isEnabled = false phải bị nhảy qua.");
            Assert.AreEqual(0, states[TutorialType.WelcomeMessage].EnterCount);
            CollectionAssert.DoesNotContain(started, TutorialType.WelcomeMessage);
        }

        [Test]
        public void Begin_BuocDauTienBiTat_NhayThangSangBuocSau()
        {
            configuration.Get(TutorialType.MainMenuForcedPlay).isEnabled = false;

            Dictionary<TutorialType, FakeTutorialState> states;
            TutorialStateContext context = BuildContext(out states);

            context.Begin(TutorialType.MainMenuForcedPlay);

            Assert.AreEqual(TutorialType.WelcomeMessage, context.CurrentType);
            Assert.AreEqual(0, states[TutorialType.MainMenuForcedPlay].EnterCount);
        }

        [Test]
        public void Begin_BuocDaHoanThanh_BiBoQua()
        {
            configuration.ApplyCompletedSteps(new List<TutorialType>
            {
                TutorialType.MainMenuForcedPlay,
                TutorialType.WelcomeMessage
            });

            Dictionary<TutorialType, FakeTutorialState> states;
            TutorialStateContext context = BuildContext(out states);

            context.Begin(TutorialType.MainMenuForcedPlay);

            Assert.AreEqual(TutorialType.BasicHit, context.CurrentType);
        }

        [Test]
        public void Skip_DanhDauMoiBuocXongVaPhatOnTutorialFinished()
        {
            Dictionary<TutorialType, FakeTutorialState> states;
            TutorialStateContext context = BuildContext(out states);

            int finishedCount = 0;
            context.OnTutorialFinished += () => finishedCount++;

            context.Begin(TutorialType.MainMenuForcedPlay);
            context.Skip();

            Assert.AreEqual(1, finishedCount);
            Assert.IsTrue(context.IsFinished);

            for (int i = 0; i < Chain.Length; i++)
            {
                Assert.IsTrue(configuration.Get(Chain[i]).isCompleted);
            }
        }

        [Test]
        public void Stop_DungMayMaKhongPhatOnTutorialFinished()
        {
            Dictionary<TutorialType, FakeTutorialState> states;
            TutorialStateContext context = BuildContext(out states);

            int finishedCount = 0;
            context.OnTutorialFinished += () => finishedCount++;

            context.Begin(TutorialType.MainMenuForcedPlay);
            context.Stop();

            Assert.AreEqual(0, finishedCount);
            Assert.IsTrue(context.IsFinished);
            Assert.IsNull(context.CurrentState);
            Assert.AreEqual(1, states[TutorialType.MainMenuForcedPlay].ExitCount);
        }

        [Test]
        public void Tick_SauKhiKetThuc_KhongLamGiThem()
        {
            Dictionary<TutorialType, FakeTutorialState> states;
            TutorialStateContext context = BuildContext(out states);

            context.Begin(TutorialType.MainMenuForcedPlay);
            context.Skip();

            Assert.DoesNotThrow(() => context.Tick());
            Assert.IsNull(context.CurrentState);
        }

        // ------------------------------------------------------------------
        // Helper
        // ------------------------------------------------------------------

        /// <summary>Dựng context gắn với <see cref="configuration"/> và 4 state giả.</summary>
        private TutorialStateContext BuildContext(out Dictionary<TutorialType, FakeTutorialState> states)
        {
            TutorialStateContext context = new TutorialStateContext();
            context.configuration = configuration;

            states = new Dictionary<TutorialType, FakeTutorialState>();
            for (int i = 0; i < Chain.Length; i++)
            {
                FakeTutorialState state = new FakeTutorialState(Chain[i]);
                states[Chain[i]] = state;
                context.Register(state);
            }

            return context;
        }

        /// <summary>Dựng bảng cấu hình đầy đủ 11 bước giống asset do TutorialDataGenerator sinh ra.</summary>
        private static TutorialConfiguration BuildFullConfiguration()
        {
            TutorialConfiguration full = ScriptableObject.CreateInstance<TutorialConfiguration>();
            full.enableTutorialSystem = true;
            full.startingTutorial = TutorialType.MainMenuForcedPlay;
            full.tutorialSteps = new List<TutorialConfiguration.TutorialStepConfig>
            {
                Step(TutorialType.MainMenuForcedPlay, TutorialType.WelcomeMessage, false),
                Step(TutorialType.WelcomeMessage, TutorialType.CourtSidesAndKitchenInfo, false),
                Step(TutorialType.CourtSidesAndKitchenInfo, TutorialType.ReceiveInfo, false),
                Step(TutorialType.ReceiveInfo, TutorialType.CrossServeInfo, false),
                Step(TutorialType.CrossServeInfo, TutorialType.BasicHit, false),
                Step(TutorialType.BasicHit, TutorialType.TargetedHit, true),
                Step(TutorialType.TargetedHit, TutorialType.KitchenHit, true),
                Step(TutorialType.KitchenHit, TutorialType.TwoPointEasyBotMatch, true),
                Step(TutorialType.TwoPointEasyBotMatch, TutorialType.TutorialKitbagReward, true),
                Step(TutorialType.TutorialKitbagReward, TutorialType.ForcedGripUpgrade, false),
                Step(TutorialType.ForcedGripUpgrade, TutorialType.TutorialCompleted, false)
            };
            return full;
        }
    }
}
