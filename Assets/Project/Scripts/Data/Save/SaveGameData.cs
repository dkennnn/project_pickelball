using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Tầng đọc/ghi file lưu: serialize sang JSON bằng Newtonsoft, mã hoá bằng
    /// <see cref="DataEncoder"/> rồi ghi ra một file duy nhất trong
    /// <see cref="Application.persistentDataPath"/>.
    /// <para>
    /// Ghi theo kiểu an toàn: nội dung mới luôn xuống file tạm <c>.tmp</c> trước rồi mới thay
    /// file thật, nên mất điện giữa chừng cũng không làm hỏng save cũ.
    /// </para>
    /// <para>
    /// <see cref="Load{T}"/> không bao giờ ném ngoại lệ: file thiếu, giải mã hỏng hay JSON sai
    /// đều rơi về <c>defaultData</c> kèm một cảnh báo nói rõ lý do.
    /// </para>
    /// </summary>
    public static class SaveGameData
    {
        /// <summary>Tên file lưu.</summary>
        private const string FileName = "pickleball.sav";

        /// <summary>Đuôi của file tạm dùng cho bước ghi an toàn.</summary>
        private const string TempSuffix = ".tmp";

        /// <summary>Đuôi của file sao lưu do <see cref="File.Replace(string,string,string)"/> sinh ra.</summary>
        private const string BackupSuffix = ".bak";

        /// <summary>Cấu hình serialize dùng chung: bỏ trường null, không xuống dòng cho gọn file.</summary>
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            TypeNameHandling = TypeNameHandling.None,
            Formatting = Formatting.None
        };

        /// <summary>Đường dẫn tuyệt đối của file lưu.</summary>
        public static string filePath
        {
            get { return Path.Combine(Application.persistentDataPath, FileName); }
        }

        /// <summary>True khi file lưu đã tồn tại trên đĩa.</summary>
        public static bool Exists
        {
            get
            {
                try { return File.Exists(filePath); }
                catch (Exception e)
                {
                    Debug.LogWarning("[SaveGameData] Không kiểm tra được file lưu: " + e.Message);
                    return false;
                }
            }
        }

        /// <summary>
        /// Ghi dữ liệu xuống đĩa: JSON → mã hoá → file tạm → thay file thật.
        /// Mọi lỗi I/O đều được nuốt và ghi cảnh báo, không làm gián đoạn game.
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu cần lưu.</typeparam>
        /// <param name="data">Dữ liệu cần lưu; bỏ qua nếu <c>null</c>.</param>
        /// <param name="password">Mật khẩu mã hoá.</param>
        public static void Save<T>(T data, string password)
        {
            if (data == null)
            {
                Debug.LogWarning("[SaveGameData] Bỏ qua Save vì dữ liệu rỗng.");
                return;
            }

            string path = filePath;
            string tempPath = path + TempSuffix;

            try
            {
                string json = JsonConvert.SerializeObject(data, Settings);
                string cipher = DataEncoder.Encrypt(json, password);

                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(tempPath, cipher);
                ReplaceFile(tempPath, path);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SaveGameData] Ghi file lưu thất bại: " + e.Message);
                TryDelete(tempPath);
            }
        }

        /// <summary>
        /// Đọc dữ liệu từ đĩa. Trả về <paramref name="defaultData"/> khi file không tồn tại,
        /// giải mã hỏng, JSON hỏng hoặc lỗi I/O — mỗi trường hợp đều kèm cảnh báo nêu rõ lý do.
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu cần đọc.</typeparam>
        /// <param name="defaultData">Giá trị trả về khi không đọc được.</param>
        /// <param name="password">Mật khẩu đã dùng khi mã hoá.</param>
        public static T Load<T>(T defaultData, string password)
        {
            string path = filePath;
            string cipher;

            try
            {
                if (!File.Exists(path))
                {
                    Debug.Log("[SaveGameData] Chưa có file lưu tại '" + path + "' — dùng dữ liệu mặc định.");
                    return defaultData;
                }

                cipher = File.ReadAllText(path);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SaveGameData] Đọc file lưu thất bại (" + e.Message + ") — dùng dữ liệu mặc định.");
                return defaultData;
            }

            if (string.IsNullOrEmpty(cipher))
            {
                Debug.LogWarning("[SaveGameData] File lưu rỗng — dùng dữ liệu mặc định.");
                return defaultData;
            }

            string json;
            if (!DataEncoder.TryDecrypt(cipher, password, out json))
            {
                Debug.LogWarning("[SaveGameData] Giải mã file lưu thất bại (sai mật khẩu hoặc dữ liệu hỏng) — dùng dữ liệu mặc định.");
                return defaultData;
            }

            try
            {
                T loaded = JsonConvert.DeserializeObject<T>(json, Settings);
                if (loaded == null)
                {
                    Debug.LogWarning("[SaveGameData] JSON giải mã ra rỗng — dùng dữ liệu mặc định.");
                    return defaultData;
                }
                return loaded;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SaveGameData] Phân tích JSON thất bại (" + e.Message + ") — dùng dữ liệu mặc định.");
                return defaultData;
            }
        }

        /// <summary>Xoá file lưu cùng mọi file tạm/sao lưu đi kèm. An toàn khi file không tồn tại.</summary>
        public static void Delete()
        {
            string path = filePath;
            TryDelete(path);
            TryDelete(path + TempSuffix);
            TryDelete(path + BackupSuffix);
        }

        // ------------------------------------------------------------------
        // Nội bộ
        // ------------------------------------------------------------------

        /// <summary>
        /// Thay file đích bằng file tạm theo kiểu nguyên tử nhất có thể trên nền tảng hiện tại.
        /// </summary>
        /// <param name="tempPath">File tạm đã ghi xong.</param>
        /// <param name="targetPath">File lưu thật.</param>
        private static void ReplaceFile(string tempPath, string targetPath)
        {
            if (!File.Exists(targetPath))
            {
                File.Move(tempPath, targetPath);
                return;
            }

            try
            {
                // File.Replace giữ lại bản cũ ở .bak nên vẫn còn đường lui nếu ghi hỏng.
                File.Replace(tempPath, targetPath, targetPath + BackupSuffix, true);
                TryDelete(targetPath + BackupSuffix);
            }
            catch (Exception e)
            {
                // Một số nền tảng (và một số hệ thống file) không hỗ trợ File.Replace.
                Debug.LogWarning("[SaveGameData] File.Replace không dùng được (" + e.Message + ") — chuyển sang xoá rồi đổi tên.");
                TryDelete(targetPath);
                File.Move(tempPath, targetPath);
            }
        }

        /// <summary>Xoá một file, bỏ qua mọi lỗi.</summary>
        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SaveGameData] Không xoá được '" + path + "': " + e.Message);
            }
        }
    }
}
