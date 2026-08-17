using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Pickleball
{
    /// <summary>
    /// Phơi bốn sự kiện chuột/chạm cơ bản ra Inspector dưới dạng <see cref="UnityEvent"/>.
    /// Dùng cho nút booster giữ-để-dùng ở GameplayUI, nút có phản hồi nhấn xuống/thả ra.
    /// <para>
    /// Bản gốc kế thừa <c>EventTrigger</c> của UGUI; ở đây implement thẳng các interface
    /// để không nuốt mất event của những component khác trên cùng node.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public class UIButtonEventTrigger : MonoBehaviour,
                                        IPointerDownHandler,
                                        IPointerUpHandler,
                                        IPointerEnterHandler,
                                        IPointerExitHandler
    {
        [Tooltip("Gọi khi ngón tay/chuột nhấn xuống trên node này.")]
        public UnityEvent onPointerDown = new UnityEvent();

        [Tooltip("Gọi khi ngón tay/chuột nhả ra.")]
        public UnityEvent onPointerUp = new UnityEvent();

        [Tooltip("Gọi khi con trỏ đi vào vùng node này.")]
        public UnityEvent onPointerEnter = new UnityEvent();

        [Tooltip("Gọi khi con trỏ rời khỏi vùng node này.")]
        public UnityEvent onPointerExit = new UnityEvent();

        [Tooltip("Bỏ qua mọi sự kiện khi tắt — tiện để khoá nút tạm thời.")]
        public bool interactable = true;

        /// <summary>True khi con trỏ đang nhấn giữ trên node này.</summary>
        public bool IsPressed { get; private set; }

        /// <summary>True khi con trỏ đang nằm trong vùng node này.</summary>
        public bool IsHovered { get; private set; }

        /// <inheritdoc/>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (!interactable) return;
            IsPressed = true;
            onPointerDown?.Invoke();
        }

        /// <inheritdoc/>
        public void OnPointerUp(PointerEventData eventData)
        {
            if (!interactable) return;
            IsPressed = false;
            onPointerUp?.Invoke();
        }

        /// <inheritdoc/>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!interactable) return;
            IsHovered = true;
            onPointerEnter?.Invoke();
        }

        /// <inheritdoc/>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (!interactable) return;
            IsHovered = false;
            onPointerExit?.Invoke();
        }

        private void OnDisable()
        {
            // Nút bị tắt giữa lúc đang giữ: phát nốt onPointerUp để bên nghe không kẹt ở trạng thái "đang giữ".
            if (!IsPressed)
            {
                IsHovered = false;
                return;
            }

            IsPressed = false;
            IsHovered = false;
            onPointerUp?.Invoke();
        }
    }
}
