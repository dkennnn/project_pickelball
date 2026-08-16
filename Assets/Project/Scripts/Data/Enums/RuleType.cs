namespace Pickleball
{
    /// <summary>Các lỗi luật pickleball mà RuleEngine phát hiện.</summary>
    public enum RuleType
    {
        None = 0,
        ServeInNet = 1,
        ServeNotInArea = 2,
        ServeVolley = 3,
        DoubleBounceOnSide = 4,
        VolleyInsideKitchen = 5,
        BounceOutOfCourt = 6,
        WrongPlayerTurn = 7,
        ServeTimeout = 8
    }
}
