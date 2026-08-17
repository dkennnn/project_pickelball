using System.Collections.Generic;
using StarterKit.UIKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Màn tuỳ biến hình thức: hai tab Balls / Paddles, mỗi tab là một lưới skin.
    /// Bấm skin đã mua thì trang bị, chưa mua thì trừ coin để mở qua
    /// <see cref="Shop.BuyBall"/> / <see cref="Shop.BuyRacket"/>.
    /// </summary>
    public class CustomizationUI : UIScreenBase
    {
        /// <inheritdoc/>
        public override ScreenType DefaultScreenType => ScreenType.CustomizationUI;

        /// <summary>Nút quay lại.</summary>
        [SerializeField] private Button backButton;

        /// <summary>Tiêu đề tab đang mở.</summary>
        [SerializeField] private TextMeshProUGUI titleTxt;

        /// <summary>Nút chuyển sang tab bóng.</summary>
        [SerializeField] private Button ballsButton;

        /// <summary>Nút chuyển sang tab vợt.</summary>
        [SerializeField] private Button paddleButton;

        /// <summary>Node bọc tab bóng.</summary>
        [SerializeField] private GameObject ballsContent;

        /// <summary>Node bọc tab vợt.</summary>
        [SerializeField] private GameObject paddleContent;

        /// <summary>Node cha lưới skin bóng.</summary>
        [SerializeField] private Transform ballsGrid;

        /// <summary>Node cha lưới skin vợt.</summary>
        [SerializeField] private Transform paddleGrid;

        /// <summary>Prefab một ô skin.</summary>
        [SerializeField] private ShopItemCellView skinCellPrefab;

        /// <summary>True nếu tab bóng đang mở.</summary>
        public bool IsBallTab { get; private set; } = true;

        private readonly List<ShopItemCellView> ballCells = new List<ShopItemCellView>();
        private readonly List<ShopItemCellView> paddleCells = new List<ShopItemCellView>();

        /// <inheritdoc/>
        public override void OnInit()
        {
            if (backButton != null) backButton.onClick.AddListener(HandleBack);
            if (ballsButton != null) ballsButton.onClick.AddListener(ShowBallTab);
            if (paddleButton != null) paddleButton.onClick.AddListener(ShowPaddleTab);
        }

        /// <inheritdoc/>
        public override void OnShow(object data)
        {
            Shop.OnShopChanged += RefreshAll;
            SetTab(IsBallTab);
        }

        /// <inheritdoc/>
        public override void OnHide()
        {
            Shop.OnShopChanged -= RefreshAll;
        }

        protected override void OnDestroy()
        {
            Shop.OnShopChanged -= RefreshAll;

            if (backButton != null) backButton.onClick.RemoveListener(HandleBack);
            if (ballsButton != null) ballsButton.onClick.RemoveListener(ShowBallTab);
            if (paddleButton != null) paddleButton.onClick.RemoveListener(ShowPaddleTab);

            for (int i = 0; i < ballCells.Count; i++)
            {
                if (ballCells[i] != null) ballCells[i].OnIndexClicked -= HandleBallClicked;
            }
            for (int i = 0; i < paddleCells.Count; i++)
            {
                if (paddleCells[i] != null) paddleCells[i].OnIndexClicked -= HandlePaddleClicked;
            }

            ballCells.Clear();
            paddleCells.Clear();

            base.OnDestroy();
        }

        /// <summary>Mở tab skin bóng.</summary>
        public void ShowBallTab() { SetTab(true); }

        /// <summary>Mở tab skin vợt.</summary>
        public void ShowPaddleTab() { SetTab(false); }

        /// <summary>Vẽ lại cả hai lưới skin.</summary>
        public void RefreshAll()
        {
            RefreshBalls();
            RefreshPaddles();
        }

        private void SetTab(bool ballTab)
        {
            IsBallTab = ballTab;

            if (ballsContent != null) ballsContent.SetActive(ballTab);
            if (paddleContent != null) paddleContent.SetActive(!ballTab);
            if (titleTxt != null) titleTxt.text = ballTab ? "Balls" : "Paddles";

            RefreshAll();
        }

        private void RefreshBalls()
        {
            Shop shop = ResolveShop();
            if (shop == null || ballsGrid == null || skinCellPrefab == null) return;

            List<BallShopItem> list = shop.ballsList;
            int count = list != null ? list.Count : 0;

            while (ballCells.Count < count)
            {
                ShopItemCellView cell = Instantiate(skinCellPrefab, ballsGrid);
                cell.OnIndexClicked += HandleBallClicked;
                ballCells.Add(cell);
            }

            for (int i = 0; i < ballCells.Count; i++)
            {
                ShopItemCellView cell = ballCells[i];
                if (cell == null) continue;

                bool used = i < count;
                cell.gameObject.SetActive(used);
                if (used) cell.Bind(list[i], i, shop.IsBallEquiped(i));
            }
        }

        private void RefreshPaddles()
        {
            Shop shop = ResolveShop();
            if (shop == null || paddleGrid == null || skinCellPrefab == null) return;

            List<ShopItem> list = shop.racketsList;
            int count = list != null ? list.Count : 0;

            while (paddleCells.Count < count)
            {
                ShopItemCellView cell = Instantiate(skinCellPrefab, paddleGrid);
                cell.OnIndexClicked += HandlePaddleClicked;
                paddleCells.Add(cell);
            }

            for (int i = 0; i < paddleCells.Count; i++)
            {
                ShopItemCellView cell = paddleCells[i];
                if (cell == null) continue;

                bool used = i < count;
                cell.gameObject.SetActive(used);
                if (used) cell.Bind(list[i], i, shop.IsRacketEquiped(i));
            }
        }

        private void HandleBallClicked(int index)
        {
            Shop shop = ResolveShop();
            if (shop == null) return;

            if (shop.IsBallPurchased(index))
            {
                shop.EquipBall(index);
            }
            else if (!shop.BuyBall(index) && ToastHandler.HasInstance)
            {
                ToastHandler.Instance.Show("Not enough coins");
            }

            RefreshBalls();
        }

        private void HandlePaddleClicked(int index)
        {
            Shop shop = ResolveShop();
            if (shop == null) return;

            if (shop.IsRacketPurchased(index))
            {
                shop.EquipRacket(index);
            }
            else if (!shop.BuyRacket(index) && ToastHandler.HasInstance)
            {
                ToastHandler.Instance.Show("Not enough coins");
            }

            RefreshPaddles();
        }

        private static Shop ResolveShop()
        {
            GameData gameData = GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null;
            return gameData != null ? gameData.shopData : null;
        }

        private void HandleBack()
        {
            if (UIController.HasInstance) UIController.Instance.Show(ScreenType.MainMenu);
            else Hide();
        }
    }
}
