using System;
using System.Collections.Generic;
using Pickleball.Data;
using StarterKit.Utilities;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Cầu nối giữa file lưu trên đĩa và các asset dữ liệu đang chạy.
    /// <para>
    /// Hai chiều đối xứng: <see cref="CaptureCurrentState"/> chụp trạng thái runtime thành
    /// <see cref="SaveData"/>, còn <see cref="ApplyData"/> rải dữ liệu đã lưu ngược lại vào
    /// <see cref="GameData"/>, <see cref="Shop"/> và các manager, rồi bắt buộc dựng lại loadout
    /// để chỉ số nhân vật khớp đúng với đồ đã nâng cấp.
    /// </para>
    /// <para>
    /// Tự lưu: mỗi lần cửa hàng đổi hoặc một trận vừa được trả thưởng, handler đặt một yêu cầu
    /// lưu có debounce (<see cref="autoSaveDebounceSeconds"/>) để hàng chục thay đổi liên tiếp
    /// chỉ tốn một lần ghi đĩa. Ngoài ra luôn lưu ngay khi ứng dụng bị tạm dừng hoặc thoát.
    /// </para>
    /// <para>
    /// Ngoài phạm vi: không đồng bộ backend, không quảng cáo, không IAP.
    /// </para>
    /// </summary>
    [DefaultExecutionOrder(-600)]
    public class SavedDataHandler : IndestructibleSingleton<SavedDataHandler>
    {
        /// <summary>Mật khẩu mã hoá file lưu cục bộ.</summary>
        [Header("Data Credentials")]
        public string password = "pickleball-local";

        /// <summary>Bản dữ liệu đang giữ trong bộ nhớ; là nguồn sự thật cho các cờ hướng dẫn.</summary>
        [Header("Runtime")]
        public SaveData _saveData;

        /// <summary>Hub dữ liệu trung tâm sẽ được nạp/chụp.</summary>
        [Header("Data Containers")]
        public GameData _gameData;

        /// <summary>Asset cửa hàng sẽ được nạp/chụp.</summary>
        public Shop _shop;

        /// <summary>Phát ngay sau khi dữ liệu đã được nạp và rải vào các asset.</summary>
        public static event Action<SaveData> OnDataLoaded;

        /// <summary>True khi <see cref="LoadOrCreate"/> đã chạy xong.</summary>
        public bool IsDataLoaded { get; private set; }

        /// <summary>Bật/tắt cơ chế tự lưu khi cửa hàng đổi hoặc trận đấu vừa trả thưởng.</summary>
        [Header("Auto Save")]
        public bool autoSaveOnChange = true;

        /// <summary>Thời gian gộp các yêu cầu lưu liên tiếp (giây).</summary>
        public float autoSaveDebounceSeconds = 1f;

        /// <summary>True khi đang có một yêu cầu lưu chờ hết thời gian debounce.</summary>
        private bool isSaveRequested;

        /// <summary>Thời gian còn lại trước khi yêu cầu lưu được thực thi.</summary>
        private float saveCountdown;

        /// <summary>True khi đã đăng ký các sự kiện tự lưu (chống đăng ký lặp).</summary>
        private bool eventsSubscribed;

        /// <summary>
        /// True khi vẫn còn phần dữ liệu phải chờ các manager theo scene xuất hiện mới rải được
        /// (<see cref="DailyChallengeManager"/>, <see cref="DailyRewardManager"/>).
        /// </summary>
        private bool isManagerApplyPending;

        /// <summary>Thời gian còn lại cho phép chờ các manager xuất hiện (giây).</summary>
        private float managerApplyCountdown;

        /// <summary>Trần thời gian chờ manager; hết hạn thì bỏ qua phần đó và chạy tiếp.</summary>
        private const float ManagerApplyTimeoutSeconds = 10f;

        // ------------------------------------------------------------------
        // Vòng đời
        // ------------------------------------------------------------------

        /// <inheritdoc/>
        protected override void OnAwake()
        {
            base.OnAwake();

            if (_saveData == null) _saveData = CreateDefaultSaveData();
            SubscribeAutoSave();
        }

        /// <inheritdoc/>
        protected override void OnDestroy()
        {
            UnsubscribeAutoSave();
            base.OnDestroy();
        }

        private void Update()
        {
            float deltaTime = Time.unscaledDeltaTime;

            if (isManagerApplyPending)
            {
                managerApplyCountdown -= deltaTime;
                if (TryApplyManagerData(_saveData) || managerApplyCountdown <= 0f)
                {
                    isManagerApplyPending = false;
                }
            }

            if (!isSaveRequested) return;

            saveCountdown -= deltaTime;
            if (saveCountdown > 0f) return;

            isSaveRequested = false;
            SaveNow();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) SaveNow();
        }

        private void OnApplicationQuit()
        {
            SaveNow();
        }

        // ------------------------------------------------------------------
        // API chính
        // ------------------------------------------------------------------

        /// <summary>
        /// Nạp file lưu và rải vào các asset. Nếu chưa có file (hoặc file hỏng) thì giữ nguyên
        /// giá trị mặc định của asset và ghi ngay một file lưu đầu tiên từ trạng thái đó.
        /// An toàn khi gọi nhiều lần.
        /// </summary>
        public void LoadOrCreate()
        {
            // Bootstrap có thể gọi trước khi OnAwake kịp chạy (component vừa được tìm thấy).
            if (_saveData == null) _saveData = CreateDefaultSaveData();
            SubscribeAutoSave();

            bool hadFile = SaveGameData.Exists;

            SaveData loaded = SaveGameData.Load<SaveData>(null, password);

            if (loaded == null)
            {
                // Chưa có save hợp lệ: asset giữ nguyên giá trị mặc định, chỉ chốt lại thành file.
                _saveData = CreateDefaultSaveData();
                IsDataLoaded = true;

                SaveNow();
                Debug.Log(hadFile
                    ? "[SavedDataHandler] File lưu không đọc được — đã tạo lại save mới từ dữ liệu mặc định."
                    : "[SavedDataHandler] Chưa có file lưu — đã tạo save mới từ dữ liệu mặc định.");
            }
            else
            {
                _saveData = loaded;
                ApplyData(_saveData);
                IsDataLoaded = true;
            }

            Action<SaveData> handler = OnDataLoaded;
            if (handler != null) handler.Invoke(_saveData);
        }

        /// <summary>Chụp trạng thái hiện tại và ghi xuống đĩa ngay lập tức.</summary>
        public void SaveNow()
        {
            isSaveRequested = false;

            _saveData = CaptureCurrentState();
            SaveGameData.Save(_saveData, password);
        }

        /// <summary>
        /// Đặt một yêu cầu lưu, gộp mọi yêu cầu trong <see cref="autoSaveDebounceSeconds"/> giây
        /// kế tiếp thành một lần ghi đĩa duy nhất.
        /// </summary>
        public void RequestSave()
        {
            if (!autoSaveOnChange)
            {
                SaveNow();
                return;
            }

            isSaveRequested = true;
            saveCountdown = Mathf.Max(0f, autoSaveDebounceSeconds);
        }

        /// <summary>
        /// Xoá sạch tiến trình đã lưu: xoá file trên đĩa và đưa bản trong bộ nhớ về mặc định.
        /// Các asset dữ liệu KHÔNG bị ghi đè ở đây — lần khởi động sau chúng sẽ tự là mặc định.
        /// </summary>
        [ContextMenu("Reset To Default")]
        public void ResetToDefault()
        {
            SaveGameData.Delete();

            _saveData = CreateDefaultSaveData();
            isSaveRequested = false;

            Debug.Log("[SavedDataHandler] Đã xoá file lưu và đưa dữ liệu về mặc định.");

            Action<SaveData> handler = OnDataLoaded;
            if (handler != null) handler.Invoke(_saveData);
        }

        // ------------------------------------------------------------------
        // Chụp trạng thái
        // ------------------------------------------------------------------

        /// <summary>
        /// Chụp toàn bộ tiến trình đang chạy thành một <see cref="SaveData"/> mới.
        /// Các nguồn dữ liệu chưa được gán (hoặc manager chưa tồn tại) được bỏ qua an toàn,
        /// giữ nguyên giá trị cũ trong <see cref="_saveData"/> nếu có.
        /// </summary>
        public SaveData CaptureCurrentState()
        {
            SaveData data = new SaveData();
            SaveData previous = _saveData;

            // --- Hướng dẫn: chỉ tồn tại trong save, không có asset nào giữ hộ. ---
            data.isTutorialCompleted = previous != null && previous.isTutorialCompleted;
            data.completedTutorialSteps = previous != null && previous.completedTutorialSteps != null
                ? new List<TutorialType>(previous.completedTutorialSteps)
                : new List<TutorialType>();

            // --- Giữ lại các trường không chụp được để không mất dữ liệu cũ. ---
            if (previous != null)
            {
                data.lastDailyRewardTime = previous.lastDailyRewardTime;
                data.lastDailyChallengeResetTime = previous.lastDailyChallengeResetTime;
                data.dailyChallenges = previous.dailyChallenges;
            }

            CaptureGameData(data);
            CaptureShop(data);
            CaptureManagers(data);

            return data;
        }

        /// <summary>Chụp ví tiền, hồ sơ, kho tazo/booster, level, locker, giải đấu và tay thuận.</summary>
        private void CaptureGameData(SaveData data)
        {
            if (_gameData == null) return;

            data.coins = _gameData.totalCoins;
            data.gems = _gameData.totalGems;
            data.handSide = _gameData.handSide;
            data.savedProfileData = SavedPlayerData.From(_gameData.playerProfileData);

            if (_gameData.tazoData != null && _gameData.tazoData.tazos != null)
            {
                data.tazoCounts = new List<TazoCountData>();
                for (int i = 0; i < _gameData.tazoData.tazos.Count; i++)
                {
                    TazoCountData entry = _gameData.tazoData.tazos[i];
                    if (entry == null) continue;
                    data.tazoCounts.Add(new TazoCountData(entry.ShopItemType, entry.count));
                }
            }

            if (_gameData.boostersData != null && _gameData.boostersData.boosters != null)
            {
                data.boosters = new List<BoosterCountData>();
                for (int i = 0; i < _gameData.boostersData.boosters.Count; i++)
                {
                    BoosterCountData entry = _gameData.boostersData.boosters[i];
                    if (entry == null) continue;
                    data.boosters.Add(new BoosterCountData { boosterType = entry.boosterType, count = entry.count });
                }
            }

            if (_gameData.playerLevels != null)
            {
                data.playerLevelsCollectStatuses = _gameData.playerLevels.GetCollectStatuses();
            }

            if (_gameData.slotsData != null && _gameData.slotsData.slots != null)
            {
                data.slotDataArray = new List<SlotSaveData>();
                for (int i = 0; i < _gameData.slotsData.slots.Length; i++)
                {
                    SlotsData.SlotData slot = _gameData.slotsData.slots[i];
                    if (slot == null)
                    {
                        data.slotDataArray.Add(new SlotSaveData());
                        continue;
                    }

                    // Chốt lại chuỗi thời gian phòng khi mốc runtime mới hơn bản đã ghi.
                    slot.UpdateTimeToStored();
                    data.slotDataArray.Add(SlotSaveData.From(slot));
                }
            }

            if (_gameData.tournamentsData != null)
            {
                PlayerTournamentProgress progress = _gameData.tournamentsData.currentProgress;
                if (progress != null)
                {
                    data.tournamentProgress = new PlayerTournamentProgress
                    {
                        tournamentId = progress.tournamentId,
                        currentStage = progress.currentStage,
                        isEliminated = progress.isEliminated,
                        isCompleted = progress.isCompleted
                    };
                }
            }
        }

        /// <summary>Chụp skin đã mua, trang bị đang chọn, nước tăng lực, các tab shop và túi đồ.</summary>
        private void CaptureShop(SaveData data)
        {
            if (_shop == null) return;

            data.energyDrinks = _shop.energyDrinks;
            data.equipedBallIndex = _shop.selectedBallIndex;
            data.equipedRacketIndex = _shop.selectedRacketIndex;

            if (_shop.ballsList != null)
            {
                data.ballPurchasedStatuses = new List<bool>();
                for (int i = 0; i < _shop.ballsList.Count; i++)
                {
                    BallShopItem ball = _shop.ballsList[i];
                    data.ballPurchasedStatuses.Add(ball != null && ball.isPurchased);
                }
            }

            if (_shop.racketsList != null)
            {
                data.racketPurchasedStatuses = new List<bool>();
                for (int i = 0; i < _shop.racketsList.Count; i++)
                {
                    ShopItem racket = _shop.racketsList[i];
                    data.racketPurchasedStatuses.Add(racket != null && racket.isPurchased);
                }
            }

            if (_shop.shopCategories != null)
            {
                data.shopCategories = new List<ShopCategorySaveData>();
                for (int i = 0; i < _shop.shopCategories.Count; i++)
                {
                    ShopCategoryData category = _shop.shopCategories[i];
                    if (category == null) continue;

                    ShopCategorySaveData saved = new ShopCategorySaveData
                    {
                        categoryType = category.categoryType,
                        items = new List<ItemSaveData>(),
                        selectedItemName = category.selectedItem != null ? category.selectedItem.itemName : string.Empty
                    };

                    if (category.items != null)
                    {
                        for (int j = 0; j < category.items.Count; j++)
                        {
                            Item item = category.items[j];
                            if (item == null) continue;
                            saved.items.Add(new ItemSaveData(item.itemName, item.currentLevel, item.isPurchased));
                        }
                    }

                    data.shopCategories.Add(saved);
                }
            }

            if (_shop.kitbags != null)
            {
                data.kitbagsData = new List<KitbagSaveData>();
                for (int i = 0; i < _shop.kitbags.Count; i++)
                {
                    Kitbag kitbag = _shop.kitbags[i];
                    if (kitbag == null) continue;

                    data.kitbagsData.Add(new KitbagSaveData(
                        kitbag.kitbagType,
                        kitbag.totalAds,
                        kitbag.watchedAds,
                        kitbag.isAdAvailable,
                        kitbag.isRewardCollected,
                        kitbag.shouldShowInterstitial));
                }
            }
        }

        /// <summary>Chụp nhiệm vụ hằng ngày và mốc quà hằng ngày nếu manager tương ứng đang sống.</summary>
        private void CaptureManagers(SaveData data)
        {
            if (DailyChallengeManager.HasInstance)
            {
                DailyChallengeManager manager = DailyChallengeManager.Instance;
                data.dailyChallenges = manager.GetDailyChallenges();
                data.lastDailyChallengeResetTime = manager.GetLastResetTimeString();
            }

            if (DailyRewardManager.HasInstance)
            {
                data.lastDailyRewardTime = DailyRewardManager.Instance.GetLastClaimTimeString();
            }
        }

        // ------------------------------------------------------------------
        // Rải dữ liệu
        // ------------------------------------------------------------------

        /// <summary>
        /// Rải dữ liệu đã lưu vào toàn bộ asset đang chạy rồi dựng lại trang bị và chỉ số nhân vật.
        /// Danh sách <c>null</c> trong <paramref name="saveData"/> được coi là "không có gì để nạp"
        /// và giữ nguyên giá trị mặc định của asset.
        /// </summary>
        /// <param name="saveData">Dữ liệu cần rải; bỏ qua nếu <c>null</c>.</param>
        public void ApplyData(SaveData saveData)
        {
            if (saveData == null) return;

            ApplyGameData(saveData);
            ApplyShop(saveData);

            // Manager theo scene có thể chưa Awake ở thời điểm này (bootstrap chạy trước),
            // nên phần của chúng được thử lại ở các frame sau.
            if (!TryApplyManagerData(saveData))
            {
                isManagerApplyPending = true;
                managerApplyCountdown = ManagerApplyTimeoutSeconds;
            }

            // Bắt buộc: chỉ số nhân vật phải khớp với đồ vừa nạp.
            if (_shop != null) _shop.SetupLoadout();
            if (_gameData != null && _gameData.playerLoadout != null) _gameData.playerLoadout.UpdateProfile();
        }

        /// <summary>Rải ví tiền, hồ sơ, kho tazo/booster, level, locker, giải đấu và tay thuận.</summary>
        private void ApplyGameData(SaveData saveData)
        {
            if (_gameData == null) return;

            SetCoins(saveData.coins);
            SetGems(saveData.gems);
            _gameData.handSide = saveData.handSide;

            if (saveData.savedProfileData != null)
            {
                PlayerProfileData profile = saveData.savedProfileData.ToPlayerProfileData();
                PlayerProfileData target = _gameData.playerProfileData;

                if (target == null)
                {
                    _gameData.playerProfileData = profile;
                }
                else
                {
                    // Ghi đè tại chỗ để mọi tham chiếu đang giữ hồ sơ cũ vẫn thấy dữ liệu mới.
                    target.playerName = profile.playerName;
                    target.totalMatches = profile.totalMatches;
                    target.totalWins = profile.totalWins;
                    target.winRate = profile.winRate;
                    target.avatarIndex = profile.avatarIndex;
                    target.trophies = profile.trophies;
                    target.consecutiveWins = profile.consecutiveWins;
                    target.level = profile.level;
                    target.hasUsedFreeRename = profile.hasUsedFreeRename;
                }

                if (_gameData.playerLevels != null && _gameData.playerProfileData != null)
                {
                    _gameData.playerProfileData.RecalculateLevel(_gameData.playerLevels);
                }
            }

            if (saveData.tazoCounts != null && _gameData.tazoData != null)
            {
                _gameData.tazoData.ApplyTazoCounts(saveData.tazoCounts);
            }

            if (saveData.boosters != null && _gameData.boostersData != null)
            {
                ApplyBoosters(saveData.boosters);
            }

            if (saveData.playerLevelsCollectStatuses != null && _gameData.playerLevels != null)
            {
                _gameData.playerLevels.LoadCollectStatus(saveData.playerLevelsCollectStatuses);
            }

            if (saveData.slotDataArray != null && _gameData.slotsData != null)
            {
                SlotsData.SlotData[] slots = new SlotsData.SlotData[saveData.slotDataArray.Count];
                for (int i = 0; i < saveData.slotDataArray.Count; i++)
                {
                    SlotSaveData saved = saveData.slotDataArray[i];
                    slots[i] = saved != null ? saved.ToSlotData() : new SlotsData.SlotData();
                }
                _gameData.slotsData.ApplySlotsData(slots);
            }

            if (saveData.tournamentProgress != null && _gameData.tournamentsData != null)
            {
                _gameData.tournamentsData.ApplyProgress(saveData.tournamentProgress);
            }
        }

        /// <summary>Rải skin đã mua, trang bị đang chọn, nước tăng lực, các tab shop và túi đồ.</summary>
        private void ApplyShop(SaveData saveData)
        {
            if (_shop == null) return;

            _shop.energyDrinks = Mathf.Max(0, saveData.energyDrinks);

            if (saveData.ballPurchasedStatuses != null && _shop.ballsList != null)
            {
                int count = Mathf.Min(saveData.ballPurchasedStatuses.Count, _shop.ballsList.Count);
                for (int i = 0; i < count; i++)
                {
                    BallShopItem ball = _shop.ballsList[i];
                    if (ball != null) ball.isPurchased = saveData.ballPurchasedStatuses[i];
                }
            }

            if (saveData.racketPurchasedStatuses != null && _shop.racketsList != null)
            {
                int count = Mathf.Min(saveData.racketPurchasedStatuses.Count, _shop.racketsList.Count);
                for (int i = 0; i < count; i++)
                {
                    ShopItem racket = _shop.racketsList[i];
                    if (racket != null) racket.isPurchased = saveData.racketPurchasedStatuses[i];
                }
            }

            // SetupLoadout() ở cuối ApplyData sẽ kẹp lại nếu chỉ số trỏ vào skin chưa sở hữu.
            _shop.selectedBallIndex = saveData.equipedBallIndex;
            _shop.selectedRacketIndex = saveData.equipedRacketIndex;

            ApplyShopCategories(saveData.shopCategories);
            ApplyKitbags(saveData.kitbagsData);
        }

        /// <summary>Khớp từng tab shop theo loại và từng vật phẩm theo TÊN (không theo chỉ số).</summary>
        private void ApplyShopCategories(List<ShopCategorySaveData> savedCategories)
        {
            if (savedCategories == null || _shop.shopCategories == null) return;

            for (int i = 0; i < savedCategories.Count; i++)
            {
                ShopCategorySaveData saved = savedCategories[i];
                if (saved == null) continue;

                ShopCategoryData category = FindCategory(saved.categoryType);
                if (category == null || category.items == null) continue;

                if (saved.items != null)
                {
                    for (int j = 0; j < saved.items.Count; j++)
                    {
                        ItemSaveData savedItem = saved.items[j];
                        if (savedItem == null || string.IsNullOrEmpty(savedItem.itemName)) continue;

                        Item item = FindItemByName(category, savedItem.itemName);
                        if (item == null) continue;

                        item.isPurchased = savedItem.isPurchased;
                        item.currentLevel = Mathf.Clamp(savedItem.currentLevel, 0, Mathf.Max(0, item.maxLevel));
                    }
                }

                if (string.IsNullOrEmpty(saved.selectedItemName)) continue;

                Item selected = FindItemByName(category, saved.selectedItemName);
                if (selected != null && selected.isPurchased) category.selectedItem = selected;
            }
        }

        /// <summary>Khớp bộ đếm quảng cáo của từng bậc túi đồ theo <see cref="KitbagType"/>.</summary>
        private void ApplyKitbags(List<KitbagSaveData> savedKitbags)
        {
            if (savedKitbags == null || _shop.kitbags == null) return;

            for (int i = 0; i < savedKitbags.Count; i++)
            {
                KitbagSaveData saved = savedKitbags[i];
                if (saved == null) continue;

                Kitbag kitbag = _shop.GetKitbag(saved.kitbagType);
                if (kitbag == null) continue;

                kitbag.totalAds = saved.totalAds;
                kitbag.watchedAds = saved.watchedAds;
                kitbag.isAdAvailable = saved.isAdAvailable;
                kitbag.isRewardCollected = saved.isRewardCollected;
                kitbag.shouldShowInterstitial = saved.shouldShowInterstitial;
            }
        }

        /// <summary>
        /// Rải phần dữ liệu thuộc về các manager theo scene.
        /// Trả về <c>true</c> khi không còn gì phải chờ nữa (đã rải xong hoặc không có gì để rải).
        /// </summary>
        /// <param name="saveData">Dữ liệu đã lưu.</param>
        private bool TryApplyManagerData(SaveData saveData)
        {
            if (saveData == null) return true;

            bool needChallenges = saveData.dailyChallenges != null
                                  || !string.IsNullOrEmpty(saveData.lastDailyChallengeResetTime);
            bool needDailyReward = !string.IsNullOrEmpty(saveData.lastDailyRewardTime);

            if (needChallenges && DailyChallengeManager.HasInstance)
            {
                DailyChallengeManager manager = DailyChallengeManager.Instance;

                // Đặt mốc reset TRƯỚC khi nạp danh sách, nếu không lần kiểm tra kế tiếp
                // sẽ tưởng chưa từng reset và phát lại nhiệm vụ mới.
                manager.SetLastResetTimeString(saveData.lastDailyChallengeResetTime);
                if (saveData.dailyChallenges != null) manager.LoadDailyChallenges(saveData.dailyChallenges);

                needChallenges = false;
            }

            if (needDailyReward && DailyRewardManager.HasInstance)
            {
                DailyRewardManager.Instance.SetLastClaimTimeString(saveData.lastDailyRewardTime);
                needDailyReward = false;
            }

            return !needChallenges && !needDailyReward;
        }

        // ------------------------------------------------------------------
        // Hướng dẫn (tutorial)
        // ------------------------------------------------------------------

        /// <summary>True khi người chơi đã hoàn thành hướng dẫn.</summary>
        public bool GetTutorialCompletion()
        {
            return _saveData != null && _saveData.isTutorialCompleted;
        }

        /// <summary>
        /// Đánh dấu một bước hướng dẫn đã hoàn thành và đặt yêu cầu lưu.
        /// Bước <see cref="TutorialType.TutorialCompleted"/> đồng thời bật cờ hoàn thành tổng.
        /// </summary>
        /// <param name="tutorialType">Bước hướng dẫn vừa xong.</param>
        public void SetTutorialStepCompletion(TutorialType tutorialType)
        {
            if (tutorialType == TutorialType.None) return;
            if (_saveData == null) _saveData = CreateDefaultSaveData();
            if (_saveData.completedTutorialSteps == null) _saveData.completedTutorialSteps = new List<TutorialType>();

            if (!_saveData.completedTutorialSteps.Contains(tutorialType))
            {
                _saveData.completedTutorialSteps.Add(tutorialType);
            }

            if (tutorialType == TutorialType.TutorialCompleted) _saveData.isTutorialCompleted = true;
            if (AreAllTutorialsCompleted()) _saveData.isTutorialCompleted = true;

            RequestSave();
        }

        /// <summary>Bản sao danh sách các bước hướng dẫn đã hoàn thành.</summary>
        public List<TutorialType> GetCompletedTutorialSteps()
        {
            if (_saveData == null || _saveData.completedTutorialSteps == null) return new List<TutorialType>();
            return new List<TutorialType>(_saveData.completedTutorialSteps);
        }

        /// <summary>
        /// True khi mọi bước trong <see cref="TutorialType"/> (trừ <c>None</c>) đã hoàn thành,
        /// hoặc cờ hoàn thành tổng đã được bật.
        /// </summary>
        public bool AreAllTutorialsCompleted()
        {
            if (_saveData == null) return false;
            if (_saveData.isTutorialCompleted) return true;
            if (_saveData.completedTutorialSteps == null) return false;

            Array values = Enum.GetValues(typeof(TutorialType));
            for (int i = 0; i < values.Length; i++)
            {
                TutorialType type = (TutorialType)values.GetValue(i);
                if (type == TutorialType.None) continue;
                if (!_saveData.completedTutorialSteps.Contains(type)) return false;
            }
            return true;
        }

        /// <summary>Xoá sạch tiến trình hướng dẫn để chơi lại từ đầu, rồi đặt yêu cầu lưu.</summary>
        [ContextMenu("Clear Tutorial Data")]
        public void ClearTutorialData()
        {
            if (_saveData == null) _saveData = CreateDefaultSaveData();

            _saveData.isTutorialCompleted = false;
            if (_saveData.completedTutorialSteps == null) _saveData.completedTutorialSteps = new List<TutorialType>();
            else _saveData.completedTutorialSteps.Clear();

            RequestSave();
        }

        // ------------------------------------------------------------------
        // Nội bộ
        // ------------------------------------------------------------------

        /// <summary>Đăng ký các nguồn sự kiện kích hoạt tự lưu. An toàn khi gọi nhiều lần.</summary>
        private void SubscribeAutoSave()
        {
            if (eventsSubscribed) return;
            eventsSubscribed = true;

            Shop.OnShopChanged += HandleShopChanged;
            MatchRewardHandler.OnMatchRewarded += HandleMatchRewarded;
        }

        /// <summary>Huỷ đăng ký các nguồn sự kiện tự lưu. An toàn khi gọi nhiều lần.</summary>
        private void UnsubscribeAutoSave()
        {
            if (!eventsSubscribed) return;
            eventsSubscribed = false;

            Shop.OnShopChanged -= HandleShopChanged;
            MatchRewardHandler.OnMatchRewarded -= HandleMatchRewarded;
        }

        /// <summary>Cửa hàng vừa đổi (mua, nâng cấp, trang bị) — hẹn lưu.</summary>
        private void HandleShopChanged()
        {
            if (!IsDataLoaded) return;
            RequestSave();
        }

        /// <summary>Một trận vừa được trả thưởng xong — hẹn lưu.</summary>
        /// <param name="result">Bảng tổng hợp phần thưởng (không dùng, chỉ cần biết là đã xong).</param>
        private void HandleMatchRewarded(MatchRewardHandler.MatchRewardResult result)
        {
            RequestSave();
        }

        /// <summary>Đặt số coin về đúng giá trị mong muốn qua API của <see cref="GameData"/> để UI nhận sự kiện.</summary>
        private void SetCoins(int value)
        {
            int target = Mathf.Max(0, value);
            if (_gameData.totalCoins > target) _gameData.ReduceCoins(_gameData.totalCoins - target);
            else if (_gameData.totalCoins < target) _gameData.IncreaseCoins(target - _gameData.totalCoins);
        }

        /// <summary>Đặt số gem về đúng giá trị mong muốn qua API của <see cref="GameData"/> để UI nhận sự kiện.</summary>
        private void SetGems(int value)
        {
            int target = Mathf.Max(0, value);
            if (_gameData.totalGems > target) _gameData.ReduceGems(_gameData.totalGems - target);
            else if (_gameData.totalGems < target) _gameData.IncreaseGems(target - _gameData.totalGems);
        }

        /// <summary>
        /// Ghi đè kho booster từ dữ liệu lưu: đưa mọi loại về 0 rồi cộng lại theo bản lưu.
        /// (<see cref="BoostersData"/> chưa có hàm <c>Apply</c> tương đương <c>TazoData.ApplyTazoCounts</c>.)
        /// </summary>
        private void ApplyBoosters(List<BoosterCountData> savedBoosters)
        {
            BoostersData boostersData = _gameData.boostersData;
            if (boostersData.boosters != null)
            {
                for (int i = 0; i < boostersData.boosters.Count; i++)
                {
                    BoosterCountData entry = boostersData.boosters[i];
                    if (entry != null) entry.count = 0;
                }
            }

            for (int i = 0; i < savedBoosters.Count; i++)
            {
                BoosterCountData saved = savedBoosters[i];
                if (saved == null || saved.boosterType == BoosterType.None) continue;
                boostersData.Add(saved.boosterType, Mathf.Max(0, saved.count));
            }
        }

        /// <summary>Tìm tab shop theo loại vật phẩm; <c>null</c> nếu không có.</summary>
        private ShopCategoryData FindCategory(ShopItemType categoryType)
        {
            for (int i = 0; i < _shop.shopCategories.Count; i++)
            {
                ShopCategoryData category = _shop.shopCategories[i];
                if (category != null && category.categoryType == categoryType) return category;
            }
            return null;
        }

        /// <summary>Tìm vật phẩm trong một tab theo tên; <c>null</c> nếu không có.</summary>
        private static Item FindItemByName(ShopCategoryData category, string itemName)
        {
            for (int i = 0; i < category.items.Count; i++)
            {
                Item item = category.items[i];
                if (item != null && item.itemName == itemName) return item;
            }
            return null;
        }

        /// <summary>Tạo một <see cref="SaveData"/> rỗng đúng chuẩn (danh sách đã khởi tạo).</summary>
        private static SaveData CreateDefaultSaveData()
        {
            return new SaveData
            {
                version = 1,
                completedTutorialSteps = new List<TutorialType>()
            };
        }
    }
}
