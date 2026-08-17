using UnityEngine;

namespace Pickleball
{
    /// <summary>Cán vợt: đóng góp chỉ số (trọng số 0.1) và quyết định vật liệu hiển thị của cán.</summary>
    [CreateAssetMenu(fileName = "NewGrip", menuName = "ScriptableObjects/Shop/Grip")]
    public class Grip : Item
    {
        /// <summary>Vật liệu áp lên phần cán của vợt khi vào trận.</summary>
        public Material gripMaterial;
    }
}
