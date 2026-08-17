using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Ô "nhãn — giá trị" dùng chung: một dòng thống kê cuối trận, một phần thưởng
    /// trong màn Reward, hoặc một mục booster trong màn Collections.
    /// </summary>
    public class StatItemCell : MonoBehaviour
    {
        /// <summary>Nhãn của dòng.</summary>
        [SerializeField] private TextMeshProUGUI label;

        /// <summary>Giá trị của dòng.</summary>
        [SerializeField] private TextMeshProUGUI value;

        /// <summary>Biểu tượng đi kèm (tuỳ chọn).</summary>
        [SerializeField] private Image icon;

        /// <summary>Nút bấm tuỳ chọn của ô.</summary>
        [SerializeField] private Button button;

        /// <summary>Phát khi người chơi bấm vào ô; tham số là nhãn của ô.</summary>
        public event Action<string> OnClicked;

        /// <summary>Nhãn đang hiển thị.</summary>
        public string Label { get; private set; }

        private bool wired;

        /// <summary>
        /// Gắn một cặp nhãn — giá trị vào ô.
        /// </summary>
        /// <param name="labelText">Nhãn hiển thị bên trái.</param>
        /// <param name="valueText">Giá trị hiển thị bên phải.</param>
        /// <param name="sprite">Biểu tượng đi kèm; null thì giữ ảnh sẵn có.</param>
        public void Bind(string labelText, string valueText, Sprite sprite = null)
        {
            Wire();

            Label = labelText ?? string.Empty;

            if (label != null) label.text = Label;
            if (value != null) value.text = valueText ?? string.Empty;

            if (icon != null)
            {
                if (sprite != null) icon.sprite = sprite;
                icon.enabled = icon.sprite != null;
            }
        }

        /// <summary>Gắn một phần thưởng thu thập được vào ô.</summary>
        /// <param name="reward">Phần thưởng cần hiển thị; null sẽ ẩn ô.</param>
        public void Bind(CollectibleReward reward)
        {
            if (reward == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            Bind(FormatRewardName(reward.Type), "x" + Utilities.FormatCount(reward.Amount));
        }

        /// <summary>Gắn một phần thưởng cấu hình sẵn vào ô.</summary>
        /// <param name="reward">Phần thưởng cần hiển thị; null sẽ ẩn ô.</param>
        public void Bind(DynamicReward reward)
        {
            if (reward == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            Bind(FormatRewardName(reward.rewardType), "x" + Utilities.FormatCount(reward.value));
        }

        /// <summary>Đổi tên enum phần thưởng sang chuỗi dễ đọc ("PaddleTazos" → "PADDLE TAZOS").</summary>
        /// <param name="type">Loại phần thưởng.</param>
        public static string FormatRewardName(RewardType type)
        {
            string raw = type.ToString();
            System.Text.StringBuilder builder = new System.Text.StringBuilder(raw.Length + 4);

            for (int i = 0; i < raw.Length; i++)
            {
                if (i > 0 && char.IsUpper(raw[i])) builder.Append(' ');
                builder.Append(char.ToUpperInvariant(raw[i]));
            }
            return builder.ToString();
        }

        private void Wire()
        {
            if (wired) return;
            wired = true;

            if (button != null) button.onClick.AddListener(HandleClick);
        }

        private void HandleClick()
        {
            OnClicked?.Invoke(Label);
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(HandleClick);
            OnClicked = null;
        }
    }
}
