using System.Collections.Generic;
using Pickleball.Data;
using UnityEditor;
using UnityEngine;

namespace Pickleball.EditorTools
{
    /// <summary>
    /// Sinh toàn bộ asset vật phẩm shop (Character/Grip/Paddle/Workout) cùng các asset hạ tầng
    /// đi kèm: PropertiesData, TazoData, PlayerLoadout và GameData.
    /// <para>
    /// Mọi con số lấy nguyên văn từ dữ liệu giải mã của bản gốc
    /// (<c>reverse-engineering/data/decoded_scriptableobjects_1.json</c>), không tự chế.
    /// </para>
    /// <para>
    /// Chạy lại nhiều lần an toàn: asset đã tồn tại sẽ được ghi đè giá trị thay vì tạo trùng.
    /// Lưu ý mỗi lần chạy sẽ đưa toàn bộ vật phẩm về bậc 0 và ví tiền về mốc test.
    /// </para>
    /// </summary>
    public static class ItemDataGenerator
    {
        private const string RootFolder = "Assets/Project/ScriptableObjects";
        private const string ShopFolder = RootFolder + "/Shop";
        private const string GameFolder = RootFolder + "/Game";
        private const string ProfilesFolder = RootFolder + "/Profiles";

        /// <summary>Bậc nâng cấp cao nhất của mọi vật phẩm trong bản gốc.</summary>
        private const int MaxLevel = 4;

        /// <summary>Trọng số đóng góp chỉ số theo loại vật phẩm (tổng = 1.0).</summary>
        private const float CharacterEffectiveness = 0.5f;
        private const float GripEffectiveness = 0.1f;
        private const float PaddleEffectiveness = 0.2f;
        private const float WorkoutEffectiveness = 0.2f;

        /// <summary>Tạo/cập nhật toàn bộ asset vật phẩm và dữ liệu shop rồi lưu xuống đĩa.</summary>
        [MenuItem("Pickleball/Generate Item Data")]
        public static void GenerateAll()
        {
            EnsureFolder(RootFolder);
            EnsureFolder(ShopFolder);
            EnsureFolder(GameFolder);

            List<Item> characters = GenerateCharacters();
            List<Item> grips = GenerateGrips();
            List<Item> paddles = GeneratePaddles();
            List<Item> workouts = GenerateWorkouts();

            PropertiesData propertiesData = GeneratePropertiesData();
            TazoData tazoData = GenerateTazoData();
            PlayerLoadout loadout = GeneratePlayerLoadout(characters, grips, paddles, workouts);
            GameData gameData = GenerateGameData(propertiesData, tazoData, loadout);

            loadout.gameData = gameData;
            EditorUtility.SetDirty(loadout);

            // Tính sẵn chỉ số cho PlayerProfile của người chơi để vào trận là chơi được ngay.
            loadout.UpdateProfile();
            if (loadout.profile != null) EditorUtility.SetDirty(loadout.profile);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ItemDataGenerator] Đã sinh {characters.Count} character, {grips.Count} grip, " +
                      $"{paddles.Count} paddle, {workouts.Count} workout vào {ShopFolder}; " +
                      $"PlayerLoadout tại {ShopFolder}; PropertiesData/TazoData/GameData tại {GameFolder}.");
        }

        // ------------------------------------------------------------------- Characters

        private static List<Item> GenerateCharacters()
        {
            List<Item> items = new List<Item>();

            // MaleC1 — nhân vật mặc định, miễn phí.
            Character ryder = ConfigureItem<Character>(ShopFolder + "/Ryder.asset", ShopItemType.Character,
                "Ryder", 0, CharacterEffectiveness,
                Costs(100, 200, 300, 400, 500),
                Costs(50, 100, 150, 200, 250),
                Costs(500, 1000, 2000, 4000, 8000),
                Prop(PropertyType.Stamina, 10f, 28f, 50f, 75f, 100f),
                Prop(PropertyType.Agility, 15f, 32f, 52f, 75f, 100f),
                Prop(PropertyType.Swing, 12f, 30f, 50f, 75f, 100f),
                Prop(PropertyType.Spin, 10f, 28f, 50f, 75f, 100f),
                Prop(PropertyType.Accuracy, 12f, 30f, 50f, 75f, 100f),
                Prop(PropertyType.Speed, 20f, 38f, 60f, 85f, 100f),
                Prop(PropertyType.Power, 10f, 28f, 50f, 75f, 100f));
            ryder.volleyLevels = new[] { 2f, 2.5f, 3.5f, 4f, 4.5f };
            ryder.shotRange = 1.5f;
            ryder.height = 1.7f;
            ryder.staminaRechargeRate = 3f;
            ryder.staminaDepletionRate = 7f;
            EditorUtility.SetDirty(ryder);
            items.Add(ryder);

            // FemaleC1 — mở khoá bằng 7000 coin.
            Character zara = ConfigureItem<Character>(ShopFolder + "/Zara.asset", ShopItemType.Character,
                "Zara", 7000, CharacterEffectiveness,
                Costs(100, 200, 300, 400, 500),
                Costs(50, 100, 150, 200, 250),
                Costs(500, 1000, 2000, 4000, 8000),
                Prop(PropertyType.Stamina, 15f, 35f, 60f, 85f, 100f),
                Prop(PropertyType.Agility, 18f, 35f, 60f, 85f, 100f),
                Prop(PropertyType.Swing, 15f, 35f, 60f, 85f, 100f),
                Prop(PropertyType.Spin, 15f, 35f, 60f, 85f, 100f),
                Prop(PropertyType.Accuracy, 25f, 45f, 60f, 85f, 100f),
                Prop(PropertyType.Speed, 25f, 45f, 70f, 90f, 100f),
                Prop(PropertyType.Power, 15f, 35f, 60f, 85f, 100f));
            zara.volleyLevels = new[] { 3f, 4f, 4.5f, 5f, 6f };
            zara.shotRange = 1.7f;
            zara.height = 1.8f;
            zara.staminaRechargeRate = 4f;
            zara.staminaDepletionRate = 8f;
            EditorUtility.SetDirty(zara);
            items.Add(zara);

            return items;
        }

        // ------------------------------------------------------------------------ Grips

        private static List<Item> GenerateGrips()
        {
            List<Item> items = new List<Item>
            {
                ConfigureItem<Grip>(ShopFolder + "/ComfortGrip.asset", ShopItemType.Grip,
                    "Comfort Grip", 0, GripEffectiveness,
                    Costs(10, 20, 30, 40, 50),
                    Costs(5, 10, 15, 20, 25),
                    Costs(20, 100, 200, 400, 800),
                    Prop(PropertyType.Spin, 5f, 10f, 18f, 28f, 35f),
                    Prop(PropertyType.Swing, 7f, 12f, 20f, 30f, 38f),
                    Prop(PropertyType.Agility, 5f, 10f, 18f, 28f, 35f),
                    Prop(PropertyType.Accuracy, 10f, 15f, 25f, 40f, 50f),
                    Prop(PropertyType.Power, 8f, 18f, 30f, 45f, 60f)),

                ConfigureItem<Grip>(ShopFolder + "/PrecisionGrip.asset", ShopItemType.Grip,
                    "Precision Grip", 1000, GripEffectiveness,
                    Costs(15, 30, 45, 60, 75),
                    Costs(7, 14, 21, 28, 35),
                    Costs(75, 150, 300, 600, 1200),
                    Prop(PropertyType.Spin, 10f, 20f, 30f, 40f, 50f),
                    Prop(PropertyType.Swing, 12f, 22f, 32f, 42f, 55f),
                    Prop(PropertyType.Agility, 10f, 20f, 30f, 40f, 50f),
                    Prop(PropertyType.Accuracy, 15f, 25f, 35f, 50f, 65f),
                    Prop(PropertyType.Power, 12f, 25f, 40f, 55f, 70f)),

                ConfigureItem<Grip>(ShopFolder + "/PerformanceGrip.asset", ShopItemType.Grip,
                    "Performance Grip", 1200, GripEffectiveness,
                    Costs(20, 40, 60, 80, 100),
                    Costs(10, 20, 30, 40, 50),
                    Costs(100, 200, 400, 800, 1600),
                    Prop(PropertyType.Spin, 15f, 25f, 35f, 45f, 55f),
                    Prop(PropertyType.Swing, 17f, 27f, 37f, 47f, 60f),
                    Prop(PropertyType.Agility, 15f, 25f, 35f, 45f, 55f),
                    Prop(PropertyType.Accuracy, 20f, 30f, 40f, 55f, 70f),
                    Prop(PropertyType.Power, 18f, 35f, 50f, 65f, 80f)),

                // Lưu ý: coinsRequired của Control Grip trong bản gốc là 125/520/500/... (số 520
                // nhiều khả năng là lỗi gõ của nhà phát triển) — giữ nguyên để bám dữ liệu thật.
                ConfigureItem<Grip>(ShopFolder + "/ControlGrip.asset", ShopItemType.Grip,
                    "Control Grip", 1500, GripEffectiveness,
                    Costs(25, 50, 75, 100, 125),
                    Costs(12, 24, 36, 48, 60),
                    Costs(125, 520, 500, 1000, 2000),
                    Prop(PropertyType.Spin, 20f, 30f, 40f, 50f, 60f),
                    Prop(PropertyType.Swing, 22f, 32f, 42f, 52f, 65f),
                    Prop(PropertyType.Agility, 20f, 30f, 40f, 50f, 60f),
                    Prop(PropertyType.Accuracy, 25f, 35f, 45f, 60f, 75f),
                    Prop(PropertyType.Power, 20f, 40f, 60f, 80f, 100f)),

                ConfigureItem<Grip>(ShopFolder + "/UltimateGrip.asset", ShopItemType.Grip,
                    "Ultimate Grip", 2000, GripEffectiveness,
                    Costs(30, 60, 90, 120, 150),
                    Costs(15, 30, 45, 60, 75),
                    Costs(150, 300, 600, 1200, 2400),
                    Prop(PropertyType.Spin, 25f, 35f, 45f, 55f, 70f),
                    Prop(PropertyType.Swing, 30f, 40f, 50f, 60f, 75f),
                    Prop(PropertyType.Agility, 25f, 35f, 45f, 55f, 70f),
                    Prop(PropertyType.Accuracy, 30f, 40f, 50f, 65f, 90f),
                    Prop(PropertyType.Power, 25f, 45f, 65f, 85f, 100f))
            };

            return items;
        }

        // ---------------------------------------------------------------------- Paddles

        private static List<Item> GeneratePaddles()
        {
            List<Item> items = new List<Item>
            {
                ConfigureItem<Paddle>(ShopFolder + "/SpeedPaddle.asset", ShopItemType.Paddle,
                    "Speed Paddle", 0, PaddleEffectiveness,
                    Costs(10, 20, 30, 40, 50),
                    Costs(5, 10, 15, 20, 25),
                    Costs(50, 100, 200, 400, 800),
                    Prop(PropertyType.Spin, 10f, 20f, 30f, 40f, 50f),
                    Prop(PropertyType.Swing, 8f, 18f, 28f, 38f, 50f),
                    Prop(PropertyType.Agility, 5f, 15f, 25f, 35f, 45f),
                    Prop(PropertyType.Accuracy, 7f, 17f, 27f, 37f, 47f),
                    Prop(PropertyType.Power, 12f, 25f, 40f, 55f, 70f)),

                ConfigureItem<Paddle>(ShopFolder + "/ForcePaddle.asset", ShopItemType.Paddle,
                    "Force Paddle", 1000, PaddleEffectiveness,
                    Costs(15, 30, 45, 60, 75),
                    Costs(7, 14, 21, 28, 35),
                    Costs(75, 150, 300, 600, 1200),
                    Prop(PropertyType.Spin, 15f, 25f, 35f, 45f, 55f),
                    Prop(PropertyType.Swing, 12f, 22f, 32f, 42f, 55f),
                    Prop(PropertyType.Agility, 10f, 20f, 30f, 40f, 50f),
                    Prop(PropertyType.Accuracy, 12f, 22f, 32f, 45f, 55f),
                    Prop(PropertyType.Power, 18f, 30f, 45f, 60f, 80f)),

                ConfigureItem<Paddle>(ShopFolder + "/ChampionPaddle.asset", ShopItemType.Paddle,
                    "Champion Paddle", 1500, PaddleEffectiveness,
                    Costs(20, 40, 60, 80, 100),
                    Costs(10, 20, 30, 40, 50),
                    Costs(100, 200, 400, 800, 1600),
                    Prop(PropertyType.Spin, 20f, 30f, 40f, 50f, 60f),
                    Prop(PropertyType.Swing, 18f, 28f, 38f, 48f, 60f),
                    Prop(PropertyType.Agility, 15f, 25f, 35f, 45f, 55f),
                    Prop(PropertyType.Accuracy, 17f, 27f, 37f, 47f, 60f),
                    Prop(PropertyType.Power, 20f, 35f, 50f, 65f, 80f)),

                ConfigureItem<Paddle>(ShopFolder + "/VictoryPaddle.asset", ShopItemType.Paddle,
                    "Victory Paddle", 2000, PaddleEffectiveness,
                    Costs(25, 50, 75, 100, 125),
                    Costs(12, 24, 36, 48, 60),
                    Costs(125, 250, 500, 1000, 2000),
                    Prop(PropertyType.Spin, 25f, 35f, 45f, 55f, 65f),
                    Prop(PropertyType.Swing, 22f, 32f, 45f, 52f, 65f),
                    Prop(PropertyType.Agility, 20f, 30f, 40f, 50f, 60f),
                    Prop(PropertyType.Accuracy, 22f, 32f, 42f, 52f, 65f),
                    Prop(PropertyType.Power, 25f, 40f, 55f, 70f, 85f)),

                ConfigureItem<Paddle>(ShopFolder + "/EdgePaddle.asset", ShopItemType.Paddle,
                    "Edge Paddle", 3000, PaddleEffectiveness,
                    Costs(30, 60, 90, 120, 150),
                    Costs(15, 30, 45, 60, 75),
                    Costs(150, 300, 600, 1200, 2400),
                    Prop(PropertyType.Spin, 30f, 40f, 50f, 60f, 70f),
                    Prop(PropertyType.Swing, 28f, 38f, 48f, 58f, 70f),
                    Prop(PropertyType.Agility, 25f, 35f, 45f, 55f, 65f),
                    Prop(PropertyType.Accuracy, 27f, 37f, 47f, 57f, 70f),
                    Prop(PropertyType.Power, 30f, 50f, 70f, 90f, 100f))
            };

            return items;
        }

        // --------------------------------------------------------------------- Workouts

        private static List<Item> GenerateWorkouts()
        {
            // Bài tập chuyên sâu tăng mạnh 1-2 chỉ số (10/20/40/70/100), các chỉ số còn lại 2/4/6/8/10.
            float[] focus = { 10f, 20f, 40f, 70f, 100f };
            float[] minor = { 2f, 4f, 6f, 8f, 10f };

            int[] standardTazos = Costs(15, 30, 45, 60, 75);
            int[] standardGems = Costs(15, 30, 45, 60, 75);
            int[] standardCoins = Costs(150, 300, 600, 1200, 2400);

            List<Item> items = new List<Item>
            {
                ConfigureItem<Workout>(ShopFolder + "/TwistersEdge.asset", ShopItemType.Workout,
                    "Twister's Edge", 0, WorkoutEffectiveness,
                    standardTazos, standardGems, standardCoins,
                    Prop(PropertyType.Spin, focus),
                    Prop(PropertyType.Swing, focus),
                    Prop(PropertyType.Agility, minor),
                    Prop(PropertyType.Accuracy, minor),
                    Prop(PropertyType.Power, minor),
                    Prop(PropertyType.Stamina, minor),
                    Prop(PropertyType.Speed, minor)),

                ConfigureItem<Workout>(ShopFolder + "/EndurancePlus.asset", ShopItemType.Workout,
                    "Endurance Plus", 2500, WorkoutEffectiveness,
                    standardTazos, standardGems, standardCoins,
                    Prop(PropertyType.Spin, minor),
                    Prop(PropertyType.Swing, minor),
                    Prop(PropertyType.Agility, focus),
                    Prop(PropertyType.Accuracy, minor),
                    Prop(PropertyType.Power, minor),
                    Prop(PropertyType.Stamina, minor),
                    Prop(PropertyType.Speed, minor)),

                // Power Surge: chuyên Accuracy, riêng Power có nhánh phụ 3/6/9/12/15.
                ConfigureItem<Workout>(ShopFolder + "/PowerSurge.asset", ShopItemType.Workout,
                    "Power Surge", 2500, WorkoutEffectiveness,
                    standardTazos, standardGems, standardCoins,
                    Prop(PropertyType.Spin, minor),
                    Prop(PropertyType.Swing, minor),
                    Prop(PropertyType.Agility, minor),
                    Prop(PropertyType.Accuracy, focus),
                    Prop(PropertyType.Power, 3f, 6f, 9f, 12f, 15f),
                    Prop(PropertyType.Stamina, minor),
                    Prop(PropertyType.Speed, minor)),

                ConfigureItem<Workout>(ShopFolder + "/SpeedAgilityMaster.asset", ShopItemType.Workout,
                    "Speed Agility Master", 3000, WorkoutEffectiveness,
                    standardTazos, standardGems, standardCoins,
                    Prop(PropertyType.Spin, minor),
                    Prop(PropertyType.Swing, minor),
                    Prop(PropertyType.Agility, minor),
                    Prop(PropertyType.Accuracy, minor),
                    Prop(PropertyType.Power, minor),
                    Prop(PropertyType.Stamina, focus),
                    Prop(PropertyType.Speed, focus)),

                ConfigureItem<Workout>(ShopFolder + "/PrecisionBoost.asset", ShopItemType.Workout,
                    "Precision Boost", 2500, WorkoutEffectiveness,
                    standardTazos, standardGems, standardCoins,
                    Prop(PropertyType.Spin, minor),
                    Prop(PropertyType.Swing, minor),
                    Prop(PropertyType.Agility, minor),
                    Prop(PropertyType.Accuracy, minor),
                    Prop(PropertyType.Power, focus),
                    Prop(PropertyType.Stamina, minor),
                    Prop(PropertyType.Speed, minor)),

                ConfigureItem<Workout>(ShopFolder + "/AllRounderTraining.asset", ShopItemType.Workout,
                    "All-Rounder Training", 3500, WorkoutEffectiveness,
                    Costs(10, 20, 30, 40, 50),
                    Costs(10, 20, 30, 40, 50),
                    Costs(100, 200, 400, 800, 1600),
                    Prop(PropertyType.Spin, 5f, 10f, 15f, 20f, 25f),
                    Prop(PropertyType.Swing, 5f, 10f, 15f, 20f, 25f),
                    Prop(PropertyType.Agility, 5f, 10f, 15f, 20f, 25f),
                    Prop(PropertyType.Accuracy, 5f, 10f, 15f, 20f, 25f),
                    Prop(PropertyType.Power, 5f, 10f, 15f, 20f, 25f),
                    Prop(PropertyType.Stamina, 5f, 10f, 15f, 20f, 25f),
                    Prop(PropertyType.Speed, 5f, 10f, 15f, 20f, 25f)),

                ConfigureItem<Workout>(ShopFolder + "/TotalFitness.asset", ShopItemType.Workout,
                    "Total Fitness", 3500, WorkoutEffectiveness,
                    Costs(10, 20, 30, 40, 50),
                    Costs(10, 20, 30, 40, 50),
                    Costs(100, 200, 400, 800, 1600),
                    Prop(PropertyType.Spin, 6f, 12f, 18f, 24f, 30f),
                    Prop(PropertyType.Swing, 6f, 12f, 18f, 24f, 30f),
                    Prop(PropertyType.Agility, 6f, 12f, 18f, 24f, 30f),
                    Prop(PropertyType.Accuracy, 6f, 12f, 18f, 24f, 30f),
                    Prop(PropertyType.Power, 6f, 12f, 18f, 24f, 30f),
                    Prop(PropertyType.Stamina, 6f, 12f, 18f, 24f, 30f),
                    Prop(PropertyType.Speed, 6f, 12f, 18f, 24f, 30f))
            };

            return items;
        }

        // --------------------------------------------------------------- PropertiesData

        private static PropertiesData GeneratePropertiesData()
        {
            PropertiesData data = LoadOrCreate<PropertiesData>(GameFolder + "/PropertiesData.asset");

            data.properties = new List<PropertyData>
            {
                MakeProperty(PropertyType.Agility, "AGILITY"),
                MakeProperty(PropertyType.Accuracy, "ACCURACY"),
                MakeProperty(PropertyType.Power, "POWER"),
                MakeProperty(PropertyType.Swing, "SWING"),
                MakeProperty(PropertyType.Spin, "SPIN"),
                MakeProperty(PropertyType.Speed, "SPEED"),
                MakeProperty(PropertyType.Stamina, "STAMINA")
            };

            EditorUtility.SetDirty(data);
            return data;
        }

        private static PropertyData MakeProperty(PropertyType propertyType, string propertyName)
        {
            return new PropertyData { propertyType = propertyType, propertyName = propertyName };
        }

        // --------------------------------------------------------------------- TazoData

        private static TazoData GenerateTazoData()
        {
            TazoData data = LoadOrCreate<TazoData>(GameFolder + "/TazoData.asset");

            data.tazos = new List<TazoCountData>
            {
                new TazoCountData(ShopItemType.Character, 0),
                new TazoCountData(ShopItemType.Grip, 0),
                new TazoCountData(ShopItemType.Paddle, 0),
                new TazoCountData(ShopItemType.Workout, 0)
            };

            EditorUtility.SetDirty(data);
            return data;
        }

        // ----------------------------------------------------------------- PlayerLoadout

        private static PlayerLoadout GeneratePlayerLoadout(List<Item> characters, List<Item> grips,
            List<Item> paddles, List<Item> workouts)
        {
            PlayerLoadout loadout = LoadOrCreate<PlayerLoadout>(ShopFolder + "/PlayerLoadout.asset");

            loadout.categories = new List<ShopCategoryData>
            {
                MakeCategory(ShopItemType.Character, "Characters", characters),
                MakeCategory(ShopItemType.Grip, "Grips", grips),
                MakeCategory(ShopItemType.Paddle, "Paddles", paddles),
                MakeCategory(ShopItemType.Workout, "Workouts", workouts)
            };

            loadout.defaultCharacter = characters.Count > 0 ? characters[0] as Character : null;
            loadout.profileLimits = AssetDatabase.LoadAssetAtPath<PlayerProfileLimits>(
                ProfilesFolder + "/PlayerProfileLimits.asset");
            loadout.profile = AssetDatabase.LoadAssetAtPath<PlayerProfile>(
                ProfilesFolder + "/Player_Default.asset");

            EditorUtility.SetDirty(loadout);
            return loadout;
        }

        private static ShopCategoryData MakeCategory(ShopItemType categoryType, string categoryName, List<Item> items)
        {
            return new ShopCategoryData
            {
                categoryType = categoryType,
                categoryName = categoryName,
                items = new List<Item>(items),
                selectedItem = items.Count > 0 ? items[0] : null
            };
        }

        // --------------------------------------------------------------------- GameData

        private static GameData GenerateGameData(PropertiesData propertiesData, TazoData tazoData,
            PlayerLoadout loadout)
        {
            GameData gameData = LoadOrCreate<GameData>(GameFolder + "/GameData.asset");

            gameData.playerProfileData = new PlayerProfileData
            {
                playerName = "Player",
                totalMatches = 0,
                totalWins = 0,
                winRate = 0f,
                avatarIndex = 0,
                trophies = 0,
                consecutiveWins = 0,
                level = 1,
                hasUsedFreeRename = false
            };

            // Ví tiền khởi điểm để test vòng lặp kinh tế.
            gameData.totalCoins = 1000;
            gameData.totalGems = 100;

            gameData.propertiesData = propertiesData;
            gameData.tazoData = tazoData;
            gameData.playerLoadout = loadout;

            gameData.playerLevels = AssetDatabase.LoadAssetAtPath<PlayerLevels>(GameFolder + "/PlayerLevels.asset");
            gameData.stadiumsData = AssetDatabase.LoadAssetAtPath<StadiumsData>(GameFolder + "/StadiumsData.asset");
            gameData.boostersData = AssetDatabase.LoadAssetAtPath<BoostersData>(GameFolder + "/BoostersData.asset");
            gameData.namesData = FindFirstAsset<AINamesData>();

            if (gameData.difficultySettings == null) gameData.difficultySettings = new AIDifficultySettings();
            gameData.AIProfileConfigs = MakeAIProfileConfigs();

            gameData.aiDifficultyMode = AIDifficultyMode.Auto;
            gameData.gameMode = GameMode.Career;
            gameData.handSide = HandSide.Right;

            EditorUtility.SetDirty(gameData);
            return gameData;
        }

        /// <summary>Gom các asset PlayerProfile của AI theo bậc độ khó tương ứng với tên file.</summary>
        private static PlayerProfileConfigs MakeAIProfileConfigs()
        {
            return new PlayerProfileConfigs
            {
                aiProfileDataList = new List<AIProfileData>
                {
                    MakeAIProfiles(AIDifficulty.Tutorial, "AI_0_Tutorial"),
                    MakeAIProfiles(AIDifficulty.Newbie, "AI_1_Newbie"),
                    MakeAIProfiles(AIDifficulty.Amateur, "AI_2_Amateur"),
                    MakeAIProfiles(AIDifficulty.Competitor, "AI_3_Competitor", "AI_4_PowerHitter"),
                    MakeAIProfiles(AIDifficulty.Pro, "AI_5_Pro", "AI_6_VeryHard"),
                    MakeAIProfiles(AIDifficulty.Master, "AI_7_Master")
                }
            };
        }

        private static AIProfileData MakeAIProfiles(AIDifficulty difficulty, params string[] assetNames)
        {
            List<PlayerProfile> profiles = new List<PlayerProfile>();
            for (int i = 0; i < assetNames.Length; i++)
            {
                PlayerProfile profile = AssetDatabase.LoadAssetAtPath<PlayerProfile>(
                    ProfilesFolder + "/" + assetNames[i] + ".asset");
                if (profile != null) profiles.Add(profile);
            }

            return new AIProfileData { difficulty = difficulty, profiles = profiles };
        }

        // ---------------------------------------------------------------------- Helpers

        /// <summary>
        /// Nạp hoặc tạo asset vật phẩm rồi ghi đè toàn bộ số liệu cân bằng.
        /// Vật phẩm luôn về bậc 0; chỉ vật phẩm giá 0 mới được coi là đã sở hữu sẵn.
        /// Các tham chiếu hình ảnh/prefab gán tay trong Inspector không bị đụng tới.
        /// </summary>
        private static T ConfigureItem<T>(string assetPath, ShopItemType itemType, string itemName,
            int purchaseCoins, float effectiveness, int[] tazosRequired, int[] gemsRequired, int[] coinsRequired,
            params PropertyLevelValues[] properties) where T : Item
        {
            T item = LoadOrCreate<T>(assetPath);

            item.itemType = itemType;
            item.itemName = itemName;
            item.currentLevel = 0;
            item.maxLevel = MaxLevel;
            item.isPurchased = purchaseCoins == 0;
            item.isActive = true;
            item.purchaseCoins = purchaseCoins;
            item.propertiesEffectiveness = effectiveness;
            item.tazosRequired = (int[])tazosRequired.Clone();
            item.gemsRequired = (int[])gemsRequired.Clone();
            item.coinsRequired = (int[])coinsRequired.Clone();
            item.propertyValues = new List<PropertyLevelValues>(properties);

            EditorUtility.SetDirty(item);
            return item;
        }

        /// <summary>Gói một bảng chỉ số theo 5 bậc.</summary>
        private static PropertyLevelValues Prop(PropertyType propertyType, params float[] values)
        {
            return new PropertyLevelValues { propertyType = propertyType, value = (float[])values.Clone() };
        }

        /// <summary>Gói một mảng chi phí 5 bậc (chỉ để đọc cho dễ ở nơi gọi).</summary>
        private static int[] Costs(int l0, int l1, int l2, int l3, int l4)
        {
            return new[] { l0, l1, l2, l3, l4 };
        }

        /// <summary>Tìm asset đầu tiên thuộc kiểu yêu cầu trong toàn project.</summary>
        private static T FindFirstAsset<T>() where T : ScriptableObject
        {
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            if (guids == null || guids.Length == 0) return null;

            return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
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
