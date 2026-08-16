using System;
using System.Collections.Generic;
using Pickleball.Network;
using StarterKit.Utilities;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Hợp đồng tối giản mà mọi thực thể tham gia trận đấu (người chơi, AI) phải cài đặt.
    /// <para>
    /// <see cref="GameManager"/> chỉ nói chuyện với các participant qua interface này để không phụ thuộc
    /// vào <c>BasePlayerController</c> — bước sau sẽ cho controller implement lại interface này.
    /// </para>
    /// </summary>
    public interface IMatchParticipant
    {
        /// <summary>Định danh đội của participant (khớp với player1TeamID / player2TeamID).</summary>
        string TeamID { get; }

        /// <summary>True nếu participant đang ở nửa sân dương (z &gt; 0).</summary>
        bool IsPositiveCourt { get; }

        /// <summary>Vị trí hiện tại trong world space (dùng để xét luật kitchen).</summary>
        Vector3 Position { get; }

        /// <summary>Được gọi mỗi khi trạng thái trận đấu đổi.</summary>
        void OnMatchStateChanged(GameState state);

        /// <summary>Được gọi khi participant này là người giao bóng, kèm ô giao cần đứng.</summary>
        void PrepareServe(ServeSide side);
    }

    /// <summary>
    /// Bộ điều phối vòng đời trận đấu: máy trạng thái, đếm giờ giao bóng, xử lý lỗi luật,
    /// đổi điểm/đổi lượt giao và kết thúc trận.
    /// <para>
    /// Phase 1 chạy hoàn toàn offline nhưng giữ nguyên cấu trúc server-authoritative của bản gốc:
    /// những chỗ sẽ mang attribute Mirror ở Phase 2 được đánh dấu bằng comment
    /// <c>// [Server]</c>, <c>// [ServerCallback]</c>, <c>// [ClientRpc]</c>.
    /// </para>
    /// </summary>
    public class GameManager : NetworkSingleton<GameManager>
    {
        /// <summary>Cấu hình trận (maxScore, winMargin, serveTimeout...).</summary>
        [Header("Data")]
        public GameSettings settings;

        /// <summary>Kho thông báo lỗi luật / deuce hiển thị cho người chơi.</summary>
        public GameMessages gameMessages;

        /// <summary>Camera chính của scene gameplay.</summary>
        public Camera mainCamera;

        /// <summary>Sân đấu — nguồn dữ liệu hình học cho <see cref="ruleEngine"/>.</summary>
        public Court court;

        /// <summary>Giới hạn chỉ số hồ sơ người chơi (do hệ thống profile cung cấp).</summary>
        public PlayerProfileLimits profileLimits;

        /// <summary>Vị trí spawn của người chơi 1.</summary>
        public Transform player1Location;

        /// <summary>Vị trí spawn của người chơi 2.</summary>
        public Transform player2Location;

        /// <summary>Điểm camera nhìn về khi người chơi cục bộ ở nửa sân dương.</summary>
        public Transform positiveCourtLookAtLocation;

        /// <summary>Điểm camera nhìn về khi người chơi cục bộ ở nửa sân âm.</summary>
        public Transform negativeCourtLookAtLocation;

        /// <summary>Object chứa toàn bộ dàn cảnh kết thúc trận, bật lên khi vào <see cref="GameState.GameOver"/>.</summary>
        [Header("End Sequence")]
        public GameObject endSequenceObject;

        /// <summary>Time scale mặc định khi đang thi đấu (dùng để khôi phục sau slow-motion).</summary>
        public float defaultGameplayTimeScale = 1f;

        /// <summary>Bật khi scene hiện tại là màn tutorial gameplay.</summary>
        public bool isTutorial;

        /// <summary>TeamID của người chơi 1. Phase 2 sẽ thành [SyncVar].</summary>
        public string player1TeamID = "P1";

        /// <summary>TeamID của người chơi 2. Phase 2 sẽ thành [SyncVar].</summary>
        public string player2TeamID = "P2";

        /// <summary>Trạng thái trận đấu hiện tại. Phase 2 sẽ thành [SyncVar].</summary>
        public GameState currentState;

        /// <summary>Ô giao bóng đang được sử dụng. Phase 2 sẽ thành [SyncVar].</summary>
        public ServeSide currentServeSide;

        /// <summary>TeamID của đội vừa phạm lỗi gần nhất (dùng cho UI và thống kê).</summary>
        public string lastFaultbyTeamID;

        private int serverBounceCount;
        private int receiverBounceCount;
        private bool canServeTakeVolley;
        private bool isServerTurn;
        private float serveTimer;
        private bool isServerLocked;
        private string lockedServerTeamID;
        private string currentPlayerTeamID;
        private string opponentPlayerTeamID;

        /// <summary>TeamID của đội đang cầm quyền giao bóng (không đổi trong suốt một pha bóng).</summary>
        private string currentServerTeamID;

        /// <summary>TeamID của đội chạm bóng gần nhất — dùng để quy lỗi khi bóng vào lưới / ra ngoài.</summary>
        private string lastHitByTeamID;

        private readonly Dictionary<string, IMatchParticipant> participants =
            new Dictionary<string, IMatchParticipant>();

        /// <summary>Bộ luật dùng cho trận này, tạo từ <see cref="court"/>.</summary>
        public RuleEngine ruleEngine { get; private set; }

        private const float ResultScreenShowingTimeDelay = 5f;

        /// <summary>Thời gian chuẩn bị trước khi chuyển từ PreServe sang Serving.</summary>
        private const float PreServeDuration = 1f;

        /// <summary>Thời gian hiển thị điểm vừa ghi trước khi bắt đầu pha bóng mới.</summary>
        private const float PointScoredDuration = 1.5f;

        // ------------------------------------------------------------------
        // Static events (client lắng nghe)
        // ------------------------------------------------------------------

        /// <summary>Trận đấu vừa bắt đầu.</summary>
        public static event Action OnMatchStarted;

        /// <summary>Trạng thái trận đấu vừa đổi.</summary>
        public static event Action<GameState> OnGameStateChanged;

        /// <summary>Một participant vừa được đăng ký vào trận.</summary>
        public static event Action OnPlayerControllerAdded;

        /// <summary>Có lỗi luật: (loại lỗi, teamID phạm lỗi, thông báo hiển thị).</summary>
        public static event Action<RuleType, string, string> OnRuleViolated;

        /// <summary>Đồng hồ giao bóng còn lại (giây).</summary>
        public static event Action<float> OnServiceTimerUpdated;

        /// <summary>Trận đấu đã kết thúc hẳn (sau màn hình kết quả); tham số là teamID đội thắng.</summary>
        public static event Action<string> OnMatchEnded;

        // ------------------------------------------------------------------
        // Vòng đời Unity
        // ------------------------------------------------------------------

        protected override void OnAwake()
        {
            base.OnAwake();

            if (court == null) court = FindAnyObjectByType<Court>();
            ruleEngine = new RuleEngine(court);

            if (mainCamera == null) mainCamera = Camera.main;

            currentState = GameState.PreServe;
            currentPlayerTeamID = player1TeamID;
            opponentPlayerTeamID = player2TeamID;
            currentServerTeamID = player1TeamID;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            StartMatch();
        }

        /// <summary>
        /// Đếm giờ giao bóng. Hết <c>settings.serveTimeout</c> mà người giao chưa phát bóng
        /// thì mất điểm với lỗi <see cref="RuleType.ServeTimeout"/>.
        /// </summary>
        private void Update() // [ServerCallback]
        {
            if (!IsServer) return;
            if (currentState != GameState.Serving) return;

            float timeout = settings != null ? settings.serveTimeout : 10f;
            serveTimer += Time.deltaTime;

            float remaining = Mathf.Max(0f, timeout - serveTimer);
            OnServiceTimerUpdated?.Invoke(remaining); // [ClientRpc] RpcServerTimerUpdated

            if (remaining > 0f) return;

            serveTimer = 0f;
            HandlePointAndServeChange(GetCurrentServerTeamID(), RuleType.ServeTimeout);
        }

        // ------------------------------------------------------------------
        // Participant
        // ------------------------------------------------------------------

        /// <summary>
        /// Đăng ký một người chơi / AI vào trận. Gọi từ chính participant khi nó sẵn sàng.
        /// </summary>
        public void RegisterParticipant(IMatchParticipant p)
        {
            if (p == null || string.IsNullOrEmpty(p.TeamID)) return;

            participants[p.TeamID] = p;
            OnPlayerControllerAdded?.Invoke();
        }

        /// <summary>Lấy participant theo teamID; null nếu chưa đăng ký.</summary>
        public IMatchParticipant GetParticipant(string teamID)
        {
            if (string.IsNullOrEmpty(teamID)) return null;
            return participants.TryGetValue(teamID, out IMatchParticipant p) ? p : null;
        }

        // ------------------------------------------------------------------
        // Máy trạng thái
        // ------------------------------------------------------------------

        /// <summary>
        /// Đổi trạng thái trận đấu, thông báo cho UI và mọi participant, đồng thời chạy
        /// các hành động đi kèm từng trạng thái (chuẩn bị giao bóng, reset timer, kết thúc trận).
        /// </summary>
        public void SetGameState(GameState newState) // [Server]
        {
            currentState = newState;

            OnGameStateChanged?.Invoke(newState); // [ClientRpc] InvokeStateChanged

            foreach (IMatchParticipant participant in participants.Values)
            {
                participant?.OnMatchStateChanged(newState);
            }

            switch (newState)
            {
                case GameState.PreServe:
                    ResetRallyData();
                    string serverTeamID = GetCurrentServerTeamID();
                    currentServeSide = GetPlayerSideForServe(serverTeamID);
                    SetCurrentPlayer(serverTeamID);
                    GetParticipant(serverTeamID)?.PrepareServe(currentServeSide);
                    if (IsServer)
                    {
                        DelayedAction.Run(PreServeDuration, () =>
                        {
                            if (currentState == GameState.PreServe) SetGameState(GameState.Serving);
                        });
                    }
                    break;

                case GameState.Serving:
                    serveTimer = 0f;
                    isServerTurn = true;
                    break;

                case GameState.GameOver:
                    HandleGameOver();
                    break;
            }
        }

        /// <summary>
        /// Khởi động trận: reset tỉ số, chọn người giao đầu tiên, đưa máy trạng thái về
        /// <see cref="GameState.PreServe"/> và phát <see cref="OnMatchStarted"/>.
        /// </summary>
        public void StartMatch() // [Server]
        {
            if (!IsServer) return;

            if (settings != null) settings.ApplySelectedModeSettings();
            Time.timeScale = defaultGameplayTimeScale;

            if (ScoreManager.HasInstance)
            {
                ScoreManager score = ScoreManager.Instance;
                score.player1TeamID = player1TeamID;
                score.player2TeamID = player2TeamID;
                if (score.Settings == null) score.Settings = settings;
                if (score.Messages == null) score.Messages = gameMessages;
                score.ResetScore();
            }

            lastFaultbyTeamID = null;
            lastHitByTeamID = null;

            if (string.IsNullOrEmpty(currentServerTeamID)) currentServerTeamID = player1TeamID;
            SetCurrentPlayer(GetCurrentServerTeamID());
            currentServeSide = GetPlayerSideForServe(GetCurrentServerTeamID());
            ResetRallyData();

            OnMatchStarted?.Invoke(); // [ClientRpc] RpcOnMatchStarted
            SetGameState(GameState.PreServe);
        }

        /// <summary>
        /// Xử lý một lỗi luật: cộng điểm cho đối phương, phát thông báo, kiểm tra kết thúc trận,
        /// chuyển quyền giao bóng và mở pha bóng tiếp theo.
        /// </summary>
        /// <param name="teamFault">TeamID của đội phạm lỗi (mất điểm).</param>
        /// <param name="ruleViolated">Loại lỗi đã vi phạm.</param>
        public void HandlePointAndServeChange(string teamFault, RuleType ruleViolated) // [Server]
        {
            if (!IsServer) return;
            if (currentState == GameState.GameOver) return;
            if (string.IsNullOrEmpty(teamFault)) return;

            lastFaultbyTeamID = teamFault;
            string scoringTeam = GetOpponentTeamID(teamFault);

            if (ScoreManager.HasInstance) ScoreManager.Instance.AddScore(scoringTeam);

            string message = gameMessages != null ? gameMessages.GetRuleMessage(ruleViolated) : ruleViolated.ToString();
            OnRuleViolated?.Invoke(ruleViolated, teamFault, message); // [ClientRpc] RpcOnRuleViolated

            SetGameState(GameState.PointScored);

            if (ScoreManager.HasInstance && ScoreManager.Instance.CheckForGameComplete())
            {
                SetGameState(GameState.GameOver);
                return;
            }

            // Người thắng pha bóng cầm quyền giao — trừ khi quyền giao đang bị khoá (tutorial).
            if (!isServerLocked)
            {
                if (scoringTeam != currentServerTeamID) currentServerTeamID = scoringTeam;
                else SwitchSide();
            }

            ResetRallyData();
            SetCurrentPlayer(GetCurrentServerTeamID());

            DelayedAction.Run(PointScoredDuration, () =>
            {
                if (currentState == GameState.PointScored) SetGameState(GameState.PreServe);
            });
        }

        /// <summary>Đổi ô giao bóng hiện tại (Left ⇄ Right).</summary>
        public void SwitchSide() // [Server]
        {
            currentServeSide = currentServeSide == ServeSide.Right ? ServeSide.Left : ServeSide.Right;
        }

        /// <summary>Hoán đổi lượt đánh giữa hai đội.</summary>
        public void SwitchCurrentPlayer() // [Server]
        {
            string previous = currentPlayerTeamID;
            currentPlayerTeamID = opponentPlayerTeamID;
            opponentPlayerTeamID = previous;
        }

        /// <summary>Xoá toàn bộ dữ liệu tạm của pha bóng (bounce count, timer, quyền volley).</summary>
        public void ResetRallyData()
        {
            serverBounceCount = 0;
            receiverBounceCount = 0;
            canServeTakeVolley = false;
            isServerTurn = true;
            serveTimer = 0f;
            lastHitByTeamID = null;
        }

        // ------------------------------------------------------------------
        // Truy vấn trận đấu
        // ------------------------------------------------------------------

        /// <summary>
        /// Ô giao bóng của một đội theo luật pickleball đơn: điểm chẵn giao từ ô phải,
        /// điểm lẻ giao từ ô trái. Trả về <see cref="currentServeSide"/> nếu chưa có ScoreManager.
        /// </summary>
        public ServeSide GetPlayerSideForServe(string teamID)
        {
            if (string.IsNullOrEmpty(teamID) || !ScoreManager.HasInstance) return currentServeSide;
            int score = ScoreManager.Instance.GetScore(teamID);
            return (score % 2 == 0) ? ServeSide.Right : ServeSide.Left;
        }

        /// <summary>TeamID của đội đang cầm quyền giao bóng (ưu tiên đội bị khoá quyền giao nếu có).</summary>
        public string GetCurrentServerTeamID()
        {
            if (isServerLocked && !string.IsNullOrEmpty(lockedServerTeamID)) return lockedServerTeamID;
            return string.IsNullOrEmpty(currentServerTeamID) ? player1TeamID : currentServerTeamID;
        }

        /// <summary>TeamID của đối thủ của <paramref name="teamID"/>.</summary>
        public string GetOpponentTeamID(string teamID)
        {
            if (teamID == player1TeamID) return player2TeamID;
            if (teamID == player2TeamID) return player1TeamID;
            return string.Empty;
        }

        /// <summary>Khoá quyền giao bóng cho một đội (dùng cho tutorial / kịch bản dựng sẵn).</summary>
        public void LockServerToTeam(string teamID) // [Server]
        {
            isServerLocked = true;
            lockedServerTeamID = teamID;
            currentServerTeamID = teamID;
            // [ClientRpc] RpcNotifyServerLock(teamID, true)
        }

        /// <summary>Bỏ khoá quyền giao bóng, trả về luật bình thường.</summary>
        public void UnlockServer() // [Server]
        {
            if (!string.IsNullOrEmpty(lockedServerTeamID)) currentServerTeamID = lockedServerTeamID;
            isServerLocked = false;
            lockedServerTeamID = null;
            // [ClientRpc] RpcNotifyServerLock(null, false)
        }

        /// <summary>True nếu quyền giao bóng đang bị khoá.</summary>
        public bool IsServerLocked()
        {
            return isServerLocked;
        }

        /// <summary>True nếu trận hiện tại là màn tutorial gameplay.</summary>
        public bool IsGameplayTutorial()
        {
            return isTutorial;
        }

        // ------------------------------------------------------------------
        // Sự kiện từ quả bóng
        // ------------------------------------------------------------------

        /// <summary>
        /// Bóng vừa chạm đất. Cập nhật bộ đếm nảy hai bên, kiểm tra hợp lệ cú giao
        /// (nếu đang chờ kết quả giao bóng) hoặc đánh giá luật nảy bóng trong pha bóng.
        /// </summary>
        /// <param name="position">Vị trí chạm đất trong world space.</param>
        /// <param name="bouncedOnPositiveCourt">True nếu bóng chạm đất ở nửa sân dương (z &gt; 0).</param>
        public void OnBallBounced(Vector3 position, bool bouncedOnPositiveCourt) // [Server]
        {
            if (!IsServer) return;
            if (currentState == GameState.GameOver || currentState == GameState.PointScored) return;

            string serverTeamID = GetCurrentServerTeamID();
            bool serverIsPositive = IsTeamOnPositiveCourt(serverTeamID);
            bool bouncedOnServerSide = bouncedOnPositiveCourt == serverIsPositive;
            string sideOwnerTeamID = bouncedOnServerSide ? serverTeamID : GetOpponentTeamID(serverTeamID);

            if (bouncedOnServerSide) serverBounceCount++;
            else receiverBounceCount++;

            int bounceCountOnThisSide = bouncedOnServerSide ? serverBounceCount : receiverBounceCount;

            // Sau khi mỗi bên đã để bóng nảy một lần thì two-bounce rule đã thoả -> được volley.
            canServeTakeVolley = serverBounceCount >= 1 && receiverBounceCount >= 1;

            // Cú giao: điểm rơi đầu tiên phải nằm trong ô giao chéo sân.
            if (currentState == GameState.WaitingForServeResult)
            {
                if (!ruleEngine.CheckServeValidity(position, serverIsPositive, currentServeSide))
                {
                    HandlePointAndServeChange(serverTeamID, RuleType.ServeNotInArea);
                    return;
                }

                SetGameState(GameState.InPlay);
                return;
            }

            if (currentState != GameState.InPlay) return;

            // Người vừa đánh đứng ở nửa sân đối diện điểm rơi.
            bool hitterIsPositive = !bouncedOnPositiveCourt;
            RuleType violation = ruleEngine.EvaluateBounce(position, hitterIsPositive, bounceCountOnThisSide);
            if (violation == RuleType.None) return;

            // Nảy hai lần: bên sở hữu nửa sân đó thua điểm. Bóng ra ngoài: người đánh thua điểm.
            string faultTeamID = violation == RuleType.DoubleBounceOnSide
                ? sideOwnerTeamID
                : (lastHitByTeamID ?? GetTeamOnCourt(hitterIsPositive));

            HandlePointAndServeChange(faultTeamID, violation);
        }

        /// <summary>
        /// Bóng chạm lưới. Nếu đang trong cú giao thì tính <see cref="RuleType.ServeInNet"/>,
        /// ngược lại tính <see cref="RuleType.BounceOutOfCourt"/> cho người vừa đánh.
        /// </summary>
        public void OnBallHitNet() // [Server]
        {
            if (!IsServer) return;
            if (currentState == GameState.GameOver || currentState == GameState.PointScored) return;

            if (currentState == GameState.WaitingForServeResult || currentState == GameState.Serving)
            {
                HandlePointAndServeChange(GetCurrentServerTeamID(), RuleType.ServeInNet);
                return;
            }

            string faultTeamID = string.IsNullOrEmpty(lastHitByTeamID) ? currentPlayerTeamID : lastHitByTeamID;
            HandlePointAndServeChange(faultTeamID, RuleType.BounceOutOfCourt);
        }

        /// <summary>
        /// Một đội vừa chạm bóng. Kiểm tra đúng lượt, kiểm tra luật volley/kitchen,
        /// chuyển trạng thái khi đây là cú giao và trao lượt cho đối thủ.
        /// </summary>
        /// <param name="teamID">TeamID của đội vừa chạm bóng.</param>
        public void OnBallHit(string teamID) // [Server]
        {
            if (!IsServer) return;
            if (string.IsNullOrEmpty(teamID)) return;
            if (currentState == GameState.GameOver || currentState == GameState.PointScored) return;

            // Đánh bóng khi không phải lượt của mình.
            if (!string.IsNullOrEmpty(currentPlayerTeamID) && teamID != currentPlayerTeamID)
            {
                HandlePointAndServeChange(teamID, RuleType.WrongPlayerTurn);
                return;
            }

            lastHitByTeamID = teamID;

            if (currentState == GameState.Serving || currentState == GameState.PreServe)
            {
                serveTimer = 0f;
                isServerTurn = false;
                SetCurrentPlayer(GetOpponentTeamID(teamID));
                SetGameState(GameState.WaitingForServeResult);
                return;
            }

            // Trong pha bóng: nếu bóng chưa nảy trên nửa sân người đánh thì đây là volley.
            IMatchParticipant hitter = GetParticipant(teamID);
            if (hitter != null)
            {
                int bouncesOnHitterSide = teamID == GetCurrentServerTeamID() ? serverBounceCount : receiverBounceCount;
                bool isVolley = bouncesOnHitterSide == 0;

                if (isVolley && !ruleEngine.CheckVolleyValidity(hitter.Position, canServeTakeVolley))
                {
                    RuleType violation = ruleEngine.IsVolleyInKitchen(hitter.Position)
                        ? RuleType.VolleyInsideKitchen
                        : RuleType.ServeVolley;
                    HandlePointAndServeChange(teamID, violation);
                    return;
                }
            }

            // Sang lượt đối thủ, bộ đếm nảy của bên nhận được reset cho lượt bóng mới.
            if (teamID == GetCurrentServerTeamID()) receiverBounceCount = 0;
            else serverBounceCount = 0;

            SetCurrentPlayer(GetOpponentTeamID(teamID));
        }

        // ------------------------------------------------------------------
        // Nội bộ
        // ------------------------------------------------------------------

        /// <summary>Bật dàn cảnh kết thúc và hẹn giờ phát <see cref="OnMatchEnded"/>.</summary>
        private void HandleGameOver()
        {
            Time.timeScale = 1f;

            if (endSequenceObject != null) endSequenceObject.SetActive(true);

            string winnerTeamID = ScoreManager.HasInstance ? ScoreManager.Instance.GetWinner() : string.Empty;

            // [ClientRpc] RpcOnGameOver(winnerTeamID, winnerPosition)
            DelayedAction.Run(ResultScreenShowingTimeDelay, () => OnMatchEnded?.Invoke(winnerTeamID));
        }

        /// <summary>Đặt đội đang tới lượt đánh và cập nhật luôn đội đối diện.</summary>
        private void SetCurrentPlayer(string teamID)
        {
            if (string.IsNullOrEmpty(teamID)) return;
            currentPlayerTeamID = teamID;
            opponentPlayerTeamID = GetOpponentTeamID(teamID);
        }

        /// <summary>
        /// Nửa sân của một đội. Ưu tiên hỏi participant đã đăng ký;
        /// nếu chưa có thì mặc định player1 ở nửa sân dương.
        /// </summary>
        private bool IsTeamOnPositiveCourt(string teamID)
        {
            IMatchParticipant participant = GetParticipant(teamID);
            if (participant != null) return participant.IsPositiveCourt;
            return teamID == player1TeamID;
        }

        /// <summary>TeamID của đội đang đứng ở nửa sân chỉ định.</summary>
        private string GetTeamOnCourt(bool positiveCourt)
        {
            return IsTeamOnPositiveCourt(player1TeamID) == positiveCourt ? player1TeamID : player2TeamID;
        }
    }
}
