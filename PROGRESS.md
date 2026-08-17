# Tiến trình dựng lại Pickleball — project_pickelball

- **Unity**: 6000.4.12f1
- **Đặc tả nguồn**: `../00_NGHIEN_CUU_TOAN_BO_GAME.md`
- **Bộ prompt**: `../01_PROMPTS_DUNG_LAI_UNITY.md`
- **Mục tiêu**: bản build **chơi thử được trọn vòng lặp** (menu → đấu AI → kết quả → thưởng →
  nâng cấp → chơi tiếp), để đánh giá gameplay và cân bằng trước khi dựng lại bản chính thức.
- **UI phải dựng đúng cấu trúc + bố cục của bản gốc** (không phải UI tạm bợ), để chỉ việc thay art.
  Bản vẽ layout đã trích sẵn: `../ui_layout/` — 29 màn hình + 21 cell prefab, kèm RectTransform,
  tên sprite, nội dung text, và cấu hình CanvasScaler (1080x1920, match 0.5).
- **Hoãn tới cuối cùng**: P13 (Mirror multiplayer) và P15 (ads/IAP/analytics/remote config).
  Backend cũng bỏ — chỉ save cục bộ.

## Bảng tiến trình

| # | Prompt | Nội dung | Trạng thái | Commit |
|---|---|---|---|---|
| 0 | — | Tạo project Unity 6000.4.12f1, packages, .gitignore, cây thư mục | ✅ Xong | `chore: bootstrap Unity 6000.4 project` |
| 1 | P00 | Framework nền (Singleton, FSM, TickManager, Tweener, Utilities) + 28 enum | ✅ Xong | `feat(p00): core framework + gameplay enums` |
| 2 | — | Tag/Layer, meta files, sửa asmdef trùng reference | ✅ Xong | `chore: unity metadata, tags & layers` |
| 3 | P01 | ScriptableObject data layer + editor generator + 14 asset số liệu thật | ✅ Xong | `feat(p01): scriptableobject data layer` |
| 4 | P02+P05 | Court, RuleEngine, ScoreManager, GameManager, InputManager, SwipeTracer + 18 EditMode test | ✅ Xong | `feat(p02): rule engine, match flow & swipe input` |
| 5 | P03 | BallController, EnhancedBounce, PhysicsTrajectoryHandler, SimulationBall, PickleNet | ✅ Xong | `feat(p03): ball physics & trajectory prediction` |
| 6 | P04 | BasePlayerController + FSM 4 state + animation + CameraFollow | ✅ Xong | `feat(p04): player controller fsm` |
| 7 | P06 | PickleballAIController + 5 strategy + AIHelper + 4 AI state + AINamesData | ✅ Xong | `feat(p06): ai opponent` |
| 8 | P07 | Boosters (5 loại) + BoosterManager + AIBoosterController + VFXPlayer | ✅ Xong | `feat(p07): booster system` |
| 9 | P08 | SaveData + AES + SavedDataHandler + GameBootstrap (bỏ backend) | ✅ Xong | `feat(p08): local encrypted save & persistence` |
| 10a | P09 | **Stats**: StatsData/GameStatsData/StatsManager + 15 EditMode test | ✅ Xong | `feat(p09): match statistics subsystem` |
| 10b | P09 | League UI (GameData hub + PlayerProfileData đã xong ở P10a) | ⬜ Chưa | làm cùng P12 |
| 11a | P10 | Item/Character/Grip/Paddle/Workout, PlayerLoadout, TazoData, **GameData hub**, PlayerProfileData | ✅ Xong | `feat(p10): item, loadout & gamedata hub` |
| 11b | P10 | Rewards, Kitbag, DynamicKitbag, RewardsCollection, RewardManager, **SlotsData (Locker)** | ✅ Xong | `feat(p10): rewards, kitbag & locker` |
| 11c | P10 | `Shop.cs` aggregator + `MatchRewardHandler` + `GameBootstrap` | ✅ Xong | `feat(p10): shop aggregator & match reward loop` |
| 12 | P11 | Daily Challenge (tự cắm event), Daily Reward, Tournament 3 vòng | ✅ Xong | `feat(p11): daily challenge, daily reward & tournament` |
| 13 | P12 | UI framework + 29 màn + 21 cell prefab, **dựng theo `../ui_layout/`** | ⬜ Chưa | |
| 14 | P14 | Tutorial 11 bước | ⬜ Chưa | |
| 15 | — | Effect / Managers / Misc / ReplaySystem / ProfanityFilter | ⬜ Chưa | |
| — | P13 | Mirror multiplayer + LAN | ⏸ **hoãn cuối** | cần cài package Mirror |
| — | P15 | Ads / IAP / Analytics / RemoteConfig / Localization | ⏸ **hoãn cuối** | cần tài khoản thật |

| 17 | — | **Scene greybox chạy được** + 4 PlayMode smoke test | ✅ Xong | `feat(scene): greybox playable match scene` |

Chú thích: ✅ Xong · 🔄 Đang làm · ⬜ Chưa · ⚠️ Có vấn đề

## Quy ước làm việc

- Mỗi mục trong bảng = **một commit** riêng, message theo Conventional Commits:
  `feat(pXX): <mô tả>` / `fix(pXX): ...` / `chore: ...` / `test(pXX): ...`
- Trước khi commit: mở Unity một lần để chắc chắn **compile sạch, 0 error**.
- Cập nhật bảng trên (trạng thái + commit) trong CÙNG commit của bước đó.
- Không commit `Library/`, `Temp/`, `Logs/`, `UserSettings/` (đã có trong `.gitignore`).
- **KHÔNG chạy hai tiến trình Unity batchmode cùng lúc trên project này.** Unity khoá project
  bằng `Temp/UnityLockfile`; chạy song song thì tiến trình thứ hai thoát sớm không sinh
  kết quả, và mỗi instance chiếm ~700 MB - 1.2 GB commit memory. Nếu chia việc cho nhiều
  agent thì để chúng CHỈ viết code, còn compile/test do một tiến trình duy nhất chạy tuần tự.
- Số liệu cân bằng chỉ nằm trong ScriptableObject, không hardcode trong logic.

## Chạy thử ngay

1. Mở project bằng Unity 6000.4.12f1.
2. Mở `Assets/Project/Scenes/GreyboxMatch.unity` (đã ở Build Settings index 0) rồi bấm Play.
3. **Tap** xuống sân để di chuyển, **vuốt** để đánh bóng. Đối thủ AI ở bậc Amateur.
4. Dựng lại scene bất cứ lúc nào: menu `Pickleball/Build Greybox Match Scene` (idempotent).
5. Sinh lại toàn bộ asset số liệu: menu `Pickleball/Generate Balance Data` (idempotent).

Kiểm tra tự động:
```
powershell -ExecutionPolicy Bypass -File ..\check-compile.ps1
Unity.exe -batchmode -nographics -runTests -testPlatform EditMode -projectPath <path> -testResults r.xml
Unity.exe -batchmode -nographics -runTests -testPlatform PlayMode -projectPath <path> -testResults r.xml
```

## Sự cố đã gặp & cách xử lý

### `Unexpected transport error from import worker ... code=10054`

**Triệu chứng**: 3 import worker chết cùng lúc khi mở project lần đầu trong Editor.

**Nguyên nhân thật**: hết bộ nhớ ở mức *commit* của Windows, KHÔNG phải Library hỏng.
Bằng chứng trong `Logs/AssetImportWorker0.log`:
- Allocation thất bại chỉ **87.520 byte (85 KB)**, trong khi Unity mới dùng 78 MB
  → không phải project quá nặng.
- Stack: `LoadAndRegisterAllKnownShaders` → `SerializedFile::ReadMetadata` → OOM,
  tức là chết lúc nạp shader URP khi khởi động worker.
- Máy: 32 GB RAM nhưng **commit limit chỉ 40.7 GB** (pagefile nhỏ), commit còn trống 7.9 GB.

**Unity tự phục hồi**: worker 3/4/5 sinh lại sau đó 1 phút, `grep -c "Crash!!!"` = 0 trên cả ba;
`Library/ScriptAssemblies/` có đủ 4 assembly (Runtime 158 KB, Editor 34 KB, 2 assembly test);
`Editor.log` chạy refresh bình thường ở 282 MB. **Không cần làm gì, project vẫn dùng được.**

**Giảm khả năng tái diễn**:
1. `Edit > Preferences > Asset Pipeline > Desired Import Worker Count` → đặt **2** (mặc định
   theo số nhân CPU, mỗi worker là một tiến trình Unity đầy đủ).
2. Tăng pagefile Windows (Control Panel > System > Advanced > Performance > Virtual Memory),
   đặt tối thiểu bằng dung lượng RAM.
3. Đóng bớt ứng dụng nặng trước khi mở Unity.

**Nếu về sau thực sự hỏng Library** (hiếm): đóng Unity, xoá thư mục `Library/` và `Temp/`,
mở lại — Unity dựng lại từ đầu. Cả hai đều nằm trong `.gitignore` nên không mất gì.

### Toàn bộ scene màu hồng (magenta)

**Nguyên nhân**: project được tạo bằng `-createProject` (template Built-in) rồi mới thêm package
URP. Unity chỉ tự sinh `UniversalRenderPipelineGlobalSettings.asset`, KHÔNG tạo URP Asset và
không gán vào Graphics Settings — kiểm chứng bằng `ProjectSettings/GraphicsSettings.asset`:
`m_CustomRenderPipeline: {fileID: 0}`. Pipeline đang chạy vẫn là Built-in, nên mọi material
dùng shader `Universal Render Pipeline/Lit` không có subshader hợp lệ → Unity vẽ màu hồng.

**Cách sửa**: menu `Pickleball/Setup URP Pipeline` (script `Editor/UrpPipelineSetup.cs`).
Tạo `Assets/Project/Settings/PickleballUniversalRP.asset` + `PickleballUniversalRenderer.asset`
rồi gán vào `GraphicsSettings.defaultRenderPipeline` **và cả 6 quality level**
(bỏ sót một level thì đổi Quality lại ra màu hồng). Script idempotent.

**Lưu ý khi tự kiểm tra bằng grep**: trong `ProjectSettings/QualitySettings.asset` khoá là
`customRenderPipeline`, KHÔNG phải `renderPipeline` — grep sai tên sẽ tưởng chưa gán.

## Quyết định kiến trúc

1. **Không dùng Mirror ở Phase 1.** Code gốc là server-authoritative, nhưng để project compile
   được ngay mà không cần cài package ngoài, Phase 1 dùng `NetworkSingleton<T>` là một
   Singleton thuần với các hook `OnStartServer/OnStopServer/OnStartClient` gọi cục bộ. Tất cả
   attribute mạng được thay bằng comment `// [Server]`, `// [ClientRpc]` đúng vị trí, nên khi
   thêm Mirror ở Phase 2 chỉ cần bỏ comment và đổi lớp cha.
2. **Không dùng DOTween** (Asset Store). Thay bằng `StarterKit.Utilities.Tweener` (coroutine
   nội bộ, đủ cho fade/scale/move/timeScale lerp).
3. **Legacy Input Manager** (không dùng Input System package) — khớp bản gốc và bớt phụ thuộc.
4. **Newtonsoft.Json** qua UPM (`com.unity.nuget.newtonsoft-json`) cho save data.
5. Assembly definitions: `Pickleball.Runtime` (Assets/Project/Scripts) và
   `Pickleball.Editor` (Assets/Project/Scripts/Editor) — tránh recompile toàn project.

## Việc cần làm thủ công trong Unity Editor

- [x] ~~Mở project lần đầu để Unity resolve package~~ (đã chạy batchmode, package resolve xong).
- [x] ~~Tạo URP Asset + gán vào Graphics Settings / Quality Settings~~ (menu `Pickleball/Setup URP Pipeline`, đã chạy).
- [x] ~~Chạy `Pickleball/Generate Balance Data`~~ (đã chạy bằng `-executeMethod`, sinh 14 asset).
- [x] ~~Tạo Tag/Layer~~ (đã ghi thẳng vào `ProjectSettings/TagManager.asset`).
- [ ] Player Settings: Portrait, Android IL2CPP ARM64, minSdk 24.

## Vòng lặp hiện đã khép kín (chưa cần UI)

Mở `GreyboxMatch.unity` → Play → đấu với AI → thắng/thua:
1. `MatchRewardHandler` cộng/trừ coin, trophy, tính lại level
2. Thắng → kitbag vào locker theo chuỗi thắng (>=5 Level3, >=3 Level2, còn lại Level1)
3. `SavedDataHandler` tự lưu (debounce 1s) — file `pickleball.sav` mã hoá AES trong
   `Application.persistentDataPath`
4. Thoát Play rồi vào lại → `GameBootstrap` nạp save, `Shop.SetupLoadout()`,
   `PlayerLoadout.UpdateProfile()` → chỉ số nhân vật khớp đồ đã nâng cấp

Test nhanh không cần đánh: chọn object `MatchRewardHandler` trong scene → context menu
"Debug Award Win" / "Debug Award Loss".
Nâng cấp đồ để thử: chọn asset item trong `ScriptableObjects/Shop/`, tăng `currentLevel`,
hoặc gọi `Shop.MaxAllItems()`.
