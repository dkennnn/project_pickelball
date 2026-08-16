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
| 1 | P00 | Framework nền: Singleton, FSM, TickManager, Constants, Utilities, asmdef | ✅ Xong | `feat(p00): core framework` |
| 2 | P01a | Toàn bộ enum gameplay (28 enum) | ✅ Xong | `feat(p01): gameplay enums` |
| 3 | P01b | ScriptableObject data layer + editor generator nạp số liệu thật | ✅ Xong | `feat(p01): scriptableobject data layer` |
| 4 | P02 | Court, CourtBounds, RuleEngine, ScoreManager, GameSettings, GameManager | ✅ Xong | `feat(p02): rule engine & match flow` |
| 5 | P05 | InputManager + SwipeData + phân tích độ cong swipe | ✅ Xong | `feat(p05): swipe input system` |
| 6 | P03 | BallController, EnhancedBounce, PhysicsTrajectoryHandler, SimulationBall | ✅ Xong | `feat(p03): ball physics & trajectory prediction` |
| 7 | P04 | BasePlayerController + FSM 4 state + animation controller | ✅ Xong | `feat(p04): player controller fsm` |
| 8 | P06 | PickleballAIController + 5 strategy + 4 AI state + auto-difficulty | ✅ Xong | `feat(p06): ai opponent` |
| 9 | P07 | Boosters (5 loại) + BoosterManager + Energy Drink | ⬜ Chưa | |
| 10 | P08 | SaveData + SaveGameData mã hoá + SavedDataHandler + Backend stub | ⬜ Chưa | |
| 11 | P09 | PlayerLevels, League, Stats, Stadiums, GameData hub | ⬜ Chưa | |
| 12 | P10 | Shop, Item/Character/Grip/Paddle/Workout, PlayerLoadout, Tazo, Kitbag, Locker | ⬜ Chưa | |
| 13 | P11 | Daily Challenge, Daily Reward, Tournament | ⬜ Chưa | |
| 14 | P12 | UI framework + 28 màn hình | ⬜ Chưa | |
| 15 | P13 | Mirror networking + matchmaking + LAN (Phase 2) | ⬜ Chưa | |
| 16 | P14 | Tutorial 11 bước | ⬜ Chưa | |
| 17 | P15 | Ads/IAP/Analytics/RemoteConfig/Localization + ráp scene | ⬜ Chưa | |

Chú thích: ✅ Xong · 🔄 Đang làm · ⬜ Chưa · ⚠️ Có vấn đề

## Quy ước làm việc

- Mỗi mục trong bảng = **một commit** riêng, message theo Conventional Commits:
  `feat(pXX): <mô tả>` / `fix(pXX): ...` / `chore: ...` / `test(pXX): ...`
- Trước khi commit: mở Unity một lần để chắc chắn **compile sạch, 0 error**.
- Cập nhật bảng trên (trạng thái + commit) trong CÙNG commit của bước đó.
- Không commit `Library/`, `Temp/`, `Logs/`, `UserSettings/` (đã có trong `.gitignore`).
- Số liệu cân bằng chỉ nằm trong ScriptableObject, không hardcode trong logic.

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

- [ ] Mở project lần đầu để Unity resolve package (URP, Newtonsoft).
- [ ] Tạo URP Asset + gán vào Graphics Settings / Quality Settings.
- [ ] Chạy menu `Pickleball/Generate Balance Data` để sinh toàn bộ ScriptableObject asset.
- [ ] Tạo Tag: `Net`, `Ground`, `Ball`, `Player`; Layer: `Ground`, `Ball`, `Player`, `CourtBounds`.
- [ ] Player Settings: Portrait, Android IL2CPP ARM64, minSdk 24.
