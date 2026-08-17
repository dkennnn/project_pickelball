using System.Collections;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Một khán giả trên khán đài: chạy idle với tốc độ/pha ngẫu nhiên, và reo hò khi có điểm.
    /// <para>
    /// Nghe <see cref="ScoreManager.OnScoreUpdated"/> (event instance) — vì ScoreManager có thể
    /// chưa tồn tại lúc khán giả được sinh ra, component chờ bằng coroutine rồi mới đăng ký,
    /// và luôn huỷ đăng ký ở <c>OnDisable</c>.
    /// </para>
    /// <para>Không có <see cref="Animator"/> thì component im lặng không làm gì.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public class AudienceMember : MonoBehaviour
    {
        [Tooltip("Animator của khán giả. Bỏ trống thì tự tìm trên GameObject này hoặc trong con.")]
        [SerializeField] private Animator animator;

        [Tooltip("Mã đội mà khán giả này cổ vũ. Khớp với ScoreManager.player1TeamID / player2TeamID.")]
        public string supportingTeam = "P1";

        [Header("Animator Parameters")]
        [Tooltip("Tên trigger reo hò khi đội mình ghi điểm.")]
        [SerializeField] private string cheerTrigger = "Cheer";

        [Tooltip("Tên trigger vỗ tay lịch sự khi đội kia ghi điểm.")]
        [SerializeField] private string applaudTrigger = "Applaud";

        [Header("Idle Randomization")]
        [Tooltip("Tốc độ phát idle nhỏ nhất.")]
        [SerializeField] private float minIdleSpeed = 0.85f;

        [Tooltip("Tốc độ phát idle lớn nhất.")]
        [SerializeField] private float maxIdleSpeed = 1.2f;

        [Tooltip("Số giây tối đa chờ ScoreManager xuất hiện trước khi bỏ cuộc.")]
        [SerializeField] private float subscribeTimeout = 10f;

        private ScoreManager subscribedManager;
        private Coroutine subscribeRoutine;

        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
        }

        private void OnEnable()
        {
            RandomizeIdle();
            subscribeRoutine = StartCoroutine(WaitAndSubscribe());
        }

        private void OnDisable()
        {
            if (subscribeRoutine != null)
            {
                StopCoroutine(subscribeRoutine);
                subscribeRoutine = null;
            }

            Unsubscribe();
        }

        /// <summary>Bốc lại tốc độ và pha idle ngẫu nhiên.</summary>
        public void RandomizeIdle()
        {
            if (animator == null) return;

            float lo = Mathf.Min(minIdleSpeed, maxIdleSpeed);
            float hi = Mathf.Max(minIdleSpeed, maxIdleSpeed);
            animator.speed = Random.Range(lo, hi);

            if (animator.runtimeAnimatorController == null) return;

            // Lệch pha để cả khán đài không nhấp nhô đồng loạt.
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            animator.Play(state.fullPathHash, 0, Random.value);
        }

        private IEnumerator WaitAndSubscribe()
        {
            float waited = 0f;

            while (!ScoreManager.HasInstance || ScoreManager.Instance == null)
            {
                waited += Time.deltaTime;
                if (waited >= subscribeTimeout)
                {
                    subscribeRoutine = null;
                    yield break;
                }

                yield return null;
            }

            Subscribe(ScoreManager.Instance);
            subscribeRoutine = null;
        }

        private void Subscribe(ScoreManager manager)
        {
            if (manager == null || subscribedManager == manager) return;

            Unsubscribe();
            subscribedManager = manager;
            subscribedManager.OnScoreUpdated += OnScoreUpdated;
        }

        private void Unsubscribe()
        {
            if (subscribedManager == null) return;
            subscribedManager.OnScoreUpdated -= OnScoreUpdated;
            subscribedManager = null;
        }

        /// <summary>Đội <paramref name="teamID"/> vừa ghi điểm — chọn phản ứng phù hợp.</summary>
        /// <param name="newScore">Tỉ số mới của đội đó.</param>
        /// <param name="teamID">Mã đội vừa ghi điểm.</param>
        private void OnScoreUpdated(int newScore, string teamID)
        {
            // ResetScore phát 0-0 cho cả hai đội — không phải dịp để reo hò.
            if (newScore <= 0) return;

            if (!string.IsNullOrEmpty(supportingTeam) && supportingTeam == teamID) TriggerCheer();
            else TriggerApplaud();
        }

        /// <summary>Reo hò (đội mình ghi điểm).</summary>
        public void TriggerCheer()
        {
            SetTrigger(cheerTrigger);
        }

        /// <summary>Vỗ tay lịch sự (đội kia ghi điểm).</summary>
        public void TriggerApplaud()
        {
            SetTrigger(applaudTrigger);
        }

        private void SetTrigger(string triggerName)
        {
            if (animator == null || string.IsNullOrEmpty(triggerName)) return;
            if (animator.runtimeAnimatorController == null) return;
            if (!HasParameter(triggerName)) return;

            animator.SetTrigger(triggerName);
        }

        /// <summary>Animator hiện tại có parameter tên này không (tránh log cảnh báo của Unity).</summary>
        /// <param name="parameterName">Tên parameter cần tìm.</param>
        private bool HasParameter(string parameterName)
        {
            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].name == parameterName) return true;
            }

            return false;
        }
    }
}
