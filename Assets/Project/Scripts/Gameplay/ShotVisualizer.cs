using System.Collections;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Vẽ đường bay dự đoán của bóng bằng <see cref="LineRenderer"/>.
    /// <para>
    /// Dữ liệu đầu vào là <see cref="TrajectoryData"/> do <see cref="PhysicsTrajectoryHandler"/>
    /// sinh ra, nên đường vẽ trùng khít với đường bóng bay thật chứ không phải một đường cong
    /// xấp xỉ. Đây là object thuần trình diễn phía client.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class ShotVisualizer : MonoBehaviour
    {
        [Tooltip("Chỉ lấy 1 trong N mẫu quỹ đạo để vẽ — quỹ đạo mô phỏng dày ~50 mẫu/giây, " +
                 "vẽ hết là thừa và tốn vertex.")]
        [SerializeField] private int sampleStride = 2;

        [Tooltip("Thời lượng (giây) hiệu ứng đường bay hiện dần.")]
        [SerializeField] private float fadeInDuration = 0.2f;

        [Tooltip("Nhấc đường vẽ lên khỏi quỹ đạo thật vài cm để không bị mặt sân che.")]
        [SerializeField] private float heightOffset = 0.02f;

        private LineRenderer lineRenderer;
        private Material lineMaterial;
        private Color initialColor = Color.white;
        private Coroutine fadeRoutine;

        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = 0;

            // Dùng instance material để đổi alpha không ảnh hưởng các visualizer khác.
            lineMaterial = lineRenderer.material;
            if (lineMaterial != null && lineMaterial.HasProperty("_Color")) initialColor = lineMaterial.color;

            Hide();
        }

        /// <summary>
        /// Hiện đường bay dự đoán.
        /// </summary>
        /// <param name="data">Quỹ đạo cần vẽ; null hoặc không hợp lệ thì tự ẩn.</param>
        public void Show(TrajectoryData data)
        {
            if (data == null || !data.IsValid)
            {
                Hide();
                return;
            }

            int stride = Mathf.Max(1, sampleStride);
            int count = data.Count;
            int drawn = 0;

            lineRenderer.positionCount = Mathf.CeilToInt(count / (float)stride);

            for (int i = 0; i < count; i += stride)
            {
                if (drawn >= lineRenderer.positionCount) break;
                lineRenderer.SetPosition(drawn, data.Points[i] + Vector3.up * heightOffset);
                drawn++;
            }

            lineRenderer.positionCount = drawn;
            lineRenderer.enabled = drawn >= 2;

            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            if (isActiveAndEnabled) fadeRoutine = StartCoroutine(FadeInLine(fadeInDuration));
        }

        /// <summary>Ẩn đường bay và xoá toàn bộ điểm đang vẽ.</summary>
        public void Hide()
        {
            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }

            if (lineRenderer == null) return;

            lineRenderer.positionCount = 0;
            lineRenderer.enabled = false;
        }

        /// <summary>Tăng dần alpha của đường vẽ từ 0 lên màu gốc trong <paramref name="duration"/> giây.</summary>
        private IEnumerator FadeInLine(float duration)
        {
            if (lineMaterial == null || duration <= 0f) yield break;

            float currentTime = 0f;
            Color color = initialColor;

            while (currentTime < duration)
            {
                currentTime += Time.deltaTime;
                color.a = initialColor.a * Mathf.Clamp01(currentTime / duration);
                lineMaterial.color = color;
                yield return null;
            }

            lineMaterial.color = initialColor;
            fadeRoutine = null;
        }
    }
}
