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
                    name VARCHAR(255),
                    password VARCHAR(255) NOT NULL, 
                    coins INTEGER DEFAULT 20 NOT NULL,
                    elo INTEGER DEFAULT 100 NOT NULL,
                    image VARCHAR(255),
                    bio VARCHAR(255),
                    gamesPlayed INTEGER DEFAULT 0 NOT NULL,
                    wins INTEGER DEFAULT 0 NOT NULL,
                    draws INTEGER DEFAULT 0 NOT NULL,
                    losses INTEGER DEFAULT 0 NOT NULL,
                	lastLogin TIMESTAMP
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
                    tradeID VARCHAR(255) PRIMARY KEY,
                    traderID INTEGER,
                    cardToTradeID VARCHAR(255),
                	type VARCHAR(50),
                	minimumDamage DECIMAL,
                    isClosed BOOLEAN,
                    FOREIGN KEY (traderID) REFERENCES users(userID),
                    FOREIGN KEY (cardToTradeID) REFERENCES cards(cardID)
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

        public bool TokenExist(string token)
        {
            bool exists = false;
            this.query = "SELECT 1 FROM usersessions WHERE token = @token LIMIT 1";
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(this.query, connection))
                {
                    cmd.Parameters.AddWithValue("@token", token);

                    exists = (cmd.ExecuteScalar() != null);
                }
                connection.Close();
            }
            return exists;
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

        public void UpdateUserCoins(string username, int updatedCoins)
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

        public User GetUserDataByUsername(string username)
        {
            User userData = null;

            // Query to retrieve user data based on the provided username
            this.query = "SELECT name, bio, image FROM users WHERE username = @username";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(this.query, connection))
                {
                    cmd.Parameters.AddWithValue("@username", username);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            userData = new User
                            {
                                Name = reader["name"].ToString(),
                                Bio = reader["bio"].ToString(),
                                Image = reader["image"].ToString()
                            };
                        }
                    }
                }
            }

            return userData;
        }

        public void UpdateUserDataByUsername(string username, string newName, string newBio, string newImage)
        {
            // Query to update user data (bio and image) based on the provided username
            this.query = "UPDATE users SET name = @newName, bio = @newBio, image = @newImage WHERE username = @username";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(this.query, connection))
                {
                    cmd.Parameters.AddWithValue("@newName", newName);
                    cmd.Parameters.AddWithValue("@newBio", newBio);
                    cmd.Parameters.AddWithValue("@newImage", newImage);
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

        public User GetStatsByUsername(string username)
        {
            User userStats = null;

            // Query to retrieve user statistics based on the provided username
            this.query = "SELECT name, elo, gamesPlayed, wins, draws, losses FROM users WHERE username = @username";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(this.query, connection))
                {
                    cmd.Parameters.AddWithValue("@username", username);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            userStats = new User
                            {
                                Name = reader["name"].ToString(),
                                Elo = Convert.ToInt32(reader["elo"]),
                                GamesPlayed = Convert.ToInt32(reader["gamesPlayed"]),
                                Wins = Convert.ToInt32(reader["wins"]),
                                Draws = Convert.ToInt32(reader["draws"]),
                                Losses = Convert.ToInt32(reader["losses"])
                            };
                        }
                    }
                }
            }

            return userStats;
        }

        public void UpdateUserStatsByUsername(string username, int newElo, int newGamesPlayed, int newWins, int newDraws ,int newLosses)
        {
            // Query to update user data (bio and image) based on the provided username
            this.query = "UPDATE users SET elo = @newElo, gamesPlayed = @newGamesPlayed, wins = @newWins, draws = @newDraws, losses = @newLosses WHERE username = @username";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(this.query, connection))
                {
                    cmd.Parameters.AddWithValue("@newElo", newElo);
                    cmd.Parameters.AddWithValue("@newGamesPlayed", newGamesPlayed);
                    cmd.Parameters.AddWithValue("@newWins", newWins);
                    cmd.Parameters.AddWithValue("@newDraws", newDraws);
                    cmd.Parameters.AddWithValue("@newLosses", newLosses);
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

        public List<User> GetScoreboard()
        {
            List<User> scoreboard = new List<User>();

            // Query to retrieve user statistics ordered by ELO
            this.query = "SELECT name, elo, gamesPlayed, wins, draws, losses FROM users ORDER BY elo DESC";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(this.query, connection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            User userStats = new User
                            {
                                Name = reader["name"].ToString(),
                                Elo = Convert.ToInt32(reader["elo"]),
                                GamesPlayed = Convert.ToInt32(reader["gamesPlayed"]),
                                Wins = Convert.ToInt32(reader["wins"]),
                                Draws = Convert.ToInt32(reader["draws"]),
                                Losses = Convert.ToInt32(reader["losses"])
                            };

                            scoreboard.Add(userStats);
                        }
                    }
                }
            }

            return scoreboard;
        }

        public List<Trading> GetTrades()
        {
            List<Trading> trades = new List<Trading>();

            // Query to retrieve trading information from the tradings table
            this.query = "SELECT tradeID, traderID, cardToTradeID, type, minimumDamage FROM tradings WHERE isClosed = false";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(this.query, connection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Trading trade = new Trading
                            {
                                ID = reader["tradeID"].ToString(),
                                CardToTrade = reader["cardToTradeID"].ToString(),
                                Type = reader["type"].ToString(),
                                MinimumDamage = Convert.ToDecimal(reader["minimumDamage"]),
                            };

                            trades.Add(trade);
                        }
                    }
                }
            }

            return trades;
        }

        public bool CardInDeck(string username, string cardID)
        {
            // Query to check if the card is in the user's deck
            this.query = "SELECT 1 FROM decks WHERE userID = (SELECT userID FROM users WHERE username = @username) AND cardID = @cardID LIMIT 1";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(this.query, connection))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@cardID", cardID);

                    // If a data set is found, ExecuteScalar() does not return null
                    return (cmd.ExecuteScalar() != null);
                }
            }
        }

        public bool OpenTradeExist(string tradeID)
        {
            // Query to check if the trade with the given ID exists
            this.query = "SELECT 1 FROM tradings WHERE tradeID = @tradeID AND isClosed = false LIMIT 1";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(this.query, connection))
                {
                    cmd.Parameters.AddWithValue("@tradeID", tradeID);

                    // If a data set is found, ExecuteScalar() does not return null
                    return (cmd.ExecuteScalar() != null);
                }
            }
        }

        public void CreateTrade(string tradeID, string username, string cardID, string type, string minDamage)
        {
            // Query to insert a new trade record into the tradings table
            this.query = "INSERT INTO tradings (tradeID, traderID, cardToTradeID, type, minimumDamage, isClosed) " +
                         "VALUES (@tradeID, (SELECT userID FROM users WHERE username = @username), " +
                         "(SELECT cardID FROM cards WHERE cardID = @cardID), @type, @minDamage, false)";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(this.query, connection))
                {
                    cmd.Parameters.AddWithValue("@tradeID", tradeID);
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@cardID", cardID);
                    cmd.Parameters.AddWithValue("@type", type);
                    cmd.Parameters.AddWithValue("@minDamage", Convert.ToDecimal(minDamage)); // Assuming minDamage is a decimal

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

        public void DeleteTradeByID(string tradeID)
        {
            // Query to delete a trade record from the tradings table based on tradeID
            this.query = "DELETE FROM tradings WHERE tradeID = @tradeID";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(this.query, connection))
                {
                    cmd.Parameters.AddWithValue("@tradeID", tradeID);

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

        public Trading GetTradeByTradingID(string tradeID)
        {
            Trading trade = null;

            // Query to retrieve a trading deal based on the provided tradeID
            this.query = "SELECT * FROM tradings WHERE tradeID = @tradeID";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(this.query, connection))
                {
                    cmd.Parameters.AddWithValue("@tradeID", tradeID);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            trade = new Trading
                            {
                                ID = tradeID,
                                TraderID = Convert.ToInt32(reader["traderID"]),
                                CardToTrade = reader["cardToTradeID"].ToString(),
                                Type = reader["type"].ToString(),
                                MinimumDamage = Convert.ToDecimal(reader["minimumDamage"]),
                                IsClosed = Convert.ToBoolean(reader["isClosed"])
                            };
                        }
                    }
                }
            }

            return trade;
        }

        public string GetUsernameByUserID(int userID)
        {
            string username = null;

            // Query to retrieve the username based on the provided userID
            this.query = "SELECT username FROM users WHERE userID = @userID";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(this.query, connection))
                {
                    cmd.Parameters.AddWithValue("@userID", userID);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            username = reader["username"].ToString();
                        }
                    }
                }
            }

            return username;
        }

        public Card GetCardByUsernameAndCardID(string username, string cardID)
        {
            Card card = null;

            // Query to retrieve the card based on the provided username and cardID
            this.query = "SELECT * FROM cards WHERE userID = (SELECT userID FROM users WHERE username = @username) AND cardID = @cardID";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(this.query, connection))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@cardID", cardID);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            card = new Card
                            {
                                ID = reader["cardID"].ToString(),
                                Name = reader["name"].ToString(),
                                Damage = Convert.ToDecimal(reader["damage"]),
                                Element = reader["element"].ToString(),
                                Type = reader["type"].ToString(),
                                UserID = Convert.ToInt32(reader["userID"]),
                                PackID = Convert.ToInt32(reader["packID"])
                            };
                        }
                    }
                }
            }

            return card;
        }

        public void TradeCards(string tradeID, string traderUsername, string username, string cardToTradeID, string cardFromUserID)
        {
            // Perform the trading operation by updating the userIDs for the cards
            this.query = "UPDATE cards SET userID = (SELECT userID FROM users WHERE username = @traderUsername) WHERE cardID = @cardFromUserID;" +
                          "UPDATE cards SET userID = (SELECT userID FROM users WHERE username = @username) WHERE cardID = @cardToTradeID;";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(this.query, connection))
                {
                    cmd.Parameters.AddWithValue("@traderUsername", traderUsername);
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@cardToTradeID", cardToTradeID);
                    cmd.Parameters.AddWithValue("@cardFromUserID", cardFromUserID);

                    try
                    {
                        cmd.ExecuteNonQuery();
                        SetTradingAsClosed(tradeID);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error: " + ex.Message);
                    }
                }
            }
        }

        private void SetTradingAsClosed(string tradeID)
        {
            // Update the trading deal to set it as closed
            this.query = "UPDATE tradings SET isClosed = true WHERE tradeID = @tradeID";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(this.query, connection))
                {
                    cmd.Parameters.AddWithValue("@tradeID", tradeID);

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

        public void SetTimestampByUsername(string username)
        {
            this.query = "UPDATE users SET lastLogin = CURRENT_TIMESTAMP WHERE username = @username";

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
        }

        public DateTime GetTimestampByUsername(string username)
        {
            DateTime timestamp = DateTime.MinValue;

            this.query = "SELECT lastLogin FROM users WHERE username = @username";

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var cmd = new NpgsqlCommand(this.query, connection))
                {
                    cmd.Parameters.AddWithValue("@username", username);

                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        timestamp = Convert.ToDateTime(result);
                    }
                }
            }

            return timestamp;
        }
    }
}
