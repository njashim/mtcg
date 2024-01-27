using Newtonsoft.Json;
using System.Reflection;

namespace MonsterTradingCardGame.Tests
{
    [TestFixture]
    public class TradingTests
    {
        [Test]
        public void Trading_ID_SetAndGet()
        {
            // Arrange
            var trading = new Trading();

            // Act
            trading.ID = "id1234";

            // Assert
            Assert.AreEqual("id1234", trading.ID);
        }

        [Test]
        public void Trading_TraderID_SetAndGet()
        {
            // Arrange
            var trading = new Trading();

            // Act
            trading.TraderID = 6;

            // Assert
            Assert.AreEqual(6, trading.TraderID);
        }

        [Test]
        public void Trading_CardToTrade_SetAndGet()
        {
            // Arrange
            var trading = new Trading();

            // Act
            trading.CardToTrade = "cardid1234";

            // Assert
            Assert.AreEqual("cardid1234", trading.CardToTrade);
        }

        [Test]
        public void Trading_Type_SetAndGet()
        {
            // Arrange
            var trading = new Trading();

            // Act
            trading.Type = "monster";

            // Assert
            Assert.AreEqual("monster", trading.Type);
        }

        [Test]
        public void Trading_MinimumDamage_SetAndGet()
        {
            // Arrange
            var trading = new Trading();

            // Act
            trading.MinimumDamage = 10.5m;

            // Assert
            Assert.AreEqual(10.5m, trading.MinimumDamage);
        }

        [Test]
        public void Trading_IsClosed_SetAndGet()
        {
            // Arrange
            var trading = new Trading();

            // Act
            trading.IsClosed = true;

            // Assert
            Assert.IsTrue(trading.IsClosed);
        }

        [Test]
        public void Trading_DefaultValues()
        {
            // Arrange
            var trading = new Trading();

            // Assert
            Assert.IsNull(trading.ID);
            Assert.AreEqual(0, trading.TraderID);
            Assert.IsNull(trading.CardToTrade);
            Assert.IsNull(trading.Type);
            Assert.AreEqual(0m, trading.MinimumDamage);
            Assert.IsFalse(trading.IsClosed);
        }

        [Test]
        public void Trading_WithValuesConstructor()
        {
            // Arrange
            var trading = new Trading
            {
                ID = "id1234",
                TraderID = 2,
                CardToTrade = "cardid1234",
                Type = "monster",
                MinimumDamage = 5.5m,
                IsClosed = true
            };

            // Assert
            Assert.AreEqual("id1234", trading.ID);
            Assert.AreEqual(2, trading.TraderID);
            Assert.AreEqual("cardid1234", trading.CardToTrade);
            Assert.AreEqual("monster", trading.Type);
            Assert.AreEqual(5.5m, trading.MinimumDamage);
            Assert.IsTrue(trading.IsClosed);
        }
    }
}