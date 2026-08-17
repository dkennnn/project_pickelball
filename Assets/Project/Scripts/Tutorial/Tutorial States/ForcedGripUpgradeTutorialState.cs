using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Bước 11 — ép người chơi tiêu số Grip Tazo vừa nhận: mở phòng thay đồ, chỉ vào nút nâng cấp
    /// bằng tazo và chờ tới khi <see cref="Item.currentLevel"/> của grip tăng.
    /// <para>
    /// Bước KHÔNG tự nâng cấp hộ; nó chỉ theo dõi kết quả của
    /// <see cref="Shop.UpgradeItemWithTazos"/> do UI gọi. <see cref="ForceUpgradeNow"/> tồn tại
    /// để kịch bản test/QA có thể thúc bước qua mà không cần bấm tay.
    /// </para>
    /// </summary>
    public class ForcedGripUpgradeTutorialState : BaseTutorialState
    {
        private const string Message = "Spend your Grip Tazos: upgrade your Grip in the Dressing Room.";
        private const string DoneMessage = "Upgraded! Stronger grip, better control.";

        private Item gripItem;
        private int startingLevel;
        private bool completedShown;

        /// <summary>Khởi tạo bước với ngữ cảnh dùng chung.</summary>
        /// <param name="context">Ngữ cảnh do <see cref="TutorialManager"/> cấp.</param>
        public ForcedGripUpgradeTutorialState(TutorialStateContext context) : base(context) { }

        /// <inheritdoc/>
        public override TutorialType Type => TutorialType.ForcedGripUpgrade;

        /// <summary>Vật phẩm grip đang được theo dõi; null nếu không tìm thấy.</summary>
        public Item TrackedGrip => gripItem;

        /// <inheritdoc/>
        protected override void OnEnter()
        {
            completedShown = false;
            gripItem = ResolveGripItem();

            if (gripItem == null)
            {
                Debug.LogWarning("[ForcedGripUpgradeTutorialState] Không tìm thấy vật phẩm Grip, bỏ qua bước.");
                Complete();
                return;
            }

            startingLevel = gripItem.currentLevel;

            ShowScreen(ScreenType.DressingRoomUI);
            ShowMessage(Message);
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            if (gripItem == null) return;
            if (gripItem.currentLevel <= startingLevel) return;

            if (!completedShown)
            {
                completedShown = true;
                ShowMessage(DoneMessage);
            }

            Complete();
        }

        /// <inheritdoc/>
        protected override void OnExit()
        {
            gripItem = null;
        }

        /// <summary>
        /// Thực hiện nâng cấp thay người chơi (dùng cho QA / bỏ qua bước).
        /// Trả về false nếu thiếu shop hoặc grip.
        /// </summary>
        public bool ForceUpgradeNow()
        {
            Shop shop = ResolveShop();
            if (shop == null || gripItem == null) return false;

            shop.UpgradeItemWithTazos(gripItem);
            return true;
        }

        /// <summary>Tìm vật phẩm grip đang trang bị; dự phòng là vật phẩm đầu tiên của tab Grip.</summary>
        private Item ResolveGripItem()
        {
            GameData gameData = ResolveGameData();

            if (gameData != null && gameData.playerLoadout != null)
            {
                Item selected = gameData.playerLoadout.GetSelectedItemByType(ShopItemType.Grip);
                if (selected != null) return selected;
            }

            Shop shop = ResolveShop();
            if (shop == null || shop.shopCategories == null) return null;

            for (int i = 0; i < shop.shopCategories.Count; i++)
            {
                ShopCategoryData category = shop.shopCategories[i];
                if (category == null || category.categoryType != ShopItemType.Grip) continue;

                if (category.selectedItem != null) return category.selectedItem;
                if (category.items != null && category.items.Count > 0) return category.items[0];
            }
            return null;
        }

        /// <summary>Lấy <see cref="Shop"/> từ GameData, dự phòng là bản trong SavedDataHandler.</summary>
        private Shop ResolveShop()
        {
            GameData gameData = ResolveGameData();
            if (gameData != null && gameData.shopData != null) return gameData.shopData;

            return SavedDataHandler.HasInstance ? SavedDataHandler.Instance._shop : null;
        }

        /// <summary>Lấy <see cref="GameData"/> từ context, dự phòng là bản trong SavedDataHandler.</summary>
        private GameData ResolveGameData()
        {
            if (context != null && context.gameData != null) return context.gameData;
            return SavedDataHandler.HasInstance ? SavedDataHandler.Instance._gameData : null;
        }
    }
}
