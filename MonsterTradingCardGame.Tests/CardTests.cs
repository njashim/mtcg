using Newtonsoft.Json;
using System.Reflection;

namespace MonsterTradingCardGame.Tests
{
    [TestFixture]
    public class CardTests
    {
        [Test]
        public void Card_SetterAndGetters_ShouldNotBeNull()
        {
            // Arrange
            Card card = new Card();

            // Act
            card.ID = "card123";
            card.Name = "Fire Dragon";
            card.Damage = 20.5m;
            card.Element = "Fire";
            card.Type = "Monster";
            card.UserID = 1;
            card.PackID = 2;

            // Assert
            Assert.IsNotNull(card.ID);
            Assert.IsNotNull(card.Name);
            Assert.IsNotNull(card.Element);
            Assert.IsNotNull(card.Type);
        }

        [Test]
        public void Card_ToString_ShouldNotBeNull()
        {
            // Arrange
            Card card = new Card
            {
                ID = "card123",
                Name = "Fire Dragon",
                Damage = 20.5m,
                Element = "Fire"
            };

            // Act
            string cardString = card.ToString();

            // Assert
            Assert.IsNotNull(cardString);
            Assert.IsTrue(cardString.Contains("card123"));
            Assert.IsTrue(cardString.Contains("Fire Dragon"));
            Assert.IsTrue(cardString.Contains("20.5"));
        }

        [Test]
        public void Card_Initialization_DefaultConstructor()
        {
            // Arrange
            Card card = new Card();

            // Assert
            Assert.IsNotNull(card);
            Assert.IsNull(card.ID);
            Assert.IsNull(card.Name);
            Assert.AreEqual(0, card.Damage);
            Assert.IsNull(card.Element);
            Assert.IsNull(card.Type);
            Assert.AreEqual(0, card.UserID);
            Assert.AreEqual(0, card.PackID);
        }

        [Test]
        public void Card_Initialization_ParameterizedConstructor()
        {
            // Arrange
            Card card = new Card{
                ID ="card123", 
                Name = "Fire Dragon", 
                Damage = 20.5m, 
                Element = "Fire", 
                Type = "Monster"
            };

            // Assert
            Assert.IsNotNull(card);
            Assert.AreEqual("card123", card.ID);
            Assert.AreEqual("Fire Dragon", card.Name);
            Assert.AreEqual(20.5m, card.Damage);
            Assert.AreEqual("Fire", card.Element);
            Assert.AreEqual("Monster", card.Type);
        }
    }
}