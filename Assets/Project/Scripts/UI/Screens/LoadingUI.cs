using StarterKit.UIKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Màn chờ khởi động: thanh tiến trình và dòng chữ "LOADING..".
    /// Tự đóng và mở màn chính khi <see cref="GameBootstrap"/> báo đã nạp xong dữ liệu.
    /// </summary>
    public class LoadingUI : UIScreenBase
    {
        /// <inheritdoc/>
        public override ScreenType DefaultScreenType => ScreenType.LoadingScreen;

        /// <summary>Thanh tiến trình dạng Slider.</summary>
        [SerializeField] private Slider progressBar;

        /// <summary>Ảnh fill của thanh tiến trình (dùng khi không có Slider).</summary>
        [SerializeField] private Image filler;

        /// <summary>Dòng chữ trạng thái.</summary>
        [SerializeField] private TextMeshProUGUI textTMPLoading;

        /// <summary>Biểu tượng/logo giữa màn.</summary>
        [SerializeField] private Image icon;

        /// <summary>Thời gian tối thiểu giữ màn chờ, tính bằng giây.</summary>
        [SerializeField] private float minimumDuration = 1f;

        /// <summary>Màn hình mở sau khi nạp xong.</summary>
        [SerializeField] private ScreenType nextScreen = ScreenType.MainMenu;

        /// <summary>Tiến trình đang hiển thị, trong khoảng 0..1.</summary>
        public float Progress { get; private set; }

        private float elapsed;
        private bool finished;

        /// <inheritdoc/>
        public override bool CanGoBack => false;

        /// <inheritdoc/>
        public override void OnShow(object data)
        {
            elapsed = 0f;
            finished = false;
            SetProgress(0f);

            if (textTMPLoading != null) textTMPLoading.text = "LOADING..";
            if (icon != null) icon.enabled = icon.sprite != null;
        }

        private void Update()
        {
            if (!IsVisible || finished) return;

            elapsed += Time.unscaledDeltaTime;

            bool ready = GameBootstrap.HasInstance && GameBootstrap.Instance.IsReady;
            float target = ready ? 1f : 0.85f;
            float duration = Mathf.Max(0.1f, minimumDuration);

            SetProgress(Mathf.Min(target, elapsed / duration));

            if (ready && elapsed >= duration && Progress >= 1f) Finish();
        }

        /// <summary>Đặt tiến trình hiển thị.</summary>
        /// <param name="value">Giá trị trong khoảng 0..1.</param>
        public void SetProgress(float value)
        {
            Progress = Mathf.Clamp01(value);

            if (progressBar != null)
            {
                progressBar.minValue = 0f;
                progressBar.maxValue = 1f;
                progressBar.value = Progress;
            }

            if (filler != null) filler.fillAmount = Progress;
        }

        /// <summary>Kết thúc màn chờ và chuyển sang <see cref="nextScreen"/>.</summary>
        public void Finish()
        {
            if (finished) return;
            finished = true;

            if (UIController.HasInstance) UIController.Instance.Show(nextScreen);
            else Hide();
        }
    }
}
