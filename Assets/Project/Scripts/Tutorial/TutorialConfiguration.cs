using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Bảng cấu hình chuỗi hướng dẫn: mỗi bước khai báo bước kế tiếp, độ trễ chuyển bước,
    /// có phải bước gameplay hay không và màn hình UI bắt buộc trước khi được chạy.
    /// <para>
    /// Cờ <see cref="TutorialStepConfig.isCompleted"/> là trạng thái RUNTIME, nạp từ save data
    /// qua <see cref="ApplyCompletedSteps"/>; giá trị lưu trong asset chỉ là mặc định.
    /// </para>
    /// </summary>
    [CreateAssetMenu(fileName = "TutorialConfiguration", menuName = "ScriptableObjects/TutorialConfiguration")]
    public class TutorialConfiguration : ScriptableObject
    {
        /// <summary>
        /// Cấu hình một bước hướng dẫn. Là kiểu lồng để giữ đúng quy ước "một class public
        /// mỗi file" của project mà vẫn nằm chung asset với <see cref="TutorialConfiguration"/>.
        /// </summary>
        [Serializable]
        public class TutorialStepConfig
        {
            /// <summary>Bước hướng dẫn mà bản ghi này mô tả.</summary>
            public TutorialType tutorialType;

            /// <summary>Bước có được bật trong build này không; tắt thì chuỗi nhảy thẳng qua.</summary>
            public bool isEnabled = true;

            /// <summary>Bước đã hoàn thành chưa (nạp từ save data lúc chạy).</summary>
            public bool isCompleted;

            /// <summary>Bước sẽ chạy sau khi bước này xong.</summary>
            public TutorialType nextTutorial;

            /// <summary>Số giây chờ trước khi bước kế tiếp bắt đầu.</summary>
            public float nextTutorialStartAfterDelay;

            /// <summary>True nếu đây là bước cần vào trận (scene gameplay).</summary>
            public bool isGameplayTutorial;

            /// <summary>Màn hình phải đang hiển thị thì bước mới được vào; None = không ràng buộc.</summary>
            public ScreenType requiredUIScreen = ScreenType.None;
        }

        /// <summary>Toàn bộ bước hướng dẫn, xếp theo thứ tự onboarding.</summary>
        [Header("Tutorial Steps Configuration")]
        public List<TutorialStepConfig> tutorialSteps = new List<TutorialStepConfig>();

        /// <summary>Tắt cờ này là toàn bộ hệ hướng dẫn không chạy.</summary>
        [Header("Tutorial Settings")]
        public bool enableTutorialSystem = true;

        /// <summary>Bước đầu tiên của chuỗi.</summary>
        [Header("Starting Tutorial")]
        public TutorialType startingTutorial = TutorialType.MainMenuForcedPlay;

        /// <summary>Tra cấu hình của một bước; null nếu bảng không khai báo bước đó.</summary>
        /// <param name="tutorialType">Bước cần tra.</param>
        public TutorialStepConfig Get(TutorialType tutorialType)
        {
            if (tutorialSteps == null) return null;

            for (int i = 0; i < tutorialSteps.Count; i++)
            {
                TutorialStepConfig step = tutorialSteps[i];
                if (step != null && step.tutorialType == tutorialType) return step;
            }
            return null;
        }

        /// <summary>
        /// Bước kế tiếp theo khai báo <see cref="TutorialStepConfig.nextTutorial"/>.
        /// Trả về <see cref="TutorialType.None"/> nếu bước không có trong bảng.
        /// </summary>
        /// <param name="currentTutorial">Bước vừa hoàn thành.</param>
        public TutorialType GetNext(TutorialType currentTutorial)
        {
            TutorialStepConfig step = Get(currentTutorial);
            return step != null ? step.nextTutorial : TutorialType.None;
        }

        /// <summary>
        /// Đồng bộ cờ <see cref="TutorialStepConfig.isCompleted"/> với danh sách bước đã xong
        /// đọc từ save data. Bước không có trong danh sách bị đặt lại thành chưa xong.
        /// </summary>
        /// <param name="completed">Danh sách bước đã hoàn thành; null coi như rỗng.</param>
        public void ApplyCompletedSteps(List<TutorialType> completed)
        {
            if (tutorialSteps == null) return;

            for (int i = 0; i < tutorialSteps.Count; i++)
            {
                TutorialStepConfig step = tutorialSteps[i];
                if (step == null) continue;

                step.isCompleted = completed != null && completed.Contains(step.tutorialType);
            }
        }

        /// <summary>
        /// True khi mọi bước gameplay đang bật đều đã hoàn thành.
        /// Nếu bảng không có bước gameplay nào thì trả về false (chưa có gì để hoàn thành).
        /// </summary>
        public bool AreAllGameplayStepsCompleted()
        {
            if (tutorialSteps == null) return false;

            bool found = false;
            for (int i = 0; i < tutorialSteps.Count; i++)
            {
                TutorialStepConfig step = tutorialSteps[i];
                if (step == null || !step.isGameplayTutorial || !step.isEnabled) continue;

                found = true;
                if (!step.isCompleted) return false;
            }
            return found;
        }

        /// <summary>Đặt cờ hoàn thành cho một bước; bỏ qua nếu bảng không khai báo bước đó.</summary>
        /// <param name="tutorialType">Bước cần cập nhật.</param>
        /// <param name="isCompleted">Giá trị mới.</param>
        public void SetCompleted(TutorialType tutorialType, bool isCompleted)
        {
            TutorialStepConfig step = Get(tutorialType);
            if (step != null) step.isCompleted = isCompleted;
        }

        /// <summary>Xoá sạch cờ hoàn thành của mọi bước (dùng khi chơi lại hướng dẫn).</summary>
        [ContextMenu("Reset All Tutorial Completion Status")]
        public void ResetAllCompletion()
        {
            ApplyCompletedSteps(null);
        }
    }
}
