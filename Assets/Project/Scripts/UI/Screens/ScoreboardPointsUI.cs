using StarterKit.UIKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Bảng điểm chớp nhoáng hiện giữa màn sau mỗi lần ghi điểm.
    /// <para>
    /// Bản gốc đặt tên node là "PointScoredUI" nhưng KHÔNG có entry tương ứng trong enum;
    /// đã bổ sung <see cref="ScreenType.ScoreboardPoints"/> (giá trị 33, thêm ở cuối để không
    /// đổi số của các mục cũ vì save data phụ thuộc vào chúng).
    /// </para>
    /// </summary>
    public class ScoreboardPointsUI : PopupUI
    {
        /// <inheritdoc />
        public override ScreenType DefaultScreenType => ScreenType.ScoreboardPoints;

        /// <summary>Lớp phủ mờ phía sau.</summary>
        [SerializeField] private CanvasGroup tint;

        /// <summary>Số điểm vừa ghi, hiện to giữa màn.</summary>
        [SerializeField] private TextMeshProUGUI pointsAnimation;

        /// <summary>Điểm của người chơi.</summary>
        [SerializeField] private TextMeshProUGUI player1Points;

        /// <summary>Tên người chơi.</summary>
        [SerializeField] private TextMeshProUGUI player1Name;

        /// <summary>Ảnh đại diện người chơi.</summary>
        [SerializeField] private Image player1Avatar;

        /// <summary>Điểm của đối thủ.</summary>
        [SerializeField] private TextMeshProUGUI player2Points;

        /// <summary>Tên đối thủ.</summary>
        [SerializeField] private TextMeshProUGUI player2Name;

        /// <summary>Ảnh đại diện đối thủ.</summary>
        [SerializeField] private Image player2Avatar;

        /// <summary>Biểu tượng "VS".</summary>
        [SerializeField] private GameObject vs;

        /// <summary>Thời gian bảng điểm tự ẩn, tính bằng giây.</summary>
        [SerializeField] private float autoHideDelay = 1.5f;

        private ScoreManager subscribedScoreManager;
        private float hideTimer;

        /// <inheritdoc/>
        public override bool CanGoBack => false;

        /// <inheritdoc/>
        public override void OnShow(object data)
        {
            TrySubscribe();
            Refresh();

            if (tint != null) tint.alpha = 1f;
            hideTimer = Mathf.Max(0.1f, autoHideDelay);
        }

        /// <inheritdoc/>
        public override void OnHide()
        {
            Unsubscribe();
        }

        protected override void OnDestroy()
        {
            Unsubscribe();
            base.OnDestroy();
        }

        private void Update()
        {
            // Vẫn phải bám ScoreManager khi đang ẩn, vì chính sự kiện ghi điểm mới bật màn này lên.
            TrySubscribe();

            if (!IsVisible) return;

            hideTimer -= Time.unscaledDeltaTime;
            if (hideTimer > 0f) return;

            if (UIController.HasInstance) UIController.Instance.Hide(screenType);
            else Hide();
        }

        /// <summary>Vẽ lại điểm và tên hai bên.</summary>
        public void Refresh()
        {
            GameData gameData = GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null;
            PlayerProfileData profile = gameData != null ? gameData.playerProfileData : null;

            if (player1Name != null && profile != null) player1Name.text = profile.playerName ?? "PLAYER";

            if (player1Avatar != null && gameData != null && profile != null
                && gameData.avatarSprites != null && gameData.avatarSprites.Count > 0)
            {
                int index = Mathf.Clamp(profile.avatarIndex, 0, gameData.avatarSprites.Count - 1);
                Sprite sprite = gameData.avatarSprites[index];
                if (sprite != null) player1Avatar.sprite = sprite;
            }

            if (player2Avatar != null) player2Avatar.enabled = player2Avatar.sprite != null;
            if (player2Name != null && string.IsNullOrEmpty(player2Name.text)) player2Name.text = "OPPONENT";
            if (vs != null) vs.SetActive(true);

            if (!ScoreManager.HasInstance) return;

            ScoreManager manager = ScoreManager.Instance;
            if (player1Points != null) player1Points.text = manager.player1Score.ToString();
            if (player2Points != null) player2Points.text = manager.player2Score.ToString();
        }

        private void HandleScoreUpdated(int score, string teamID)
        {
            if (pointsAnimation != null) pointsAnimation.text = score.ToString();
            Refresh();

            hideTimer = Mathf.Max(0.1f, autoHideDelay);
            if (!IsVisible && UIController.HasInstance) UIController.Instance.Show(screenType);
        }

        private void TrySubscribe()
        {
            if (subscribedScoreManager != null || !ScoreManager.HasInstance) return;

            subscribedScoreManager = ScoreManager.Instance;
            subscribedScoreManager.OnScoreUpdated += HandleScoreUpdated;
        }

        private void Unsubscribe()
        {
            if (subscribedScoreManager == null) return;

            subscribedScoreManager.OnScoreUpdated -= HandleScoreUpdated;
            subscribedScoreManager = null;
        }
    }
}
