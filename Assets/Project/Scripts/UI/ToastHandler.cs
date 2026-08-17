using System.Collections;
using System.Collections.Generic;
using StarterKit.Utilities;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Hàng đợi thông báo nhanh: mỗi lúc chỉ hiện một toast, các toast sau xếp hàng chờ.
    /// Không có prefab thì chỉ ghi log chứ không ném lỗi.
    /// </summary>
    public class ToastHandler : Singleton<ToastHandler>
    {
        /// <summary>Prefab một dòng toast.</summary>
        [SerializeField] private ToastMessage toastPrefab;

        /// <summary>Node cha để sinh toast; để trống thì dùng chính transform này.</summary>
        [SerializeField] private RectTransform container;

        /// <summary>Số toast tối đa xếp hàng; toast vượt quá sẽ bị bỏ.</summary>
        [SerializeField] private int maxQueued = 5;

        private readonly Queue<PendingToast> queue = new Queue<PendingToast>();
        private ToastMessage active;
        private Coroutine pump;

        private struct PendingToast
        {
            public string message;
            public float duration;
        }

        /// <summary>
        /// Xếp một thông báo vào hàng đợi để hiển thị.
        /// </summary>
        /// <param name="message">Nội dung thông báo; bỏ qua nếu rỗng.</param>
        /// <param name="duration">Thời gian giữ trên màn hình, tính bằng giây.</param>
        public void Show(string message, float duration = 2f)
        {
            if (string.IsNullOrEmpty(message)) return;

            if (toastPrefab == null)
            {
                Debug.Log("[ToastHandler] " + message);
                return;
            }

            if (queue.Count >= Mathf.Max(1, maxQueued)) return;

            queue.Enqueue(new PendingToast { message = message, duration = duration });

            if (pump == null && isActiveAndEnabled) pump = StartCoroutine(Pump());
        }

        /// <summary>Xoá sạch hàng đợi và ẩn toast đang hiện.</summary>
        public void Clear()
        {
            queue.Clear();

            if (pump != null)
            {
                StopCoroutine(pump);
                pump = null;
            }

            if (active != null)
            {
                active.OnFinished -= HandleToastFinished;
                active.HideImmediate();
                Destroy(active.gameObject);
                active = null;
            }
        }

        private IEnumerator Pump()
        {
            while (queue.Count > 0)
            {
                while (active != null) yield return null;

                PendingToast pending = queue.Dequeue();

                Transform parent = container != null ? (Transform)container : transform;
                active = Instantiate(toastPrefab, parent);
                active.OnFinished += HandleToastFinished;
                active.Show(pending.message, pending.duration);

                while (active != null) yield return null;
            }

            pump = null;
        }

        private void HandleToastFinished(ToastMessage toast)
        {
            if (toast == null) return;

            toast.OnFinished -= HandleToastFinished;
            if (active == toast) active = null;

            Destroy(toast.gameObject);
        }

        protected override void OnDestroy()
        {
            if (active != null) active.OnFinished -= HandleToastFinished;
            queue.Clear();
            base.OnDestroy();
        }
    }
}
