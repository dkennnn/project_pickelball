using UnityEngine;

namespace Pickleball
{
    /// <summary>Vợt: đóng góp chỉ số (trọng số 0.2) và quyết định mô hình vợt cầm trong trận.</summary>
    [CreateAssetMenu(fileName = "NewPaddle", menuName = "ScriptableObjects/Shop/Paddle")]
    public class Paddle : Item
    {
        /// <summary>Prefab mô hình vợt được gắn vào tay nhân vật.</summary>
        public GameObject paddlePrefab;
    }
}
