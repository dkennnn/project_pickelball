using TMPro;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Hiện <see cref="Application.version"/> lên một <see cref="TMP_Text"/>, kèm tiền tố/hậu tố
    /// cấu hình được. Dùng ở góc màn Settings / Main Menu.
    /// <para>Không có <see cref="TMP_Text"/> thì component im lặng không làm gì.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public class VersionNumber : MonoBehaviour
    {
        [Tooltip("Text hiển thị. Bỏ trống thì tự lấy TMP_Text trên GameObject này hoặc trong con.")]
        [SerializeField] private TMP_Text label;

        [Tooltip("Chuỗi ghép trước số phiên bản.")]
        [SerializeField] private string prefix = "v";

        [Tooltip("Chuỗi ghép sau số phiên bản.")]
        [SerializeField] private string suffix = string.Empty;

        [Tooltip("Ghép thêm mã build (Application.buildGUID rút gọn) sau số phiên bản.")]
        [SerializeField] private bool showPlatform;

        /// <summary>Chuỗi phiên bản đang hiển thị.</summary>
        public string DisplayedVersion { get; private set; } = string.Empty;

        private void Awake()
        {
            if (label == null) label = GetComponent<TMP_Text>();
            if (label == null) label = GetComponentInChildren<TMP_Text>(true);
        }

        private void OnEnable()
        {
            Refresh();
        }

        /// <summary>Ghi lại chuỗi phiên bản lên text. Gọi lại được bất cứ lúc nào.</summary>
        public void Refresh()
        {
            if (label == null) return;

            string version = Application.version;
            if (string.IsNullOrEmpty(version)) version = "0.0.0";

            DisplayedVersion = prefix + version + suffix;
            if (showPlatform) DisplayedVersion += " (" + Application.platform + ")";

            label.text = DisplayedVersion;
        }
    }
}
