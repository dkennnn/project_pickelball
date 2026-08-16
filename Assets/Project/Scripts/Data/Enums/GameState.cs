namespace Pickleball
{
    /// <summary>Vòng đời một trận đấu.</summary>
    public enum GameState { PreServe = 0, Serving = 1, WaitingForServeResult = 2, InPlay = 3, PointScored = 4, GameOver = 5 }
}
