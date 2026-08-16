using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Điều khiển hệ hạt "gió" trong sân. Đây là hiệu ứng CHỈ THỊ GIÁC — nó không tác động
    /// vào vật lý quả bóng (mọi tính toán quỹ đạo vẫn nằm trong <see cref="BallController"/>).
    /// Mọi hàm đều no-op an toàn khi <see cref="windParticles"/> chưa được gán.
    /// </summary>
    public class WindController : MonoBehaviour
    {
        /// <summary>Hệ hạt biểu diễn luồng gió.</summary>
        [Header("Wind Particles")]
        [SerializeField] private ParticleSystem windParticles;

        /// <summary>Tốc độ hạt (m/s) ứng với <c>strength</c> = 1.</summary>
        [SerializeField] private float speedPerStrength = 5f;

        /// <summary>Lượng hạt phát ra mỗi giây ứng với <c>strength</c> = 1.</summary>
        [SerializeField] private float emissionPerStrength = 30f;

        private Vector3 currentDirection;
        private float currentStrength;

        /// <summary>Hướng gió đang hiển thị (đã chuẩn hoá); <see cref="Vector3.zero"/> nếu đang tắt.</summary>
        public Vector3 CurrentDirection => currentDirection;

        /// <summary>Cường độ gió đang hiển thị; 0 nếu đang tắt.</summary>
        public float CurrentStrength => currentStrength;

        /// <summary>
        /// Bật gió theo một hướng với cường độ cho trước. Hướng bằng 0 hoặc cường độ &lt;= 0
        /// được coi như lệnh tắt gió.
        /// </summary>
        /// <param name="direction">Hướng thổi (world space, không cần chuẩn hoá).</param>
        /// <param name="strength">Cường độ gió; 1 tương ứng cấu hình chuẩn của hệ hạt.</param>
        public void SetWind(Vector3 direction, float strength)
        {
            if (strength <= 0f || direction.sqrMagnitude <= Mathf.Epsilon)
            {
                StopWind();
                return;
            }

            currentDirection = direction.normalized;
            currentStrength = strength;

            if (windParticles == null) return;

            windParticles.transform.rotation = Quaternion.LookRotation(currentDirection, Vector3.up);

            ParticleSystem.MainModule main = windParticles.main;
            main.startSpeed = speedPerStrength * strength;

            ParticleSystem.EmissionModule emission = windParticles.emission;
            emission.rateOverTime = emissionPerStrength * strength;

            if (!windParticles.isPlaying) windParticles.Play(true);
        }

        /// <summary>Tắt gió và xoá sạch hạt đang bay.</summary>
        public void StopWind()
        {
            currentDirection = Vector3.zero;
            currentStrength = 0f;

            if (windParticles == null) return;
            windParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
