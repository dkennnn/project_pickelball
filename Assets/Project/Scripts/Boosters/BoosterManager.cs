using System;
using System.Collections.Generic;
using Pickleball.Network;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Quản lý toàn bộ booster in-match: spawn prefab booster lên tay vợt, giới hạn số lần dùng
    /// mỗi loại, theo dõi booster nào đang chạy và dọn sạch khi trận kết thúc.
    ///
    /// <para><b>Quy tắc</b></para>
    /// <list type="bullet">
    /// <item><description>Mỗi tay vợt chỉ có TỐI ĐA 1 booster mỗi loại cùng lúc; gán lại loại đang chạy
    /// sẽ reset đồng hồ chứ không cộng dồn chỉ số.</description></item>
    /// <item><description>Mỗi loại chỉ được dùng tối đa <see cref="maxBoosterUses"/> lần trong một trận.</description></item>
    /// <item><description><see cref="RemoveAllBoosters"/> chạy khi vào <see cref="GameState.GameOver"/>
    /// và trong <see cref="OnStopServer"/>, bảo đảm chỉ số của mọi tay vợt được trả về nguyên trạng.</description></item>
    /// </list>
    ///
    /// <para>
    /// Phase 1 chạy offline. Những chỗ bản gốc dùng Mirror được đánh dấu bằng comment
    /// <c>// [Command]</c> / <c>// [ClientRpc]</c>; hai dictionary trạng thái sẽ thành
    /// <c>SyncDictionary</c> ở Phase 2.
    /// </para>
    /// </summary>
    public class BoosterManager : NetworkSingleton<BoosterManager>
    {
        /// <summary>Bắn ra mỗi khi danh sách booster đang chạy / số lần dùng còn lại thay đổi (UI lắng nghe).</summary>
        public static event Action OnBoostersUpdated;

        /// <summary>Ánh xạ loại booster → prefab + thời lượng.</summary>
        [SerializeField] private List<BoosterPrefabMapping> boosterPrefabs = new List<BoosterPrefabMapping>();

        /// <summary>Số lần tối đa mỗi đội được dùng MỘT loại booster trong một trận.</summary>
        [SerializeField] private int maxBoosterUses = 3;

        /// <summary>Tra cứu nhanh prefab theo loại (dựng từ <see cref="boosterPrefabs"/>).</summary>
        private Dictionary<BoosterType, Booster> boosterPrefabDict;

        /// <summary>Booster đang chạy: teamID → (loại → instance).</summary>
        private Dictionary<string, Dictionary<BoosterType, Booster>> activeBoosters;

        /// <summary>Danh sách loại booster đang chạy của từng đội. Phase 2: <c>SyncDictionary</c>.</summary>
        private Dictionary<string, List<BoosterType>> activeBoosterTypes;

        /// <summary>Số lần đã dùng từng loại booster của từng đội. Phase 2: <c>SyncDictionary</c>.</summary>
        private Dictionary<string, List<BoosterUsageCount>> boosterUsageCounts;

        /// <summary>Tay vợt đã đăng ký, tra theo teamID (dùng để tìm chủ sở hữu khi gán booster).</summary>
        private readonly Dictionary<string, BasePlayerController> registeredPlayers =
            new Dictionary<string, BasePlayerController>();

        private bool isSubscribed;

        /// <summary>Một dòng cấu hình: loại booster ↔ prefab ↔ thời lượng hiệu lực.</summary>
        [Serializable]
        public class BoosterPrefabMapping
        {
            /// <summary>Loại booster.</summary>
            public BoosterType type;

            /// <summary>Prefab chứa component <see cref="Booster"/> tương ứng.</summary>
            public Booster prefab;

            /// <summary>Thời gian hiệu lực (giây); &lt;= 0 thì dùng <c>activeDuration</c> của prefab.</summary>
            public float duration;
        }

        /// <summary>Số lần một đội đã dùng một loại booster trong trận.</summary>
        [Serializable]
        public class BoosterUsageCount
        {
            /// <summary>Loại booster.</summary>
            public BoosterType type;

            /// <summary>Số lần đã dùng.</summary>
            public int count;
        }

        // ------------------------------------------------------------------
        // Vòng đời
        // ------------------------------------------------------------------

        protected override void OnAwake()
        {
            base.OnAwake();

            activeBoosters = new Dictionary<string, Dictionary<BoosterType, Booster>>();
            activeBoosterTypes = new Dictionary<string, List<BoosterType>>();
            boosterUsageCounts = new Dictionary<string, List<BoosterUsageCount>>();

            InitializeBoosterDictionary();
        }

        /// <inheritdoc/>
        public override void OnStartServer()
        {
            base.OnStartServer();

            InitializeBoosterDictionary();
            Subscribe();
        }

        /// <inheritdoc/>
        public override void OnStopServer()
        {
            Unsubscribe();
            RemoveAllBoosters();

            base.OnStopServer();
        }

        /// <inheritdoc/>
        public override void OnStartClient()
        {
            base.OnStartClient();
            NotifyBoostersUpdated();
        }

        /// <inheritdoc/>
        public override void OnStopClient()
        {
            // Không xoá OnBoostersUpdated ở đây: UI tự huỷ đăng ký trong OnDestroy của nó.
            base.OnStopClient();
        }

        private void Subscribe()
        {
            if (isSubscribed) return;
            isSubscribed = true;
            GameManager.OnGameStateChanged += OnGameStateChanged;
        }

        private void Unsubscribe()
        {
            if (!isSubscribed) return;
            isSubscribed = false;
            GameManager.OnGameStateChanged -= OnGameStateChanged;
        }

        // ------------------------------------------------------------------
        // API công khai
        // ------------------------------------------------------------------

        /// <summary>
        /// Ghi nhận một tay vợt để manager biết tìm ai khi gán booster cho teamID đó.
        /// Gọi lúc dựng trận (hoặc khi player spawn).
        /// </summary>
        /// <param name="player">Tay vợt cần đăng ký.</param>
        public void RegisterPlayer(BasePlayerController player)
        {
            if (player == null || string.IsNullOrEmpty(player.teamID)) return;

            registeredPlayers[player.teamID] = player;

            if (!activeBoosters.ContainsKey(player.teamID))
                activeBoosters[player.teamID] = new Dictionary<BoosterType, Booster>();
            if (!activeBoosterTypes.ContainsKey(player.teamID))
                activeBoosterTypes[player.teamID] = new List<BoosterType>();
            if (!boosterUsageCounts.ContainsKey(player.teamID))
                boosterUsageCounts[player.teamID] = new List<BoosterUsageCount>();
        }

        /// <summary>
        /// Kích hoạt một booster cho một đội. Bị bỏ qua nếu đã dùng hết lượt hoặc không tìm ra tay vợt.
        /// Nếu loại đó đang chạy thì chỉ RESET lại đồng hồ (vẫn tính là một lượt dùng).
        /// </summary>
        /// <param name="teamID">Đội được nhận booster.</param>
        /// <param name="type">Loại booster.</param>
        // [Command(requiresAuthority = false)]
        public void AssignBooster(string teamID, BoosterType type)
        {
            if (string.IsNullOrEmpty(teamID) || type == BoosterType.None) return;
            if (!IsServer) return;

            if (GetRemainingBoosterUses(teamID, type) <= 0) return;

            BasePlayerController player = ResolvePlayer(teamID);
            if (player == null)
            {
                Debug.LogWarning($"[BoosterManager] Không tìm thấy tay vợt của đội '{teamID}' để gán booster {type}.");
                return;
            }

            if (!activeBoosters.TryGetValue(teamID, out Dictionary<BoosterType, Booster> teamBoosters))
            {
                teamBoosters = new Dictionary<BoosterType, Booster>();
                activeBoosters[teamID] = teamBoosters;
            }

            if (teamBoosters.TryGetValue(type, out Booster existing) && existing != null)
            {
                // Cùng loại đang chạy: Initialize sẽ tự gỡ hiệu ứng cũ (trả chỉ số gốc) rồi áp lại,
                // nên chỉ số KHÔNG bị cộng dồn, chỉ đồng hồ được reset.
                existing.Initialize(player, GetDuration(type), () => OnBoosterFinished(teamID, type));
            }
            else
            {
                Booster booster = SpawnBooster(type, player);
                if (booster == null) return;

                teamBoosters[type] = booster;
            }

            AddActiveType(teamID, type);
            IncreaseUsage(teamID, type);

            RpcOnBoosterApplied(teamID, type);
        }

        /// <summary>True nếu đội đang có booster loại này chạy.</summary>
        /// <param name="teamID">Đội cần kiểm tra.</param>
        /// <param name="type">Loại booster.</param>
        public bool IsBoosterActive(string teamID, BoosterType type)
        {
            if (string.IsNullOrEmpty(teamID) || type == BoosterType.None) return false;
            if (activeBoosters == null) return false;

            if (!activeBoosters.TryGetValue(teamID, out Dictionary<BoosterType, Booster> teamBoosters)) return false;
            if (!teamBoosters.TryGetValue(type, out Booster booster)) return false;

            return booster != null && booster.IsActive;
        }

        /// <summary>True nếu đội đang có BẤT KỲ booster nào chạy.</summary>
        /// <param name="teamID">Đội cần kiểm tra.</param>
        public bool IsAnyBoosterActive(string teamID)
        {
            if (string.IsNullOrEmpty(teamID) || activeBoosters == null) return false;
            if (!activeBoosters.TryGetValue(teamID, out Dictionary<BoosterType, Booster> teamBoosters)) return false;

            foreach (Booster booster in teamBoosters.Values)
            {
                if (booster != null && booster.IsActive) return true;
            }
            return false;
        }

        /// <summary>Số lượt còn dùng được của một loại booster trong trận này.</summary>
        /// <param name="teamID">Đội cần tra cứu.</param>
        /// <param name="type">Loại booster.</param>
        public int GetRemainingBoosterUses(string teamID, BoosterType type)
        {
            if (string.IsNullOrEmpty(teamID) || type == BoosterType.None) return 0;

            BoosterUsageCount entry = FindUsage(teamID, type, false);
            int used = entry != null ? entry.count : 0;

            return Mathf.Max(0, maxBoosterUses - used);
        }

        /// <summary>
        /// Phần trăm thời gian còn lại (0..1) của một booster đang chạy; trả 0 nếu không chạy.
        /// </summary>
        /// <param name="teamID">Đội cần tra cứu.</param>
        /// <param name="type">Loại booster.</param>
        public float GetBoosterRemainingTimePercentage(string teamID, BoosterType type)
        {
            if (string.IsNullOrEmpty(teamID) || type == BoosterType.None) return 0f;
            if (activeBoosters == null) return 0f;

            if (!activeBoosters.TryGetValue(teamID, out Dictionary<BoosterType, Booster> teamBoosters)) return 0f;
            if (!teamBoosters.TryGetValue(type, out Booster booster)) return 0f;
            if (booster == null || !booster.IsActive) return 0f;

            return booster.GetRemainingTimePercentage();
        }

        /// <summary>Danh sách loại booster đang chạy của một đội (bản sao, an toàn để duyệt trên UI).</summary>
        /// <param name="teamID">Đội cần tra cứu.</param>
        public List<BoosterType> GetActiveBoosterTypes(string teamID)
        {
            if (string.IsNullOrEmpty(teamID) || activeBoosterTypes == null) return new List<BoosterType>();
            return activeBoosterTypes.TryGetValue(teamID, out List<BoosterType> list)
                ? new List<BoosterType>(list)
                : new List<BoosterType>();
        }

        // ------------------------------------------------------------------
        // Nội bộ
        // ------------------------------------------------------------------

        /// <summary>Dựng lại bảng tra prefab từ danh sách cấu hình trong Inspector.</summary>
        private void InitializeBoosterDictionary()
        {
            boosterPrefabDict = new Dictionary<BoosterType, Booster>();
            if (boosterPrefabs == null) return;

            for (int i = 0; i < boosterPrefabs.Count; i++)
            {
                BoosterPrefabMapping mapping = boosterPrefabs[i];
                if (mapping == null || mapping.type == BoosterType.None || mapping.prefab == null) continue;

                boosterPrefabDict[mapping.type] = mapping.prefab;
            }
        }

        /// <summary>Tạo instance booster gắn dưới tay vợt và kích hoạt nó.</summary>
        /// <param name="type">Loại booster.</param>
        /// <param name="player">Tay vợt sở hữu.</param>
        /// <returns>Instance vừa tạo, hoặc <c>null</c> nếu chưa cấu hình prefab.</returns>
        private Booster SpawnBooster(BoosterType type, BasePlayerController player)
        {
            if (player == null) return null;
            if (boosterPrefabDict == null) InitializeBoosterDictionary();

            if (!boosterPrefabDict.TryGetValue(type, out Booster prefab) || prefab == null)
            {
                Debug.LogWarning($"[BoosterManager] Chưa cấu hình prefab cho booster {type}.");
                return null;
            }

            Booster booster = Instantiate(prefab, player.transform);
            booster.transform.localPosition = Vector3.zero;
            booster.transform.localRotation = Quaternion.identity;

            string teamID = player.teamID;
            booster.Initialize(player, GetDuration(type), () => OnBoosterFinished(teamID, type));

            return booster;
        }

        /// <summary>Booster tự kết thúc (hết giờ / đủ cú đánh / hết trận) — dọn khỏi bảng theo dõi.</summary>
        private void OnBoosterFinished(string teamID, BoosterType type)
        {
            if (activeBoosters != null
                && activeBoosters.TryGetValue(teamID, out Dictionary<BoosterType, Booster> teamBoosters))
            {
                teamBoosters.Remove(type);
            }

            RemoveActiveType(teamID, type);
            NotifyBoostersUpdated();
        }

        /// <summary>
        /// Gỡ SẠCH mọi booster của mọi đội và trả chỉ số gốc về cho từng tay vợt.
        /// Gọi khi trận kết thúc và trong <see cref="OnStopServer"/>.
        /// </summary>
        private void RemoveAllBoosters()
        {
            if (activeBoosters != null)
            {
                foreach (Dictionary<BoosterType, Booster> teamBoosters in activeBoosters.Values)
                {
                    foreach (Booster booster in teamBoosters.Values)
                    {
                        if (booster == null) continue;

                        // Silently: khôi phục chỉ số + huỷ event nhưng KHÔNG gọi callback
                        // (tránh sửa dictionary đang duyệt), rồi manager tự huỷ GameObject.
                        booster.RemoveBoosterSilently();
                        Destroy(booster.gameObject);
                    }
                    teamBoosters.Clear();
                }
            }

            if (activeBoosterTypes != null)
            {
                foreach (List<BoosterType> list in activeBoosterTypes.Values) list.Clear();
            }

            NotifyBoostersUpdated();
        }

        /// <summary>Trận kết thúc thì dọn sạch booster.</summary>
        private void OnGameStateChanged(GameState newState)
        {
            if (newState == GameState.GameOver) RemoveAllBoosters();
        }

        /// <summary>Client được báo có booster vừa kích hoạt (điểm móc cho VFX/SFX/UI).</summary>
        /// <param name="teamID">Đội vừa kích hoạt booster.</param>
        /// <param name="type">Loại booster.</param>
        // [ClientRpc]
        public void RpcOnBoosterApplied(string teamID, BoosterType type)
        {
            NotifyBoostersUpdated();
        }

        /// <summary>Báo cho UI rằng trạng thái booster đã đổi.</summary>
        // [ClientRpc]
        private void NotifyBoostersUpdated()
        {
            OnBoostersUpdated?.Invoke();
        }

        /// <summary>Thời lượng cấu hình cho một loại booster; 0 nghĩa là dùng giá trị của prefab.</summary>
        private float GetDuration(BoosterType type)
        {
            if (boosterPrefabs == null) return 0f;

            for (int i = 0; i < boosterPrefabs.Count; i++)
            {
                BoosterPrefabMapping mapping = boosterPrefabs[i];
                if (mapping != null && mapping.type == type) return mapping.duration;
            }
            return 0f;
        }

        /// <summary>Tìm tay vợt của một đội: bảng đăng ký → <see cref="GameManager"/> → quét scene.</summary>
        private BasePlayerController ResolvePlayer(string teamID)
        {
            if (registeredPlayers.TryGetValue(teamID, out BasePlayerController registered) && registered != null)
                return registered;

            if (GameManager.HasInstance)
            {
                if (GameManager.Instance.GetParticipant(teamID) is BasePlayerController participant
                    && participant != null)
                {
                    registeredPlayers[teamID] = participant;
                    return participant;
                }
            }

            BasePlayerController[] players = FindObjectsByType<BasePlayerController>(FindObjectsSortMode.None);
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] != null && players[i].teamID == teamID)
                {
                    registeredPlayers[teamID] = players[i];
                    return players[i];
                }
            }

            return null;
        }

        private void AddActiveType(string teamID, BoosterType type)
        {
            if (!activeBoosterTypes.TryGetValue(teamID, out List<BoosterType> list))
            {
                list = new List<BoosterType>();
                activeBoosterTypes[teamID] = list;
            }
            if (!list.Contains(type)) list.Add(type);
        }

        private void RemoveActiveType(string teamID, BoosterType type)
        {
            if (activeBoosterTypes == null) return;
            if (activeBoosterTypes.TryGetValue(teamID, out List<BoosterType> list)) list.Remove(type);
        }

        private void IncreaseUsage(string teamID, BoosterType type)
        {
            BoosterUsageCount entry = FindUsage(teamID, type, true);
            if (entry != null) entry.count++;
        }

        private BoosterUsageCount FindUsage(string teamID, BoosterType type, bool createIfMissing)
        {
            if (boosterUsageCounts == null) return null;

            if (!boosterUsageCounts.TryGetValue(teamID, out List<BoosterUsageCount> list))
            {
                if (!createIfMissing) return null;
                list = new List<BoosterUsageCount>();
                boosterUsageCounts[teamID] = list;
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].type == type) return list[i];
            }

            if (!createIfMissing) return null;

            BoosterUsageCount created = new BoosterUsageCount { type = type, count = 0 };
            list.Add(created);
            return created;
        }
    }
}
