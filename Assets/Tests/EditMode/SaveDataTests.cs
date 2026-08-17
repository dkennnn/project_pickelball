using System.Collections.Generic;
using NUnit.Framework;
using Pickleball;
using Pickleball.Data;

namespace Pickleball.Tests
{
    /// <summary>
    /// Kiểm chứng lớp lưu tiến trình cục bộ: <see cref="DataEncoder"/> và <see cref="SaveGameData"/>.
    ///
    /// <para>Hai điểm dễ chết nhất được nhắm tới ở đây:</para>
    /// <list type="bullet">
    /// <item><description>Save hỏng phải rơi về mặc định chứ tuyệt đối không được ném ngoại lệ —
    /// nếu <c>TryDecrypt</c> để lọt exception thì một file rác sẽ làm game không khởi động được.</description></item>
    /// <item><description>Round-trip phải giữ nguyên dữ liệu lồng nhau (list, enum, nested class),
    /// vì đây chính là chỗ tiến trình người chơi bị mất khi serialize sai.</description></item>
    /// </list>
    /// </summary>
    public class SaveDataTests
    {
        private const string Password = "pickleball-test";

        [SetUp]
        public void SetUp()
        {
            SaveGameData.Delete();
        }

        [TearDown]
        public void TearDown()
        {
            SaveGameData.Delete();
        }

        // ------------------------------------------------------------------
        // DataEncoder
        // ------------------------------------------------------------------

        [Test]
        public void Encrypt_RoiDecrypt_TraVeDungChuoiGoc()
        {
            const string original = "Hello Pickleball 12345";

            string cipher = DataEncoder.Encrypt(original, Password);

            Assert.AreNotEqual(original, cipher, "Bản mã không được trùng bản rõ.");
            Assert.AreEqual(original, DataEncoder.Decrypt(cipher, Password));
        }

        [Test]
        public void Encrypt_GiuNguyenTiengVietCoDauVaKyTuDacBiet()
        {
            const string original =
                "Người chơi số một — nâng cấp vợt {\"level\":4} & \\ \" ' \n\t ✔ ★ 東京";

            string cipher = DataEncoder.Encrypt(original, Password);

            Assert.AreEqual(original, DataEncoder.Decrypt(cipher, Password),
                "Chuỗi UTF-8 nhiều byte phải quay về nguyên vẹn.");
        }

        [Test]
        public void TryDecrypt_ChuoiRac_TraFalseVaKhongNemException()
        {
            string plain = null;

            Assert.DoesNotThrow(() => DataEncoder.TryDecrypt("khong-phai-base64-!!!", Password, out plain),
                "Dữ liệu rác phải được nuốt, không được ném exception.");
            Assert.IsFalse(DataEncoder.TryDecrypt("khong-phai-base64-!!!", Password, out plain));
            Assert.IsEmpty(plain);

            // Base64 hợp lệ nhưng không phải bản mã AES.
            Assert.IsFalse(DataEncoder.TryDecrypt("aGVsbG8gd29ybGQ=", Password, out plain));
            Assert.IsFalse(DataEncoder.TryDecrypt(string.Empty, Password, out plain));
            Assert.IsFalse(DataEncoder.TryDecrypt(null, Password, out plain));
        }

        [Test]
        public void TryDecrypt_SaiPassword_TraFalse()
        {
            string cipher = DataEncoder.Encrypt("tien trinh nguoi choi", Password);

            string plain = null;
            Assert.DoesNotThrow(() => DataEncoder.TryDecrypt(cipher, "sai-mat-khau", out plain));
            Assert.IsFalse(DataEncoder.TryDecrypt(cipher, "sai-mat-khau", out plain),
                "Sai mật khẩu phải trả false chứ không ném exception.");
        }

        // ------------------------------------------------------------------
        // SaveGameData
        // ------------------------------------------------------------------

        [Test]
        public void Load_KhiChuaCoFile_TraVeDefaultData()
        {
            Assert.IsFalse(SaveGameData.Exists, "SetUp phải xoá sạch file lưu trước khi test.");

            SaveData fallback = new SaveData { coins = 777 };
            SaveData loaded = SaveGameData.Load(fallback, Password);

            Assert.AreSame(fallback, loaded, "Không có file thì phải trả về đúng đối tượng mặc định.");
        }

        [Test]
        public void SaveRoiLoad_GiuNguyenMoiTruong()
        {
            SaveData original = BuildSampleSaveData();

            SaveGameData.Save(original, Password);
            Assert.IsTrue(SaveGameData.Exists, "Save xong thì file phải tồn tại.");

            SaveData loaded = SaveGameData.Load<SaveData>(null, Password);
            Assert.IsNotNull(loaded, "Load phải dựng lại được đối tượng.");
            AssertSaveDataEqual(original, loaded);
        }

        [Test]
        public void Load_KhiFileHong_TraVeDefaultDataVaKhongNemException()
        {
            SaveGameData.Save(BuildSampleSaveData(), Password);

            System.IO.File.WriteAllText(SaveGameData.filePath, "day-la-rac-khong-giai-ma-duoc");

            SaveData fallback = new SaveData { coins = 42 };
            SaveData loaded = null;

            Assert.DoesNotThrow(() => loaded = SaveGameData.Load(fallback, Password));
            Assert.AreSame(fallback, loaded, "File hỏng phải rơi về mặc định.");
        }

        [Test]
        public void Load_KhiSaiPassword_TraVeDefaultData()
        {
            SaveGameData.Save(BuildSampleSaveData(), Password);

            SaveData fallback = new SaveData { coins = 5 };
            SaveData loaded = SaveGameData.Load(fallback, "mat-khau-khac");

            Assert.AreSame(fallback, loaded);
        }

        [Test]
        public void Delete_XoaHetFileLuu()
        {
            SaveGameData.Save(BuildSampleSaveData(), Password);
            Assert.IsTrue(SaveGameData.Exists);

            SaveGameData.Delete();

            Assert.IsFalse(SaveGameData.Exists, "Delete phải xoá được file lưu.");
        }

        // ------------------------------------------------------------------
        // Dữ liệu mẫu
        // ------------------------------------------------------------------

        /// <summary>Dựng một <see cref="SaveData"/> có đủ list, enum và lớp lồng nhau.</summary>
        private static SaveData BuildSampleSaveData()
        {
            SaveData data = new SaveData
            {
                version = 1,
                isTutorialCompleted = true,
                completedTutorialSteps = new List<TutorialType>
                {
                    TutorialType.WelcomeMessage,
                    TutorialType.FirstMatch,
                    TutorialType.TutorialCompleted
                },
                coins = 12345,
                gems = 678,
                ballPurchasedStatuses = new List<bool> { true, false, true },
                racketPurchasedStatuses = new List<bool> { true, true, false, false },
                equipedBallIndex = 2,
                equipedRacketIndex = 1,
                savedProfileData = new SavedPlayerData("Người Chơi", 40, 25, 0.625f, 3, 1450, 4, 7, true),
                energyDrinks = 9,
                shopCategories = new List<ShopCategorySaveData>
                {
                    new ShopCategorySaveData
                    {
                        categoryType = ShopItemType.Character,
                        selectedItemName = "Nhân vật B",
                        items = new List<ItemSaveData>
                        {
                            new ItemSaveData("Nhân vật A", 4, true),
                            new ItemSaveData("Nhân vật B", 2, true),
                            new ItemSaveData("Nhân vật C", 0, false)
                        }
                    },
                    new ShopCategorySaveData
                    {
                        categoryType = ShopItemType.Grip,
                        selectedItemName = "Grip 1",
                        items = new List<ItemSaveData> { new ItemSaveData("Grip 1", 3, true) }
                    }
                },
                tazoCounts = new List<TazoCountData>
                {
                    new TazoCountData(ShopItemType.Grip, 30),
                    new TazoCountData(ShopItemType.Paddle, 12)
                },
                boosters = new List<BoosterCountData>
                {
                    new BoosterCountData { boosterType = BoosterType.Stamina, count = 3 },
                    new BoosterCountData { boosterType = BoosterType.Spin, count = 1 }
                },
                dailyChallenges = new List<DailyChallenge>
                {
                    new DailyChallenge("WinMatches_20260817_0", ChallengeType.WinMatches, 5f,
                        "Win 5 matches", new CollectibleReward(RewardType.Coins, 250))
                    {
                        Progress = 2f
                    },
                    new DailyChallenge("ScorePoints_20260817_1", ChallengeType.ScorePoints, 20f,
                        "Score 20 points", new CollectibleReward(RewardType.Gems, 15))
                    {
                        Progress = 20f,
                        IsCompleted = true,
                        RewardCollected = true
                    }
                },
                playerLevelsCollectStatuses = new List<bool> { true, true, false, false, true },
                kitbagsData = new List<KitbagSaveData>
                {
                    new KitbagSaveData(KitbagType.Level1, 3, 1, true, false, true),
                    new KitbagSaveData(KitbagType.Level3, 5, 5, false, true, false)
                },
                slotDataArray = new List<SlotSaveData>
                {
                    new SlotSaveData(KitbagType.Level2, false, "2026-08-17T10:20:30.0000000Z"),
                    new SlotSaveData(KitbagType.None, true, string.Empty)
                },
                lastDailyRewardTime = "2026-08-16T00:00:00.0000000Z",
                lastDailyChallengeResetTime = "2026-08-17T00:00:00.0000000Z",
                tournamentProgress = new PlayerTournamentProgress
                {
                    tournamentId = "tournament_001",
                    currentStage = 2,
                    isEliminated = false,
                    isCompleted = false
                },
                handSide = HandSide.Left
            };

            return data;
        }

        /// <summary>So khớp từng trường của hai <see cref="SaveData"/>, kể cả phần lồng nhau.</summary>
        private static void AssertSaveDataEqual(SaveData expected, SaveData actual)
        {
            Assert.AreEqual(expected.version, actual.version, "version");
            Assert.AreEqual(expected.isTutorialCompleted, actual.isTutorialCompleted, "isTutorialCompleted");
            CollectionAssert.AreEqual(expected.completedTutorialSteps, actual.completedTutorialSteps,
                "completedTutorialSteps");

            Assert.AreEqual(expected.coins, actual.coins, "coins");
            Assert.AreEqual(expected.gems, actual.gems, "gems");
            CollectionAssert.AreEqual(expected.ballPurchasedStatuses, actual.ballPurchasedStatuses,
                "ballPurchasedStatuses");
            CollectionAssert.AreEqual(expected.racketPurchasedStatuses, actual.racketPurchasedStatuses,
                "racketPurchasedStatuses");
            Assert.AreEqual(expected.equipedBallIndex, actual.equipedBallIndex, "equipedBallIndex");
            Assert.AreEqual(expected.equipedRacketIndex, actual.equipedRacketIndex, "equipedRacketIndex");

            Assert.IsNotNull(actual.savedProfileData, "savedProfileData");
            Assert.AreEqual(expected.savedProfileData.playerName, actual.savedProfileData.playerName, "playerName");
            Assert.AreEqual(expected.savedProfileData.totalMatches, actual.savedProfileData.totalMatches, "totalMatches");
            Assert.AreEqual(expected.savedProfileData.totalWins, actual.savedProfileData.totalWins, "totalWins");
            Assert.AreEqual(expected.savedProfileData.winRate, actual.savedProfileData.winRate, 0.0001f, "winRate");
            Assert.AreEqual(expected.savedProfileData.avatarIndex, actual.savedProfileData.avatarIndex, "avatarIndex");
            Assert.AreEqual(expected.savedProfileData.trophies, actual.savedProfileData.trophies, "trophies");
            Assert.AreEqual(expected.savedProfileData.consecutiveWins, actual.savedProfileData.consecutiveWins,
                "consecutiveWins");
            Assert.AreEqual(expected.savedProfileData.level, actual.savedProfileData.level, "level");
            Assert.AreEqual(expected.savedProfileData.hasUsedFreeRename, actual.savedProfileData.hasUsedFreeRename,
                "hasUsedFreeRename");

            Assert.AreEqual(expected.energyDrinks, actual.energyDrinks, "energyDrinks");

            Assert.AreEqual(expected.shopCategories.Count, actual.shopCategories.Count, "shopCategories.Count");
            for (int i = 0; i < expected.shopCategories.Count; i++)
            {
                ShopCategorySaveData e = expected.shopCategories[i];
                ShopCategorySaveData a = actual.shopCategories[i];

                Assert.AreEqual(e.categoryType, a.categoryType, "categoryType");
                Assert.AreEqual(e.selectedItemName, a.selectedItemName, "selectedItemName");
                Assert.AreEqual(e.items.Count, a.items.Count, "items.Count");

                for (int j = 0; j < e.items.Count; j++)
                {
                    Assert.AreEqual(e.items[j].itemName, a.items[j].itemName, "itemName");
                    Assert.AreEqual(e.items[j].currentLevel, a.items[j].currentLevel, "currentLevel");
                    Assert.AreEqual(e.items[j].isPurchased, a.items[j].isPurchased, "isPurchased");
                }
            }

            Assert.AreEqual(expected.tazoCounts.Count, actual.tazoCounts.Count, "tazoCounts.Count");
            for (int i = 0; i < expected.tazoCounts.Count; i++)
            {
                Assert.AreEqual(expected.tazoCounts[i].ShopItemType, actual.tazoCounts[i].ShopItemType, "tazo type");
                Assert.AreEqual(expected.tazoCounts[i].count, actual.tazoCounts[i].count, "tazo count");
            }

            Assert.AreEqual(expected.boosters.Count, actual.boosters.Count, "boosters.Count");
            for (int i = 0; i < expected.boosters.Count; i++)
            {
                Assert.AreEqual(expected.boosters[i].boosterType, actual.boosters[i].boosterType, "booster type");
                Assert.AreEqual(expected.boosters[i].count, actual.boosters[i].count, "booster count");
            }

            Assert.AreEqual(expected.dailyChallenges.Count, actual.dailyChallenges.Count, "dailyChallenges.Count");
            for (int i = 0; i < expected.dailyChallenges.Count; i++)
            {
                DailyChallenge e = expected.dailyChallenges[i];
                DailyChallenge a = actual.dailyChallenges[i];

                Assert.AreEqual(e.Id, a.Id, "challenge Id");
                Assert.AreEqual(e.Description, a.Description, "challenge Description");
                Assert.AreEqual(e.Type, a.Type, "challenge Type");
                Assert.AreEqual(e.TargetValue, a.TargetValue, 0.0001f, "challenge TargetValue");
                Assert.AreEqual(e.Progress, a.Progress, 0.0001f, "challenge Progress");
                Assert.AreEqual(e.IsCompleted, a.IsCompleted, "challenge IsCompleted");
                Assert.AreEqual(e.RewardCollected, a.RewardCollected, "challenge RewardCollected");

                Assert.IsNotNull(a.Reward, "challenge Reward");
                Assert.AreEqual(e.Reward.Type, a.Reward.Type, "reward Type");
                Assert.AreEqual(e.Reward.Amount, a.Reward.Amount, "reward Amount");
                Assert.AreEqual(e.Reward.isCollected, a.Reward.isCollected, "reward isCollected");
            }

            CollectionAssert.AreEqual(expected.playerLevelsCollectStatuses, actual.playerLevelsCollectStatuses,
                "playerLevelsCollectStatuses");

            Assert.AreEqual(expected.kitbagsData.Count, actual.kitbagsData.Count, "kitbagsData.Count");
            for (int i = 0; i < expected.kitbagsData.Count; i++)
            {
                KitbagSaveData e = expected.kitbagsData[i];
                KitbagSaveData a = actual.kitbagsData[i];

                Assert.AreEqual(e.kitbagType, a.kitbagType, "kitbagType");
                Assert.AreEqual(e.totalAds, a.totalAds, "totalAds");
                Assert.AreEqual(e.watchedAds, a.watchedAds, "watchedAds");
                Assert.AreEqual(e.isAdAvailable, a.isAdAvailable, "isAdAvailable");
                Assert.AreEqual(e.isRewardCollected, a.isRewardCollected, "isRewardCollected");
                Assert.AreEqual(e.shouldShowInterstitial, a.shouldShowInterstitial, "shouldShowInterstitial");
            }

            Assert.AreEqual(expected.slotDataArray.Count, actual.slotDataArray.Count, "slotDataArray.Count");
            for (int i = 0; i < expected.slotDataArray.Count; i++)
            {
                SlotSaveData e = expected.slotDataArray[i];
                SlotSaveData a = actual.slotDataArray[i];

                Assert.AreEqual(e.kitbagType, a.kitbagType, "slot kitbagType");
                Assert.AreEqual(e.isSlotLocked, a.isSlotLocked, "slot isSlotLocked");
                Assert.AreEqual(e.unlockTimeString ?? string.Empty, a.unlockTimeString ?? string.Empty,
                    "slot unlockTimeString");
            }

            Assert.AreEqual(expected.lastDailyRewardTime, actual.lastDailyRewardTime, "lastDailyRewardTime");
            Assert.AreEqual(expected.lastDailyChallengeResetTime, actual.lastDailyChallengeResetTime,
                "lastDailyChallengeResetTime");

            Assert.IsNotNull(actual.tournamentProgress, "tournamentProgress");
            Assert.AreEqual(expected.tournamentProgress.tournamentId, actual.tournamentProgress.tournamentId,
                "tournamentId");
            Assert.AreEqual(expected.tournamentProgress.currentStage, actual.tournamentProgress.currentStage,
                "currentStage");
            Assert.AreEqual(expected.tournamentProgress.isEliminated, actual.tournamentProgress.isEliminated,
                "isEliminated");
            Assert.AreEqual(expected.tournamentProgress.isCompleted, actual.tournamentProgress.isCompleted,
                "isCompleted");

            Assert.AreEqual(expected.handSide, actual.handSide, "handSide");
        }
    }
}
