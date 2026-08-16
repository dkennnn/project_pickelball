using System;
using System.Collections;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Lớp cha của mọi booster in-match. Một booster là một component sống trên một GameObject
    /// con của tay vợt sở hữu nó; nó sửa tạm thời chỉ số trong <see cref="BasePlayerController.profile"/>
    /// rồi TỰ KHÔI PHỤC khi hết hiệu lực.
    ///
    /// <para><b>Ba điều kiện tự huỷ (điều kiện nào tới trước thì booster kết thúc)</b></para>
    /// <list type="number">
    /// <item><description>Hết <see cref="activeDuration"/> giây (đếm ngược trong <c>UpdateRoutine</c>,
    /// tạm dừng khi <see cref="timerPaused"/>).</description></item>
    /// <item><description>Chủ sở hữu đã đánh đủ <c>GameSettings.BoosterMaxShots</c> cú
    /// (bắt <see cref="BallController.OnBallHit"/>, chỉ đếm cú của <see cref="BasePlayerController.teamID"/> mình).</description></item>
    /// <item><description>Trận đấu chuyển sang <see cref="GameState.GameOver"/>
    /// (bắt <see cref="GameManager.OnGameStateChanged"/>).</description></item>
    /// </list>
    ///
    /// <para><b>Bảo đảm khôi phục chỉ số</b></para>
    /// Chỉ số gốc được lưu trong <c>ApplyEffect</c> và trả lại trong <c>RemoveEffect</c>. Mọi đường
    /// thoát đều đi qua <see cref="RemoveBooster"/> hoặc <see cref="RemoveBoosterSilently"/>, và
    /// <see cref="OnDisable"/> cũng gọi <see cref="RemoveBoosterSilently"/> nếu booster còn đang chạy —
    /// nên kể cả khi GameObject bị destroy/disable đột ngột, chỉ số vẫn được trả về nguyên trạng.
    /// </summary>
    public abstract class Booster : MonoBehaviour
    {
        /// <summary>Số cú đánh mặc định nếu <c>GameSettings</c> chưa cấu hình <c>BoosterMaxShots</c>.</summary>
        private const int DefaultMaxShots = 5;

        /// <summary>Tay vợt đang sở hữu booster này.</summary>
        protected BasePlayerController player;

        /// <summary>True khi hiệu ứng đang được áp dụng.</summary>
        protected bool isActive;

        /// <summary>Callback báo cho <see cref="BoosterManager"/> biết booster đã kết thúc.</summary>
        protected Action onBoosterComplete;

        /// <summary>Tổng thời gian hiệu lực (giây).</summary>
        public float activeDuration;

        /// <summary>Thời gian hiệu lực còn lại (giây).</summary>
        protected float activeTimeRemaining;

        /// <summary>Tạm dừng đồng hồ đếm ngược (ví dụ giữa các pha bóng).</summary>
        protected bool timerPaused;

        /// <summary>Loại booster; lớp con tự đặt trong <c>Awake</c>/<c>Initialize</c>.</summary>
        public BoosterType Type { get; protected set; }

        /// <summary>True khi booster đang có hiệu lực.</summary>
        public bool IsActive => isActive;

        private int shotsSinceActivation;
        private int maxShots = DefaultMaxShots;
        private bool isSubscribed;
        private Coroutine updateRoutine;

        // ------------------------------------------------------------------
        // Vòng đời
        // ------------------------------------------------------------------

        protected virtual void Awake() { }

        /// <summary>
        /// Booster bị tắt/destroy: gỡ hiệu ứng (nếu còn chạy) và huỷ đăng ký toàn bộ event.
        /// Đây là chốt an toàn cuối cùng bảo đảm chỉ số gốc luôn được trả lại.
        /// </summary>
        protected virtual void OnDisable()
        {
            if (isActive) RemoveBoosterSilently();
            else Unsubscribe();
        }

        // ------------------------------------------------------------------
        // API chính
        // ------------------------------------------------------------------

        /// <summary>
        /// Kích hoạt booster cho một tay vợt. Gọi lại trên booster đang chạy sẽ gỡ hiệu ứng cũ
        /// (khôi phục chỉ số gốc) rồi áp lại từ đầu — tức là RESET đồng hồ, không cộng dồn chỉ số.
        /// </summary>
        /// <param name="player">Tay vợt sở hữu booster.</param>
        /// <param name="duration">Thời gian hiệu lực (giây); &lt;= 0 thì giữ <see cref="activeDuration"/> hiện có.</param>
        /// <param name="onComplete">Callback khi booster kết thúc (hết giờ / đủ cú đánh / hết trận).</param>
        public virtual void Initialize(BasePlayerController player, float duration, Action onComplete = null)
        {
            // Đang chạy mà gán lại: gỡ sạch hiệu ứng cũ trước để không lưu nhầm "chỉ số gốc" đã bị boost.
            if (isActive) RemoveBoosterSilently();

            this.player = player;
            onBoosterComplete = onComplete;

            if (duration > 0f) activeDuration = duration;
            activeTimeRemaining = activeDuration;
            timerPaused = false;
            shotsSinceActivation = 0;

            GameSettings settings = GameManager.HasInstance ? GameManager.Instance.settings : null;
            maxShots = settings != null && settings.BoosterMaxShots > 0 ? settings.BoosterMaxShots : DefaultMaxShots;

            if (this.player == null)
            {
                Debug.LogWarning("[Booster] Initialize không có player — bỏ qua.");
                return;
            }

            isActive = true;

            ApplyEffect();
            ApplyBallEffect();
            Subscribe();

            if (updateRoutine != null) StopCoroutine(updateRoutine);
            if (isActiveAndEnabled) updateRoutine = StartCoroutine(UpdateRoutine());
        }

        /// <summary>Phần trăm thời gian còn lại (0..1) — dùng cho thanh tiến trình trên UI.</summary>
        public float GetRemainingTimePercentage()
        {
            if (activeDuration <= 0f) return 0f;
            return Mathf.Clamp01(activeTimeRemaining / activeDuration);
        }

        /// <summary>Thời gian hiệu lực còn lại tính bằng giây.</summary>
        public float GetRemainingTime()
        {
            return Mathf.Max(0f, activeTimeRemaining);
        }

        /// <summary>
        /// Kết thúc booster: gỡ hiệu ứng, huỷ đăng ký event, báo
        /// <see cref="onBoosterComplete"/> rồi huỷ GameObject của booster.
        /// </summary>
        protected void RemoveBooster()
        {
            bool wasActive = isActive;

            Teardown();

            if (wasActive) onBoosterComplete?.Invoke();
            onBoosterComplete = null;

            if (this != null && gameObject != null) Destroy(gameObject);
        }

        /// <summary>
        /// Gỡ booster mà KHÔNG báo <see cref="onBoosterComplete"/> và KHÔNG huỷ GameObject.
        /// Dùng khi <see cref="BoosterManager"/> tự dọn hàng loạt hoặc khi gán đè booster cùng loại.
        /// Chỉ số gốc vẫn được khôi phục đầy đủ.
        /// </summary>
        public void RemoveBoosterSilently()
        {
            Teardown();
        }

        // ------------------------------------------------------------------
        // Hook cho lớp con
        // ------------------------------------------------------------------

        /// <summary>Lưu chỉ số gốc và áp chỉ số đã boost lên <see cref="player"/>.</summary>
        protected virtual void ApplyEffect() { }

        /// <summary>Trả chỉ số gốc đã lưu ở <see cref="ApplyEffect"/> về cho <see cref="player"/>.</summary>
        protected virtual void RemoveEffect() { }

        /// <summary>Chạy mỗi frame trong lúc booster còn hiệu lực.</summary>
        protected virtual void UpdateBooster() { }

        /// <summary>Bật hiệu ứng hình ảnh gắn với quả bóng / nhân vật.</summary>
        protected virtual void ApplyBallEffect() { }

        /// <summary>Tắt hiệu ứng hình ảnh đã bật ở <see cref="ApplyBallEffect"/>.</summary>
        protected virtual void RemoveBallEffect() { }

        // ------------------------------------------------------------------
        // Nội bộ
        // ------------------------------------------------------------------

        /// <summary>Gỡ toàn bộ trạng thái: dừng coroutine, huỷ event, tắt VFX, trả chỉ số gốc.</summary>
        private void Teardown()
        {
            if (updateRoutine != null)
            {
                StopCoroutine(updateRoutine);
                updateRoutine = null;
            }

            Unsubscribe();

            if (!isActive) return;
            isActive = false;

            RemoveBallEffect();
            RemoveEffect();
        }

        private void Subscribe()
        {
            if (isSubscribed) return;
            isSubscribed = true;

            BallController.OnBallHit += CheckShotCount;
            GameManager.OnGameStateChanged += OnGameStateChanged;
        }

        private void Unsubscribe()
        {
            if (!isSubscribed) return;
            isSubscribed = false;

            BallController.OnBallHit -= CheckShotCount;
            GameManager.OnGameStateChanged -= OnGameStateChanged;
        }

        /// <summary>Trận kết thúc thì mọi booster đều bị gỡ ngay.</summary>
        private void OnGameStateChanged(GameState state)
        {
            if (!isActive) return;
            if (state == GameState.GameOver) RemoveBooster();
        }

        /// <summary>
        /// Đếm số cú đánh của CHÍNH chủ sở hữu booster; đủ <c>BoosterMaxShots</c> cú thì booster hết hiệu lực.
        /// </summary>
        /// <param name="teamId">Đội vừa đánh bóng.</param>
        /// <param name="shotLocation">Vị trí cú đánh (không dùng, giữ để khớp chữ ký event).</param>
        /// <param name="shotTrajetoryInfo">Quỹ đạo cú đánh (không dùng, giữ để khớp chữ ký event).</param>
        private void CheckShotCount(string teamId, Vector3 shotLocation, ShotTrajetoryInfo shotTrajetoryInfo)
        {
            if (!isActive || player == null) return;
            if (teamId != player.teamID) return;

            shotsSinceActivation++;
            if (maxShots > 0 && shotsSinceActivation >= maxShots) RemoveBooster();
        }

        /// <summary>Đếm ngược thời gian (bỏ qua khi <see cref="timerPaused"/>) và gọi <see cref="UpdateBooster"/> mỗi frame.</summary>
        private IEnumerator UpdateRoutine()
        {
            while (isActive)
            {
                if (!timerPaused)
                {
                    activeTimeRemaining -= Time.deltaTime;

                    if (activeTimeRemaining <= 0f)
                    {
                        activeTimeRemaining = 0f;
                        RemoveBooster();
                        yield break;
                    }
                }

                UpdateBooster();
                yield return null;
            }
        }
    }
}
