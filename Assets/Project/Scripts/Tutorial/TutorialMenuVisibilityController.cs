using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Ẩn/hiện các nút menu theo bước hướng dẫn đang chạy. Gắn lên node cha của MainMenu,
    /// khai báo từng nút cùng danh sách bước mà nút được phép hiện.
    /// </summary>
    public class TutorialMenuVisibilityController : MonoBehaviour
    {
        /// <summary>Một đối tượng menu kèm quy tắc hiển thị theo bước hướng dẫn.</summary>
        [Serializable]
        public class MenuTarget
        {
            /// <summary>Đối tượng cần bật/tắt.</summary>
            public GameObject target;

            /// <summary>Các bước hướng dẫn mà đối tượng được hiện; rỗng = không hiện trong lúc hướng dẫn.</summary>
            public List<TutorialType> visibleDuring = new List<TutorialType>();

            /// <summary>Đối tượng có hiện lại sau khi hướng dẫn kết thúc không.</summary>
            public bool visibleWhenTutorialDone = true;
        }

        /// <summary>Danh sách đối tượng menu chịu điều khiển.</summary>
        public List<MenuTarget> targets = new List<MenuTarget>();

        private void OnEnable()
        {
            TutorialManager.OnTutorialStepChanged += HandleStepChanged;
            Apply(CurrentStep);
        }

        private void OnDisable()
        {
            TutorialManager.OnTutorialStepChanged -= HandleStepChanged;
        }

        /// <summary>Bước hướng dẫn đang chạy; <see cref="TutorialType.TutorialCompleted"/> nếu đã xong.</summary>
        private static TutorialType CurrentStep
        {
            get
            {
                if (!TutorialManager.HasInstance) return TutorialType.TutorialCompleted;

                TutorialManager manager = TutorialManager.Instance;
                if (!manager.IsRunning) return TutorialType.TutorialCompleted;

                return manager.CurrentTutorialType;
            }
        }

        private void HandleStepChanged(TutorialType tutorialType)
        {
            Apply(tutorialType);
        }

        /// <summary>Cập nhật hiển thị của mọi đối tượng theo bước cho trước.</summary>
        /// <param name="tutorialType">Bước hướng dẫn hiện tại.</param>
        private void Apply(TutorialType tutorialType)
        {
            bool tutorialDone = tutorialType == TutorialType.TutorialCompleted ||
                                tutorialType == TutorialType.None;

            for (int i = 0; i < targets.Count; i++)
            {
                MenuTarget entry = targets[i];
                if (entry == null || entry.target == null) continue;

                bool visible = tutorialDone
                    ? entry.visibleWhenTutorialDone
                    : entry.visibleDuring != null && entry.visibleDuring.Contains(tutorialType);

                if (entry.target.activeSelf != visible) entry.target.SetActive(visible);
            }
        }
    }
}
