using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Pickleball
{
    /// <summary>
    /// Mã hoá/giải mã chuỗi bằng AES-256-CBC, khoá và IV được dẫn xuất từ mật khẩu
    /// qua <see cref="Rfc2898DeriveBytes"/> (PBKDF2) với salt cố định trong code.
    /// Kết quả trả về dạng Base64 để ghi thẳng ra file văn bản.
    /// <para>
    /// Mục tiêu ở đây là chống sửa file lưu bằng tay chứ không phải bảo mật cấp server:
    /// mật khẩu nằm trong build nên bất kỳ ai dịch ngược cũng lấy được. Vì IV cũng dẫn xuất
    /// từ mật khẩu nên cùng một nội dung sẽ cho cùng một chuỗi mã — chấp nhận được cho save cục bộ,
    /// và đổi lại file lưu không cần chỗ chứa IV riêng.
    /// </para>
    /// </summary>
    public static class DataEncoder
    {
        /// <summary>Salt cố định của game — đổi giá trị này sẽ làm mọi file lưu cũ không đọc được nữa.</summary>
        private static readonly byte[] Salt =
        {
            0x50, 0x69, 0x63, 0x6B, 0x6C, 0x65, 0x62, 0x61,
            0x6C, 0x6C, 0x2D, 0x53, 0x61, 0x76, 0x65, 0x21
        };

        /// <summary>Độ dài khoá AES (byte) — 32 byte = AES-256.</summary>
        private const int KeySizeInBytes = 32;

        /// <summary>Độ dài IV (byte) — luôn bằng kích thước khối AES.</summary>
        private const int IvSizeInBytes = 16;

        /// <summary>Số vòng lặp PBKDF2.</summary>
        private const int DerivationIterations = 10000;

        /// <summary>
        /// Mã hoá một chuỗi bằng mật khẩu cho trước.
        /// Chuỗi rỗng hoặc <c>null</c> cho ra chuỗi rỗng.
        /// </summary>
        /// <param name="plain">Nội dung gốc (UTF-8).</param>
        /// <param name="password">Mật khẩu dùng để dẫn xuất khoá.</param>
        /// <returns>Chuỗi Base64 của bản mã.</returns>
        public static string Encrypt(string plain, string password)
        {
            if (string.IsNullOrEmpty(plain)) return string.Empty;

            byte[] plainBytes = Encoding.UTF8.GetBytes(plain);

            using (Aes aes = CreateAes(password))
            using (ICryptoTransform encryptor = aes.CreateEncryptor())
            {
                byte[] cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                return Convert.ToBase64String(cipherBytes);
            }
        }

        /// <summary>
        /// Giải mã một chuỗi Base64 đã mã hoá bằng <see cref="Encrypt"/>.
        /// Ném ngoại lệ nếu dữ liệu hỏng hoặc sai mật khẩu — dùng
        /// <see cref="TryDecrypt"/> khi nguồn dữ liệu không đáng tin.
        /// </summary>
        /// <param name="cipher">Chuỗi Base64 của bản mã.</param>
        /// <param name="password">Mật khẩu đã dùng khi mã hoá.</param>
        /// <returns>Nội dung gốc.</returns>
        public static string Decrypt(string cipher, string password)
        {
            if (string.IsNullOrEmpty(cipher)) return string.Empty;

            byte[] cipherBytes = Convert.FromBase64String(cipher);

            using (Aes aes = CreateAes(password))
            using (ICryptoTransform decryptor = aes.CreateDecryptor())
            {
                byte[] plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                return Encoding.UTF8.GetString(plainBytes);
            }
        }

        /// <summary>
        /// Giải mã an toàn: trả về <c>false</c> thay vì ném ngoại lệ khi dữ liệu hỏng,
        /// không phải Base64, sai mật khẩu hoặc sai padding.
        /// Nhờ đó một file lưu hỏng chỉ khiến game rơi về mặc định chứ không làm crash.
        /// </summary>
        /// <param name="cipher">Chuỗi Base64 của bản mã.</param>
        /// <param name="password">Mật khẩu đã dùng khi mã hoá.</param>
        /// <param name="plain">Nội dung gốc khi thành công; chuỗi rỗng khi thất bại.</param>
        /// <returns>True nếu giải mã thành công.</returns>
        public static bool TryDecrypt(string cipher, string password, out string plain)
        {
            plain = string.Empty;
            if (string.IsNullOrEmpty(cipher)) return false;

            try
            {
                plain = Decrypt(cipher, password);
                return true;
            }
            catch (FormatException)
            {
                // Không phải Base64 hợp lệ.
                return false;
            }
            catch (CryptographicException)
            {
                // Sai mật khẩu, sai padding hoặc bản mã bị cắt cụt.
                return false;
            }
            catch (DecoderFallbackException)
            {
                // Giải mã ra chuỗi byte không phải UTF-8 hợp lệ.
                // (Phải bắt TRƯỚC ArgumentException vì đây là lớp con của nó.)
                return false;
            }
            catch (ArgumentException)
            {
                // Độ dài bản mã không hợp lệ.
                return false;
            }
            catch (IOException)
            {
                return false;
            }
        }

        /// <summary>Tạo đối tượng AES đã nạp khoá và IV dẫn xuất từ mật khẩu.</summary>
        /// <param name="password">Mật khẩu; <c>null</c> được coi như chuỗi rỗng.</param>
        private static Aes CreateAes(string password)
        {
            Aes aes = Aes.Create();
            try
            {
                aes.KeySize = KeySizeInBytes * 8;
                aes.BlockSize = IvSizeInBytes * 8;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (Rfc2898DeriveBytes derive =
                       new Rfc2898DeriveBytes(password ?? string.Empty, Salt, DerivationIterations))
                {
                    aes.Key = derive.GetBytes(KeySizeInBytes);
                    aes.IV = derive.GetBytes(IvSizeInBytes);
                }

                return aes;
            }
            catch
            {
                aes.Dispose();
                throw;
            }
        }
    }
}
