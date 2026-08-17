using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Một ô trong locker: có thể đang rỗng, đang đếm giờ mở túi, sẵn sàng mở,
    /// hoặc còn khoá (phải trả gem để mở ô).
    /// </summary>
    public class LockerSlotCellView : MonoBehaviour
    {
        /// <summary>Ảnh túi đồ đang nằm trong ô.</summary>
        [SerializeField] private Image bagImage;

        /// <summary>Nút xem thông tin túi.</summary>
        [SerializeField] private Button infoBtn;

        /// <summary>Node hiển thị khi ô rỗng.</summary>
        [SerializeField] private GameObject empty;

        /// <summary>Nút mở túi khi đồng hồ đã hết.</summary>
        [SerializeField] private Button claimButton;

        /// <summary>Chữ trên nút mở túi.</summary>
        [SerializeField] private TextMeshProUGUI claim;

        /// <summary>Node hiển thị khi túi đang đếm giờ.</summary>
        [SerializeField] private GameObject waiting;

        /// <summary>Ô chữ đếm ngược thời gian còn lại.</summary>
        [SerializeField] private TextMeshProUGUI timeText;

        /// <summary>Nút trả gem để bỏ qua đồng hồ.</summary>
        [SerializeField] private Button claimNowButton;

        /// <summary>Số gem cần trả để bỏ qua đồng hồ.</summary>
        [SerializeField] private TextMeshProUGUI coinsCount;

        /// <summary>Node hiển thị khi ô còn khoá.</summary>
        [SerializeField] private GameObject locked;

        /// <summary>Nút trả gem để mở khoá ô.</summary>
        [SerializeField] private Button buyButton;

        /// <summary>Số gem cần trả để mở khoá ô.</summary>
        [SerializeField] private TextMeshProUGUI lockedCoinsCount;

        /// <summary>Nút xem quảng cáo để rút ngắn đồng hồ (chưa dùng ở bản offline).</summary>
        [SerializeField] private Button skipAdButton;

        /// <summary>Phát khi bấm vào ô (không phải nút con); tham số là chỉ số ô.</summary>
        public event Action<int> OnClicked;

        /// <summary>Phát khi bấm mở túi; tham số là chỉ số ô.</summary>
        public event Action<int> OnOpenClicked;

        /// <summary>Phát khi bấm trả gem bỏ qua đồng hồ; tham số là chỉ số ô.</summary>
        public event Action<int> OnSkipTimerClicked;

        /// <summary>Phát khi bấm trả gem mở khoá ô; tham số là chỉ số ô.</summary>
        public event Action<int> OnUnlockSlotClicked;

        /// <summary>Chỉ số của ô này trong locker.</summary>
        public int SlotIndex { get; private set; } = -1;

        private SlotsData slotsData;
        private bool wired;

        /// <summary>
        /// Gắn dữ liệu một ô locker vào cell.
        /// </summary>
        /// <param name="data">Bảng dữ liệu locker.</param>
        /// <param name="index">Chỉ số ô cần hiển thị.</param>
        public void Bind(SlotsData data, int index)
        {
            Wire();

            slotsData = data;
            SlotIndex = index;
            Refresh();
        }

        /// <summary>Vẽ lại toàn bộ trạng thái ô theo dữ liệu hiện tại.</summary>
        public void Refresh()
        {
            if (slotsData == null || slotsData.slots == null
                || SlotIndex < 0 || SlotIndex >= slotsData.slots.Length)
            {
                SetState(false, false, false, false);
                return;
            }

            SlotsData.SlotData slot = slotsData.slots[SlotIndex];
            if (slot == null)
            {
                SetState(true, false, false, false);
                return;
            }

            if (slot.isSlotLocked)
            {
                SetState(false, false, false, true);
                if (lockedCoinsCount != null) lockedCoinsCount.text = slotsData.gemsToUnlockSlot.ToString();
                return;
            }

            if (slot.kitbagType == KitbagType.None)
            {
                SetState(true, false, false, false);
                if (bagImage != null) bagImage.enabled = false;
                return;
            }

            if (bagImage != null)
            {
                Sprite sprite = ResolveKitbagSprite(slot.kitbagType);
                bagImage.enabled = sprite != null;
                if (sprite != null) bagImage.sprite = sprite;
            }

            TimeSpan remaining = slotsData.GetRemainingUnlockTime(SlotIndex);
            bool ready = remaining <= TimeSpan.Zero;

            SetState(false, ready, !ready, false);

            if (!ready)
            {
                if (timeText != null) timeText.text = Utilities.FormatTimeSpan(remaining);
                if (coinsCount != null) coinsCount.text = slotsData.GetGemsToSkipTimer(slot.kitbagType).ToString();
            }
        }

        /// <summary>Cập nhật riêng đồng hồ đếm ngược (gọi mỗi giây từ màn hình locker).</summary>
        public void RefreshTimer()
        {
            if (slotsData == null || SlotIndex < 0) return;

            TimeSpan remaining = slotsData.GetRemainingUnlockTime(SlotIndex);
            if (remaining <= TimeSpan.Zero)
            {
                Refresh();
                return;
            }

            if (timeText != null) timeText.text = Utilities.FormatTimeSpan(remaining);
        }

        private Sprite ResolveKitbagSprite(KitbagType type)
        {
            GameData gameData = GameBootstrap.HasInstance ? GameBootstrap.Instance.gameData : null;
            Shop shop = gameData != null ? gameData.shopData : null;
            return shop != null ? shop.GetKitbagSprite(type) : null;
        }

        private void SetState(bool isEmpty, bool isReady, bool isWaiting, bool isLocked)
        {
            if (empty != null) empty.SetActive(isEmpty);
            if (claimButton != null) claimButton.gameObject.SetActive(isReady);
            if (claim != null && isReady) claim.text = "OPEN";
            if (waiting != null) waiting.SetActive(isWaiting);
            if (locked != null) locked.SetActive(isLocked);
            if (infoBtn != null) infoBtn.gameObject.SetActive(!isEmpty && !isLocked);
            if (bagImage != null && (isEmpty || isLocked)) bagImage.enabled = false;
        }

        private void Wire()
        {
            if (wired) return;
            wired = true;

            if (claimButton != null) claimButton.onClick.AddListener(HandleOpen);
            if (claimNowButton != null) claimNowButton.onClick.AddListener(HandleSkip);
            if (buyButton != null) buyButton.onClick.AddListener(HandleUnlock);
            if (infoBtn != null) infoBtn.onClick.AddListener(HandleInfo);
            if (skipAdButton != null) skipAdButton.gameObject.SetActive(false); // Không làm quảng cáo ở bản này.
        }

        private void HandleOpen() { OnOpenClicked?.Invoke(SlotIndex); }

        private void HandleSkip() { OnSkipTimerClicked?.Invoke(SlotIndex); }

        private void HandleUnlock() { OnUnlockSlotClicked?.Invoke(SlotIndex); }

        private void HandleInfo() { OnClicked?.Invoke(SlotIndex); }

        private void OnDestroy()
        {
            if (claimButton != null) claimButton.onClick.RemoveListener(HandleOpen);
            if (claimNowButton != null) claimNowButton.onClick.RemoveListener(HandleSkip);
            if (buyButton != null) buyButton.onClick.RemoveListener(HandleUnlock);
            if (infoBtn != null) infoBtn.onClick.RemoveListener(HandleInfo);

            OnClicked = null;
            OnOpenClicked = null;
            OnSkipTimerClicked = null;
            OnUnlockSlotClicked = null;
        }
    }
}
