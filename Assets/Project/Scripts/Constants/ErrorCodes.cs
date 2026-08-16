using System.Collections.Generic;

namespace Pickleball
{
    /// <summary>Mã lỗi backend/gameplay và chuỗi hiển thị tương ứng.</summary>
    public enum ErrorCode
    {
        None = 0,
        NoInternet = 1001,
        ServerUnreachable = 1002,
        RequestTimeout = 1003,
        InvalidResponse = 1004,
        Unauthorized = 1005,
        Maintenance = 1006,
        NotEnoughCoins = 2001,
        NotEnoughGems = 2002,
        NotEnoughTazos = 2003,
        ItemAlreadyMaxLevel = 2004,
        SlotsFull = 2005,
        MatchmakingFailed = 3001,
        ForceUpdateRequired = 4001
    }

    public static class ErrorCodes
    {
        private static readonly Dictionary<ErrorCode, string> Keys = new Dictionary<ErrorCode, string>
        {
            { ErrorCode.NoInternet, "error_no_internet" },
            { ErrorCode.ServerUnreachable, "error_server_unreachable" },
            { ErrorCode.RequestTimeout, "error_request_timeout" },
            { ErrorCode.InvalidResponse, "error_invalid_response" },
            { ErrorCode.Unauthorized, "error_unauthorized" },
            { ErrorCode.Maintenance, "error_maintenance" },
            { ErrorCode.NotEnoughCoins, "error_not_enough_coins" },
            { ErrorCode.NotEnoughGems, "error_not_enough_gems" },
            { ErrorCode.NotEnoughTazos, "error_not_enough_tazos" },
            { ErrorCode.ItemAlreadyMaxLevel, "error_item_max_level" },
            { ErrorCode.SlotsFull, "error_slots_full" },
            { ErrorCode.MatchmakingFailed, "error_matchmaking_failed" },
            { ErrorCode.ForceUpdateRequired, "error_force_update" }
        };

        /// <summary>Trả về khoá localization của mã lỗi (fallback là "error_unknown").</summary>
        public static string GetLocalizationKey(ErrorCode code)
            => Keys.TryGetValue(code, out var key) ? key : "error_unknown";
    }
}
