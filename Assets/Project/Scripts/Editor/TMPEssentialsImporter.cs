using System.IO;
using UnityEditor;
using UnityEngine;

namespace Pickleball.EditorTools
{
    /// <summary>
    /// Import "TMP Essential Resources" bằng lệnh, chạy được cả trong batchmode.
    /// <para>
    /// Bình thường phải vào <c>Window &gt; TextMeshPro &gt; Import TMP Essential Resources</c> bằng tay.
    /// Nhưng nếu chạy <c>UILayoutImporter</c> khi chưa có bộ resource này thì 337 component
    /// TextMeshProUGUI sẽ được serialize với <c>font = null</c>, và **không tự khỏi** khi import
    /// TMP về sau — phải gán lại font cho từng cái. Vì vậy bước này bắt buộc chạy TRƯỚC.
    /// </para>
    /// </summary>
    public static class TMPEssentialsImporter
    {
        private const string MarkerFolder = "Assets/TextMesh Pro";
        private const string PackageRelativePath = "Package Resources/TMP Essential Resources.unitypackage";

        /// <summary>True nếu bộ resource đã nằm trong Assets.</summary>
        public static bool IsImported => AssetDatabase.IsValidFolder(MarkerFolder);

        [MenuItem("Pickleball/Import TMP Essential Resources")]
        public static void Import()
        {
            if (IsImported)
            {
                Debug.Log("[TMPEssentialsImporter] TMP Essential Resources đã có sẵn, bỏ qua.");
                return;
            }

            string packagePath = FindPackage();
            if (string.IsNullOrEmpty(packagePath))
            {
                Debug.LogError("[TMPEssentialsImporter] Không tìm thấy 'TMP Essential Resources.unitypackage' " +
                               "trong PackageCache của com.unity.ugui. Hãy import tay qua " +
                               "Window > TextMeshPro > Import TMP Essential Resources.");
                return;
            }

            Debug.Log($"[TMPEssentialsImporter] Đang import {packagePath}");
            AssetDatabase.ImportPackage(packagePath, false); // false = không hiện dialog
            AssetDatabase.Refresh();
            Debug.Log("[TMPEssentialsImporter] Xong. Kiểm tra thư mục Assets/TextMesh Pro.");
        }

        private static string FindPackage()
        {
            string cache = Path.Combine(Directory.GetCurrentDirectory(), "Library", "PackageCache");
            if (!Directory.Exists(cache)) return null;

            foreach (string dir in Directory.GetDirectories(cache, "com.unity.ugui*"))
            {
                string candidate = Path.Combine(dir, PackageRelativePath);
                if (File.Exists(candidate)) return candidate.Replace('\\', '/');
            }
            return null;
        }
    }
}
