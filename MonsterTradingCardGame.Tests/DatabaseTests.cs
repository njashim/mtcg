using Newtonsoft.Json;
using System.Reflection;

namespace MonsterTradingCardGame.Tests
{
    [TestFixture]
    public class DatabaseTests
    {
        private Database database;

        [SetUp]
        public void Setup()
        {
            database = new Database();
        }

        [Test]
        public void DeleteDB_ShouldNotThrowException()
        {
            // Arrange & Act & Assert
            Assert.DoesNotThrow(() => database.DeleteDB());
        }

        [Test]
        public void SetupDB_ShouldNotThrowException()
        {
            // Arrange & Act & Assert
            Assert.DoesNotThrow(() => database.SetupDB());
        }

        [Test]
        public void UserExist_WhenUserDoesNotExist_ShouldReturnFalse()
        {
            // Arrange
            string nonExistingUsername = "NonExistingUser";

            // Act
            bool userExists = database.UserExist(nonExistingUsername);

            // Assert
            Assert.IsFalse(userExists);
        }

        [Test]
        public void RegisterUser_WhenValidUsernameAndPassword_ShouldAddUserToDatabase()
        {
            // Arrange
            string username = "TestUser";
            string password = "TestPassword";

            // Act
            database.RegisterUser(username, password);

            // Assert
            Assert.IsTrue(database.UserExist(username));
        }

        [Test]
        public void LoginUser_WhenValidUsernameAndPassword_ShouldReturnTrue()
        {
            // Arrange
            string username = "TestUser";
            string password = "TestPassword";
            database.RegisterUser(username, password);

            // Act
            bool loginSuccessful = database.LoginUser(username, password);

            // Assert
            Assert.IsTrue(loginSuccessful);
        }

        [Test]
        public void LoginUser_WhenInvalidUsernameAndPassword_ShouldReturnFalse()
        {
            // Arrange
            string username = "TestUser";
            string password = "TestPassword";
            database.RegisterUser(username, password);

            // Act
            bool loginSuccessful = database.LoginUser(username, "WrongPassword");

            // Assert
            Assert.IsFalse(loginSuccessful);
        }

        [Test]
        public void SaveToken_ShouldNotThrowException()
        {
            // Arrange
            string username = "TestUser";
            string token = "TestToken";

            // Act & Assert
            Assert.DoesNotThrow(() => database.SaveToken(username, token));
        }

        [Test]
        public void GetTokenByUsername_ShouldReturnTokenForExistingUser()
        {
            // Arrange
            string username = "TestUser";
            string password = "TestPassword";
            database.RegisterUser(username, password);
            database.LoginUser(username, password);

            string token = "TestUser-mtcgToken";
            database.SaveToken(username, token);

            // Act
            string retrievedToken = database.GetTokenByUsername(username);

            // Assert
            Assert.AreEqual(token, retrievedToken);
        }

        [Test]
        public void TokenExist_ShouldReturnFalseForNonExistingToken()
        {
            // Arrange
            string nonExistingToken = "NonExistingToken";

            // Act
            bool tokenExists = database.TokenExist(nonExistingToken);

            // Assert
            Assert.IsFalse(tokenExists);
        }

        [Test]
        public void CreatePack_ShouldReturnValidPackID()
        {
            // Act
            int packID = database.CreatePack();

            // Assert
            Assert.IsTrue(packID > 0);
        }

        [Test]
        public void CreateCard_ShouldNotThrowException()
        {
            // Arrange
            string cardID = "TestCardID";
            string name = "Test Card";
            decimal damage = 10.5m;
            int packID = database.CreatePack();

            // Act & Assert
            Assert.DoesNotThrow(() => database.CreateCard(cardID, name, damage, packID));
        }

        [Test]
        public void CardExist_ShouldReturnTrueForExistingCard()
        {
            // Arrange
            string cardID = "TestCardID";
            int packID = database.CreatePack();
            database.CreateCard(cardID, "Test Card", 10.5m, packID);

            // Act
            bool cardExists = database.CardExist(cardID);

            // Assert
            Assert.IsTrue(cardExists);
        }

        [Test]
        public void CardExist_ShouldReturnFalseForNonExistingCard()
        {
            // Act
            bool cardExists = database.CardExist("NonExistingCardID");

            // Assert
            Assert.IsFalse(cardExists);
        }

        [Test]
        public void GetCoinsByUsername_ShouldReturnZeroForNonExistingUser()
        {
            // Act
            int coins = database.GetCoinsByUsername("NonExistingUser");

            // Assert
            Assert.AreEqual(0, coins);
        }

        [Test]
        public void PackAvailable_ShouldReturnTrueForAvailablePack()
        {
            int packID = database.CreatePack();
            
            // Act
            bool packAvailable = database.PackAvailable();

            // Assert
            Assert.IsTrue(packAvailable);
        }

        [TearDown]
        public void TearDown()
        {
            database.DeleteDB();
        }
    }
}