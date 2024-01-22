using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Npgsql;
using System.Text.RegularExpressions;
using System.Drawing;

namespace MonsterTradingCardGame
{
    public class Database
    {
        private string connectionString = "Host=localhost;Port=5432;Username=postgres;Password=Pass2020!;Database=mtcgdb";
        private string query = "";

        public Database()
        {
            DeleteDB();
            SetupDB();
        }

        public void DeleteDB()
        {
            Console.WriteLine("Started deleting all tables from the database");
            this.query = """
                DROP TABLE IF EXISTS tradings;
                DROP TABLE IF EXISTS decks;
                DROP TABLE IF EXISTS cards;
                DROP TABLE IF EXISTS packs;
                DROP TABLE IF EXISTS usersessions;
                DROP TABLE IF EXISTS users;
                """;
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(this.query,connection))
                {
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error: " + ex.Message);
                    }
                }
                connection.Close();
            }
            Console.WriteLine("Finished deleting all tables from the database");
        }

        public void SetupDB()
        {
            Console.WriteLine("Started setting up all tables in the database");
            this.query = """
                CREATE TABLE IF NOT EXISTS users (
                    userID SERIAL PRIMARY KEY,
                    username VARCHAR(255) UNIQUE NOT NULL,
                    password VARCHAR(255) NOT NULL, 
                    coins INTEGER DEFAULT 20 NOT NULL,
                    elo INTEGER DEFAULT 100 NOT NULL,
                    image VARCHAR(255),
                    bio VARCHAR(255),
                    gamesPlayed INTEGER DEFAULT 0 NOT NULL
                );

                CREATE TABLE IF NOT EXISTS usersessions (
                    sessionID SERIAL PRIMARY KEY,
                    userID INTEGER,
                    token VARCHAR(255) NOT NULL,
                    FOREIGN KEY (userID) REFERENCES users(userID)
                );

                CREATE TABLE IF NOT EXISTS packs (
                    packID SERIAL PRIMARY KEY,
                    isPurchased BOOLEAN
                );

                CREATE TABLE IF NOT EXISTS cards (
                    cardID VARCHAR(255) PRIMARY KEY,
                    name VARCHAR(255) NOT NULL,
                    damage DECIMAL NOT NULL,
                    element VARCHAR(50) NOT NULL,
                    type VARCHAR(50) NOT NULL,
                    userID INTEGER,
                    packID INTEGER,
                    FOREIGN KEY (userID) REFERENCES users(userID),
                    FOREIGN KEY (packID) REFERENCES packs(packID)
                );

                CREATE TABLE IF NOT EXISTS decks (
                    deckID SERIAL PRIMARY KEY,
                    userID INTEGER,
                    cardID VARCHAR(255),
                    FOREIGN KEY (userID) REFERENCES users(userID),
                    FOREIGN KEY (cardID) REFERENCES cards(cardID)
                );

                CREATE TABLE IF NOT EXISTS tradings (
                    tradeID SERIAL PRIMARY KEY,
                    traderID INTEGER,
                    receiverID INTEGER,
                    tradercardID VARCHAR(255),
                    receivercardID VARCHAR(255),
                    FOREIGN KEY (traderID) REFERENCES users(userID),
                    FOREIGN KEY (receiverID) REFERENCES users(userID),
                    FOREIGN KEY (tradercardID) REFERENCES cards(cardID),
                    FOREIGN KEY (receivercardID) REFERENCES cards(cardID)
                );
                """;
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(this.query, connection))
                {
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error: " + ex.Message);
                    }
                }
                connection.Close();
            }
            Console.WriteLine("Finished setting up all tables in the database");
        }

        public bool UserExist(string username)
        {
            bool exists = false;
            this.query = "SELECT 1 FROM users WHERE username = @username LIMIT 1";
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(this.query, connection))
                {
                    cmd.Parameters.AddWithValue("@username", username);

                    exists = (cmd.ExecuteScalar() != null);
                }
                connection.Close();
            }
            return exists;
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                // Compute the hash of the password bytes
                byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));

                // Convert the hashed bytes to a hexadecimal string
                StringBuilder stringBuilder = new StringBuilder();
                foreach (byte b in hashedBytes)
                {
                    stringBuilder.Append(b.ToString("x2"));
                }

                return stringBuilder.ToString();
            }
        }

        public void RegisterUser(string username, string password)
        {
            string hashedPassword = HashPassword(password);
            this.query = "INSERT INTO users (username, password) VALUES (@username, @password)";
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(this.query, connection))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@password", hashedPassword);

                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex) {
                        Console.WriteLine("Error: " + ex.Message); 
                    }
                }
                connection.Close();
            }
        }

        public bool LoginUser(string username, string password)
        {
            string hashedPassword = HashPassword(password);
            this.query = "SELECT 1 FROM users WHERE username = @username AND password = @password LIMIT 1";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(this.query, connection))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@password", hashedPassword);

                    // If a data set is found, ExecuteScalar() does not return null
                    return (cmd.ExecuteScalar() != null);
                }
            }
        }

        public void SaveToken(string username, string token)
        {
            this.query = "INSERT INTO usersessions (userID, token) VALUES ((SELECT userID FROM users WHERE username = @username), @token)";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(this.query, connection))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@token", token);

                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error: " + ex.Message);
                    }
                }
                connection.Close();
            }
        }

        public string GetTokenByUsername(string username)
        {
            this.query = "SELECT token FROM usersessions WHERE userID = (SELECT userID FROM users WHERE username = @username) LIMIT 1";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(this.query, connection))
                {
                    cmd.Parameters.AddWithValue("@username", username);

                    // If a data set is found, ExecuteScalar() returns the token, otherwise null
                    return cmd.ExecuteScalar() as string;
                }
            }
        }

        public int CreatePack()
        {
            int packID = 0;

            // Create a new pack in the database and return the generated packID
            this.query = "INSERT INTO packs (isPurchased) VALUES (false) RETURNING packID";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(this.query, connection))
                {
                    // ExecuteScalar is used to retrieve a single value (packID in this case)
                    packID = (int)cmd.ExecuteScalar();
                }
                connection.Close();
            }

            return packID;
        }

        public void CreateCard(string cardID, string name, decimal damage, int packID)
        {
            // Determine card type based on the name
            string cardType = name.ToLower().Contains("spell") ? "Spell" : "Monster";

            // Determine card element based on the name
            string element = name.ToLower().Contains("fire") ? "Fire" : (name.ToLower().Contains("water") ? "Water" : "Normal");

            // Create a new card in the database
            this.query = "INSERT INTO cards (cardID, name, damage, element, type, userID, packID) " +
                         "VALUES (@cardID, @name, @damage, @element, @type, NULL, @packID) RETURNING cardID";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(this.query, connection))
                {
                    cmd.Parameters.AddWithValue("@cardID", cardID); // Use the provided card ID
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@damage", damage);
                    cmd.Parameters.AddWithValue("@element", element);
                    cmd.Parameters.AddWithValue("@type", cardType);
                    cmd.Parameters.AddWithValue("@packID", packID);

                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error: " + ex.Message);
                    }
                }
                connection.Close();
            }
        }

        public void AddCardToPack(string cardID)
        {
            // Find the latest packID (the one that was just created)
            this.query = "SELECT packID FROM packs ORDER BY packID DESC LIMIT 1";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(this.query, connection))
                {
                    int packID = (int)cmd.ExecuteScalar();

                    // Update the card with the associated packID
                    this.query = "UPDATE cards SET packID = @packID WHERE cardID = @cardID";

                    cmd.Parameters.AddWithValue("@packID", packID);
                    cmd.Parameters.AddWithValue("@cardID", cardID);

                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error: " + ex.Message);
                    }
                }
                connection.Close();
            }
        }

        public bool CardExist(string cardID)
        {
            bool exists = false;
            this.query = "SELECT 1 FROM cards WHERE cardID = @cardID LIMIT 1";
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(this.query, connection))
                {
                    cmd.Parameters.AddWithValue("@cardID", cardID);

                    exists = (cmd.ExecuteScalar() != null);
                }
                connection.Close();
            }
            return exists;
        }

        public int GetCoinsByUsername(string username)
        {
            int coins = 0;

            // Query to retrieve the coins based on the provided username
            this.query = "SELECT coins FROM users WHERE username = @username";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(this.query, connection))
                {
                    cmd.Parameters.AddWithValue("@username", username);

                    // ExecuteScalar is used to retrieve a single value (coins in this case)
                    object result = cmd.ExecuteScalar();

                    if (result != null)
                    {
                        coins = Convert.ToInt32(result);
                    }
                }
            }

            return coins;
        }

        public bool PackAvailable()
        {
            bool isAvailable = false;

            // Query to check if there is an available pack (isPurchased == false)
            this.query = "SELECT 1 FROM packs WHERE isPurchased = false LIMIT 1";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(this.query, connection))
                {
                    // If a data set is found, ExecuteScalar() does not return null
                    isAvailable = (cmd.ExecuteScalar() != null);
                }
            }

            return isAvailable;
        }

        private int GetLatestAvailablePackID()
        {
            int packID = 0;

            // Query to get the packID of the latest available pack
            this.query = "SELECT packID FROM packs WHERE isPurchased = false LIMIT 1";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(this.query, connection))
                {
                    // If a data set is found, ExecuteScalar() does not return null
                    object result = cmd.ExecuteScalar();

                    if (result != null)
                    {
                        packID = Convert.ToInt32(result);
                    }
                }
            }

            return packID;
        }

        private void AddUserToPackCards(string username, int packID)
        {
            // Update all cards in the purchased pack with the associated user ID
            this.query = "UPDATE cards SET userID = (SELECT userID FROM users WHERE username = @username) WHERE packID = @packID";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(this.query, connection))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@packID", packID);

                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error: " + ex.Message);
                    }
                }
            }
        }

        private void SetPackAsPurchased(int packID)
        {
            // Update the pack to set it as purchased
            this.query = "UPDATE packs SET isPurchased = true WHERE packID = @packID";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(this.query, connection))
                {
                    cmd.Parameters.AddWithValue("@packID", packID);

                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error: " + ex.Message);
                    }
                }
            }
        }

        private void UpdateUserCoins(string username, int updatedCoins)
        {
            // Query to update the coins for the specified username
            this.query = "UPDATE users SET coins = @updatedCoins WHERE username = @username";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(this.query, connection))
                {
                    cmd.Parameters.AddWithValue("@updatedCoins", updatedCoins);
                    cmd.Parameters.AddWithValue("@username", username);

                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error: " + ex.Message);
                    }
                }
            }
        }

        public void BuyPack(string username)
        {
            // Get the latest available pack ID
            int packID = GetLatestAvailablePackID();

            // Update the user's coins
            int newCoins = GetCoinsByUsername(username) - 5;
            UpdateUserCoins(username, newCoins);

            // Add the user's ID to all cards in the purchased pack
            AddUserToPackCards(username, packID);

            // Set the pack as purchased
            SetPackAsPurchased(packID);
        }

        public List<Card> GetCardsByUsername(string username)
        {
            List<Card> userCards = new List<Card>();

            // Query to retrieve all cards belonging to the user
            this.query = "SELECT * FROM cards WHERE userID = (SELECT userID FROM users WHERE username = @username)";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(this.query, connection))
                {
                    cmd.Parameters.AddWithValue("@username", username);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Card card = new Card
                            {
                                ID = reader["cardID"].ToString(),
                                Name = reader["name"].ToString(),
                                Damage = Convert.ToDecimal(reader["damage"]),
                                Element = reader["element"].ToString(),
                                Type = reader["type"].ToString(),
                                UserID = Convert.ToInt32(reader["userID"]),
                                PackID = Convert.ToInt32(reader["packID"])
                            };

                            userCards.Add(card);
                        }
                    }
                }
            }

            return userCards;
        }

        public List<Card> GetDeckByUsername(string username)
        {
            List<Card> userDeck = new List<Card>();

            // Query to retrieve all cards in the user's deck
            this.query = "SELECT c.* FROM cards c " +
                         "JOIN decks d ON c.cardID = d.cardID " +
                         "WHERE d.userID = (SELECT userID FROM users WHERE username = @username)";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(this.query, connection))
                {
                    cmd.Parameters.AddWithValue("@username", username);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Card card = new Card
                            {
                                ID = reader["cardID"].ToString(),
                                Name = reader["name"].ToString(),
                                Damage = Convert.ToDecimal(reader["damage"]),
                                Element = reader["element"].ToString(),
                                Type = reader["type"].ToString(),
                                UserID = Convert.ToInt32(reader["userID"]),
                                PackID = Convert.ToInt32(reader["packID"])
                            };

                            userDeck.Add(card);
                        }
                    }
                }
            }

            return userDeck;
        }

        public bool UserHasCards(string username, string cardID)
        {
            bool hasCard = false;

            // Query to check if the user has the specified card
            this.query = "SELECT 1 FROM cards WHERE userID = (SELECT userID FROM users WHERE username = @username) AND cardID = @cardID LIMIT 1";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(this.query, connection))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@cardID", cardID);

                    // If a data set is found, ExecuteScalar() does not return null
                    hasCard = (cmd.ExecuteScalar() != null);
                }
            }

            return hasCard;
        }

        public void AddCardsToUserDeck(string username, List<string> cardIDs)
        {
            ClearUserDeck(username);
            foreach (string cardID in cardIDs)
            {
                this.query = "INSERT INTO decks (userID, cardID) VALUES ((SELECT userID FROM users WHERE username = @username), @cardID)";

                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    using (var cmd = new NpgsqlCommand(this.query, connection))
                    {
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.Parameters.AddWithValue("@cardID", cardID);

                        try
                        {
                            cmd.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error: " + ex.Message);
                        }
                    }
                }
            }

            Console.WriteLine($"Added {cardIDs.Count} cards to the deck for user '{username}'.");
        }

        private void ClearUserDeck(string username)
        {
            // Clear all cards from the user's deck
            this.query = "DELETE FROM decks WHERE userID = (SELECT userID FROM users WHERE username = @username)";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(this.query, connection))
                {
                    cmd.Parameters.AddWithValue("@username", username);

                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error: " + ex.Message);
                    }
                }
            }

            Console.WriteLine($"Cleared the deck for user '{username}'.");
        }
    }
}
