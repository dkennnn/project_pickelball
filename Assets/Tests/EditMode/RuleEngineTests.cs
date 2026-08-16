using NUnit.Framework;
using Pickleball;
using UnityEngine;

namespace Pickleball.Tests
{
    /// <summary>
    /// Kiểm chứng hình học sân và bộ luật pickleball.
    ///
    /// <para>Nhắc lại quy ước hệ toạ độ được test bám theo:</para>
    /// <list type="bullet">
    /// <item><description>Lưới tại <c>z = 0</c>; positive court là <c>z &gt; 0</c>, negative court là <c>z &lt; 0</c>.</description></item>
    /// <item><description>Sân 6.096 × 13.4112 ⇒ <c>halfWidth = 3.048</c>, <c>halfLength = 6.7056</c>.</description></item>
    /// <item><description>Kitchen: <c>|z| &lt; 2.1336</c>.</description></item>
    /// <item><description>Ô giao: positive + Right ⇒ <c>x ∈ [0, 3.048]</c>; negative + Right ⇒ <c>x ∈ [-3.048, 0]</c>.</description></item>
    /// </list>
    /// </summary>
    public class RuleEngineTests
    {
        private const float HalfWidth = 3.048f;
        private const float HalfLength = 6.7056f;
        private const float KitchenDepth = 2.1336f;

        private GameObject courtObject;
        private Court court;
        private RuleEngine ruleEngine;

        [SetUp]
        public void SetUp()
        {
            courtObject = new GameObject("Court");
            court = courtObject.AddComponent<Court>();
            court.courtDimentions = new Vector2(6.096f, 13.4112f);
            court.kitchenDepth = KitchenDepth;
            court.RecalculateDimentions();

            ruleEngine = new RuleEngine(court);
        }

        [TearDown]
        public void TearDown()
        {
            if (courtObject != null) Object.DestroyImmediate(courtObject);
        }

        // ------------------------------------------------------------------
        // Giao bóng
        // ------------------------------------------------------------------

        [Test]
        public void Serve_LandingInDiagonalServeBox_IsValid()
        {
            // Người giao ở positive court, ô Right (x > 0).
            // Ô giao chéo là negative court + Right ⇒ x ∈ [-3.048, 0], z ∈ [-6.7056, -2.1336].
            Vector3 landing = new Vector3(-1.5f, 0f, -4.4f);

            Assert.IsTrue(court.IsServeValid(landing, true, ServeSide.Right));
            Assert.IsTrue(ruleEngine.CheckServeValidity(landing, true, ServeSide.Right));
        }

        [Test]
        public void Serve_LandingOnServerOwnHalf_IsInvalid()
        {
            // Rơi cùng phía với người giao (positive court) ⇒ không hợp lệ.
            Vector3 landing = new Vector3(1.5f, 0f, 4.4f);

            Assert.IsFalse(court.IsServeValid(landing, true, ServeSide.Right));
            Assert.IsFalse(ruleEngine.CheckServeValidity(landing, true, ServeSide.Right));
        }

        [Test]
        public void Serve_LandingInReceiverKitchen_IsInvalid()
        {
            // Đúng nửa sân người nhận nhưng nằm trong kitchen (|z| < 2.1336) ⇒ lỗi giao bóng.
            Vector3 landing = new Vector3(-1.5f, 0f, -1.0f);

            Assert.IsTrue(court.IsInKitchen(landing), "Điểm test phải nằm trong kitchen.");
            Assert.IsFalse(court.IsServeValid(landing, true, ServeSide.Right));
            Assert.IsFalse(ruleEngine.CheckServeValidity(landing, true, ServeSide.Right));
        }

        [Test]
        public void Serve_LandingInWrongServeBoxOfCorrectHalf_IsInvalid()
        {
            // Đúng nửa sân người nhận, ngoài kitchen, nhưng sai ô theo trục X
            // (negative + Right nằm ở x < 0, còn điểm này ở x > 0).
            Vector3 landing = new Vector3(1.5f, 0f, -4.4f);

            Assert.IsFalse(court.IsServeValid(landing, true, ServeSide.Right));
        }

        [Test]
        public void Serve_FromNegativeCourt_LandsInPositiveRightBox()
        {
            // Người giao ở negative court, ô Right (x < 0) ⇒ ô chéo là positive + Right (x > 0).
            Vector3 landing = new Vector3(1.5f, 0f, 4.4f);

            Assert.IsTrue(court.IsServeValid(landing, false, ServeSide.Right));
            Assert.IsFalse(court.IsServeValid(new Vector3(-1.5f, 0f, 4.4f), false, ServeSide.Right));
        }

        // ------------------------------------------------------------------
        // Kitchen
        // ------------------------------------------------------------------

        [Test]
        public void IsInKitchen_TrueInsideBand_FalseOutside()
        {
            Assert.IsTrue(court.IsInKitchen(new Vector3(0f, 0f, 0f)));
            Assert.IsTrue(court.IsInKitchen(new Vector3(1f, 0f, KitchenDepth - 0.01f)));
            Assert.IsTrue(court.IsInKitchen(new Vector3(-1f, 0f, -(KitchenDepth - 0.01f))));

            Assert.IsFalse(court.IsInKitchen(new Vector3(0f, 0f, KitchenDepth + 0.01f)));
            Assert.IsFalse(court.IsInKitchen(new Vector3(0f, 0f, -(KitchenDepth + 0.01f))));
            Assert.IsFalse(court.IsInKitchen(new Vector3(0f, 0f, 3f)));

            // Ngoài biên ngang thì không tính là kitchen dù |z| nhỏ.
            Assert.IsFalse(court.IsInKitchen(new Vector3(HalfWidth + 0.5f, 0f, 0.5f)));
        }

        [Test]
        public void IsVolleyInKitchen_MatchesCourtCheck()
        {
            Assert.IsTrue(ruleEngine.IsVolleyInKitchen(new Vector3(0f, 0f, 1f)));
            Assert.IsFalse(ruleEngine.IsVolleyInKitchen(new Vector3(0f, 0f, 5f)));
        }

        // ------------------------------------------------------------------
        // Two-bounce rule
        // ------------------------------------------------------------------

        [Test]
        public void IsDoubleBounceOnGround_TrueAtTwo_FalseAtOne()
        {
            Assert.IsTrue(ruleEngine.IsDoubleBounceOnGround(2));
            Assert.IsTrue(ruleEngine.IsDoubleBounceOnGround(3));
            Assert.IsFalse(ruleEngine.IsDoubleBounceOnGround(1));
            Assert.IsFalse(ruleEngine.IsDoubleBounceOnGround(0));
        }

        [Test]
        public void IsDoubleBounceRuleViolated_RequiresOneBounceOnEachSide()
        {
            Assert.IsTrue(ruleEngine.IsDoubleBounceRuleViolated(0, 1));
            Assert.IsTrue(ruleEngine.IsDoubleBounceRuleViolated(1, 0));
            Assert.IsFalse(ruleEngine.IsDoubleBounceRuleViolated(1, 1));
        }

        [Test]
        public void CheckVolleyValidity_FalseWhenBallHasNotBounced()
        {
            Vector3 outsideKitchen = new Vector3(0f, 0f, 5f);

            Assert.IsFalse(ruleEngine.CheckVolleyValidity(outsideKitchen, false));
            Assert.IsTrue(ruleEngine.CheckVolleyValidity(outsideKitchen, true));

            // Đã thoả two-bounce rule nhưng đứng trong kitchen ⇒ vẫn không được volley.
            Assert.IsFalse(ruleEngine.CheckVolleyValidity(new Vector3(0f, 0f, 1f), true));
        }

        // ------------------------------------------------------------------
        // Biên sân
        // ------------------------------------------------------------------

        [Test]
        public void IsBounceInCorrectArea_FalseWhenOutsideSideline()
        {
            // x = 4 > halfWidth = 3.048 ⇒ ngoài biên.
            Vector3 outside = new Vector3(4f, 0f, 3f);

            Assert.IsFalse(court.IsBounceInCorrectArea(outside, true));
            Assert.IsFalse(ruleEngine.CheckBounceInCorrectArea(outside, true));
            Assert.IsFalse(court.IsInsideCourt(outside));
        }

        [Test]
        public void IsBounceInCorrectArea_FalseWhenBeyondBaseline()
        {
            Vector3 outside = new Vector3(0f, 0f, HalfLength + 0.5f);

            Assert.IsFalse(court.IsBounceInCorrectArea(outside, true));
        }

        [Test]
        public void IsBounceInCorrectArea_FalseWhenOnWrongHalf()
        {
            Vector3 negativeHalfPoint = new Vector3(0f, 0f, -3f);

            Assert.IsTrue(court.IsBounceInCorrectArea(negativeHalfPoint, false));
            Assert.IsFalse(court.IsBounceInCorrectArea(negativeHalfPoint, true));
        }

        // ------------------------------------------------------------------
        // EvaluateBounce
        // ------------------------------------------------------------------

        [Test]
        public void EvaluateBounce_ReturnsExpectedRuleTypes()
        {
            // Người đánh ở positive court, bóng rơi hợp lệ ở negative court, lần nảy đầu tiên.
            Assert.AreEqual(RuleType.None, ruleEngine.EvaluateBounce(new Vector3(0f, 0f, -3f), true, 1));

            // Nảy lần thứ hai trên cùng một bên.
            Assert.AreEqual(RuleType.DoubleBounceOnSide, ruleEngine.EvaluateBounce(new Vector3(0f, 0f, -3f), true, 2));

            // Bóng ra ngoài biên.
            Assert.AreEqual(RuleType.BounceOutOfCourt, ruleEngine.EvaluateBounce(new Vector3(4f, 0f, -3f), true, 1));

            // Bóng rơi lại nửa sân của chính người đánh.
            Assert.AreEqual(RuleType.BounceOutOfCourt, ruleEngine.EvaluateBounce(new Vector3(0f, 0f, 3f), true, 1));
        }

        // ------------------------------------------------------------------
        // Vùng sân
        // ------------------------------------------------------------------

        [Test]
        public void GetServeBoxBounds_UsesMirroredXConvention()
        {
            CourtBounds positiveRight = court.GetServeBoxBounds(true, ServeSide.Right);
            CourtBounds negativeRight = court.GetServeBoxBounds(false, ServeSide.Right);

            Assert.Greater(positiveRight.center.x, 0f, "positive + Right phải nằm ở x > 0");
            Assert.Less(negativeRight.center.x, 0f, "negative + Right phải nằm ở x < 0");
            Assert.Greater(positiveRight.center.z, 0f);
            Assert.Less(negativeRight.center.z, 0f);

            // Ô giao bắt đầu ngay sau vạch kitchen và kết thúc tại baseline.
            Assert.AreEqual(KitchenDepth, positiveRight.MinZ, 0.001f);
            Assert.AreEqual(HalfLength, positiveRight.MaxZ, 0.001f);
        }

        [Test]
        public void GetServeHorizontalClamp_MatchesServeBox()
        {
            Vector2 clamp = court.GetServeHorizontalClamp(true, ServeSide.Right);

            Assert.AreEqual(0f, clamp.x, 0.001f);
            Assert.AreEqual(HalfWidth, clamp.y, 0.001f);
        }

        [Test]
        public void GetServePosition_IsBehindBaselineAndCenteredOnBox()
        {
            Vector3 servePosition = court.GetServePosition(true, ServeSide.Right);

            Assert.Greater(servePosition.z, HalfLength, "Người giao phải đứng sau baseline.");
            Assert.AreEqual(court.GetServeBoxBounds(true, ServeSide.Right).center.x, servePosition.x, 0.001f);
        }

        [Test]
        public void GetKitchenAndHalfCourtBounds_CoverExpectedRanges()
        {
            CourtBounds kitchen = court.GetKitchenBounds(true);
            Assert.AreEqual(0f, kitchen.MinZ, 0.001f);
            Assert.AreEqual(KitchenDepth, kitchen.MaxZ, 0.001f);

            CourtBounds half = court.GetHalfCourtBounds(false);
            Assert.AreEqual(-HalfLength, half.MinZ, 0.001f);
            Assert.AreEqual(0f, half.MaxZ, 0.001f);
            Assert.IsTrue(half.Contains(new Vector3(0f, 0f, -3f)));
            Assert.IsFalse(half.Contains(new Vector3(0f, 0f, 3f)));
        }
    }
}
