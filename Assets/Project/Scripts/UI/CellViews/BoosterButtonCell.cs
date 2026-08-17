using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Nút một loại booster trong trận: hiện số lượng còn lại và vòng thời gian
    /// khi booster đang chạy.
    /// </summary>
    public class BoosterButtonCell : MonoBehaviour
    {
        /// <summary>Nút bấm; để trống thì lấy Button trên chính node.</summary>
        [SerializeField] private Button button;

        /// <summary>Ảnh biểu tượng booster; để trống thì lấy Image trên chính node.</summary>
        [SerializeField] private Image icon;

        /// <summary>Số lượng booster còn trong kho.</summary>
        [SerializeField] private TextMeshProUGUI count;

        /// <summary>Vòng thời gian còn lại của booster (fillAmount).</summary>
        [SerializeField] private Image progressImg;

        /// <summary>Phát khi người chơi bấm dùng booster.</summary>
        public event Action<BoosterType> OnClicked;

        /// <summary>Loại booster đang gắn vào nút.</summary>
        public BoosterType Type { get; private set; } = BoosterType.None;

        /// <summary>Số lượng đang hiển thị.</summary>
        public int Count { get; private set; }

        private bool wired;

        /// <summary>
        /// Gắn một loại booster kèm số lượng vào nút.
        /// </summary>
        /// <param name="type">Loại booster.</param>
        /// <param name="amount">Số lượng còn trong kho.</param>
        /// <param name="sprite">Ảnh biểu tượng; có thể null để giữ ảnh sẵn có.</param>
        public void Bind(BoosterType type, int amount, Sprite sprite = null)
        {
            Wire();

            Type = type;
            Count = Mathf.Max(0, amount);

            if (icon != null && sprite != null) icon.sprite = sprite;
            if (count != null) count.text = Count.ToString();
            if (button != null) button.interactable = Count > 0 && type != BoosterType.None;

            SetProgress(0f);
        }

        /// <summary>Cập nhật riêng số lượng còn lại.</summary>
        /// <param name="amount">Số lượng mới.</param>
        public void SetCount(int amount)
        {
            Count = Mathf.Max(0, amount);
            if (count != null) count.text = Count.ToString();
            if (button != null) button.interactable = Count > 0 && Type != BoosterType.None;
        }

        /// <summary>Cập nhật vòng thời gian còn lại của booster.</summary>
        /// <param name="normalized">Tỉ lệ thời gian còn lại trong khoảng 0..1.</param>
        public void SetProgress(float normalized)
        {
            if (progressImg == null) return;

            float value = Mathf.Clamp01(normalized);
            progressImg.fillAmount = value;
            progressImg.enabled = value > 0f;
        }

        private void Wire()
        {
            if (wired) return;
            wired = true;

            if (button == null) button = GetComponent<Button>();
            if (icon == null) icon = GetComponent<Image>();
            if (button != null) button.onClick.AddListener(HandleClick);
        }

        private void HandleClick()
        {
            if (Type == BoosterType.None || Count <= 0) return;
            OnClicked?.Invoke(Type);
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(HandleClick);
            OnClicked = null;
        }
    }
}
