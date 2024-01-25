using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace MonsterTradingCardGame
{
    public class RequestHandler
    {

        public string request { get; private set; }
        public string response { get; private set; }
        public Database db { get; private set; }

        public RequestHandler(string Request, Database db)
        {
            this.request = Request;
            this.db = db;
            handleRequest();
        }

        private void handleRequest()
        {
            if (this.request.StartsWith("POST /users"))
            {
                string body = GetLastLineFromRequest(this.request);
                dynamic? user = JsonConvert.DeserializeObject(body);
                if (db.UserExist((string)user.Username) == true)
                {
                    this.response = ResponseHandler.GetResponseMessage(409, "application/json", "User with same username already registered");
                }
                else
                {
                    db.RegisterUser((string)user.Username, (string)user.Password);
                    this.response = ResponseHandler.GetResponseMessage(201, "application/json", "User successfully created");
                }
            }
            else if (this.request.StartsWith("POST /sessions"))
            {
                string body = GetLastLineFromRequest(this.request);
                dynamic? user = JsonConvert.DeserializeObject(body);
                if ((db.UserExist((string)user.Username) == true) && (db.LoginUser((string)user.Username, (string)user.Password) == true))
                {
                    string token = (string)user.Username + "-mtcgToken";
                    db.SaveToken((string)user.Username, token);
                    this.response = ResponseHandler.GetResponseMessage(200, "application/json", "User login successful") + "\r\n" + token + "\r\n";
                }
                else
                {
                    this.response = ResponseHandler.GetResponseMessage(401, "application/json", "Invalid username/password provided");
                }
            }
            else if (this.request.StartsWith("POST /packages"))
            {
                string body = GetLastLineFromRequest(this.request);
                List<string> cardIDs = GetCardIDsFromRequestBodyPackages(body);
                string token = GetTokenFromRequest(this.request);
                if ((token.Equals("")) || (db.TokenExist(token) == false))
                {
                    this.response = ResponseHandler.GetResponseMessage(401, "application/json", "Access token is missing or invalid");
                } else if (db.GetTokenByUsername("admin").Equals(GetTokenFromRequest(this.request)))
                {
                    bool cardExists = cardIDs.Any(cardID => string.IsNullOrEmpty(cardID) || db.CardExist(cardID));
                    if (cardExists)
                    {
                        this.response = ResponseHandler.GetResponseMessage(409, "application/json", "At least one card in the packages already exists");
                    }
                    else
                    {
                        // Assuming the body is an array of cards
                        dynamic? cards = JsonConvert.DeserializeObject(body);

                        // Create a pack
                        int packID = db.CreatePack();

                        // Iterate through cards and create each one
                        foreach (var card in cards)
                        {
                            db.CreateCard((string)card.Id, (string)card.Name, (decimal)card.Damage, packID);
                            db.AddCardToPack((string)card.Id);
                        }

                        this.response = ResponseHandler.GetResponseMessage(201, "application/json", "Package and cards successfully created");
                    }
                }
                else
                {
                    this.response = ResponseHandler.GetResponseMessage(403, "application/json", "Provided user is not \"admin\"");
                }
            }
            else if (this.request.StartsWith("POST /transactions/packages"))
            {
                string token = GetTokenFromRequest(this.request);
                string username = GetUsernameFromToken(token);
                if ((token.Equals("")) || (db.TokenExist(token) == false))
                {
                    this.response = ResponseHandler.GetResponseMessage(401, "application/json", "Access token is missing or invalid");
                }
                else if(db.GetCoinsByUsername(username) < 5) 
                {
                    this.response = ResponseHandler.GetResponseMessage(403, "application/json", "Not enough money for buying a card package");
                }
                else if(db.PackAvailable() == false)
                {
                    this.response = ResponseHandler.GetResponseMessage(404, "application/json", "No card package available for buying");
                }
                else
                {
                    db.BuyPack(username);
                    this.response = ResponseHandler.GetResponseMessage(200, "application/json", "A package has been successfully bought");
                }
            }
            else if (this.request.StartsWith("GET /cards"))
            {
                string token = GetTokenFromRequest(this.request);
                string username = GetUsernameFromToken(token);
                if ((token.Equals("")) || (db.TokenExist(token) == false))
                {
                    this.response = ResponseHandler.GetResponseMessage(401, "application/json", "Access token is missing or invalid");
                }
                else if((db.GetCardsByUsername(username) == null) || (db.GetCardsByUsername(username).Count == 0))
                {
                    this.response = ResponseHandler.GetResponseMessage(204, "application/json", "The request was fine, but the user doesn't have any cards");
                }
                else
                {
                    List <Card> userCards = db.GetCardsByUsername(username);
                    string userCardsToJSON = JsonSerializer.Serialize(userCards, new JsonSerializerOptions { WriteIndented = true });
                    this.response = ResponseHandler.GetResponseMessage(200, "application/json", "The user has cards, the response contains these") + "\r\n" + userCardsToJSON + "\r\n";
                }
            }
            else if (this.request.StartsWith("GET /deck?format=plain"))
            {
                string token = GetTokenFromRequest(this.request);
                string username = GetUsernameFromToken(token);
                if ((token.Equals("")) || (db.TokenExist(token) == false))
                {
                    this.response = ResponseHandler.GetResponseMessage(401, "text/plain", "Access token is missing or invalid");
                }
                else if ((db.GetDeckByUsername(username) == null) || (db.GetDeckByUsername(username).Count == 0))
                {
                    this.response = ResponseHandler.GetResponseMessage(204, "text/plain", "The request was fine, but the deck doesn't have any cards");
                }
                else
                {
                    List<Card> userDeck = db.GetDeckByUsername(username);
                    string userDeckToString = String.Join("\n", userDeck.Select(x => x.ToString()).ToArray());
                    this.response = ResponseHandler.GetResponseMessage(200, "text/plain", "The deck has cards, the response contains these") + "\r\n" + userDeckToString + "\r\n";
                }
            }
            else if (this.request.StartsWith("GET /deck"))
            {
                string token = GetTokenFromRequest(this.request);
                string username = GetUsernameFromToken(token);
                if ((token.Equals("")) || (db.TokenExist(token) == false))
                {
                    this.response = ResponseHandler.GetResponseMessage(401, "application/json", "Access token is missing or invalid");
                }
                else if ((db.GetDeckByUsername(username) == null) || (db.GetDeckByUsername(username).Count == 0))
                {
                    this.response = ResponseHandler.GetResponseMessage(204, "application/json", "The request was fine, but the deck doesn't have any cards");
                }
                else
                {
                    List<Card> userDeck = db.GetDeckByUsername(username);
                    string userDeckToJSON = JsonSerializer.Serialize(userDeck, new JsonSerializerOptions { WriteIndented = true });
                    this.response = ResponseHandler.GetResponseMessage(200, "application/json", "The deck has cards, the response contains these") + "\r\n" + userDeckToJSON + "\r\n";
                }
            }
            else if (this.request.StartsWith("PUT /deck"))
            {
                string token = GetTokenFromRequest(this.request);
                string username = GetUsernameFromToken(token);
                string body = GetLastLineFromRequest(this.request);
                List<string> cardIDs = GetCardIDsFromRequestBodyDeck(body);
                bool cardExists = cardIDs.Any(cardID => string.IsNullOrEmpty(cardID) || (db.CardExist(cardID) && db.UserHasCards(username, cardID)));
                if ((token.Equals("")) || (db.TokenExist(token) == false))
                {
                    this.response = ResponseHandler.GetResponseMessage(401, "application/json", "Access token is missing or invalid");
                } else if(db.UserExist(username) && cardExists)
                {
                    if(cardIDs.Count == 4)
                    {
                        db.AddCardsToUserDeck(username, cardIDs);
                        this.response = ResponseHandler.GetResponseMessage(200, "application/json", "The deck has been successfully configured");
                    }
                    else
                    {
                        this.response = ResponseHandler.GetResponseMessage(400, "application/json", "The provided deck did not include the required amount of cards");
                    }
                }
                else
                {
                    this.response = ResponseHandler.GetResponseMessage(403, "application/json", "At least one of the provided cards does not belong to the user or is not available.");
                }
            }
            else if (this.request.StartsWith("GET /users/"))
            {
                string username = GetUsernameFromRequest(this.request);
                string token = GetTokenFromRequest(this.request);
                if(db.UserExist(username) == false)
                {
                    this.response = ResponseHandler.GetResponseMessage(404, "application/json", "User not found.");
                }
                else if ((token.Equals("")) || (db.TokenExist(token) == false) || (username.Equals(GetUsernameFromToken(token)) == false))
                {
                    this.response = ResponseHandler.GetResponseMessage(401, "application/json", "Access token is missing or invalid");
                } 
                else
                {
                    User userData = db.GetUserDataByUsername(username);
                    // Create an anonymous type with the desired properties
                    var userDataSerializationModel = new
                    {
                        Name = userData.Name,
                        Bio = userData.Bio,
                        Image = userData.Image
                    };
                    string userDataToJson = JsonSerializer.Serialize(userDataSerializationModel, new JsonSerializerOptions { WriteIndented = true });
                    this.response = ResponseHandler.GetResponseMessage(200, "application/json", "Data successfully retrieved") + "\r\n" + userDataToJson + "\r\n";
                }
            }
            else if (this.request.StartsWith("PUT /users/"))
            {
                string username = GetUsernameFromRequest(this.request);
                string token = GetTokenFromRequest(this.request);
                if (db.UserExist(username) == false)
                {
                    this.response = ResponseHandler.GetResponseMessage(404, "application/json", "User not found.");
                }
                else if ((token.Equals("")) || (db.TokenExist(token) == false) || (username.Equals(GetUsernameFromToken(token)) == false))
                {
                    this.response = ResponseHandler.GetResponseMessage(401, "application/json", "Access token is missing or invalid");
                }
                else
                {
                    string body = GetLastLineFromRequest(this.request);
                    dynamic? user = JsonConvert.DeserializeObject(body);
                    db.UpdateUserDataByUsername(username, (string)user.Name, (string)user.Bio, (string)user.Image);
                    this.response = ResponseHandler.GetResponseMessage(200, "application/json", "User sucessfully updated.");
                }
            }
            else if (this.request.StartsWith("GET /stats"))
            {
                string token = GetTokenFromRequest(this.request);
                string username = GetUsernameFromToken(token);
                if ((token.Equals("")) || (db.TokenExist(token) == false))
                {
                    this.response = ResponseHandler.GetResponseMessage(401, "application/json", "Access token is missing or invalid");
                }
                else
                {
                    User userStats = db.GetStatsByUsername(username);
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
                    this.response = ResponseHandler.GetResponseMessage(200, "application/json", "The stats could be retrieved successfully.") + "\r\n" + userStatsToJSON + "\r\n";
                }
            }
            else if (this.request.StartsWith("GET /scoreboard"))
            {
                string token = GetTokenFromRequest(this.request);
                string username = GetUsernameFromToken(token);
                if ((token.Equals("")) || (db.TokenExist(token) == false))
                {
                    this.response = ResponseHandler.GetResponseMessage(401, "application/json", "Access token is missing or invalid");
                }
                else
                {
                    List<User> scoreboard = db.GetScoreboard();
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
                    this.response = ResponseHandler.GetResponseMessage(200, "application/json", "The scoreboard could be retrieved successfully.") + "\r\n" + scoreboardToJSON + "\r\n";
                }
            }
            else if (this.request.StartsWith("POST /battles"))
            {
                // needs work
            }
            else if (this.request.StartsWith("GET /tradings"))
            {
                string token = GetTokenFromRequest(this.request);
                string username = GetUsernameFromToken(token);
                if ((token.Equals("")) || (db.TokenExist(token) == false))
                {
                    this.response = ResponseHandler.GetResponseMessage(401, "application/json", "Access token is missing or invalid");
                }
                else if((db.GetTrades() == null) || (db.GetTrades().Count == 0))
                {
                    this.response = ResponseHandler.GetResponseMessage(204, "application/json", "The request was fine, but there are no trading deals available");
                }
                else
                {
                    List<Trading> trades = db.GetTrades();
                    // Creating a list of anonymous objects for serialization
                    var tradesSerializationModel = trades.Select(trade => new
                    {
                        ID = trade.ID,
                        CardToTrade = trade.CardToTrade,
                        Type = trade.Type,
                        MinimumDamage = trade.MinimumDamage
                    }).ToList();
                    string tradesToJSON = JsonSerializer.Serialize(tradesSerializationModel, new JsonSerializerOptions { WriteIndented = true });
                    this.response = ResponseHandler.GetResponseMessage(200, "application/json", "There are trading deals available, the response contains these") + "\r\n" + tradesToJSON + "\r\n";
                }
            }
            else if (this.request.StartsWith("POST /tradings/"))
            {
                string token = GetTokenFromRequest(this.request);
                string username = GetUsernameFromToken(token);
                string tradeID = GetTradingIDFromRequest(this.request);
                string body = GetLastLineFromRequest(this.request);
                string cardIDFromBody = GetCardIDFromRequestBodyTradings(body);

                if ((token.Equals("")) || (db.TokenExist(token) == false))
                {
                    this.response = ResponseHandler.GetResponseMessage(401, "application/json", "Access token is missing or invalid");
                }
                else if (db.OpenTradeExist(tradeID) == false)
                {
                    this.response = ResponseHandler.GetResponseMessage(404, "application/json", "The provided deal ID was not found.");
                }
                else
                {
                    Trading trade = db.GetTradeByTradingID(tradeID);
                    string traderUsername = db.GetUsernameByUserID(trade.TraderID);
                    Card card = db.GetCardByUsernameAndCardID(username, cardIDFromBody);
                    if ((db.UserHasCards(username, trade.CardToTrade)) || (db.UserHasCards(traderUsername, cardIDFromBody)) || ((trade.Type.Equals(card.Type)) && (trade.MinimumDamage >= card.Damage)) || (db.CardInDeck(traderUsername, trade.CardToTrade)))
                    {
                        this.response = ResponseHandler.GetResponseMessage(403, "application/json", "The offered card is owned by the user, or the requirements are not met (Type, MinimumDamage), or the offered card is locked in the deck.");
                    }
                    else
                    {
                        db.TradeCards(tradeID, traderUsername, username, trade.CardToTrade, cardIDFromBody);
                        this.response = ResponseHandler.GetResponseMessage(200, "application/json", "Trading deal successfully executed.");
                    }
                }
            }
            else if (this.request.StartsWith("POST /tradings"))
            {
                string token = GetTokenFromRequest(this.request);
                string username = GetUsernameFromToken(token);
                string body = GetLastLineFromRequest(this.request);
                dynamic? trade = JsonConvert.DeserializeObject(body);
                if ((token.Equals("")) || (db.TokenExist(token) == false))
                {
                    this.response = ResponseHandler.GetResponseMessage(401, "application/json", "Access token is missing or invalid");
                }
                else if((db.UserHasCards(username, (string)trade.CardToTrade) == false) || (db.CardInDeck(username, (string)trade.CardToTrade)))
                {
                    this.response = ResponseHandler.GetResponseMessage(403, "application/json", "The deal contains a card that is not owned by the user or locked in the deck.");
                }
                else if(db.OpenTradeExist((string)trade.Id))
                {
                    this.response = ResponseHandler.GetResponseMessage(409, "application/json", "A deal with this deal ID already exists.");
                }
                else
                {
                    db.CreateTrade((string)trade.Id, username, (string)trade.CardToTrade, (string)trade.Type, (string)trade.MinimumDamage);
                    this.response = ResponseHandler.GetResponseMessage(201, "application/json", "Trading deal successfully created");
                }
            }
            else if (this.request.StartsWith("DELETE /tradings/"))
            {
                string token = GetTokenFromRequest(this.request);
                string username = GetUsernameFromToken(token);
                string tradeID = GetTradingIDFromRequest(this.request);
                Trading trade = db.GetTradeByTradingID(tradeID);
                if ((token.Equals("")) || (db.TokenExist(token) == false))
                {
                    this.response = ResponseHandler.GetResponseMessage(401, "application/json", "Access token is missing or invalid");
                }
                else if (db.OpenTradeExist(tradeID) == false)
                {
                    this.response = ResponseHandler.GetResponseMessage(404, "application/json", "The provided deal ID was not found.");
                }
                else if (db.UserHasCards(username, trade.CardToTrade) == false)
                {
                    this.response = ResponseHandler.GetResponseMessage(403, "application/json", "The deal contains a card that is not owned by the user.");
                }
                else
                {
                    db.DeleteTradeByID(tradeID);
                    this.response = ResponseHandler.GetResponseMessage(200, "application/json", "Trading deal successfully deleted");
                }
            }
            else if(this.request.StartsWith("POST /daily-login"))
            {

            }
            else
            {
                this.response = ResponseHandler.GetResponseMessage(400, "application/json", "Command not found");
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

    }
}
