using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace MonsterTradingCardGame
{
    public class RequestHandler
    {

        public string Request { get; private set; }
        public string Response { get; private set; }
        public Database Database { get; private set; }
        public TcpClient Client1 { get; private set; }
        public TcpListener Listener {  get; private set; }
        static int streakCounter = 1;

        public RequestHandler(TcpClient client, TcpListener listener, string Request, Database db)
        {
            this.Client1 = client;
            this.Listener = listener;
            this.Request = Request;
            this.Database = db;
            HandleRequest();
        }

        private async Task HandleRequest()
        {
            if (this.Request.StartsWith("POST /users"))
            {
                string body = GetLastLineFromRequest(this.Request);
                dynamic? user = JsonConvert.DeserializeObject(body);
                if (Database.UserExist((string)user.Username) == true)
                {
                    this.Response = ResponseHandler.GetResponseMessage(409, "application/json", "User with same username already registered");
                }
                else
                {
                    Database.RegisterUser((string)user.Username, (string)user.Password);
                    this.Response = ResponseHandler.GetResponseMessage(201, "application/json", "User successfully created");
                }
            }
            else if (this.Request.StartsWith("POST /sessions"))
            {
                string body = GetLastLineFromRequest(this.Request);
                dynamic? user = JsonConvert.DeserializeObject(body);
                if ((Database.UserExist((string)user.Username) == true) && (Database.LoginUser((string)user.Username, (string)user.Password) == true))
                {
                    string token = (string)user.Username + "-mtcgToken";
                    Database.SaveToken((string)user.Username, token);
                    this.Response = ResponseHandler.GetResponseMessage(200, "application/json", "User login successful") + "\r\n" + token + "\r\n";
                }
                else
                {
                    this.Response = ResponseHandler.GetResponseMessage(401, "application/json", "Invalid username/password provided");
                }
            }
            else if (this.Request.StartsWith("POST /packages"))
            {
                string body = GetLastLineFromRequest(this.Request);
                List<string> cardIDs = GetCardIDsFromRequestBodyPackages(body);
                string token = GetTokenFromRequest(this.Request);
                if ((token.Equals("")) || (Database.TokenExist(token) == false))
                {
                    this.Response = ResponseHandler.GetResponseMessage(401, "application/json", "Access token is missing or invalid");
                } else if (Database.GetTokenByUsername("admin").Equals(GetTokenFromRequest(this.Request)))
                {
                    bool cardExists = cardIDs.Any(cardID => string.IsNullOrEmpty(cardID) || Database.CardExist(cardID));
                    if (cardExists)
                    {
                        this.Response = ResponseHandler.GetResponseMessage(409, "application/json", "At least one card in the packages already exists");
                    }
                    else
                    {
                        // Assuming the body is an array of cards
                        dynamic? cards = JsonConvert.DeserializeObject(body);

                        // Create a pack
                        int packID = Database.CreatePack();

                        // Iterate through cards and create each one
                        foreach (var card in cards)
                        {
                            Database.CreateCard((string)card.Id, (string)card.Name, (decimal)card.Damage, packID);
                            Database.AddCardToPack((string)card.Id);
                        }

                        this.Response = ResponseHandler.GetResponseMessage(201, "application/json", "Package and cards successfully created");
                    }
                }
                else
                {
                    this.Response = ResponseHandler.GetResponseMessage(403, "application/json", "Provided user is not \"admin\"");
                }
            }
            else if (this.Request.StartsWith("POST /transactions/packages"))
            {
                string token = GetTokenFromRequest(this.Request);
                string username = GetUsernameFromToken(token);
                if ((token.Equals("")) || (Database.TokenExist(token) == false))
                {
                    this.Response = ResponseHandler.GetResponseMessage(401, "application/json", "Access token is missing or invalid");
                }
                else if(Database.GetCoinsByUsername(username) < 5) 
                {
                    this.Response = ResponseHandler.GetResponseMessage(403, "application/json", "Not enough money for buying a card package");
                }
                else if(Database.PackAvailable() == false)
                {
                    this.Response = ResponseHandler.GetResponseMessage(404, "application/json", "No card package available for buying");
                }
                else
                {
                    Database.BuyPack(username);
                    this.Response = ResponseHandler.GetResponseMessage(200, "application/json", "A package has been successfully bought");
                }
            }
            else if (this.Request.StartsWith("GET /cards"))
            {
                string token = GetTokenFromRequest(this.Request);
                string username = GetUsernameFromToken(token);
                if ((token.Equals("")) || (Database.TokenExist(token) == false))
                {
                    this.Response = ResponseHandler.GetResponseMessage(401, "application/json", "Access token is missing or invalid");
                }
                else if((Database.GetCardsByUsername(username) == null) || (Database.GetCardsByUsername(username).Count == 0))
                {
                    this.Response = ResponseHandler.GetResponseMessage(204, "application/json", "The request was fine, but the user doesn't have any cards");
                }
                else
                {
                    List <Card> userCards = Database.GetCardsByUsername(username);
                    string userCardsToJSON = JsonSerializer.Serialize(userCards, new JsonSerializerOptions { WriteIndented = true });
                    this.Response = ResponseHandler.GetResponseMessage(200, "application/json", "The user has cards, the response contains these") + "\r\n" + userCardsToJSON + "\r\n";
                }
            }
            else if (this.Request.StartsWith("GET /deck?format=plain"))
            {
                string token = GetTokenFromRequest(this.Request);
                string username = GetUsernameFromToken(token);
                if ((token.Equals("")) || (Database.TokenExist(token) == false))
                {
                    this.Response = ResponseHandler.GetResponseMessage(401, "text/plain", "Access token is missing or invalid");
                }
                else if ((Database.GetDeckByUsername(username) == null) || (Database.GetDeckByUsername(username).Count == 0))
                {
                    this.Response = ResponseHandler.GetResponseMessage(204, "text/plain", "The request was fine, but the deck doesn't have any cards");
                }
                else
                {
                    List<Card> userDeck = Database.GetDeckByUsername(username);
                    string userDeckToString = String.Join("\n", userDeck.Select(x => x.ToString()).ToArray());
                    this.Response = ResponseHandler.GetResponseMessage(200, "text/plain", "The deck has cards, the response contains these") + "\r\n" + userDeckToString + "\r\n";
                }
            }
            else if (this.Request.StartsWith("GET /deck"))
            {
                string token = GetTokenFromRequest(this.Request);
                string username = GetUsernameFromToken(token);
                if ((token.Equals("")) || (Database.TokenExist(token) == false))
                {
                    this.Response = ResponseHandler.GetResponseMessage(401, "application/json", "Access token is missing or invalid");
                }
                else if ((Database.GetDeckByUsername(username) == null) || (Database.GetDeckByUsername(username).Count == 0))
                {
                    this.Response = ResponseHandler.GetResponseMessage(204, "application/json", "The request was fine, but the deck doesn't have any cards");
                }
                else
                {
                    List<Card> userDeck = Database.GetDeckByUsername(username);
                    string userDeckToJSON = JsonSerializer.Serialize(userDeck, new JsonSerializerOptions { WriteIndented = true });
                    this.Response = ResponseHandler.GetResponseMessage(200, "application/json", "The deck has cards, the response contains these") + "\r\n" + userDeckToJSON + "\r\n";
                }
            }
            else if (this.Request.StartsWith("PUT /deck"))
            {
                string token = GetTokenFromRequest(this.Request);
                string username = GetUsernameFromToken(token);
                string body = GetLastLineFromRequest(this.Request);
                List<string> cardIDs = GetCardIDsFromRequestBodyDeck(body);
                bool cardExists = cardIDs.Any(cardID => string.IsNullOrEmpty(cardID) || (Database.CardExist(cardID) && Database.UserHasCards(username, cardID)));
                if ((token.Equals("")) || (Database.TokenExist(token) == false))
                {
                    this.Response = ResponseHandler.GetResponseMessage(401, "application/json", "Access token is missing or invalid");
                } else if(Database.UserExist(username) && cardExists)
                {
                    if(cardIDs.Count == 4)
                    {
                        Database.AddCardsToUserDeck(username, cardIDs);
                        this.Response = ResponseHandler.GetResponseMessage(200, "application/json", "The deck has been successfully configured");
                    }
                    else
                    {
                        this.Response = ResponseHandler.GetResponseMessage(400, "application/json", "The provided deck did not include the required amount of cards");
                    }
                }
                else
                {
                    this.Response = ResponseHandler.GetResponseMessage(403, "application/json", "At least one of the provided cards does not belong to the user or is not available.");
                }
            }
            else if (this.Request.StartsWith("GET /users/"))
            {
                string username = GetUsernameFromRequest(this.Request);
                string token = GetTokenFromRequest(this.Request);
                if(Database.UserExist(username) == false)
                {
                    this.Response = ResponseHandler.GetResponseMessage(404, "application/json", "User not found.");
                }
                else if ((token.Equals("")) || (Database.TokenExist(token) == false) || (username.Equals(GetUsernameFromToken(token)) == false))
                {
                    this.Response = ResponseHandler.GetResponseMessage(401, "application/json", "Access token is missing or invalid");
                } 
                else
                {
                    User userData = Database.GetUserDataByUsername(username);
                    // Create an anonymous type with the desired properties
                    var userDataSerializationModel = new
                    {
                        Name = userData.Name,
                        Bio = userData.Bio,
                        Image = userData.Image
                    };
                    string userDataToJson = JsonSerializer.Serialize(userDataSerializationModel, new JsonSerializerOptions { WriteIndented = true });
                    this.Response = ResponseHandler.GetResponseMessage(200, "application/json", "Data successfully retrieved") + "\r\n" + userDataToJson + "\r\n";
                }
            }
            else if (this.Request.StartsWith("PUT /users/"))
            {
                string username = GetUsernameFromRequest(this.Request);
                string token = GetTokenFromRequest(this.Request);
                if (Database.UserExist(username) == false)
                {
                    this.Response = ResponseHandler.GetResponseMessage(404, "application/json", "User not found.");
                }
                else if ((token.Equals("")) || (Database.TokenExist(token) == false) || (username.Equals(GetUsernameFromToken(token)) == false))
                {
                    this.Response = ResponseHandler.GetResponseMessage(401, "application/json", "Access token is missing or invalid");
                }
                else
                {
                    string body = GetLastLineFromRequest(this.Request);
                    dynamic? user = JsonConvert.DeserializeObject(body);
                    Database.UpdateUserDataByUsername(username, (string)user.Name, (string)user.Bio, (string)user.Image);
                    this.Response = ResponseHandler.GetResponseMessage(200, "application/json", "User sucessfully updated.");
                }
            }
            else if (this.Request.StartsWith("GET /stats"))
            {
                string token = GetTokenFromRequest(this.Request);
                string username = GetUsernameFromToken(token);
                if ((token.Equals("")) || (Database.TokenExist(token) == false))
                {
                    this.Response = ResponseHandler.GetResponseMessage(401, "application/json", "Access token is missing or invalid");
                }
                else
                {
                    User userStats = Database.GetStatsByUsername(username);
                    // Create an anonymous type with the desired properties
                    var userStatsSerializationModel = new
                    {
                        Name = userStats.Name,
                        Elo = userStats.Elo,
                        GamesPlayed = userStats.GamesPlayed,
                        Wins = userStats.Wins,
                        Draws = userStats.Draws,
                        Losses = userStats.Losses
                    };
                    string userStatsToJSON = JsonSerializer.Serialize(userStatsSerializationModel, new JsonSerializerOptions { WriteIndented = true });
                    this.Response = ResponseHandler.GetResponseMessage(200, "application/json", "The stats could be retrieved successfully.") + "\r\n" + userStatsToJSON + "\r\n";
                }
            }
            else if (this.Request.StartsWith("GET /scoreboard"))
            {
                string token = GetTokenFromRequest(this.Request);
                string username = GetUsernameFromToken(token);
                if ((token.Equals("")) || (Database.TokenExist(token) == false))
                {
                    this.Response = ResponseHandler.GetResponseMessage(401, "application/json", "Access token is missing or invalid");
                }
                else
                {
                    List<User> scoreboard = Database.GetScoreboard();
                    // Creating a list of anonymous objects for serialization
                    var scoreboardSerializationModel = scoreboard.Select(user => new
                    {
                        Name = user.Name,
                        Elo = user.Elo,
                        GamesPlayed = user.GamesPlayed,
                        Wins = user.Wins,
                        Draws = user.Draws,
                        Losses = user.Losses
                    }).ToList();
                    string scoreboardToJSON = JsonSerializer.Serialize(scoreboardSerializationModel, new JsonSerializerOptions { WriteIndented = true });
                    this.Response = ResponseHandler.GetResponseMessage(200, "application/json", "The scoreboard could be retrieved successfully.") + "\r\n" + scoreboardToJSON + "\r\n";
                }
            }
            else if (this.Request.StartsWith("POST /battles"))
            {
                string token1 = GetTokenFromRequest(this.Request);
                string username1 = GetUsernameFromToken(token1);
                if ((token1.Equals("")) || (Database.TokenExist(token1) == false))
                {
                    this.Response = ResponseHandler.GetResponseMessage(401, "application/json", "Access token is missing or invalid");
                }
                else
                {
                    Player player1 = new Player(this.Client1, username1, Database.GetDeckByUsername(username1));
                    Battle battle = new Battle(player1, Database);
                    // thread safe
                    lock (battle)
                    {
                        WaitForNextClient(battle);
                        // because await doesn't work in lock I did a manual await with the while and if
                        while (battle.Log == null || battle.Log == String.Empty)
                        {
                            if (true)
                            {

                            }
                        }
                    }

                    this.Response = ResponseHandler.GetResponseMessage(200, "application/json", "The battle has been carried out successfully.") + "\r\n" + battle.Log + "\r\n";
                }
            }
            else if (this.Request.StartsWith("GET /tradings"))
            {
                string token = GetTokenFromRequest(this.Request);
                string username = GetUsernameFromToken(token);
                if ((token.Equals("")) || (Database.TokenExist(token) == false))
                {
                    this.Response = ResponseHandler.GetResponseMessage(401, "application/json", "Access token is missing or invalid");
                }
                else if((Database.GetTrades() == null) || (Database.GetTrades().Count == 0))
                {
                    this.Response = ResponseHandler.GetResponseMessage(204, "application/json", "The request was fine, but there are no trading deals available");
                }
                else
                {
                    List<Trading> trades = Database.GetTrades();
                    // Creating a list of anonymous objects for serialization
                    var tradesSerializationModel = trades.Select(trade => new
                    {
                        ID = trade.ID,
                        CardToTrade = trade.CardToTrade,
                        Type = trade.Type,
                        MinimumDamage = trade.MinimumDamage
                    }).ToList();
                    string tradesToJSON = JsonSerializer.Serialize(tradesSerializationModel, new JsonSerializerOptions { WriteIndented = true });
                    this.Response = ResponseHandler.GetResponseMessage(200, "application/json", "There are trading deals available, the response contains these") + "\r\n" + tradesToJSON + "\r\n";
                }
            }
            else if (this.Request.StartsWith("POST /tradings/"))
            {
                string token = GetTokenFromRequest(this.Request);
                string username = GetUsernameFromToken(token);
                string tradeID = GetTradingIDFromRequest(this.Request);
                string body = GetLastLineFromRequest(this.Request);
                string cardIDFromBody = GetCardIDFromRequestBodyTradings(body);

                if ((token.Equals("")) || (Database.TokenExist(token) == false))
                {
                    this.Response = ResponseHandler.GetResponseMessage(401, "application/json", "Access token is missing or invalid");
                }
                else if (Database.OpenTradeExist(tradeID) == false)
                {
                    this.Response = ResponseHandler.GetResponseMessage(404, "application/json", "The provided deal ID was not found.");
                }
                else
                {
                    Trading trade = Database.GetTradeByTradingID(tradeID);
                    string traderUsername = Database.GetUsernameByUserID(trade.TraderID);
                    Card card = Database.GetCardByUsernameAndCardID(username, cardIDFromBody);
                    if ((Database.UserHasCards(username, trade.CardToTrade)) || (Database.UserHasCards(traderUsername, cardIDFromBody)) || ((trade.Type.Equals(card.Type)) && (trade.MinimumDamage >= card.Damage)) || (Database.CardInDeck(traderUsername, trade.CardToTrade)))
                    {
                        this.Response = ResponseHandler.GetResponseMessage(403, "application/json", "The offered card is owned by the user, or the requirements are not met (Type, MinimumDamage), or the offered card is locked in the deck.");
                    }
                    else
                    {
                        Database.TradeCards(tradeID, traderUsername, username, trade.CardToTrade, cardIDFromBody);
                        this.Response = ResponseHandler.GetResponseMessage(200, "application/json", "Trading deal successfully executed.");
                    }
                }
            }
            else if (this.Request.StartsWith("POST /tradings"))
            {
                string token = GetTokenFromRequest(this.Request);
                string username = GetUsernameFromToken(token);
                string body = GetLastLineFromRequest(this.Request);
                dynamic? trade = JsonConvert.DeserializeObject(body);
                if ((token.Equals("")) || (Database.TokenExist(token) == false))
                {
                    this.Response = ResponseHandler.GetResponseMessage(401, "application/json", "Access token is missing or invalid");
                }
                else if((Database.UserHasCards(username, (string)trade.CardToTrade) == false) || (Database.CardInDeck(username, (string)trade.CardToTrade)))
                {
                    this.Response = ResponseHandler.GetResponseMessage(403, "application/json", "The deal contains a card that is not owned by the user or locked in the deck.");
                }
                else if(Database.OpenTradeExist((string)trade.Id))
                {
                    this.Response = ResponseHandler.GetResponseMessage(409, "application/json", "A deal with this deal ID already exists.");
                }
                else
                {
                    Database.CreateTrade((string)trade.Id, username, (string)trade.CardToTrade, (string)trade.Type, (string)trade.MinimumDamage);
                    this.Response = ResponseHandler.GetResponseMessage(201, "application/json", "Trading deal successfully created");
                }
            }
            else if (this.Request.StartsWith("DELETE /tradings/"))
            {
                string token = GetTokenFromRequest(this.Request);
                string username = GetUsernameFromToken(token);
                string tradeID = GetTradingIDFromRequest(this.Request);
                Trading trade = Database.GetTradeByTradingID(tradeID);
                if ((token.Equals("")) || (Database.TokenExist(token) == false))
                {
                    this.Response = ResponseHandler.GetResponseMessage(401, "application/json", "Access token is missing or invalid");
                }
                else if (Database.OpenTradeExist(tradeID) == false)
                {
                    this.Response = ResponseHandler.GetResponseMessage(404, "application/json", "The provided deal ID was not found.");
                }
                else if (Database.UserHasCards(username, trade.CardToTrade) == false)
                {
                    this.Response = ResponseHandler.GetResponseMessage(403, "application/json", "The deal contains a card that is not owned by the user.");
                }
                else
                {
                    Database.DeleteTradeByID(tradeID);
                    this.Response = ResponseHandler.GetResponseMessage(200, "application/json", "Trading deal successfully deleted");
                }
            }
            else if(this.Request.StartsWith("POST /daily-login"))
            {
                string token = GetTokenFromRequest(this.Request);
                string username = GetUsernameFromToken(token);
                int coins = 5;
                DateTime currentTimestamp = Database.GetTimestampByUsername(username);
                if ((token.Equals("")) || (Database.TokenExist(token) == false))
                {
                    this.Response = ResponseHandler.GetResponseMessage(401, "application/json", "Access token is missing or invalid");
                }
                // else if (DateTime.UtcNow.Subtract(currentTimestamp).TotalHours < 24)
                else if (DateTime.UtcNow.Subtract(currentTimestamp).TotalMinutes < 1)
                {
                    this.Response = ResponseHandler.GetResponseMessage(400, "application/json", "Daily Login Bonus not available yet");
                }
                else
                {
                    // if (DateTime.UtcNow.Subtract(currentTimestamp).TotalHours > 48)
                    if (DateTime.UtcNow.Subtract(currentTimestamp).TotalMinutes > 2)
                    {
                        streakCounter = 1;
                        coins = 5;
                    }
                    else
                    {
                        streakCounter++;
                        coins = 5 * streakCounter;
                    }
                    Database.SetTimestampByUsername(username);
                    int newCoins = Database.GetCoinsByUsername(username) + coins;
                    Database.UpdateUserCoins(username, newCoins);
                    this.Response = ResponseHandler.GetResponseMessage(200, "application/json", "Daily Login Bonus has been successfully retrieved") + "\r\n" + "You retrieved " + coins + " coins for your " + streakCounter + "d Streak. Now you have " + newCoins + " coins in total" + "\r\n";
                }
            }
            else
            {
                this.Response = ResponseHandler.GetResponseMessage(400, "application/json", "Command not found");
            }
        }

        private string GetLastLineFromRequest(string request)
        {
            if (string.IsNullOrEmpty(request))
            {
                return string.Empty;
            }

            string[] lines = request.Split('\n');
            return lines.LastOrDefault()?.Trim() ?? string.Empty;
        }

        private string GetTokenFromRequest(string request)
        {
            // Find the Authorization header in the cURL request
            Match match = Regex.Match(request, @"Authorization:\s+Bearer\s+([^\s]+)");

            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            // Token not found
            return "";
        }

        private List<string> GetCardIDsFromRequestBodyPackages(string requestBody)
        {
            List<string> idValues = new List<string>();

            try
            {
                // JSON-Array parsing
                JArray jsonArray = JArray.Parse(requestBody);

                // ID values extraction
                foreach (var item in jsonArray)
                {
                    string id = item["Id"]?.Value<string>();
                    if (!string.IsNullOrEmpty(id))
                    {
                        idValues.Add(id);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error when extracting the ID values: {ex.Message}");
            }

            return idValues;
        }

        private string GetUsernameFromToken(string token)
        {
            const string tokenSuffix = "-mtcgToken";

            // Check if the token ends with the expected suffix
            if (token.EndsWith(tokenSuffix))
            {
                // Extract the username by removing the suffix
                string username = token.Substring(0, token.Length - tokenSuffix.Length);
                return username;
            }
            else
            {
                // Token format is not as expected
                return "";
            }
        }

        private List<string> GetCardIDsFromRequestBodyDeck(string requestBody)
        {
            List<string> idValues = new List<string>();

            try
            {
                // JSON-Array parsing
                JArray jsonArray = JArray.Parse(requestBody);

                // ID values extraction
                foreach (var item in jsonArray)
                {
                    string id = item?.Value<string>();
                    if (!string.IsNullOrEmpty(id))
                    {
                        idValues.Add(id);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error when extracting the ID values: {ex.Message}");
            }

            return idValues;
        }

        private string GetUsernameFromRequest(string request)
        {
            // Assuming the format is "/users/{username}"
            string[] segments = request.Split('/');
            if (segments.Length >= 3 && (segments[1] == "users" || segments[1] == "tradings"))
            {
                return segments[2].Split(" ")[0];
            }
            else
            {
                return "";
            }
        }

        private string GetTradingIDFromRequest(string request)
        {
            // Assuming the format is "/tradings/{tradeID}"
            string[] segments = request.Split('/');
            if (segments.Length >= 3 && segments[1] == "tradings")
            {
                return segments[2].Split(" ")[0];
            }
            else
            {
                return "";
            }
        }

        private string GetCardIDFromRequestBodyTradings(string requestBody)
        {
            try
            {
                // Assuming the request body is a single card ID enclosed in double quotes
                string cardID = JsonConvert.DeserializeObject<string>(requestBody);

                // Check if the extracted card ID is not null or empty
                if (!string.IsNullOrEmpty(cardID))
                {
                    return cardID;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error when extracting the card ID: {ex.Message}");
            }

            return "";
        }

        private async Task WaitForNextClient(Battle battle)
        {
            Console.WriteLine("Waiting for the next client on port 10001...");

            try
            {
                TcpClient secondClient = await this.Listener.AcceptTcpClientAsync();

                Console.WriteLine($"Second client connected: {secondClient.Client.RemoteEndPoint}");

                // second client's network stream
                using (NetworkStream networkStream = secondClient.GetStream())
                {
                    try
                    {
                        var requestBytes = new byte[1024];
                        await networkStream.ReadAsync(requestBytes, 0, requestBytes.Length);
                        string request = Encoding.UTF8.GetString(requestBytes);
                        Console.WriteLine($"Received request from {secondClient.Client.RemoteEndPoint}:\r\n{request}\r\n");

                        string token2 = GetTokenFromRequest(this.Request);
                        string username2 = GetUsernameFromToken(token2);
                        if ((token2.Equals("")) || (Database.TokenExist(token2) == false))
                        {
                            this.Response = ResponseHandler.GetResponseMessage(401, "application/json", "Access token is missing or invalid");
                        } 
                        else
                        {
                            Player player2 = new Player(this.Client1, username2, Database.GetDeckByUsername(username2));
                            battle.AddPlayer(player2);
                            battle.StartBattle();
                            this.Response = ResponseHandler.GetResponseMessage(200, "application/json", "The battle has been carried out successfully.") + "\r\n" + battle.Log + "\r\n";
                        }

                        Console.WriteLine($"Sending response:\r\n{this.Response}");

                        byte[] responseData = Encoding.UTF8.GetBytes(this.Response);
                        await networkStream.WriteAsync(responseData, 0, responseData.Length);
                        //await networkStream.FlushAsync();
                    }
                    catch (IOException e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    finally
                    {
                        Console.WriteLine($"Client disconnected: {secondClient.Client.RemoteEndPoint}");
                        secondClient.Close();
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("No connection available.");
            }
        }
    }
}
