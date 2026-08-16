using System.Collections;
using StarterKit.Utilities;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Tay vợt do NGƯỜI CHƠI điều khiển: chạm để di chuyển, vuốt để đánh bóng.
    /// <para>
    /// Toàn bộ input đến từ <see cref="InputManager"/>: <c>onTap</c> → <see cref="Move"/>,
    /// <c>onSwipe</c> → chuyển tiếp cho state đang chạy (Serve / TrackBall / TakeShot).
    /// </para>
    /// </summary>
    public class PickleballPlayerController : BasePlayerController
    {
        /// <summary>Vòng sáng dưới chân báo đang tới lượt mình.</summary>
        [Header("Input Controlled Player Properties")]
        public Transform playerTurnIndicator;

        /// <summary>Hiệu ứng gợn sóng tại điểm chạm để di chuyển.</summary>
        public ParticleSystem groundTapParticle;

        /// <summary>True khi vừa có cú vuốt mới chưa được state tiêu thụ.</summary>
        [HideInInspector] public bool isSwipeUpdated;

        /// <summary>Cú vuốt gần nhất nhận được từ <see cref="InputManager"/>.</summary>
        [HideInInspector] public SwipeData swipeData;

        /// <summary>Prefab marker hiển thị điểm rơi dự kiến trong lúc vuốt.</summary>
        public GameObject hitDestinationMarkerPrefab;

        private Transform hitDestinationMarker;
        private Coroutine inputBindRoutine;
        private bool isInputBound;

        /// <summary>Thời gian tối đa chờ <see cref="InputManager"/> xuất hiện trong scene (giây).</summary>
        private const float InputBindTimeout = 10f;

        // ------------------------------------------------------------------
        // Vòng đời
        // ------------------------------------------------------------------

        protected override void InitializeStates()
        {
            stateDictionary[PlayerState.Idle] = new IdleState(this);
            stateDictionary[PlayerState.Serve] = new ServeState(this);
            stateDictionary[PlayerState.TrackBall] = new TrackBallState(this);
            stateDictionary[PlayerState.TakeShot] = new TakeShotState(this);
        }

        private void OnEnable()
        {
            inputBindRoutine = StartCoroutine(BindInputRoutine());
        }

        private void OnDisable()
        {
            if (inputBindRoutine != null)
            {
                StopCoroutine(inputBindRoutine);
                inputBindRoutine = null;
            }
            UnbindInput();
        }

        /// <summary>Chờ <see cref="InputManager"/> có mặt trong scene rồi mới đăng ký callback input.</summary>
        private IEnumerator BindInputRoutine()
        {
            float elapsed = 0f;
            while (!InputManager.HasInstance && elapsed < InputBindTimeout)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!InputManager.HasInstance) yield break;

            BindInput();
            inputBindRoutine = null;
        }

        private void BindInput()
        {
            if (isInputBound || !InputManager.HasInstance) return;
            isInputBound = true;

            InputManager.Instance.onTap += Move;
            InputManager.Instance.onSwipe += OnSwipe;
        }

        private void UnbindInput()
        {
            if (!isInputBound) return;
            isInputBound = false;

            if (!InputManager.HasInstance) return;

            InputManager.Instance.onTap -= Move;
            InputManager.Instance.onSwipe -= OnSwipe;
        }

        protected override void OnDestroy()
        {
            UnbindInput();
            HideDestinationMarker();
            base.OnDestroy();
        }

        // ------------------------------------------------------------------
        // Giao bóng
        // ------------------------------------------------------------------

        public override void PrepareServe(ServeSide side)
        {
            base.PrepareServe(side);
            SetTurnIndicator(true);
        }

        public override void OnServe()
        {
            base.OnServe();
            SetTurnIndicator(false);
        }

        public override void ReadyForRally()
        {
            base.ReadyForRally();
            HideDestinationMarker();
            isSwipeUpdated = false;
        }

        /// <summary>Bật/tắt vòng sáng báo lượt.</summary>
        /// <param name="visible">True để hiện.</param>
        public void SetTurnIndicator(bool visible)
        {
            if (playerTurnIndicator != null) playerTurnIndicator.gameObject.SetActive(visible);
        }

        // ------------------------------------------------------------------
        // Input
        // ------------------------------------------------------------------

        /// <summary>
        /// Chạm xuống sân để ra lệnh di chuyển. Chạm ngoài vùng hợp lệ sẽ bị bỏ qua hoàn toàn.
        /// </summary>
        /// <param name="worldPosition">Điểm chạm đã chiếu xuống mặt sân.</param>
        private void Move(Vector3 worldPosition)
        {
            if (!IsValidMove(worldPosition)) return;

            SetTargetPosition(worldPosition);

            if (groundTapParticle != null)
            {
                groundTapParticle.transform.position = new Vector3(worldPosition.x, 0.02f, worldPosition.z);
                groundTapParticle.Play();
            }
        }

        /// <summary>
        /// True nếu điểm chạm là một lệnh di chuyển hợp lệ: nằm trong nửa sân của mình (đã nới biên),
        /// không quá sát lưới và trận đang ở trạng thái cho phép chạy.
        /// </summary>
        /// <param name="targetPosition">Điểm đến mong muốn.</param>
        private bool IsValidMove(Vector3 targetPosition)
        {
            if (Utilities.IsInvalid(targetPosition)) return false;
            if (!IsInPlayingSideCourtBounds(targetPosition)) return false;

            if (IsPositiveCourt ? targetPosition.z < ValidZDistanceFromNet : targetPosition.z > -ValidZDistanceFromNet)
            {
                return false;
            }

            if (GameManager.HasInstance)
            {
                GameState state = GameManager.Instance.currentState;
                if (state == GameState.GameOver || state == GameState.PointScored) return false;
            }

            return true;
        }

        /// <summary>
        /// Nhận cú vuốt và chuyển tiếp cho state đang chạy nếu state đó biết xử lý swipe.
        /// Trong lúc vuốt (<see cref="SwipePhase.Progress"/>) thì cập nhật marker điểm rơi.
        /// </summary>
        /// <param name="swipeData">Dữ liệu cú vuốt.</param>
        private void OnSwipe(SwipeData swipeData)
        {
            if (swipeData == null) return;

            this.swipeData = swipeData;
            isSwipeUpdated = true;

            switch (swipeData.SwipePhase)
            {
                case SwipePhase.Progress:
                    SetDestinationMarkerPosition(swipeData);
                    break;
                case SwipePhase.End:
                case SwipePhase.Invalid:
                    HideDestinationMarker();
                    break;
            }

            IPlayerState state = GetCurrentState();

            switch (state)
            {
                case ServeState serveState:
                    serveState.OnSwipeReceived(swipeData);
                    break;
                case TrackBallState trackBallState:
                    trackBallState.OnSwipeReceived(swipeData);
                    break;
                case TakeShotState takeShotState:
                    takeShotState.OnSwipeReceived(swipeData);
                    break;
            }
        }

        // ------------------------------------------------------------------
        // Marker điểm rơi
        // ------------------------------------------------------------------

        /// <summary>Hiện marker điểm rơi dự kiến tại một vị trí trên sân.</summary>
        /// <param name="position">Vị trí world của marker.</param>
        public void ShowDestinationMarker(Vector3 position)
        {
            if (hitDestinationMarkerPrefab == null) return;

            if (hitDestinationMarker == null)
            {
                GameObject instance = Instantiate(hitDestinationMarkerPrefab);
                hitDestinationMarker = instance.transform;
            }

            hitDestinationMarker.position = new Vector3(position.x, 0.02f, position.z);
            hitDestinationMarker.gameObject.SetActive(true);
        }

        /// <summary>Cập nhật marker theo điểm rơi suy ra từ cú vuốt đang diễn ra.</summary>
        /// <param name="swipeData">Cú vuốt hiện tại.</param>
        public void SetDestinationMarkerPosition(SwipeData swipeData)
        {
            if (swipeData == null) return;
            ShowDestinationMarker(swipeData.DestinationPoint);
        }

        /// <summary>Ẩn marker điểm rơi.</summary>
        public void HideDestinationMarker()
        {
            if (hitDestinationMarker == null) return;
            hitDestinationMarker.gameObject.SetActive(false);
        }
    }
}
