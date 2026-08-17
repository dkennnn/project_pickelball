using System;
using System.Collections.Generic;
using StarterKit.Utilities;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Điều phối các đoạn cutscene ngắn (vào trận, ăn điểm quyết định, trao cúp).
    /// <para>
    /// TODO: bản gốc dùng Animator + Timeline + camera riêng. Bản dựng lại hiện CHƯA có
    /// timeline/asset animation, nên <see cref="PlayCutScene"/> chỉ chờ đúng thời lượng khai báo
    /// rồi gọi callback. Khi có asset thật thì thay phần chờ bằng PlayableDirector và giữ nguyên
    /// chữ ký hàm để phía gọi không phải sửa.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public class CutSceneManager : Singleton<CutSceneManager>
    {
        /// <summary>Khai báo một cutscene: id để tra + thời lượng giả lập.</summary>
        [Serializable]
        public class CutSceneEntry
        {
            [Tooltip("Định danh cutscene, dùng làm khoá khi gọi PlayCutScene.")]
            public string id;

            [Tooltip("Thời lượng, tính bằng giây.")]
            public float duration = 1.5f;

            [Tooltip("Object bật lên trong lúc cutscene chạy (camera riêng, khung viền phim…).")]
            public GameObject sceneRoot;
        }

        /// <summary>Thời lượng mặc định khi gọi một id không có trong <see cref="cutScenes"/>.</summary>
        [Tooltip("Thời lượng mặc định cho cutscene không khai báo, tính bằng giây.")]
        [SerializeField] private float defaultDuration = 1.5f;

        [Tooltip("Danh sách cutscene khai báo sẵn.")]
        [SerializeField] private List<CutSceneEntry> cutScenes = new List<CutSceneEntry>();

        /// <summary>Phát khi một cutscene bắt đầu; tham số là id.</summary>
        public static event Action<string> OnCutSceneStarted;

        /// <summary>Phát khi một cutscene kết thúc; tham số là id.</summary>
        public static event Action<string> OnCutSceneEnded;

        /// <summary>Id cutscene đang chạy; rỗng nếu không có cutscene nào.</summary>
        public string CurrentCutSceneId { get; private set; } = string.Empty;

        /// <summary>True khi đang có cutscene chạy.</summary>
        public bool IsPlaying => !string.IsNullOrEmpty(CurrentCutSceneId);

        private Coroutine pendingRoutine;
        private CutSceneEntry activeEntry;
        private Action activeCallback;

        /// <summary>
        /// Phát cutscene theo id. Đang có cutscene khác chạy thì cutscene đó bị kết thúc sớm
        /// (callback của nó vẫn được gọi) trước khi cái mới bắt đầu.
        /// </summary>
        /// <param name="id">Định danh cutscene.</param>
        /// <param name="onComplete">Callback chạy khi cutscene kết thúc; có thể null.</param>
        public void PlayCutScene(string id, Action onComplete)
        {
            if (IsPlaying) FinishCurrent();

            CutSceneEntry entry = FindEntry(id);
            float duration = entry != null ? Mathf.Max(0f, entry.duration) : Mathf.Max(0f, defaultDuration);

            activeEntry = entry;
            activeCallback = onComplete;
            CurrentCutSceneId = id ?? string.Empty;

            if (entry != null && entry.sceneRoot != null) entry.sceneRoot.SetActive(true);
            OnCutSceneStarted?.Invoke(CurrentCutSceneId);

            // TODO: thay bằng PlayableDirector.Play() + đăng ký stopped khi có Timeline asset.
            pendingRoutine = DelayedAction.RunUnscaled(duration, FinishCurrent);
        }

        /// <summary>Bỏ qua cutscene đang chạy; callback vẫn được gọi ngay lập tức.</summary>
        public void SkipCutScene()
        {
            if (!IsPlaying) return;
            FinishCurrent();
        }

        private void FinishCurrent()
        {
            if (pendingRoutine != null)
            {
                DelayedAction.Cancel(pendingRoutine);
                pendingRoutine = null;
            }

            if (activeEntry != null && activeEntry.sceneRoot != null) activeEntry.sceneRoot.SetActive(false);

            string finishedId = CurrentCutSceneId;
            Action callback = activeCallback;

            activeEntry = null;
            activeCallback = null;
            CurrentCutSceneId = string.Empty;

            OnCutSceneEnded?.Invoke(finishedId);
            callback?.Invoke();
        }

        private CutSceneEntry FindEntry(string id)
        {
            if (string.IsNullOrEmpty(id) || cutScenes == null) return null;

            for (int i = 0; i < cutScenes.Count; i++)
            {
                CutSceneEntry entry = cutScenes[i];
                if (entry != null && entry.id == id) return entry;
            }

            return null;
        }

        /// <inheritdoc/>
        protected override void OnDestroy()
        {
            if (pendingRoutine != null)
            {
                DelayedAction.Cancel(pendingRoutine);
                pendingRoutine = null;
            }

            activeEntry = null;
            activeCallback = null;
            CurrentCutSceneId = string.Empty;

            base.OnDestroy();
        }
    }
}
