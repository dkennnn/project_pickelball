using System.Collections.Generic;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Bước 10 — trao túi thưởng tutorial. Quay bảng thưởng của
    /// <see cref="DynamicKitbag"/> "TutorialKitbag" qua
    /// <see cref="RewardManager.GenerateKitbagRewards(DynamicKitbag)"/>, cộng thẳng vào
    /// <see cref="GameData"/> rồi mở <see cref="ScreenType.RewardUI"/> để khoe.
    /// <para>
    /// Tên class giữ hậu tố <c>TutorialState</c> theo yêu cầu; bản gốc đặt là
    /// <c>TutorialKitbagRewardState</c>.
    /// </para>
    /// </summary>
    public class TutorialKitbagRewardTutorialState : BaseTutorialState
    {
        /// <summary>Tên túi thưởng cần dùng, khớp với asset DynamicKitbag_Tutorial.</summary>
        public const string KitbagName = "TutorialKitbag";

        private const string BeforeMessage = "Here's your first Kitbag — tap to open it.";
        private const string AfterMessage = "Those Grip Tazos will come in handy right now.";

        /// <summary>Thời gian chờ tối đa cho người chơi mở túi trước khi bước tự kết thúc.</summary>
        private const float RewardScreenTimeout = 30f;

        private List<CollectibleReward> granted;
        private bool rewardShown;

        /// <summary>Khởi tạo bước với ngữ cảnh dùng chung.</summary>
        /// <param name="context">Ngữ cảnh do <see cref="TutorialManager"/> cấp.</param>
        public TutorialKitbagRewardTutorialState(TutorialStateContext context) : base(context) { }

        /// <inheritdoc/>
        public override TutorialType Type => TutorialType.TutorialKitbagReward;

        /// <summary>Danh sách phần thưởng vừa trao; null nếu chưa quay.</summary>
        public List<CollectibleReward> GrantedRewards => granted;

        /// <inheritdoc/>
        protected override void OnEnter()
        {
            rewardShown = false;
            granted = null;

            ShowMessage(BeforeMessage);
            GrantKitbag();
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            if (!rewardShown) return;

            // Người chơi đã đóng màn thưởng, hoặc quá thời gian chờ → sang bước tiếp.
            if (ElapsedTime >= RewardScreenTimeout)
            {
                Complete();
                return;
            }

            if (StarterKit.UIKit.UIController.Instance.IsShown(ScreenType.RewardUI)) return;

            ShowMessage(AfterMessage);
            Complete();
        }

        /// <summary>
        /// Quay túi, cộng thưởng vào tài khoản rồi mở màn khoe thưởng.
        /// Thiếu bất kỳ tham chiếu nào cũng chỉ ghi cảnh báo và hoàn thành bước.
        /// </summary>
        private void GrantKitbag()
        {
            DynamicKitbag kitbag = context != null ? context.tutorialKitbag : null;
            if (kitbag == null)
            {
                Debug.LogWarning("[TutorialKitbagRewardTutorialState] Chưa gán DynamicKitbag \"" +
                                 KitbagName + "\", bỏ qua bước thưởng.");
                Complete();
                return;
            }

            if (!RewardManager.HasInstance)
            {
                Debug.LogWarning("[TutorialKitbagRewardTutorialState] Không có RewardManager, bỏ qua bước thưởng.");
                Complete();
                return;
            }

            granted = RewardManager.Instance.GenerateKitbagRewards(kitbag);
            if (granted == null || granted.Count == 0)
            {
                Complete();
                return;
            }

            GameData gameData = ResolveGameData();
            if (gameData != null)
            {
                for (int i = 0; i < granted.Count; i++)
                {
                    CollectibleReward reward = granted[i];
                    if (reward != null) reward.GrantReward(gameData);
                }

                if (SavedDataHandler.HasInstance) SavedDataHandler.Instance.RequestSave();
            }

            if (!StarterKit.UIKit.UIController.HasInstance)
            {
                // Không có UIController để khoe thưởng — thưởng đã cộng rồi nên đi tiếp luôn.
                Complete();
                return;
            }

            ShowScreen(ScreenType.RewardUI, granted);
            rewardShown = true;
        }

        /// <summary>Lấy <see cref="GameData"/> từ context, dự phòng là bản trong SavedDataHandler.</summary>
        private GameData ResolveGameData()
        {
            if (context != null && context.gameData != null) return context.gameData;
            if (SavedDataHandler.HasInstance) return SavedDataHandler.Instance._gameData;
            return null;
        }
    }
}
