using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Quản lý phần NHÌN của nhân vật: đổi mesh/material theo nhân vật đang chọn và
    /// đổi bên cầm vợt theo tay thuận.
    /// <para>
    /// Tay thuận được lưu trong SaveData nên avatar phải mirror được: nếu prefab có sẵn cặp
    /// điểm gắn trái/phải thì dùng thẳng; nếu chỉ có bên phải thì lật dấu trục X của
    /// <c>localPosition</c> để suy ra bên trái.
    /// </para>
    /// </summary>
    public class PlayerAvatar : MonoBehaviour
    {
        /// <summary>Renderer thân nhân vật (đổi mesh/material khi đổi skin).</summary>
        [Header("Character")]
        [SerializeField] private SkinnedMeshRenderer characterRenderer;

        /// <summary>Danh sách mesh của các nhân vật có thể chọn.</summary>
        [SerializeField] private Mesh[] characterMeshes;

        /// <summary>Danh sách material tương ứng với từng nhân vật.</summary>
        [SerializeField] private Material[] characterMaterials;

        /// <summary>Nút gắn vợt ở tay phải.</summary>
        [Header("Paddle Points")]
        [SerializeField] private Transform paddleHolderRight;

        /// <summary>Nút gắn vợt ở tay trái (bỏ trống thì sẽ mirror từ bên phải).</summary>
        [SerializeField] private Transform paddleHolderLeft;

        /// <summary>Điểm cầm bóng ở tay phải.</summary>
        [Header("Ball Points")]
        [SerializeField] private Transform ballHoldPointRight;

        /// <summary>Điểm cầm bóng ở tay trái (bỏ trống thì sẽ mirror từ bên phải).</summary>
        [SerializeField] private Transform ballHoldPointLeft;

        private HandSide currentHandSide = HandSide.Right;
        private int currentCharacterIndex = -1;

        /// <summary>Tay thuận đang áp dụng.</summary>
        public HandSide CurrentHandSide => currentHandSide;

        /// <summary>Chỉ số nhân vật đang hiển thị; <c>-1</c> nếu chưa áp dụng nhân vật nào.</summary>
        public int CurrentCharacterIndex => currentCharacterIndex;

        /// <summary>
        /// Đổi sang nhân vật theo chỉ số trong bộ mesh/material đã gán.
        /// Chỉ số ngoài dải sẽ bị bỏ qua.
        /// </summary>
        /// <param name="characterIndex">Chỉ số nhân vật.</param>
        public void ApplyCharacter(int characterIndex)
        {
            // TODO P10: lấy danh sách nhân vật (mesh, material, phụ kiện) từ Shop thay cho mảng gán tay.
            if (characterRenderer == null) return;

            if (characterMeshes != null && characterIndex >= 0 && characterIndex < characterMeshes.Length
                && characterMeshes[characterIndex] != null)
            {
                characterRenderer.sharedMesh = characterMeshes[characterIndex];
            }

            if (characterMaterials != null && characterIndex >= 0 && characterIndex < characterMaterials.Length
                && characterMaterials[characterIndex] != null)
            {
                characterRenderer.material = characterMaterials[characterIndex];
            }

            currentCharacterIndex = characterIndex;
        }

        /// <summary>
        /// Đổi tay thuận: chọn cặp điểm gắn vợt / cầm bóng tương ứng, và mirror vị trí
        /// nếu prefab chỉ có sẵn phiên bản tay phải.
        /// </summary>
        /// <param name="side">Tay thuận mới.</param>
        public void SetHandSide(HandSide side)
        {
            currentHandSide = side;

            if (paddleHolderLeft != null && paddleHolderRight != null)
            {
                paddleHolderRight.gameObject.SetActive(side == HandSide.Right);
                paddleHolderLeft.gameObject.SetActive(side == HandSide.Left);
            }
            else
            {
                MirrorLocalX(paddleHolderRight, side == HandSide.Left);
            }

            if (ballHoldPointLeft != null && ballHoldPointRight != null)
            {
                ballHoldPointRight.gameObject.SetActive(side == HandSide.Right);
                ballHoldPointLeft.gameObject.SetActive(side == HandSide.Left);
            }
            else
            {
                // Bóng cầm ở tay KHÔNG cầm vợt nên mirror ngược lại với vợt.
                MirrorLocalX(ballHoldPointRight, side == HandSide.Right);
            }
        }

        /// <summary>Nút gắn vợt tương ứng với tay thuận (trả về bên phải nếu chưa gán bên trái).</summary>
        /// <param name="playerHandSide">Tay thuận cần lấy.</param>
        public Transform GetPaddleHolder(HandSide playerHandSide)
        {
            if (playerHandSide == HandSide.Left && paddleHolderLeft != null) return paddleHolderLeft;
            return paddleHolderRight;
        }

        /// <summary>Điểm cầm bóng tương ứng với tay thuận (trả về bên phải nếu chưa gán bên trái).</summary>
        /// <param name="playerHandSide">Tay thuận cần lấy.</param>
        public Transform GetBallHoldPoint(HandSide playerHandSide)
        {
            if (playerHandSide == HandSide.Left && ballHoldPointLeft != null) return ballHoldPointLeft;
            return ballHoldPointRight;
        }

        /// <summary>Lật dấu trục X của <c>localPosition</c> để tạo phiên bản đối xứng.</summary>
        private static void MirrorLocalX(Transform target, bool mirrored)
        {
            if (target == null) return;

            Vector3 local = target.localPosition;
            float magnitude = Mathf.Abs(local.x);
            local.x = mirrored ? -magnitude : magnitude;
            target.localPosition = local;
        }
    }
}
