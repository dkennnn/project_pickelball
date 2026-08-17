using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Dao động vị trí theo hàm sin trên một trục cấu hình được — dùng cho vật trang trí
    /// trôi bồng bềnh, mũi tên chỉ dẫn nhấp nhô.
    /// <para>Không cần reference nào.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public class Oscillate : MonoBehaviour
    {
        /// <summary>Trục dao động.</summary>
        public enum Axis
        {
            /// <summary>Trục X.</summary>
            X = 0,

            /// <summary>Trục Y.</summary>
            Y = 1,

            /// <summary>Trục Z.</summary>
            Z = 2
        }

        [Tooltip("Trục dao động.")]
        public Axis oscillationAxis = Axis.Y;

        [Tooltip("Tốc độ dao động (số radian mỗi giây).")]
        public float speed = 2f;

        [Tooltip("Biên độ dao động, tính theo đơn vị của không gian đang dùng.")]
        public float offset = 0.25f;

        [Tooltip("Dùng localPosition thay vì position (bắt buộc cho UI).")]
        public bool useLocalSpace = true;

        [Tooltip("Lệch pha ban đầu, tính bằng radian — cho nhiều object cùng loại lệch nhịp nhau.")]
        public float phaseOffset;

        [Tooltip("Dùng unscaled time để vẫn dao động khi game đang pause.")]
        public bool useUnscaledTime = true;

        private Vector3 startPosition;
        private bool captured;
        private float elapsed;

        private void Awake()
        {
            Capture();
        }

        private void OnDisable()
        {
            // Trả về vị trí gốc để lần bật sau không bị trôi dần.
            if (!captured) return;
            if (useLocalSpace) transform.localPosition = startPosition;
            else transform.position = startPosition;
        }

        private void Capture()
        {
            if (captured) return;
            startPosition = useLocalSpace ? transform.localPosition : transform.position;
            captured = true;
        }

        private void Update()
        {
            Capture();

            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float delta = Mathf.Sin(elapsed * speed + phaseOffset) * offset;

            Vector3 p = startPosition;
            switch (oscillationAxis)
            {
                case Axis.X: p.x += delta; break;
                case Axis.Y: p.y += delta; break;
                default: p.z += delta; break;
            }

            if (useLocalSpace) transform.localPosition = p;
            else transform.position = p;
        }

        /// <summary>Ghi lại vị trí hiện tại làm tâm dao động mới.</summary>
        public void ResetOrigin()
        {
            captured = false;
            elapsed = 0f;
            Capture();
        }
    }
}
