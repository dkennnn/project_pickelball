using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Một thanh chỉ số (Agility / Accuracy / Power / Swing / Spin / Speed / Stamina):
    /// phần đặc là giá trị hiện tại, phần mờ phía sau là giá trị sau khi nâng cấp.
    /// </summary>
    public class ProfilePropertyCellView : MonoBehaviour
    {
        /// <summary>Tên chỉ số.</summary>
        [SerializeField] private TextMeshProUGUI propertyName;

        /// <summary>Biểu tượng chỉ số.</summary>
        [SerializeField] private Image icon;

        /// <summary>Thanh giá trị hiện tại (fillAmount).</summary>
        [SerializeField] private Image currentProgress;

        /// <summary>Thanh giá trị sau khi nâng cấp (fillAmount).</summary>
        [SerializeField] private Image upgradeProgress;

        /// <summary>Nền của thanh.</summary>
        [SerializeField] private Image progressBG;

        /// <summary>Số điểm chỉ số tăng thêm nếu nâng cấp.</summary>
        [SerializeField] private TextMeshProUGUI upgradeAmountTxt;

        /// <summary>Nút bấm tuỳ chọn của ô.</summary>
        [SerializeField] private Button button;

        /// <summary>Phát khi người chơi bấm vào ô.</summary>
        public event Action<PropertyType> OnClicked;

        /// <summary>Chỉ số đang gắn vào ô.</summary>
        public ProfileProperty BoundProperty { get; private set; }

        private bool wired;

        /// <summary>
        /// Gắn dữ liệu một chỉ số vào ô. Thang hiển thị lấy theo
        /// <see cref="ProfileProperty.maxValue"/>; nếu bằng 0 thì mặc định là 100.
        /// </summary>
        /// <param name="property">Chỉ số cần hiển thị; null sẽ làm rỗng ô.</param>
        public void Bind(ProfileProperty property)
        {
            Bind(property, null);
        }

        /// <summary>
        /// Gắn dữ liệu một chỉ số kèm bảng hiển thị (tên đẹp + icon).
        /// </summary>
        /// <param name="property">Chỉ số cần hiển thị; null sẽ làm rỗng ô.</param>
        /// <param name="display">Thông tin hiển thị của chỉ số; có thể null.</param>
        public void Bind(ProfileProperty property, PropertyData display)
        {
            Wire();
            BoundProperty = property;

            if (property == null)
            {
                if (propertyName != null) propertyName.text = string.Empty;
                if (currentProgress != null) currentProgress.fillAmount = 0f;
                if (upgradeProgress != null) upgradeProgress.gameObject.SetActive(false);
                if (upgradeAmountTxt != null) upgradeAmountTxt.gameObject.SetActive(false);
                return;
            }

            if (propertyName != null)
            {
                propertyName.text = display != null && !string.IsNullOrEmpty(display.propertyName)
                    ? display.propertyName
                    : property.propertyType.ToString().ToUpperInvariant();
            }

            if (icon != null)
            {
                Sprite sprite = display != null ? display.propertyIcon : null;
                icon.enabled = sprite != null;
                if (sprite != null) icon.sprite = sprite;
            }

            float max = property.maxValue > 0f ? property.maxValue : 100f;
            float current = Mathf.Clamp01(property.currentValue / max);
            float next = Mathf.Clamp01(property.nextLevelValue / max);

            if (currentProgress != null) currentProgress.fillAmount = current;
            if (progressBG != null) progressBG.enabled = true;

            float delta = property.nextLevelValue - property.currentValue;
            bool hasUpgrade = delta > 0.001f;

            if (upgradeProgress != null)
            {
                upgradeProgress.gameObject.SetActive(hasUpgrade);
                upgradeProgress.fillAmount = next;
            }

            if (upgradeAmountTxt != null)
            {
                upgradeAmountTxt.gameObject.SetActive(hasUpgrade);
                if (hasUpgrade) upgradeAmountTxt.text = "+" + Mathf.RoundToInt(delta);
            }
        }

        private void Wire()
        {
            if (wired) return;
            wired = true;

            if (button != null) button.onClick.AddListener(HandleClick);
        }

        private void HandleClick()
        {
            OnClicked?.Invoke(BoundProperty != null ? BoundProperty.propertyType : PropertyType.None);
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(HandleClick);
            OnClicked = null;
        }
    }
}
