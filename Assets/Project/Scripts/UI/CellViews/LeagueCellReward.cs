using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Một ô phần thưởng bên trong một mốc league (node <c>LeagueLevelRewardItem N</c>).
    /// Hiển thị ảnh phần thưởng, số lượng và trạng thái đã nhận.
    /// </summary>
    public class LeagueCellReward : MonoBehaviour
    {
        /// <summary>Ảnh phần thưởng (node <c>BG_L2/RewardImage</c>).</summary>
        [SerializeField] private Image rewardImage;

        /// <summary>Các lớp nền của ô; ẩn hết khi ô chỉ khoe ảnh trần.</summary>
        [SerializeField] private List<Image> bgImages = new List<Image>();

        /// <summary>Vầng sáng sau ảnh (node <c>BG_L2/BackGlow</c>).</summary>
        [SerializeField] private GameObject backGlow;

        /// <summary>Ô chữ số lượng; layout gốc không có, để trống thì bỏ qua.</summary>
        [SerializeField] private TextMeshProUGUI amountText;

        /// <summary>Dấu tick báo đã nhận; layout gốc không có, để trống thì bỏ qua.</summary>
        [SerializeField] private GameObject claimedMark;

        /// <summary>Nút bấm của ô (node <c>BG</c>).</summary>
        [SerializeField] private Button button;

        /// <summary>
        /// Ảnh theo từng <see cref="RewardType"/>, xếp đúng thứ tự giá trị enum.
        /// Project chưa có bảng tra ảnh theo loại phần thưởng nên phải gán tay trên prefab.
        /// </summary>
        [SerializeField] private List<Sprite> rewardSpritesByType = new List<Sprite>();

        /// <summary>Phát khi người chơi bấm vào ô.</summary>
        public event Action<DynamicReward> OnClicked;

        /// <summary>Phần thưởng đang gắn vào ô.</summary>
        public DynamicReward BoundReward { get; private set; }

        /// <summary>True khi phần thưởng đã được nhận.</summary>
        public bool IsClaimed { get; private set; }

        private bool wired;

        /// <summary>
        /// Gắn một phần thưởng vào ô.
        /// </summary>
        /// <param name="reward">Phần thưởng cần hiển thị; null sẽ ẩn ô.</param>
        /// <param name="claimed">True khi phần thưởng đã được nhận.</param>
        public void Bind(DynamicReward reward, bool claimed)
        {
            Wire();

            BoundReward = reward;
            IsClaimed = claimed;

            if (reward == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            SetReward(GetSprite(reward.rewardType));

            if (amountText != null)
            {
                amountText.text = reward.value > 0 ? "x" + Utilities.FormatCount(reward.value) : string.Empty;
            }

            if (claimedMark != null) claimedMark.SetActive(claimed);
            if (backGlow != null) backGlow.SetActive(!claimed);
        }

        /// <summary>
        /// Đặt thẳng ảnh phần thưởng — giữ nguyên chữ ký của bản gốc.
        /// </summary>
        /// <param name="rewardSprite">Ảnh cần hiển thị; null sẽ tắt ảnh.</param>
        /// <param name="showBG">False sẽ ẩn toàn bộ lớp nền, chỉ còn ảnh phần thưởng.</param>
        public void SetReward(Sprite rewardSprite, bool showBG = true)
        {
            if (rewardImage != null)
            {
                if (rewardSprite != null) rewardImage.sprite = rewardSprite;
                rewardImage.enabled = rewardImage.sprite != null;
            }

            for (int i = 0; i < bgImages.Count; i++)
            {
                Image bg = bgImages[i];
                if (bg != null) bg.enabled = showBG;
            }
        }

        private Sprite GetSprite(RewardType type)
        {
            if (rewardSpritesByType == null) return null;

            int index = (int)type;
            if (index < 0 || index >= rewardSpritesByType.Count) return null;

            return rewardSpritesByType[index];
        }

        private void Wire()
        {
            if (wired) return;
            wired = true;

            if (button != null) button.onClick.AddListener(HandleClick);
        }

        private void HandleClick()
        {
            OnClicked?.Invoke(BoundReward);
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(HandleClick);
            OnClicked = null;
        }
    }
}
