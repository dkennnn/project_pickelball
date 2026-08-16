# Tiến trình dựng lại Pickleball — project_pickelball

- **Unity**: 6000.4.12f1
- **Đặc tả nguồn**: `../00_NGHIEN_CUU_TOAN_BO_GAME.md`
- **Bộ prompt**: `../01_PROMPTS_DUNG_LAI_UNITY.md`
- **Chiến lược**: Phase 1 dựng lõi offline single-player (không phụ thuộc package bên thứ ba)
  → compile được ngay. Phase 2 mới bọc Mirror/backend/ads lên trên.

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
| 9 | P08 | SaveData + SaveGameData mã hoá + SavedDataHandler + Backend stub | ⬜ Chưa | |
| 10a | P09 | **Stats**: StatsData/GameStatsData/StatsManager + 15 EditMode test | ✅ Xong | `feat(p09): match statistics subsystem` |
| 10b | P09 | League UI, GameData hub, PlayerProfileData (còn lại của P09) | ⬜ Chưa | |
| 11 | P10 | Shop, Item/Character/Grip/Paddle/Workout, PlayerLoadout, Tazo, Kitbag, Locker | ⬜ Chưa | |
| 12 | P11 | Daily Challenge, Daily Reward, Tournament | ⬜ Chưa | |
| 13 | P12 | UI framework + 28 màn hình | ⬜ Chưa | |
| 14 | P13 | Mirror networking + matchmaking + LAN (Phase 2) | ⬜ Chưa | |
| 15 | P14 | Tutorial 11 bước | ⬜ Chưa | |
| 16 | P15 | Ads/IAP/Analytics/RemoteConfig/Localization + ráp scene | ⬜ Chưa | |
| 17 | — | **Scene greybox chạy được** + 4 PlayMode smoke test | ✅ Xong | `feat(scene): greybox playable match scene` |

Chú thích: ✅ Xong · 🔄 Đang làm · ⬜ Chưa · ⚠️ Có vấn đề

## Quy ước làm việc

- Mỗi mục trong bảng = **một commit** riêng, message theo Conventional Commits:
  `feat(pXX): <mô tả>` / `fix(pXX): ...` / `chore: ...` / `test(pXX): ...`
- Trước khi commit: mở Unity một lần để chắc chắn **compile sạch, 0 error**.
- Cập nhật bảng trên (trạng thái + commit) trong CÙNG commit của bước đó.
- Không commit `Library/`, `Temp/`, `Logs/`, `UserSettings/` (đã có trong `.gitignore`).
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
- [ ] Tạo URP Asset + gán vào Graphics Settings / Quality Settings (hiện dùng pipeline mặc định).
- [x] ~~Chạy `Pickleball/Generate Balance Data`~~ (đã chạy bằng `-executeMethod`, sinh 14 asset).
- [x] ~~Tạo Tag/Layer~~ (đã ghi thẳng vào `ProjectSettings/TagManager.asset`).
- [ ] Player Settings: Portrait, Android IL2CPP ARM64, minSdk 24.
