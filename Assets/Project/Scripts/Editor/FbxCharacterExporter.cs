using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Formats.Fbx.Exporter;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Pickleball.EditorTools
{
    /// <summary>
    /// Xuất nhân vật (mesh + xương) và các animation clip ra FBX để dùng lại ở engine khác
    /// (bản Cocos Creator).
    /// <para>
    /// Mesh và clip của bản gốc đang ở định dạng nhị phân riêng của Unity (<c>.asset</c> /
    /// <c>.anim</c>), engine khác không đọc được. FBX là định dạng trung gian mà cả Unity lẫn
    /// Cocos đều hiểu.
    /// </para>
    /// <para>
    /// Xuất ĐÚNG MỘT file: bộ xuất FBX tự gom mọi <c>AnimationClip</c> mà nó tìm thấy qua
    /// <c>Animator</c> của nhân vật, nên một file đã chứa đủ 25 clip (trong đó có cả 17 clip
    /// cần cho lối chơi) cùng 65 xương và 4 mesh. Bản đầu tiên xuất mỗi clip một file và cho ra
    /// 18 file giống hệt nhau, tốn 437 MB mà không thêm thông tin gì — đã bỏ cách đó.
    /// </para>
    /// <para>
    /// Chạy <c>Pickleball/Export/Verify Exported FBX</c> sau khi xuất để ĐẾM thật xem trong file
    /// có bao nhiêu clip và bao nhiêu xương. Dung lượng file không chứng minh được gì: một FBX
    /// thiếu animation trông y hệt một FBX đầy đủ.
    /// </para>
    /// </summary>
    public static class FbxCharacterExporter
    {
        private const string CharacterPrefabPath = "Assets/Project/Prefabs/Characters/Player_Male.prefab";
        private const string OutputFolder = "Assets/Project/ExportedFBX";

        /// <summary>
        /// Các clip mà lối chơi cần tới. Bộ hậu tố <c>_v3</c> là bộ mà
        /// <c>PlayerAnimationControllerv3</c> thật sự dùng; các clip không hậu tố là bản cũ còn
        /// sót lại trong build gốc.
        /// <para>
        /// Danh sách này KHÔNG điều khiển việc xuất (bộ xuất tự gom hết clip nó thấy) — nó được
        /// ghi vào báo cáo để đối chiếu với kết quả của <c>FbxExportVerifier</c>.
        /// </para>
        /// </summary>
        private static readonly string[] GameplayClips =
        {
            "Idle_v3",
            "Forward_Run_v3", "Backward_Run_v3", "Left_Run_v3", "Right_Run_v3",
            "Backward_Left_Run_v3", "Backward_Right_Run_v3",
            "Pre_Left_Hit_v3", "Pre_Right_Hit_v3", "Pre_Left_Down_Hit_v3", "Pre_Right_Down_Hit_v3",
            "Left_Hit_v3", "Right_Hit_v3", "Left_Down_Hit_v3", "Right_Down_Hit_v3",
            "Serve_v3",
            "Victory",
        };

        [MenuItem("Pickleball/Export/Character to FBX")]
        public static void ExportAll()
        {
            string absoluteOutput = Path.GetFullPath(OutputFolder);
            Directory.CreateDirectory(absoluteOutput);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[FbxCharacterExporter] Không tìm thấy prefab nhân vật tại {CharacterPrefabPath}.");
                return;
            }

            var report = new List<string>();
            int meshOk = ExportModel(prefab, absoluteOutput, report) ? 1 : 0;

            report.Add(string.Empty);
            report.Add($"Lối chơi cần {GameplayClips.Length} clip — đối chiếu với verify_report.md:");
            foreach (string clip in GameplayClips) report.Add("  - " + clip);

            AssetDatabase.Refresh();

            string reportPath = Path.Combine(absoluteOutput, "export_report.txt");
            File.WriteAllText(reportPath, string.Join(Environment.NewLine, report));

            Debug.Log($"[FbxCharacterExporter] Xong: {meshOk} file FBX tại {OutputFolder}. " +
                      $"Báo cáo: {reportPath}. Chạy 'Verify Exported FBX' để đếm clip/xương bên trong.");
        }

        /// <summary>Xuất nhân vật kèm bộ xương, chưa có animation.</summary>
        private static bool ExportModel(GameObject prefab, string outputFolder, List<string> report)
        {
            GameObject instance = Object.Instantiate(prefab);
            try
            {
                StripGameplayScripts(instance);
                instance.name = "PickleballCharacter";

                string path = Path.Combine(outputFolder, "PickleballCharacter.fbx");
                string result = ModelExporter.ExportObject(path, instance);

                bool ok = !string.IsNullOrEmpty(result) && File.Exists(path);
                report.Add(ok
                    ? $"MODEL OK  PickleballCharacter.fbx  ({new FileInfo(path).Length / 1024} KB)"
                    : "MODEL LỖI  PickleballCharacter.fbx");
                return ok;
            }
            catch (Exception e)
            {
                report.Add($"MODEL LỖI  {e.Message}");
                return false;
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        /// <summary>
        /// Gỡ toàn bộ script gameplay khỏi bản sao trước khi xuất.
        /// <para>
        /// Không gỡ thì bộ xuất FBX phải đi qua các <c>MonoBehaviour</c> có tham chiếu tới
        /// singleton và tài nguyên chưa khởi tạo — vừa vô nghĩa với FBX (định dạng này chỉ chứa
        /// hình học, xương và animation), vừa dễ ném lỗi giữa chừng.
        /// </para>
        /// </summary>
        private static void StripGameplayScripts(GameObject root)
        {
            foreach (MonoBehaviour script in root.GetComponentsInChildren<MonoBehaviour>(true).Reverse())
            {
                if (script != null) Object.DestroyImmediate(script);
            }
        }
    }
}
