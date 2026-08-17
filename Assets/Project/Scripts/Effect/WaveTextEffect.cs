using TMPro;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Làm chữ TextMeshPro nhấp nhô như sóng (dùng cho chữ "Loading…" ở màn LoadingUI).
    /// <para>
    /// Cách hoạt động: mỗi LateUpdate ép TMP dựng lại mesh, đọc <see cref="TMP_Text.textInfo"/>,
    /// dịch 4 đỉnh của từng ký tự theo hàm sin rồi đẩy ngược vào mesh bằng
    /// <see cref="TMP_Text.UpdateVertexData(TMP_VertexDataUpdateFlags)"/>.
    /// </para>
    /// <para>Không có <see cref="TMP_Text"/> trên GameObject thì component im lặng không làm gì.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public class WaveTextEffect : MonoBehaviour
    {
        [Header("Wave Settings")]
        [Range(0f, 50f)]
        [Tooltip("Biên độ sóng, tính theo đơn vị của mesh chữ.")]
        public float amplitude = 6f;

        [Range(0.01f, 10f)]
        [Tooltip("Tần số sóng — độ lệch pha giữa hai ký tự liền kề.")]
        public float frequency = 0.5f;

        [Range(0.1f, 20f)]
        [Tooltip("Tốc độ chạy của sóng.")]
        public float speed = 4f;

        [Tooltip("Tự chạy ngay khi component được bật.")]
        public bool autoStart = true;

        [Tooltip("Dùng unscaled time để sóng vẫn chạy khi game đang pause.")]
        public bool useUnscaledTime = true;

        [Tooltip("Text cần hiệu ứng. Bỏ trống thì tự lấy TMP_Text trên GameObject này.")]
        [SerializeField] private TMP_Text textComponent;

        private bool isAnimating;
        private float elapsed;

        private void Awake()
        {
            if (textComponent == null) textComponent = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            if (autoStart) StartWaveAnimation();
        }

        private void OnDisable()
        {
            StopWaveAnimation();
        }

        /// <summary>Bắt đầu chạy sóng. No-op nếu không tìm thấy <see cref="TMP_Text"/>.</summary>
        public void StartWaveAnimation()
        {
            if (textComponent == null) textComponent = GetComponent<TMP_Text>();
            if (textComponent == null) return;

            elapsed = 0f;
            isAnimating = true;
        }

        /// <summary>Dừng sóng và trả các ký tự về vị trí gốc.</summary>
        public void StopWaveAnimation()
        {
            isAnimating = false;
            ResetCharacterPositions();
        }

        /// <summary>Đổi tham số sóng lúc runtime.</summary>
        /// <param name="newAmplitude">Biên độ mới.</param>
        /// <param name="newFrequency">Tần số mới.</param>
        /// <param name="newSpeed">Tốc độ mới.</param>
        public void UpdateWaveParameters(float newAmplitude, float newFrequency, float newSpeed)
        {
            amplitude = newAmplitude;
            frequency = newFrequency;
            speed = newSpeed;
        }

        private void ResetCharacterPositions()
        {
            if (textComponent == null) return;
            // ForceMeshUpdate dựng lại mesh từ layout gốc, xoá mọi dịch chuyển đỉnh đã áp.
            textComponent.ForceMeshUpdate();
        }

        private void LateUpdate()
        {
            if (!isAnimating || textComponent == null) return;

            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            ApplyWaveEffect();
        }

        private void ApplyWaveEffect()
        {
            // Phải dựng lại mesh mỗi frame: nếu không, phần dịch đỉnh của frame trước sẽ bị cộng dồn.
            textComponent.ForceMeshUpdate();

            TMP_TextInfo textInfo = textComponent.textInfo;
            if (textInfo == null || textInfo.characterCount == 0) return;

            float t = elapsed * speed;

            for (int i = 0; i < textInfo.characterCount; i++)
            {
                TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;

                int materialIndex = charInfo.materialReferenceIndex;
                if (materialIndex < 0 || materialIndex >= textInfo.meshInfo.Length) continue;

                Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;
                if (vertices == null) continue;

                int vertexIndex = charInfo.vertexIndex;
                if (vertexIndex + 3 >= vertices.Length) continue;

                Vector3 offset = new Vector3(0f, Mathf.Sin(t + i * frequency) * amplitude, 0f);
                vertices[vertexIndex + 0] += offset;
                vertices[vertexIndex + 1] += offset;
                vertices[vertexIndex + 2] += offset;
                vertices[vertexIndex + 3] += offset;
            }

            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                if (textInfo.meshInfo[i].mesh == null) continue;
                textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
            }

            textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
        }
    }
}
