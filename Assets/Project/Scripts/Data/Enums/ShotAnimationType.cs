namespace Pickleball
{
    /// <summary>Clip animation tương ứng với cú đánh (mỗi loại có cặp Pre_* + hit).</summary>
    public enum ShotAnimationType
    {
        None = 0, RightSideShot = 1, LeftSideShot = 2, HeadUpVolley = 3, Serve = 4,
        DownRightSideShot = 5, DownLeftSideShot = 6, RightVolley = 7, LeftVolley = 8
    }
}
