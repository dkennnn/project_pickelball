using UnityEngine;

namespace StarterKit.UIKit
{
    /// <summary>
    /// Đánh dấu một node UI (và toàn bộ con của nó) được miễn trừ khỏi hiệu ứng làm xám
    /// của <see cref="GreyscaleUIHierarchy"/> — ví dụ nút Mua vẫn phải sáng khi thẻ item bị khoá.
    /// </summary>
    [DisallowMultipleComponent]
    public class IgnoreGreyscaleObject : MonoBehaviour
    {
    }
}
