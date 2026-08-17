using System.IO;
using UnityEditor;
using UnityEngine;

namespace Pickleball.EditorTools
{
    /// <summary>
    /// Sinh các asset lẻ chưa thuộc generator nào và nối chúng vào <see cref="GameData"/>.
    /// Tách riêng để không phải sửa các generator đã được đối chiếu số liệu.
    /// </summary>
    public static class MiscAssetGenerator
    {
        private const string GameFolder = "Assets/Project/ScriptableObjects/Game";
        private const string NamesPath = GameFolder + "/AINamesData.asset";
        private const string GameDataPath = GameFolder + "/GameData.asset";

        [MenuItem("Pickleball/Generate Misc Assets")]
        public static void GenerateAll()
        {
            EnsureFolder(GameFolder);

            AINamesData names = AssetDatabase.LoadAssetAtPath<AINamesData>(NamesPath);
            if (names == null)
            {
                // Danh sách tên là field initializer trong class, nên instance mới đã có sẵn dữ liệu.
                names = ScriptableObject.CreateInstance<AINamesData>();
                AssetDatabase.CreateAsset(names, NamesPath);
                Debug.Log($"[MiscAssetGenerator] Đã tạo {NamesPath} ({names.names.Count} tên).");
            }

            GameData gameData = AssetDatabase.LoadAssetAtPath<GameData>(GameDataPath);
            if (gameData == null)
            {
                Debug.LogWarning("[MiscAssetGenerator] Chưa có GameData.asset — chạy 'Pickleball/Generate Item Data' trước.");
                return;
            }

            if (gameData.namesData == null)
            {
                gameData.namesData = names;
                EditorUtility.SetDirty(gameData);
                Debug.Log("[MiscAssetGenerator] Đã nối AINamesData vào GameData.");
            }

            EditorUtility.SetDirty(names);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf)) return;
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
