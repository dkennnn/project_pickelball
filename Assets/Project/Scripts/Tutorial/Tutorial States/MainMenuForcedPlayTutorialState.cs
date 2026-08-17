using StarterKit.UIKit;
using UnityEngine;
using UnityEngine.UI;

namespace Pickleball
{
    /// <summary>
    /// Bước 1 — ép người chơi bấm nút Play trên MainMenu: mọi nút khác bị tắt
    /// <c>interactable</c>, chỉ nút Play còn bấm được và được khung sáng chỉ vào.
    /// Bước hoàn thành ngay khi nút Play được bấm.
    /// </summary>
    public class MainMenuForcedPlayTutorialState : BaseTutorialState
    {
        private const string Message = "Tap PLAY to start your first match.";

        private Button playButton;
        private bool clicked;

        /// <summary>Khởi tạo bước với ngữ cảnh dùng chung.</summary>
        /// <param name="context">Ngữ cảnh do <see cref="TutorialManager"/> cấp.</param>
        public MainMenuForcedPlayTutorialState(TutorialStateContext context) : base(context) { }

        /// <inheritdoc/>
        public override TutorialType Type => TutorialType.MainMenuForcedPlay;

        /// <inheritdoc/>
        protected override void OnEnter()
        {
            clicked = false;
            playButton = ResolvePlayButton();

            ShowMessage(Message);

            if (playButton == null)
            {
                // Không tìm được nút Play (scene test / prefab chưa gán) → không được kẹt chuỗi.
                Debug.LogWarning("[MainMenuForcedPlayTutorialState] Không tìm thấy nút Play, bỏ qua bước.");
                Complete();
                return;
            }

            BlockInputExcept(playButton.transform);
            playButton.interactable = true;

            HighlightObject(playButton.transform as RectTransform);
            playButton.onClick.AddListener(HandlePlayClicked);
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            if (clicked) Complete();
        }

        /// <inheritdoc/>
        protected override void OnExit()
        {
            if (playButton != null) playButton.onClick.RemoveListener(HandlePlayClicked);
            playButton = null;
        }

        private void HandlePlayClicked()
        {
            clicked = true;
        }

        /// <summary>
        /// Lấy nút Play: ưu tiên tham chiếu gán tay trong context, nếu không có thì dò trong
        /// cây của màn hình <see cref="ScreenType.MainMenu"/> theo tên node.
        /// </summary>
        private Button ResolvePlayButton()
        {
            if (context != null && context.mainMenuPlayButton != null) return context.mainMenuPlayButton;
            if (!UIController.HasInstance) return null;

            UIScreenBase mainMenu = UIController.Instance.Get(ScreenType.MainMenu);
            if (mainMenu == null) return null;

            Button[] buttons = mainMenu.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button b = buttons[i];
                if (b == null) continue;

                string name = b.name.ToLowerInvariant();
                if (name.Contains("play")) return b;
            }
            return null;
        }
    }
}
