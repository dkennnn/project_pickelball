using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Hút các hạt của một <see cref="ParticleSystem"/> về phía <see cref="target"/>.
    /// Dùng cho hiệu ứng "coin bay về ví" / "gem bay về túi" sau khi nhận thưởng.
    /// <para>
    /// Bản gốc dùng <c>Coffee.UIExtensions.UIParticleAttractor</c> (package bên thứ ba).
    /// Bản này tự tính bằng <see cref="ParticleSystem.GetParticles(ParticleSystem.Particle[])"/> /
    /// <see cref="ParticleSystem.SetParticles(ParticleSystem.Particle[], int)"/> nên không cần package nào.
    /// </para>
    /// <para>Thiếu particle hoặc thiếu target thì component im lặng không làm gì.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public class ParticleWithAttractor : MonoBehaviour
    {
        [Tooltip("Loại hạt (chỉ để phân loại/log, không ảnh hưởng tính toán).")]
        [SerializeField] private ParticleType particleType = ParticleType.None;

        [Tooltip("Hệ hạt cần hút. Bỏ trống thì tự tìm trên GameObject này hoặc trong con.")]
        [SerializeField] private ParticleSystem particle;

        [Tooltip("Đích hút. Bỏ trống thì component không làm gì.")]
        [SerializeField] private Transform target;

        [Tooltip("Tốc độ hút: phần đường đi được mỗi giây (1 = tới đích trong ~1 giây).")]
        [SerializeField] private float attractSpeed = 4f;

        [Tooltip("Khoảng cách coi như đã tới đích; hạt sẽ bị kết liễu.")]
        [SerializeField] private float catchDistance = 0.15f;

        [Tooltip("Chỉ bắt đầu hút sau khi hạt đã sống được ngần này giây (cho hạt bung ra trước).")]
        [SerializeField] private float attractDelay = 0.2f;

        private ParticleSystem.Particle[] buffer;

        /// <summary>Loại hạt được cấu hình cho hệ này.</summary>
        public ParticleType Kind => particleType;

        /// <summary>Đích hút hiện tại.</summary>
        public Transform Target => target;

        private void Awake()
        {
            if (particle == null) particle = GetComponent<ParticleSystem>();
            if (particle == null) particle = GetComponentInChildren<ParticleSystem>(true);
        }

        /// <summary>Đặt đích hút mới.</summary>
        /// <param name="newTarget">Transform đích; null = tắt hút.</param>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        /// <summary>Phát hệ hạt và hút về <paramref name="attractTarget"/>.</summary>
        /// <param name="attractTarget">Đích hút; null thì giữ nguyên đích đang có.</param>
        public void PlayParticleWithAttractor(Transform attractTarget)
        {
            if (attractTarget != null) target = attractTarget;
            if (particle == null) return;

            particle.Clear(true);
            particle.Play(true);
        }

        /// <summary>Dừng phát và xoá sạch hạt đang sống.</summary>
        public void StopParticle()
        {
            if (particle == null) return;
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void LateUpdate()
        {
            if (particle == null || target == null) return;

            int alive = particle.particleCount;
            if (alive == 0) return;

            EnsureBuffer();
            int count = particle.GetParticles(buffer);
            if (count == 0) return;

            // Hạt được lưu theo không gian mô phỏng của hệ — quy đổi đích cho khớp.
            ParticleSystemSimulationSpace space = particle.main.simulationSpace;
            Vector3 targetPos = space == ParticleSystemSimulationSpace.Local
                ? particle.transform.InverseTransformPoint(target.position)
                : target.position;

            float dt = Time.deltaTime;
            float step = Mathf.Clamp01(attractSpeed * dt);

            for (int i = 0; i < count; i++)
            {
                ParticleSystem.Particle p = buffer[i];

                float age = p.startLifetime - p.remainingLifetime;
                if (age < attractDelay) continue;

                Vector3 pos = Vector3.Lerp(p.position, targetPos, step);
                if (Vector3.Distance(pos, targetPos) <= catchDistance)
                {
                    // Tới đích: kết liễu hạt ngay frame này.
                    p.remainingLifetime = 0f;
                    p.position = targetPos;
                }
                else
                {
                    p.position = pos;
                    // Triệt tiêu vận tốc riêng để hạt không "kéo lê" ngược hướng hút.
                    p.velocity = Vector3.Lerp(p.velocity, Vector3.zero, step);
                }

                buffer[i] = p;
            }

            particle.SetParticles(buffer, count);
        }

        private void EnsureBuffer()
        {
            int needed = particle.main.maxParticles;
            if (needed <= 0) needed = 32;
            if (buffer == null || buffer.Length < needed) buffer = new ParticleSystem.Particle[needed];
        }
    }
}
