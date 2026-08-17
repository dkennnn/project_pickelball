using System.Collections;
using System.Text;
using NUnit.Framework;
using StarterKit.UIKit;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Pickleball.Tests
{
    /// <summary>
    /// Kiểm tra tầng UI của scene <c>GreyboxMatch</c>: hạ tầng có đủ, MainMenu tự hiện khi bấm Play,
    /// và nó THẬT SỰ vẽ ra pixel chứ không chỉ tồn tại trong hierarchy.
    ///
    /// <para>Scene phải được sinh trước bằng <c>Pickleball/Build Greybox Match Scene</c> hoặc
    /// <c>Pickleball/Build Match Scene (Original Art)</c> — cả hai đều dựng tầng UI.</para>
    ///
    /// <para><b>Tất định</b>: mọi phép chờ đều theo ĐIỀU KIỆN + timeout, không có delay cứng.
    /// <c>UIController</c> chờ 2 khung hình rồi mới mở màn đầu tiên, còn mỗi
    /// <c>UIScreenBase</c> đăng ký bằng coroutine riêng, nên thời điểm MainMenu hiện lên
    /// phụ thuộc thứ tự Awake — không được phép đoán bằng số frame cố định.</para>
    /// </summary>
    public class UISceneTests
    {
        private const string SceneName = "GreyboxMatch";

        /// <summary>Hạn chờ tối đa cho mọi điều kiện, tính bằng giây.</summary>
        private const float ConditionTimeout = 5f;

        /// <summary>Số màn hình tối thiểu phải có mặt trong scene.</summary>
        private const int MinimumScreenCount = 25;

        /// <summary>Số widget nhìn thấy được tối thiểu trên MainMenu.</summary>
        private const int MinimumVisibleGraphics = 10;

        /// <summary>Nạp lại scene sạch trước mỗi test và chờ một khung hình cho Awake chạy xong.</summary>
        [UnitySetUp]
        public IEnumerator SetUpScene()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;
        }

        // ------------------------------------------------------------------

        [UnityTest]
        public IEnumerator UI_Infrastructure_Exists()
        {
            yield return WaitForCondition(() => UIController.Instance != null);

            Assert.IsNotNull(Object.FindAnyObjectByType<EventSystem>(),
                "Thiếu EventSystem trong scene — UI vẫn hiện nhưng MỌI nút bấm đều chết.");

            Assert.IsNotNull(UIController.Instance,
                "UIController.Instance null — không có gì điều phối màn hình.");

            UIScreenBase[] screens = Object.FindObjectsByType<UIScreenBase>(FindObjectsSortMode.None);
            Assert.GreaterOrEqual(screens.Length, MinimumScreenCount,
                "Chỉ có " + screens.Length + " UIScreenBase trong scene, cần ít nhất " + MinimumScreenCount +
                " — prefab màn hình chưa được đặt vào scene.");
        }

        // ------------------------------------------------------------------

        [UnityTest]
        public IEnumerator MainMenu_Is_Visible_On_Start()
        {
            yield return WaitForCondition(IsMainMenuShown);

            Assert.IsNotNull(UIController.Instance, "UIController.Instance null sau " + ConditionTimeout + " giây.");
            Assert.IsTrue(UIController.Instance.IsShown(ScreenType.MainMenu),
                "MainMenu không tự hiện sau " + ConditionTimeout + " giây. " + DescribeUIState());

            UIScreenBase mainMenu = UIController.Instance.Get(ScreenType.MainMenu);
            Assert.IsNotNull(mainMenu, "Chưa có màn hình nào đăng ký ScreenType.MainMenu.");

            Canvas canvas = ResolveCanvas(mainMenu);
            Assert.IsNotNull(canvas, "MainMenu không có Canvas — không thể vẽ ra gì.");
            Assert.IsTrue(canvas.enabled, "Canvas của MainMenu đang tắt dù UIController báo đã hiện.");
        }

        // ------------------------------------------------------------------

        [UnityTest]
        public IEnumerator MainMenu_Has_Visible_Graphics()
        {
            yield return WaitForCondition(IsMainMenuShown);

            UIScreenBase mainMenu = UIController.Instance != null
                ? UIController.Instance.Get(ScreenType.MainMenu)
                : null;
            Assert.IsNotNull(mainMenu, "Chưa có màn hình nào đăng ký ScreenType.MainMenu.");

            Graphic[] graphics = mainMenu.GetComponentsInChildren<Graphic>(true);

            int visible = 0;
            for (int i = 0; i < graphics.Length; i++)
            {
                if (IsActuallyVisible(graphics[i])) visible++;
            }

            Assert.GreaterOrEqual(visible, MinimumVisibleGraphics,
                "MainMenu chỉ có " + visible + "/" + graphics.Length + " widget thật sự nhìn thấy được " +
                "(enabled + activeInHierarchy + alpha > 0.01 + rộng > 1px), cần ít nhất " +
                MinimumVisibleGraphics + ". UI đang tồn tại trong hierarchy nhưng KHÔNG hiện lên.");
        }

        // ------------------------------------------------------------------

        [UnityTest]
        public IEnumerator Only_One_Screen_Visible_At_Start()
        {
            yield return WaitForCondition(IsMainMenuShown);

            UIScreenBase[] screens = Object.FindObjectsByType<UIScreenBase>(FindObjectsSortMode.None);

            int visible = 0;
            StringBuilder names = new StringBuilder();

            for (int i = 0; i < screens.Length; i++)
            {
                UIScreenBase screen = screens[i];
                if (screen == null || screen.IsPopup) continue;

                Canvas canvas = ResolveCanvas(screen);
                if (canvas == null || !canvas.enabled) continue;

                visible++;
                if (names.Length > 0) names.Append(", ");
                names.Append(screen.name);
            }

            Assert.AreEqual(1, visible,
                "Phải có ĐÚNG một màn hình thường đang bật canvas lúc khởi động, đang có " + visible +
                " (" + names + ") — nhiều canvas đang chồng lên nhau.");
        }

        // ------------------------------------------------------------------
        // Helper
        // ------------------------------------------------------------------

        /// <summary>True khi UIController đã có và đang hiển thị MainMenu.</summary>
        private static bool IsMainMenuShown()
        {
            return UIController.Instance != null && UIController.Instance.IsShown(ScreenType.MainMenu);
        }

        /// <summary>
        /// Chờ tới khi điều kiện thoả hoặc hết <see cref="ConditionTimeout"/>. Dùng
        /// <c>Time.unscaledDeltaTime</c> để không phụ thuộc <c>Time.timeScale</c>.
        /// </summary>
        /// <param name="condition">Điều kiện cần chờ.</param>
        private static IEnumerator WaitForCondition(System.Func<bool> condition)
        {
            float elapsed = 0f;
            while (elapsed < ConditionTimeout && !condition())
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        /// <summary>
        /// Canvas mà một màn hình dùng để bật/tắt: ưu tiên trên chính node gốc, rơi về node con —
        /// đúng thứ tự <c>UIScreenBase.CacheReferences</c> làm.
        /// </summary>
        private static Canvas ResolveCanvas(UIScreenBase screen)
        {
            if (screen == null) return null;

            Canvas canvas = screen.GetComponent<Canvas>();
            return canvas != null ? canvas : screen.GetComponentInChildren<Canvas>(true);
        }

        /// <summary>
        /// True khi một widget thật sự đóng góp pixel lên màn hình: component bật, node cha bật,
        /// màu chưa trong suốt hoàn toàn và khung có bề rộng thật.
        /// </summary>
        /// <param name="graphic">Widget cần kiểm tra (Image, TextMeshProUGUI, RawImage...).</param>
        private static bool IsActuallyVisible(Graphic graphic)
        {
            if (graphic == null) return false;
            if (!graphic.enabled) return false;
            if (!graphic.gameObject.activeInHierarchy) return false;
            if (graphic.color.a <= 0.01f) return false;

            RectTransform rect = graphic.rectTransform;
            return rect != null && rect.rect.width > 1f;
        }

        /// <summary>Mô tả ngắn trạng thái UI để thông báo lỗi của test có ích khi fail.</summary>
        private static string DescribeUIState()
        {
            if (UIController.Instance == null) return "UIController.Instance = null.";

            UIScreenBase[] screens = Object.FindObjectsByType<UIScreenBase>(FindObjectsSortMode.None);

            StringBuilder builder = new StringBuilder();
            builder.Append("CurrentScreen=").Append(UIController.Instance.CurrentScreen)
                   .Append(", số UIScreenBase=").Append(screens.Length).Append(", đang bật canvas: ");

            bool any = false;
            for (int i = 0; i < screens.Length; i++)
            {
                Canvas canvas = ResolveCanvas(screens[i]);
                if (canvas == null || !canvas.enabled) continue;

                if (any) builder.Append(", ");
                builder.Append(screens[i].name);
                any = true;
            }

            if (!any) builder.Append("(không có)");
            return builder.ToString();
        }
    }
}
