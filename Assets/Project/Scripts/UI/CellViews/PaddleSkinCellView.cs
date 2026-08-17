using System;
using StarterKit.UIKit;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Ô chọn skin vợt trong màn <see cref="CustomizationUI"/> (prefab <c>PaddleSkinCellView</c>).
    /// Chưa mua thì làm xám; đang trang bị thì đổi nền và bật vầng sáng.
    /// </summary>
    public class PaddleSkinCellView : MonoBehaviour
    {
        /// <summary>Ảnh cây vợt (node <c>BG/PaddleMask/PaddleImage</c>).</summary>
        [Header("UI Components")]
        [SerializeField] private Image paddleImage;

        /// <summary>Nút chọn (node <c>BG - Shiny</c>).</summary>
        [SerializeField] private Button selectButton;

        /// <summary>Nền của ô, đổi sprite theo trạng thái trang bị.</summary>
        [SerializeField] private Image bgImage;

        /// <summary>Sprite nền khi ô đang được trang bị.</summary>
        [SerializeField] private Sprite selectedSprite;

        /// <summary>Sprite nền khi ô không được trang bị.</summary>
        [SerializeField] private Sprite unSelectedSprite;

        /// <summary>Vầng sáng nền (node <c>BG - Shiny/Image - BGglow</c>).</summary>
        [Header("BGglow")]
        [SerializeField] private Image bgGlow;

        /// <summary>Hiệu ứng làm xám khi chưa mua (node <c>BG</c>).</summary>
        [Header("GrayScale")]
        [SerializeField] private GreyscaleUIHierarchy greyscaleUIHierarchy;

        /// <summary>Phát khi người chơi bấm chọn ô; tham số là chỉ số skin trong <see cref="Shop.racketsList"/>.</summary>
        public event Action<int> OnSelected;

        /// <summary>Chỉ số skin đang gắn vào ô; -1 khi ô rỗng.</summary>
        public int CurrentItemIndex { get; private set; } = -1;

        /// <summary>True khi skin đã được mua.</summary>
        public bool IsPurchased { get; private set; }

        /// <summary>True khi skin đang được trang bị.</summary>
        public bool IsEquipped { get; private set; }

        private bool wired;

        /// <summary>
        /// Gắn một skin vợt vào ô.
        /// </summary>
        /// <param name="item">Skin vợt cần hiển thị; null sẽ ẩn ô.</param>
        /// <param name="index">Chỉ số skin trong <see cref="Shop.racketsList"/>.</param>
        /// <param name="purchased">True khi người chơi đã sở hữu skin.</param>
        /// <param name="equipped">True khi skin đang được trang bị.</param>
        public void Bind(ShopItem item, int index, bool purchased, bool equipped)
        {
            Wire();

            CurrentItemIndex = index;
            IsPurchased = purchased;
            IsEquipped = equipped;

            if (item == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            if (paddleImage != null)
            {
                if (item.icon != null) paddleImage.sprite = item.icon;
                paddleImage.enabled = paddleImage.sprite != null;
            }

            HandleBGEffect(purchased);
            HandleBGglow(equipped);
        }

        /// <summary>Đổi nền theo trạng thái trang bị và làm xám khi chưa mua.</summary>
        /// <param name="isPurchased">True khi skin đã được mua.</param>
        public void HandleBGEffect(bool isPurchased)
        {
            if (bgImage != null)
            {
                Sprite sprite = IsEquipped ? selectedSprite : unSelectedSprite;
                if (sprite != null) bgImage.sprite = sprite;
            }

            if (greyscaleUIHierarchy != null) greyscaleUIHierarchy.SetGreyscale(!isPurchased);
        }

        /// <summary>Bật/tắt vầng sáng nền.</summary>
        /// <param name="toEnable">True để bật.</param>
        public void HandleBGglow(bool toEnable)
        {
            if (bgGlow != null) bgGlow.enabled = toEnable;
        }

        private void Wire()
        {
            if (wired) return;
            wired = true;

            if (selectButton != null) selectButton.onClick.AddListener(SelectPaddle);
        }

        private void SelectPaddle()
        {
            if (CurrentItemIndex < 0) return;
            OnSelected?.Invoke(CurrentItemIndex);
        }

        private void OnDestroy()
        {
            if (selectButton != null) selectButton.onClick.RemoveListener(SelectPaddle);
            OnSelected = null;
        }
    }
}
