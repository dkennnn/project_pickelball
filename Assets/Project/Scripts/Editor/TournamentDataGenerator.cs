using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Pickleball.EditorTools
{
    /// <summary>
    /// Sinh dữ liệu giải đấu (<see cref="TournamentsData"/>) và bảng khoảng mục tiêu
    /// nhiệm vụ hằng ngày (<c>GameData.dailyChallengeLevels</c>).
    /// <para>
    /// Cấu trúc giải bám theo bản gốc đã giải mã (tournament_001 "Tournament Cup", 3 vòng,
    /// bracket 8 người) nhưng đặt lại tên đối thủ, phí vào giải và phần thưởng cho phù hợp
    /// với nền kinh tế của bản dựng lại.
    /// </para>
    /// Chạy lại nhiều lần an toàn: asset đã tồn tại được ghi đè giá trị thay vì tạo trùng.
    /// </summary>
    public static class TournamentDataGenerator
    {
        private const string RootFolder = "Assets/Project/ScriptableObjects";
        private const string GameFolder = RootFolder + "/Game";
        private const string GameDataAssetPath = GameFolder + "/GameData.asset";
        private const string TournamentsDataAssetPath = GameFolder + "/TournamentsData.asset";

        /// <summary>Tạo/cập nhật asset giải đấu và bảng nhiệm vụ hằng ngày rồi lưu xuống đĩa.</summary>
        [MenuItem("Pickleball/Generate Tournament & Challenge Data")]
        public static void GenerateAll()
        {
            EnsureFolder(RootFolder);
            EnsureFolder(GameFolder);

            GameData gameData = FindGameData();

            TournamentsData tournamentsData = LoadOrCreate<TournamentsData>(TournamentsDataAssetPath);
            tournamentsData.tournament = BuildTournamentCup();
            tournamentsData.currentProgress = new PlayerTournamentProgress();
            tournamentsData.currentMatch = null;
            tournamentsData.gameData = gameData;
            EditorUtility.SetDirty(tournamentsData);

            if (gameData != null)
            {
                gameData.tournamentsData = tournamentsData;
                gameData.dailyChallengeLevels = BuildDailyChallengeLevels();
                EditorUtility.SetDirty(gameData);
            }
            else
            {
                Debug.LogWarning("[TournamentDataGenerator] Chưa tìm thấy asset GameData — " +
                                 "hãy gán tay tournamentsData và dailyChallengeLevels sau khi sinh xong.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[TournamentDataGenerator] Đã sinh xong TournamentsData và bảng nhiệm vụ hằng ngày.");
        }

        // ------------------------------------------------------------------ Tournament

        /// <summary>
        /// Giải "Tournament Cup": 3 vòng Quarter → Semi → Finals, bracket 8 tay vợt
        /// (người chơi + 7 AI), đối thủ khó dần và trophy tăng dần.
        /// </summary>
        private static Tournament BuildTournamentCup()
        {
            // Bracket 8 người: người chơi + 7 AI. Chỉ số avatar 0..6 khớp GameData.avatarSprites.
            const string Marlo = "Marlo_99";      // avatar 0 — đối thủ vòng tứ kết
            const string Zephyr = "ZephyrAce";    // avatar 1 — đối thủ vòng bán kết
            const string Riven = "Riven.TV";      // avatar 2
            const string Onyx = "OnyxSmash";      // avatar 3 — đối thủ chung kết
            const string Sable = "SableDink";     // avatar 4
            const string Halo = "HaloVolley";     // avatar 5
            const string Fenn = "Fenn_Rally";     // avatar 6

            TournamentStage quarter = new TournamentStage(0, "Quarter Finals",
                new TournamentMatch(1, Marlo, 0, AIDifficulty.Amateur, 900, "Gully Grounds"),
                new List<AIMatchResult>
                {
                    new AIMatchResult(Zephyr, 1, Riven, 2),
                    new AIMatchResult(Onyx, 3, Sable, 4),
                    new AIMatchResult(Halo, 5, Fenn, 6)
                });

            TournamentStage semi = new TournamentStage(1, "Semi Finals",
                new TournamentMatch(2, Zephyr, 1, AIDifficulty.Competitor, 1400, "Club Arena"),
                new List<AIMatchResult>
                {
                    new AIMatchResult(Onyx, 3, Halo, 5)
                });

            TournamentStage finals = new TournamentStage(2, "Finals",
                new TournamentMatch(3, Onyx, 3, AIDifficulty.Pro, 1900, "Champion's Dome"),
                new List<AIMatchResult>());

            return new Tournament
            {
                id = "tournament_001",
                name = "Tournament Cup",
                description = "Play tournament and win exciting rewards",
                requiredLevel = 3,
                entryCost = 500,
                entryCostType = CurrencyType.Coins,
                stages = new List<TournamentStage> { quarter, semi, finals },
                rewards = new List<DynamicReward>
                {
                    new DynamicReward(RewardType.Coins, 2000),
                    new DynamicReward(RewardType.Gems, 100),
                    new DynamicReward(RewardType.CharacterTazos, 50)
                },
                // Khung thời gian rộng để giải luôn mở trong bản chơi thử.
                startDateString = "2025-01-01T00:00:00Z",
                endDateString = "2030-12-31T23:59:59Z",
                isActive = true
            };
        }

        // ------------------------------------------------------- Daily challenge levels

        /// <summary>
        /// Bảng khoảng mục tiêu nhiệm vụ hằng ngày cho 5 bậc trình độ (Newbie → Master).
        /// Chỉ khai báo các loại nhiệm vụ đã có nguồn sự kiện tự động trong
        /// <see cref="DailyChallengeManager"/>; các loại phụ thuộc Shop/Locker/Booster
        /// sẽ bổ sung khi những hệ đó gọi được <c>UpdateChallengeProgress</c>.
        /// </summary>
        private static List<ExperienceLevel> BuildDailyChallengeLevels()
        {
            return new List<ExperienceLevel>
            {
                MakeLevel(AIDifficulty.Newbie,
                    /* PlayMatches       */ 1, 2,
                    /* WinMatches        */ 1, 1,
                    /* ScorePoints       */ 3, 6,
                    /* PlayMinutes       */ 3, 5,
                    /* PlayMinutesInGame */ 1, 2,
                    /* RallySeconds      */ 5, 10,
                    /* HitVolleys        */ 2, 4),

                MakeLevel(AIDifficulty.Amateur,
                    2, 3,
                    1, 2,
                    6, 10,
                    5, 8,
                    2, 3,
                    8, 14,
                    4, 7),

                MakeLevel(AIDifficulty.Competitor,
                    3, 4,
                    2, 3,
                    10, 15,
                    8, 12,
                    3, 4,
                    12, 20,
                    7, 11),

                MakeLevel(AIDifficulty.Pro,
                    4, 6,
                    3, 4,
                    14, 22,
                    12, 18,
                    4, 5,
                    18, 28,
                    10, 16),

                MakeLevel(AIDifficulty.Master,
                    5, 8,
                    4, 5,
                    20, 30,
                    18, 25,
                    5, 7,
                    25, 40,
                    15, 24)
            };
        }

        /// <summary>Gói một bậc trình độ với 7 khoảng mục tiêu theo đúng thứ tự cố định.</summary>
        private static ExperienceLevel MakeLevel(AIDifficulty levelName,
            int playMatchesMin, int playMatchesMax,
            int winMatchesMin, int winMatchesMax,
            int scorePointsMin, int scorePointsMax,
            int playMinutesMin, int playMinutesMax,
            int playMinutesInGameMin, int playMinutesInGameMax,
            int rallySecondsMin, int rallySecondsMax,
            int hitVolleysMin, int hitVolleysMax)
        {
            return new ExperienceLevel(levelName, new List<ChallengeRange>
            {
                new ChallengeRange(ChallengeType.PlayMatches, playMatchesMin, playMatchesMax),
                new ChallengeRange(ChallengeType.WinMatches, winMatchesMin, winMatchesMax),
                new ChallengeRange(ChallengeType.ScorePoints, scorePointsMin, scorePointsMax),
                new ChallengeRange(ChallengeType.PlayMinutes, playMinutesMin, playMinutesMax),
                new ChallengeRange(ChallengeType.PlayMinutesInGame, playMinutesInGameMin, playMinutesInGameMax),
                new ChallengeRange(ChallengeType.RallySeconds, rallySecondsMin, rallySecondsMax),
                new ChallengeRange(ChallengeType.HitVolleys, hitVolleysMin, hitVolleysMax)
            });
        }

        // --------------------------------------------------------------------- Helpers

        /// <summary>Tìm asset GameData ở đường dẫn chuẩn, nếu không có thì quét toàn project.</summary>
        private static GameData FindGameData()
        {
            GameData gameData = AssetDatabase.LoadAssetAtPath<GameData>(GameDataAssetPath);
            if (gameData != null) return gameData;

            string[] guids = AssetDatabase.FindAssets("t:GameData");
            if (guids.Length == 0) return null;

            return AssetDatabase.LoadAssetAtPath<GameData>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        /// <summary>Nạp asset tại đường dẫn, tạo mới nếu chưa có.</summary>
        private static T LoadOrCreate<T>(string assetPath) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null) return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }

        /// <summary>Tạo thư mục asset nếu chưa tồn tại (hỗ trợ tạo lồng nhiều cấp).</summary>
        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;

            int lastSlash = folderPath.LastIndexOf('/');
            if (lastSlash <= 0) return;

            string parent = folderPath.Substring(0, lastSlash);
            string leaf = folderPath.Substring(lastSlash + 1);

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
