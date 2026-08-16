using System.Collections.Generic;
using StarterKit.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pickleball
{
    /// <summary>
    /// Bộ dự đoán quỹ đạo bóng bằng cách chạy vật lý thật trong một
    /// <b>physics scene phụ</b> hoàn toàn tách biệt khỏi scene gameplay.
    ///
    /// <para><b>Vì sao phải làm vậy</b>: công thức ném xiên giải tích không mô tả được lực xoáy
    /// (swing), lực cản, hệ số nảy của PhysicMaterial và hình học lưới. Chạy chính engine vật lý
    /// trên một bản sao của sân cho kết quả trùng khớp tuyệt đối với quả bóng thật, miễn là
    /// <see cref="SimulationBall"/> có cùng thông số Rigidbody/Collider với bóng thật và lực xoáy
    /// được cộng theo ĐÚNG cách <see cref="BallController.ApplySwingEffect"/> làm.</para>
    ///
    /// <para><b>Quy ước bắt buộc</b>: mỗi bước mô phỏng dài đúng <see cref="Time.fixedDeltaTime"/>,
    /// và lực swing được cộng TRƯỚC <c>Simulate</c> — giống hệt coroutine
    /// <c>ApplySwingEffect</c> cộng lực trong <c>FixedUpdate</c>. Lệch một trong hai điểm này là
    /// đường dự đoán sẽ trôi khỏi đường bay thật.</para>
    /// </summary>
    public class PhysicsTrajectoryHandler : Singleton<PhysicsTrajectoryHandler>
    {
        [Header("Court Colliders")]
        [Tooltip("Các object của sân cần nhân bản sang physics scene phụ: mặt sân, lưới, tường biên. " +
                 "Chỉ collider được giữ lại; renderer và mọi script khác đều bị gỡ bỏ.")]
        [SerializeField] private List<Transform> _CollidersInScene = new List<Transform>();

        [Header("Simulation")]
        [Tooltip("Số bước mô phỏng tối đa cho một lần dự đoán — chốt chặn an toàn chống treo game.")]
        [SerializeField] private int maxSimulationSteps = 1200;

        private Scene _simulationScene;
        private PhysicsScene _physicsScene;

        /// <summary>Instance bóng mô phỏng dùng lại cho mọi cú đánh (pool 1 phần tử).</summary>
        private SimulationBall _pooledBall;

        /// <summary>Prefab đã sinh ra <see cref="_pooledBall"/> — đổi prefab thì tạo lại instance.</summary>
        private SimulationBall _pooledPrefab;

        /// <summary>True khi physics scene phụ đã dựng xong và sẵn sàng nhận yêu cầu mô phỏng.</summary>
        public bool IsReady { get; private set; }

        protected override void OnAwake()
        {
            base.OnAwake();
            CreatePhysicsScene();
        }

        /// <summary>
        /// Dựng scene vật lý phụ và nhân bản các collider của sân vào đó.
        /// Gọi một lần duy nhất trong <c>Awake</c>; gọi lại sẽ bị bỏ qua.
        /// </summary>
        private void CreatePhysicsScene()
        {
            if (IsReady) return;

            _simulationScene = SceneManager.CreateScene(
                "PhysicsSimulation",
                new CreateSceneParameters(LocalPhysicsMode.Physics3D));
            _physicsScene = _simulationScene.GetPhysicsScene();

            for (int i = 0; i < _CollidersInScene.Count; i++)
            {
                Transform source = _CollidersInScene[i];
                if (source == null) continue;

                GameObject ghost = Instantiate(source.gameObject, source.position, source.rotation);
                ghost.transform.localScale = source.lossyScale;
                StripToCollidersOnly(ghost);
                SceneManager.MoveGameObjectToScene(ghost, _simulationScene);
            }

            IsReady = true;
        }

        /// <summary>
        /// Gỡ mọi thứ không phải collider khỏi bản sao: script gameplay (tránh bản sao của
        /// <see cref="PickleNet"/> gọi ngược vào <c>GameManager</c>), renderer, particle, audio, light.
        /// </summary>
        private static void StripToCollidersOnly(GameObject root)
        {
            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++) Destroy(behaviours[i]);

            ParticleSystem[] particles = root.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particles.Length; i++) Destroy(particles[i]);

            AudioSource[] audios = root.GetComponentsInChildren<AudioSource>(true);
            for (int i = 0; i < audios.Length; i++) Destroy(audios[i]);

            Light[] lights = root.GetComponentsInChildren<Light>(true);
            for (int i = 0; i < lights.Length; i++) Destroy(lights[i]);

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++) renderers[i].enabled = false;
        }

        /// <summary>
        /// Lấy (hoặc tạo lần đầu) quả bóng mô phỏng dùng chung. Không Instantiate/Destroy mỗi cú đánh
        /// vì việc đó sinh rác GC ngay giữa pha bóng.
        /// </summary>
        private SimulationBall GetSimulationBall(SimulationBall ballPrefab)
        {
            if (ballPrefab == null) return null;

            if (_pooledBall == null || _pooledPrefab != ballPrefab)
            {
                if (_pooledBall != null) Destroy(_pooledBall.gameObject);

                _pooledBall = Instantiate(ballPrefab);
                _pooledPrefab = ballPrefab;

                Renderer[] renderers = _pooledBall.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++) renderers[i].enabled = false;

                TrailRenderer[] trails = _pooledBall.GetComponentsInChildren<TrailRenderer>(true);
                for (int i = 0; i < trails.Length; i++) trails[i].enabled = false;

                SceneManager.MoveGameObjectToScene(_pooledBall.gameObject, _simulationScene);
            }

            _pooledBall.gameObject.SetActive(true);
            _pooledBall.ResetBall();
            return _pooledBall;
        }

        /// <summary>
        /// Mô phỏng quỹ đạo và trả về toàn bộ các mẫu vị trí theo thời gian.
        /// </summary>
        /// <param name="points">Kết quả: danh sách mẫu quỹ đạo, phần tử đầu là vị trí xuất phát tại t = 0.</param>
        /// <param name="ballPrefab">Prefab <see cref="SimulationBall"/> có cùng thông số vật lý với bóng thật.</param>
        /// <param name="position">Vị trí xuất phát (world).</param>
        /// <param name="rotation">Hướng xuất phát.</param>
        /// <param name="velocity">Vận tốc tuyến tính ban đầu (m/s).</param>
        /// <param name="angularVelocity">Vận tốc góc ban đầu (rad/s).</param>
        /// <param name="torque">Mô-men xoáy cộng một lần lúc đánh (ForceMode.Force).</param>
        /// <param name="swingspinDirection">Hướng lực bên của cú xoáy (đã chuẩn hoá, mặt phẳng XZ).</param>
        /// <param name="swingFactor">Độ lớn lực bên (Newton) — phải bằng biên độ swing của cú đánh thật.</param>
        /// <param name="swingDuration">Số giây đầu tiên còn cộng lực bên.</param>
        /// <param name="swingSpinIntensity">Hệ số nhân cường độ xoáy [0..1].</param>
        /// <param name="maxTimeForTrajectory">Độ dài tối đa của quỹ đạo cần mô phỏng (giây).</param>
        public void SimulateTrajectory(out List<TrajectoryPoint> points, SimulationBall ballPrefab,
            Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity, Vector3 torque,
            Vector3 swingspinDirection, float swingFactor, float swingDuration, float swingSpinIntensity,
            float maxTimeForTrajectory = 5f)
        {
            points = new List<TrajectoryPoint>();
            RunSimulation(points, null, ballPrefab, position, rotation, velocity, angularVelocity, torque,
                swingspinDirection, swingFactor, swingDuration, swingSpinIntensity, maxTimeForTrajectory);
        }

        /// <summary>
        /// Mô phỏng quỹ đạo và tách thành hai đoạn: tới lần nảy thứ nhất, và từ nảy 1 tới nảy 2.
        /// Đây là overload mà <see cref="BallController"/> dùng để dựng <see cref="ShotTrajetoryInfo"/>.
        /// </summary>
        /// <param name="firstBouncePoints">Kết quả: đoạn từ lúc đánh tới lần chạm đất thứ nhất.</param>
        /// <param name="secondBouncePoints">Kết quả: đoạn từ lần chạm đất thứ nhất tới lần thứ hai (rỗng nếu không có).</param>
        /// <param name="ballPrefab">Prefab bóng mô phỏng.</param>
        /// <param name="position">Vị trí xuất phát (world).</param>
        /// <param name="rotation">Hướng xuất phát.</param>
        /// <param name="velocity">Vận tốc tuyến tính ban đầu.</param>
        /// <param name="angularVelocity">Vận tốc góc ban đầu.</param>
        /// <param name="torque">Mô-men xoáy cộng một lần lúc đánh.</param>
        /// <param name="swingspinDirection">Hướng lực bên của cú xoáy.</param>
        /// <param name="swingFactor">Độ lớn lực bên (Newton).</param>
        /// <param name="swingDuration">Số giây đầu còn cộng lực bên.</param>
        /// <param name="swingSpinIntensity">Hệ số nhân cường độ xoáy.</param>
        /// <param name="maxTimeForTrajectory">Độ dài tối đa cần mô phỏng (giây).</param>
        public void SimulateTrajectory(out List<TrajectoryPoint> firstBouncePoints,
            out List<TrajectoryPoint> secondBouncePoints, SimulationBall ballPrefab,
            Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity, Vector3 torque,
            Vector3 swingspinDirection, float swingFactor, float swingDuration, float swingSpinIntensity,
            float maxTimeForTrajectory = 5f)
        {
            firstBouncePoints = new List<TrajectoryPoint>();
            secondBouncePoints = new List<TrajectoryPoint>();

            List<TrajectoryPoint> allPoints = new List<TrajectoryPoint>();
            List<SimulationBallCollisionData> collisions = new List<SimulationBallCollisionData>();

            RunSimulation(allPoints, collisions, ballPrefab, position, rotation, velocity, angularVelocity, torque,
                swingspinDirection, swingFactor, swingDuration, swingSpinIntensity, maxTimeForTrajectory);

            float firstBounceTime = GetGroundBounceTime(collisions, 1);
            float secondBounceTime = GetGroundBounceTime(collisions, 2);

            for (int i = 0; i < allPoints.Count; i++)
            {
                TrajectoryPoint p = allPoints[i];

                if (firstBounceTime < 0f || p.time <= firstBounceTime)
                {
                    firstBouncePoints.Add(p);
                    continue;
                }

                if (secondBounceTime < 0f || p.time <= secondBounceTime) secondBouncePoints.Add(p);
                else break;
            }
        }

        /// <summary>
        /// Mô phỏng quỹ đạo và trả về đồng thời toàn bộ mẫu vị trí lẫn danh sách va chạm thô.
        /// Dùng cho AI khi cần biết bóng có chạm lưới / rơi ngoài sân hay không.
        /// </summary>
        /// <param name="allPoints">Kết quả: toàn bộ mẫu quỹ đạo.</param>
        /// <param name="simulationBallBounceDatas">Kết quả: mọi va chạm ghi được kèm thời điểm.</param>
        /// <param name="ballPrefab">Prefab bóng mô phỏng.</param>
        /// <param name="position">Vị trí xuất phát (world).</param>
        /// <param name="rotation">Hướng xuất phát.</param>
        /// <param name="velocity">Vận tốc tuyến tính ban đầu.</param>
        /// <param name="angularVelocity">Vận tốc góc ban đầu.</param>
        /// <param name="torque">Mô-men xoáy cộng một lần lúc đánh.</param>
        /// <param name="swingspinDirection">Hướng lực bên của cú xoáy.</param>
        /// <param name="swingFactor">Độ lớn lực bên (Newton).</param>
        /// <param name="swingDuration">Số giây đầu còn cộng lực bên.</param>
        /// <param name="swingSpinIntensity">Hệ số nhân cường độ xoáy.</param>
        /// <param name="maxTimeForTrajectory">Độ dài tối đa cần mô phỏng (giây).</param>
        public void SimulateTrajectory(out List<TrajectoryPoint> allPoints,
            out List<SimulationBallCollisionData> simulationBallBounceDatas, SimulationBall ballPrefab,
            Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity, Vector3 torque,
            Vector3 swingspinDirection, float swingFactor, float swingDuration, float swingSpinIntensity,
            float maxTimeForTrajectory = 5f)
        {
            allPoints = new List<TrajectoryPoint>();
            simulationBallBounceDatas = new List<SimulationBallCollisionData>();

            RunSimulation(allPoints, simulationBallBounceDatas, ballPrefab, position, rotation, velocity,
                angularVelocity, torque, swingspinDirection, swingFactor, swingDuration, swingSpinIntensity,
                maxTimeForTrajectory);
        }

        /// <summary>
        /// Lõi mô phỏng dùng chung cho cả ba overload.
        /// </summary>
        private void RunSimulation(List<TrajectoryPoint> points, List<SimulationBallCollisionData> collisions,
            SimulationBall ballPrefab, Vector3 position, Quaternion rotation, Vector3 velocity,
            Vector3 angularVelocity, Vector3 torque, Vector3 swingspinDirection, float swingFactor,
            float swingDuration, float swingSpinIntensity, float maxTimeForTrajectory)
        {
            points?.Clear();
            collisions?.Clear();

            if (!IsReady) CreatePhysicsScene();

            SimulationBall ball = GetSimulationBall(ballPrefab);
            if (ball == null)
            {
                Debug.LogWarning("[PhysicsTrajectoryHandler] Thiếu SimulationBall prefab, không thể dự đoán quỹ đạo.");
                return;
            }

            ball.SimulateHitBall(position, rotation, velocity, angularVelocity, torque);
            points?.Add(new TrajectoryPoint(position, 0f));

            float step = Time.fixedDeltaTime;
            if (step <= 0f) step = 0.02f;

            int stepCount = Mathf.Min(Mathf.CeilToInt(maxTimeForTrajectory / step), maxSimulationSteps);

            // Lực swing chỉ có tác dụng khi CẢ hướng lẫn cường độ đều khác 0.
            Vector3 swingForce = swingspinDirection * (swingFactor * swingSpinIntensity);
            bool hasSwing = swingForce.sqrMagnitude > Mathf.Epsilon && swingDuration > 0f;

            float elapsed = 0f;
            Transform ballTransform = ball.transform;
            Rigidbody ballBody = ball.rb;

            for (int i = 0; i < stepCount; i++)
            {
                // Cộng lực bên TRƯỚC bước mô phỏng — khớp với ApplySwingEffect cộng lực ở FixedUpdate.
                if (hasSwing && elapsed < swingDuration) ballBody.AddForce(swingForce, ForceMode.Force);

                elapsed += step;
                ball.SimulatedTime = elapsed;
                _physicsScene.Simulate(step);

                points?.Add(new TrajectoryPoint(ballTransform.position, elapsed));
            }

            if (collisions != null) collisions.AddRange(ball.Collisions);

            // Cất bóng đi nhưng GIỮ instance để tái sử dụng cho cú đánh sau.
            ball.ResetBall();
            ball.gameObject.SetActive(false);
        }

        /// <summary>Thời điểm của lần chạm đất thứ <paramref name="ordinal"/>; -1 nếu không có.</summary>
        private static float GetGroundBounceTime(List<SimulationBallCollisionData> collisions, int ordinal)
        {
            if (collisions == null) return -1f;

            int found = 0;
            for (int i = 0; i < collisions.Count; i++)
            {
                if (!collisions[i].isGround) continue;

                found++;
                if (found == ordinal) return collisions[i].time;
            }

            return -1f;
        }

        protected override void OnDestroy()
        {
            if (_pooledBall != null)
            {
                Destroy(_pooledBall.gameObject);
                _pooledBall = null;
                _pooledPrefab = null;
            }

            if (IsReady && _simulationScene.IsValid() && _simulationScene.isLoaded)
            {
                SceneManager.UnloadSceneAsync(_simulationScene);
            }

            IsReady = false;
            base.OnDestroy();
        }
    }
}
