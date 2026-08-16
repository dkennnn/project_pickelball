using System.Collections.Generic;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Cho AI tự dùng booster trong trận.
    ///
    /// <para><b>Cách quyết định</b></para>
    /// Cứ mỗi <see cref="checkInterval"/> giây, controller so tỉ số của mình với đối thủ
    /// (<c>ScoreManager.Instance.GetScore</c>):
    /// <list type="bullet">
    /// <item><description>Đang bị dẫn điểm → roll <see cref="useProbabilityWhenLosing"/>;</description></item>
    /// <item><description>Đang hoà → roll <see cref="useProbabilityWhenTied"/>;</description></item>
    /// <item><description>Đang dẫn → không dùng.</description></item>
    /// </list>
    /// Roll trúng thì chọn ngẫu nhiên một loại booster còn lượt và gọi
    /// <see cref="BoosterManager.AssignBooster"/>.
    ///
    /// <para>
    /// Controller chỉ tham chiếu <see cref="BasePlayerController"/> (lớp cha chung), KHÔNG phụ thuộc
    /// vào lớp AI cụ thể — gắn component này lên cùng GameObject với controller của AI là đủ.
    /// </para>
    /// </summary>
    public class AIBoosterController : MonoBehaviour
    {
        /// <summary>Tay vợt AI sở hữu controller này; để trống thì tự tìm trên GameObject/cha.</summary>
        [SerializeField] private BasePlayerController aiPlayer;

        /// <summary>Khoảng thời gian giữa hai lần cân nhắc dùng booster (giây).</summary>
        [SerializeField] private float checkInterval = 3f;

        /// <summary>Xác suất dùng booster khi đang bị dẫn điểm.</summary>
        [SerializeField] [Range(0f, 1f)] private float useProbabilityWhenLosing = 0.4f;

        /// <summary>Xác suất dùng booster khi tỉ số đang hoà.</summary>
        [SerializeField] [Range(0f, 1f)] private float useProbabilityWhenTied = 0.15f;

        /// <summary>Các loại booster AI được phép dùng; để trống thì dùng toàn bộ 5 loại.</summary>
        [SerializeField] private List<BoosterType> allowedBoosters = new List<BoosterType>();

        /// <summary>Không dùng booster mới khi đang có booster khác chạy.</summary>
        [SerializeField] private bool avoidStackingBoosters = true;

        private static readonly BoosterType[] DefaultBoosters =
        {
            BoosterType.Stamina, BoosterType.Swing, BoosterType.Spin, BoosterType.Power, BoosterType.Speed
        };

        private float nextCheckTime;
        private readonly List<BoosterType> candidates = new List<BoosterType>();

        private void Awake()
        {
            if (aiPlayer == null) aiPlayer = GetComponent<BasePlayerController>();
            if (aiPlayer == null) aiPlayer = GetComponentInParent<BasePlayerController>();
        }

        private void OnEnable()
        {
            nextCheckTime = Time.time + Mathf.Max(0.1f, checkInterval);
        }

        private void Update()
        {
            if (aiPlayer == null || string.IsNullOrEmpty(aiPlayer.teamID)) return;
            if (!BoosterManager.HasInstance) return;

            if (GameManager.HasInstance)
            {
                GameState state = GameManager.Instance.currentState;
                if (state == GameState.GameOver) return;
            }

            if (Time.time < nextCheckTime) return;
            nextCheckTime = Time.time + Mathf.Max(0.1f, checkInterval);

            EvaluateBoosterUsage();
        }

        /// <summary>Một lần cân nhắc: xét tỉ số, roll xác suất rồi kích hoạt booster nếu trúng.</summary>
        private void EvaluateBoosterUsage()
        {
            float probability = GetUseProbability();
            if (probability <= 0f) return;

            string teamID = aiPlayer.teamID;

            if (avoidStackingBoosters && BoosterManager.Instance.IsAnyBoosterActive(teamID)) return;
            if (Random.value > probability) return;

            BoosterType type = PickRandomAvailableBooster(teamID);
            if (type == BoosterType.None) return;

            BoosterManager.Instance.AssignBooster(teamID, type);
        }

        /// <summary>Xác suất dùng booster ở tình thế hiện tại; 0 nghĩa là không cân nhắc.</summary>
        private float GetUseProbability()
        {
            if (!ScoreManager.HasInstance) return 0f;

            string teamID = aiPlayer.teamID;
            string opponentTeamID = GetOpponentTeamID(teamID);
            if (string.IsNullOrEmpty(opponentTeamID)) return 0f;

            int myScore = ScoreManager.Instance.GetScore(teamID);
            int opponentScore = ScoreManager.Instance.GetScore(opponentTeamID);

            if (myScore < opponentScore) return useProbabilityWhenLosing;
            if (myScore == opponentScore) return useProbabilityWhenTied;
            return 0f;
        }

        /// <summary>TeamID đối thủ, ưu tiên hỏi <see cref="GameManager"/> rồi tới <see cref="ScoreManager"/>.</summary>
        private static string GetOpponentTeamID(string teamID)
        {
            if (GameManager.HasInstance)
            {
                string opponent = GameManager.Instance.GetOpponentTeamID(teamID);
                if (!string.IsNullOrEmpty(opponent)) return opponent;
            }

            if (ScoreManager.HasInstance) return ScoreManager.Instance.GetOtherTeamID(teamID);
            return string.Empty;
        }

        /// <summary>Chọn ngẫu nhiên một loại booster còn lượt dùng; <see cref="BoosterType.None"/> nếu hết sạch.</summary>
        private BoosterType PickRandomAvailableBooster(string teamID)
        {
            candidates.Clear();

            if (allowedBoosters != null && allowedBoosters.Count > 0)
            {
                for (int i = 0; i < allowedBoosters.Count; i++) AddCandidate(teamID, allowedBoosters[i]);
            }
            else
            {
                for (int i = 0; i < DefaultBoosters.Length; i++) AddCandidate(teamID, DefaultBoosters[i]);
            }

            if (candidates.Count == 0) return BoosterType.None;
            return candidates[Random.Range(0, candidates.Count)];
        }

        private void AddCandidate(string teamID, BoosterType type)
        {
            if (type == BoosterType.None) return;
            if (candidates.Contains(type)) return;
            if (BoosterManager.Instance.GetRemainingBoosterUses(teamID, type) <= 0) return;
            if (BoosterManager.Instance.IsBoosterActive(teamID, type)) return;

            candidates.Add(type);
        }
    }
}
