using System;
using StarterKit.UIKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Một ô trong màn <see cref="CollectionsUI"/>: ảnh vật phẩm, tên, số lượng đang có và
    /// trạng thái sở hữu (chưa sở hữu thì làm xám và tắt vầng sáng).
    /// </summary>
    public class CollectionCellView : MonoBehaviour
    {
        /// <summary>Ảnh vật phẩm (node <c>BG/Icon</c>).</summary>
        [SerializeField] private Image icon;

        /// <summary>Tên vật phẩm (node <c>BG/NameTxt</c>).</summary>
        [SerializeField] private TextMeshProUGUI nameTxt;

        /// <summary>Node bọc phần đếm số lượng (node <c>BG/CountHolder</c>).</summary>
        [SerializeField] private GameObject countHolder;

        /// <summary>Ô chữ số lượng (node <c>BG/CountHolder/CountHolder/CountText</c>).</summary>
        [SerializeField] private TextMeshProUGUI countText;

        /// <summary>Vầng sáng của ô đã sở hữu (node <c>BG/Glow</c>).</summary>
        [SerializeField] private GameObject glow;

        /// <summary>Hiệu ứng làm xám khi chưa sở hữu (node <c>BG</c>).</summary>
        [SerializeField] private GreyscaleUIHierarchy greyscaleUIHierarchy;

        /// <summary>Nút bấm của ô; để trống thì ô không bấm được.</summary>
        [SerializeField] private Button button;

        /// <summary>Góc nghiêng áp cho ảnh khi ô yêu cầu xoay biểu tượng.</summary>
        [SerializeField] private Vector3 rotatedIconRotation = new Vector3(0f, 0f, -45f);

        /// <summary>Phát khi người chơi bấm vào ô đang gắn một <see cref="Item"/>.</summary>
        public event Action<Item> OnClicked;

        /// <summary>Vật phẩm đang gắn vào ô; null nếu ô đang hiển thị dữ liệu thô.</summary>
        public Item BoundItem { get; private set; }

        /// <summary>True khi ô đang ở trạng thái đã sở hữu.</summary>
        public bool IsOwned { get; private set; }

        private bool wired;

        /// <summary>
        /// Gắn một vật phẩm vào ô.
        /// </summary>
        /// <param name="item">Vật phẩm cần hiển thị; null sẽ ẩn ô.</param>
        /// <param name="owned">True khi người chơi đã sở hữu vật phẩm.</param>
        public void Bind(Item item, bool owned)
        {
            Wire();
            BoundItem = item;

            if (item == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            string label = string.IsNullOrEmpty(item.itemName) ? item.itemType.ToString() : item.itemName;
            SetData(item.itemSprite, label, owned ? item.currentLevel + 1 : 0, owned);
        }

        /// <summary>
        /// Gắn dữ liệu thô vào ô (dùng cho booster hoặc mục không có <see cref="Item"/>).
        /// </summary>
        /// <param name="iconSprite">Ảnh hiển thị; null sẽ tắt ảnh.</param>
        /// <param name="displayName">Tên hiển thị.</param>
        /// <param name="count">Số lượng; nhỏ hơn hoặc bằng 0 sẽ ẩn phần đếm.</param>
        /// <param name="owned">True khi đã sở hữu (không làm xám, bật vầng sáng).</param>
        /// <param name="shouldRotateIcon">True để nghiêng ảnh theo <c>rotatedIconRotation</c>.</param>
        public void SetData(Sprite iconSprite, string displayName, int count, bool owned, bool shouldRotateIcon = false)
        {
            Wire();

            IsOwned = owned;

            if (icon != null)
            {
                if (iconSprite != null) icon.sprite = iconSprite;
                icon.enabled = icon.sprite != null;
                icon.rectTransform.localEulerAngles = shouldRotateIcon ? rotatedIconRotation : Vector3.zero;
            }

            if (nameTxt != null) nameTxt.text = displayName ?? string.Empty;

            bool showCount = owned && count > 0;
            if (countHolder != null) countHolder.SetActive(showCount);
            if (countText != null) countText.text = showCount ? "x" + Utilities.FormatCount(count) : string.Empty;

            if (glow != null) glow.SetActive(owned);
            if (greyscaleUIHierarchy != null) greyscaleUIHierarchy.SetGreyscale(!owned);
        }

        private void Wire()
        {
            if (wired) return;
            wired = true;

            if (button != null) button.onClick.AddListener(HandleClick);
        }

        private void HandleClick()
        {
            if (BoundItem == null) return;
            OnClicked?.Invoke(BoundItem);
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(HandleClick);
            OnClicked = null;
        }
    }
}
