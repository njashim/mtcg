using Newtonsoft.Json;
using System.Reflection;

namespace MonsterTradingCardGame.Tests
{
    [TestFixture]
    public class UserTests
    {
        [Test]
        public void User_Initialization_DefaultConstructor()
        {
            // Arrange
            User user = new User();

            // Assert
            Assert.IsNotNull(user);
            Assert.IsNull(user.Username);
            Assert.IsNull(user.Name);
            Assert.IsNull(user.Password);
            Assert.AreEqual(0, user.Coins);
            Assert.AreEqual(0, user.Elo);
            Assert.IsNull(user.Image);
            Assert.IsNull(user.Bio);
            Assert.AreEqual(0, user.GamesPlayed);
            Assert.AreEqual(0, user.Wins);
            Assert.AreEqual(0, user.Draws);
            Assert.AreEqual(0, user.Losses);
        }

        [Test]
        public void User_Initialization_ParameterizedConstructor()
        {
            // Arrange
            User user = new User
            {
                Username = "testUser",
                Name = "Test User",
                Password = "testPassword",
                Coins = 100,
                Elo = 1500,
                Image = "testImage.jpg",
                Bio = "Test bio",
                GamesPlayed = 5,
                Wins = 2,
                Draws = 1,
                Losses = 2
            };

            // Assert
            Assert.IsNotNull(user);
            Assert.AreEqual("testUser", user.Username);
            Assert.AreEqual("Test User", user.Name);
            Assert.AreEqual("testPassword", user.Password);
            Assert.AreEqual(100, user.Coins);
            Assert.AreEqual(1500, user.Elo);
            Assert.AreEqual("testImage.jpg", user.Image);
            Assert.AreEqual("Test bio", user.Bio);
            Assert.AreEqual(5, user.GamesPlayed);
            Assert.AreEqual(2, user.Wins);
            Assert.AreEqual(1, user.Draws);
            Assert.AreEqual(2, user.Losses);
        }

        [Test]
        public void User_SetterAndGetters_ShouldNotBeNull()
        {
            // Arrange
            User user = new User();

            // Act
            user.Username = "testUser";
            user.Name = "Test User";
            user.Password = "testPassword";
            user.Coins = 100;
            user.Elo = 1500;
            user.Image = "testImage.jpg";
            user.Bio = "Test bio";
            user.GamesPlayed = 5;
            user.Wins = 2;
            user.Draws = 1;
            user.Losses = 2;

            // Assert
            Assert.IsNotNull(user.Username);
            Assert.IsNotNull(user.Name);
            Assert.IsNotNull(user.Password);
            Assert.IsNotNull(user.Image);
            Assert.IsNotNull(user.Bio);
        }

        [Test]
        public void User_Coins_DefaultValue()
        {
            // Arrange
            User user = new User();

            // Assert
            Assert.AreEqual(0, user.Coins);
        }

        [Test]
        public void User_Elo_DefaultValue()
        {
            // Arrange
            User user = new User();

            // Assert
            Assert.AreEqual(0, user.Elo);
        }
    }
}