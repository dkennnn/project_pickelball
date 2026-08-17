using System;
using System.Collections.Generic;
using StarterKit.Utilities;
using UnityEngine;

namespace ReplaySystem
{
    /// <summary>
    /// Điều phối toàn bộ hệ thống quay chậm: giữ danh sách <see cref="Replayable"/> trong scene,
    /// bật/tắt ghi và phát lại N giây cuối.
    /// <para>
    /// Ở <c>Start</c> manager tự quét scene tìm mọi <see cref="Replayable"/>; các object sinh ra
    /// sau đó tự đăng ký qua <see cref="RegisterReplayable"/> trong <c>OnEnable</c> của chúng.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public class ReplayManager : Singleton<ReplayManager>
    {
        /// <summary>Trạng thái của hệ thống replay.</summary>
        public enum ReplayManagerState
        {
            /// <summary>Không ghi, không phát.</summary>
            Idle = 0,

            /// <summary>Đang ghi dữ liệu.</summary>
            Recording = 1,

            /// <summary>Đang phát lại.</summary>
            Playing = 2
        }

        [Tooltip("Độ dài tối đa của đoạn ghi, tính bằng giây. Áp xuống mọi Replayable khi bắt đầu ghi.")]
        [SerializeField] private float maxRecordTime = 8f;

        [Tooltip("Tốc độ phát lại. 0.5 = chậm một nửa.")]
        [SerializeField] private float playbackSpeed = 0.5f;

        [Tooltip("Tự bắt đầu ghi ngay khi manager khởi động.")]
        [SerializeField] private bool recordOnStart;

        /// <summary>Phát mỗi khi trạng thái đổi: (trạng thái mới, trạng thái cũ).</summary>
        public event Action<ReplayManagerState, ReplayManagerState> OnStateChanged;

        /// <summary>Trạng thái hiện tại.</summary>
        public ReplayManagerState CurrentState { get; private set; } = ReplayManagerState.Idle;

        /// <summary>Độ dài tối đa của đoạn ghi, tính bằng giây.</summary>
        public float MaxRecordTime => maxRecordTime;

        private readonly List<Replayable> replayables = new List<Replayable>();

        private float playbackTime;
        private float playbackEndTime;
        private Action playbackCallback;

        private void Start()
        {
            CollectReplayablesInScene();
            if (recordOnStart) BeginRecording();
        }

        /// <summary>Quét scene và đăng ký mọi <see cref="Replayable"/> tìm được.</summary>
        public void CollectReplayablesInScene()
        {
            Replayable[] found = FindObjectsByType<Replayable>(FindObjectsSortMode.None);
            for (int i = 0; i < found.Length; i++) RegisterReplayable(found[i]);
        }

        /// <summary>Đăng ký một object cần ghi.</summary>
        /// <param name="replayable">Object cần đăng ký; null hoặc trùng thì bỏ qua.</param>
        public void RegisterReplayable(Replayable replayable)
        {
            if (replayable == null) return;
            if (replayables.Contains(replayable)) return;

            replayables.Add(replayable);
            replayable.maxSeconds = maxRecordTime;

            // Object sinh ra giữa lúc đang ghi thì cho ghi luôn từ thời điểm này.
            if (CurrentState == ReplayManagerState.Recording) replayable.StartRecording();
        }

        /// <summary>Huỷ đăng ký một object.</summary>
        /// <param name="replayable">Object cần huỷ; null thì bỏ qua.</param>
        public void DeregisterReplayable(Replayable replayable)
        {
            if (replayable == null) return;
            replayables.Remove(replayable);
        }

        /// <summary>Bắt đầu ghi trên toàn bộ object đã đăng ký (xoá dữ liệu cũ).</summary>
        public void BeginRecording()
        {
            StopPlaybackInternal(false);
            PruneDestroyed();

            for (int i = 0; i < replayables.Count; i++)
            {
                Replayable r = replayables[i];
                if (r == null) continue;
                r.maxSeconds = maxRecordTime;
                r.StartRecording();
            }

            ChangeState(ReplayManagerState.Recording);
        }

        /// <summary>Dừng ghi, giữ nguyên dữ liệu đã có.</summary>
        public void StopRecording()
        {
            PruneDestroyed();

            for (int i = 0; i < replayables.Count; i++)
            {
                replayables[i]?.StopRecording();
            }

            if (CurrentState == ReplayManagerState.Recording) ChangeState(ReplayManagerState.Idle);
        }

        /// <summary>Có dữ liệu để phát lại không.</summary>
        public bool IsReplayAvailable()
        {
            return GetTotalRecordedDuration() > 0f;
        }

        /// <summary>Thời lượng dài nhất trong số các object đã ghi, tính bằng giây.</summary>
        public float GetTotalRecordedDuration()
        {
            float longest = 0f;
            for (int i = 0; i < replayables.Count; i++)
            {
                Replayable r = replayables[i];
                if (r == null) continue;
                longest = Mathf.Max(longest, r.GetTotalRecordedDuration());
            }

            return longest;
        }

        /// <summary>
        /// Phát lại <paramref name="seconds"/> giây cuối cùng của đoạn đã ghi.
        /// Không có dữ liệu thì gọi thẳng <paramref name="onComplete"/> và không đổi trạng thái.
        /// </summary>
        /// <param name="seconds">Số giây cuối cần phát; &lt;= 0 nghĩa là phát toàn bộ.</param>
        /// <param name="onComplete">Callback khi phát xong hoặc bị bỏ qua; có thể null.</param>
        public void PlayLast(float seconds, Action onComplete)
        {
            StopRecording();
            PruneDestroyed();

            float total = GetTotalRecordedDuration();
            if (total <= 0f)
            {
                onComplete?.Invoke();
                return;
            }

            float window = seconds <= 0f ? total : Mathf.Min(seconds, total);

            playbackEndTime = total;
            playbackTime = total - window;
            playbackCallback = onComplete;

            ChangeState(ReplayManagerState.Playing);
            ApplyPlaybackFrame();
        }

        /// <summary>Dừng phát lại giữa chừng; callback vẫn được gọi.</summary>
        public void SkipReplay()
        {
            if (CurrentState != ReplayManagerState.Playing) return;
            StopPlaybackInternal(true);
        }

        /// <summary>Xoá sạch dữ liệu ghi trên mọi object.</summary>
        public void ClearAllData()
        {
            PruneDestroyed();
            for (int i = 0; i < replayables.Count; i++) replayables[i]?.ClearReplayData();
        }

        private void Update()
        {
            if (CurrentState != ReplayManagerState.Playing) return;

            playbackTime += Time.unscaledDeltaTime * Mathf.Max(0.01f, playbackSpeed);

            if (playbackTime >= playbackEndTime)
            {
                playbackTime = playbackEndTime;
                ApplyPlaybackFrame();
                StopPlaybackInternal(true);
                return;
            }

            ApplyPlaybackFrame();
        }

        private void ApplyPlaybackFrame()
        {
            for (int i = 0; i < replayables.Count; i++)
            {
                replayables[i]?.Playback(playbackTime);
            }
        }

        private void StopPlaybackInternal(bool invokeCallback)
        {
            bool wasPlaying = CurrentState == ReplayManagerState.Playing;

            for (int i = 0; i < replayables.Count; i++) replayables[i]?.StopPlayback();

            Action callback = playbackCallback;
            playbackCallback = null;

            if (wasPlaying) ChangeState(ReplayManagerState.Idle);
            if (invokeCallback) callback?.Invoke();
        }

        private void ChangeState(ReplayManagerState newState)
        {
            if (CurrentState == newState) return;

            ReplayManagerState old = CurrentState;
            CurrentState = newState;
            OnStateChanged?.Invoke(newState, old);
        }

        private void PruneDestroyed()
        {
            for (int i = replayables.Count - 1; i >= 0; i--)
            {
                if (replayables[i] == null) replayables.RemoveAt(i);
            }
        }

        /// <inheritdoc/>
        protected override void OnDestroy()
        {
            OnStateChanged = null;
            playbackCallback = null;
            replayables.Clear();
            base.OnDestroy();
        }
    }
}
