using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Kho tên hiển thị cho các tay vợt AI, kèm bộ sinh "username kiểu game thủ".
    /// <para>
    /// Danh sách <see cref="names"/> là các tên trung tính (không gợi giới tính, không trùng nhân vật
    /// có thật) để dùng cho đối thủ máy, bảng xếp hạng giả lập và các slot chờ người chơi.
    /// </para>
    /// <para>
    /// <see cref="GetRandomName"/> là API duy nhất mà gameplay nên gọi; nó tự quyết định trả về tên
    /// thường hay username đã trang trí dựa theo các công tắc trên Inspector.
    /// </para>
    /// </summary>
    [CreateAssetMenu(fileName = "AINamesData", menuName = "ScriptableObjects/AI NamesData")]
    public class AINamesData : ScriptableObject
    {
        /// <summary>Danh sách tên gốc (trung tính) dùng làm nền cho mọi username.</summary>
        public List<string> names = new List<string>
        {
            "Alto", "Amber", "Arlo", "Aspen", "Auri", "Bay",
            "Bexley", "Blaze", "Bodhi", "Brio", "Cadence", "Calen",
            "Cedar", "Cielo", "Coby", "Cove", "Dara", "Delta",
            "Dune", "Eero", "Elm", "Ember", "Ezra", "Fable",
            "Fenn", "Flint", "Foxx", "Gale", "Halo", "Haven",
            "Indigo", "Iver", "Jade", "Juno", "Kai", "Kestrel",
            "Lark", "Lennox", "Linden", "Lyric", "Marlo", "Merrit",
            "Nico", "Nyx", "Onyx", "Orin", "Pike", "Quill",
            "Rain", "Reef", "Riven", "Sable", "Sage", "Shiloh",
            "Sky", "Sol", "Tamsin", "Teagan", "Vale", "Wren",
            "Zephyr", "Zuri"
        };

        /// <summary>Cho phép gắn thêm chữ số vào cuối tên (kiểu "Kai97").</summary>
        [Header("Username Generation Settings")]
        public bool addNumbersToNames = true;

        /// <summary>Tỉ lệ phần trăm tên được gắn thêm chữ số.</summary>
        [Range(0, 100)]
        public int numberPercentage = 40;

        /// <summary>Cho phép chèn dấu ngăn cách ("_", ".", "-") giữa các thành phần của username.</summary>
        public bool addSeparators = true;

        /// <summary>Cho phép sinh username kiểu game thủ (có tiền tố/hậu tố).</summary>
        public bool useGamingStyle = true;

        /// <summary>Tỉ lệ phần trăm tên được chuyển thành username kiểu game thủ.</summary>
        [Range(0, 100)]
        public int gamingStylePercentage = 35;

        /// <summary>Tiền tố kiểu game thủ.</summary>
        private readonly string[] gamingPrefixes =
        {
            "Pro", "Xx", "Mr", "King", "Ace", "Smash", "Dink", "Volley", "Rally", "Iron", "Neo", "The"
        };

        /// <summary>Hậu tố kiểu game thủ.</summary>
        private readonly string[] gamingSuffixes =
        {
            "Pro", "xX", "King", "Ace", "Smash", "Dink", "Volley", "Rally", "99", "007", "_TV", "HD", "GG", "Star"
        };

        /// <summary>Các dấu ngăn cách có thể dùng khi <see cref="addSeparators"/> bật.</summary>
        private readonly string[] separators = { "_", ".", "-" };

        /// <summary>Tên dự phòng khi <see cref="names"/> rỗng.</summary>
        private const string FallbackName = "Player";

        /// <summary>
        /// Lấy ngẫu nhiên một tên hiển thị cho AI.
        /// <para>
        /// Với xác suất <see cref="gamingStylePercentage"/>% (khi <see cref="useGamingStyle"/> bật) sẽ
        /// trả về một username đã trang trí; ngược lại trả về tên gốc, có thể kèm chữ số nếu
        /// <see cref="addNumbersToNames"/> bật.
        /// </para>
        /// </summary>
        /// <returns>Tên hiển thị, không bao giờ rỗng.</returns>
        public string GetRandomName()
        {
            if (useGamingStyle && Random.Range(0, 100) < gamingStylePercentage) return GenerateUsername();

            string baseName = GetBaseName();

            if (addNumbersToNames && Random.Range(0, 100) < numberPercentage)
            {
                baseName += GetRandomNumber();
            }

            return baseName;
        }

        /// <summary>
        /// Sinh username kiểu game thủ: tên gốc + tiền tố và/hoặc hậu tố, có thể kèm dấu ngăn cách
        /// và chữ số. Luôn bảo đảm có ít nhất một thành phần trang trí.
        /// </summary>
        private string GenerateUsername()
        {
            string baseName = GetBaseName();

            bool usePrefix = gamingPrefixes.Length > 0 && Random.value < 0.5f;
            bool useSuffix = gamingSuffixes.Length > 0 && (!usePrefix || Random.value < 0.5f);

            string separator = addSeparators && Random.value < 0.5f
                ? separators[Random.Range(0, separators.Length)]
                : string.Empty;

            StringBuilder builder = new StringBuilder();

            if (usePrefix)
            {
                builder.Append(gamingPrefixes[Random.Range(0, gamingPrefixes.Length)]);
                builder.Append(separator);
            }

            builder.Append(baseName);

            if (useSuffix)
            {
                builder.Append(separator);
                builder.Append(gamingSuffixes[Random.Range(0, gamingSuffixes.Length)]);
            }

            if (addNumbersToNames && Random.Range(0, 100) < numberPercentage) builder.Append(GetRandomNumber());

            return builder.ToString();
        }

        /// <summary>Bốc một tên gốc trong <see cref="names"/>; trả về tên dự phòng nếu danh sách rỗng.</summary>
        private string GetBaseName()
        {
            if (names == null || names.Count == 0) return FallbackName;

            string picked = names[Random.Range(0, names.Count)];
            return string.IsNullOrWhiteSpace(picked) ? FallbackName : picked.Trim();
        }

        /// <summary>Chuỗi số ngẫu nhiên 2 hoặc 3 chữ số để gắn vào tên.</summary>
        private static string GetRandomNumber()
        {
            return Random.value < 0.5f
                ? Random.Range(10, 100).ToString()
                : Random.Range(100, 1000).ToString();
        }
    }
}
