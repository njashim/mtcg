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
                string body = GetLastLine(this.request);
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
                string body = GetLastLine(this.request);
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
                string body = GetLastLine(this.request);
                List<string> cardIDs = GetCardIDsFromRequestBody(body);
                if (GetTokenFromRequest(this.request).Equals(""))
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
                if (GetTokenFromRequest(this.request).Equals(""))
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
                if (GetTokenFromRequest(this.request).Equals(""))
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
            else if (this.request.StartsWith("GET /deck"))
            {
                string token = GetTokenFromRequest(this.request);
                string username = GetUsernameFromToken(token);
                if (GetTokenFromRequest(this.request).Equals(""))
                {
                    this.response = ResponseHandler.GetResponseMessage(401, "application/json", "Access token is missing or invalid");
                }
                else if ((db.GetDeckByUsername(username) == null) || (db.GetDeckByUsername(username).Count == 0))
                {
                    this.response = ResponseHandler.GetResponseMessage(204, "application/json", "The request was fine, but the deck doesn't have any cards");
                }
                else
                {
                    List<Card> userDeck = db.GetCardsByUsername(username);
                    string userDeckToJSON = JsonSerializer.Serialize(userDeck, new JsonSerializerOptions { WriteIndented = true });
                    this.response = ResponseHandler.GetResponseMessage(200, "application/json", "The user has cards, the response contains these") + "\r\n" + userDeckToJSON + "\r\n";
                }
            }
            else if (this.request.StartsWith("GET /users/{username}"))
            {
                // needs work
            }
            else if (this.request.StartsWith("PUT /users/{username}"))
            {
                // needs work
            }
            else if (this.request.StartsWith("PUT /deck"))
            {
                // needs work
            }
            else if (this.request.StartsWith("GET /stats"))
            {
                // needs work
            }
            else if (this.request.StartsWith("GET /scoreboard"))
            {
                // needs work
            }
            else if (this.request.StartsWith("POST /battles"))
            {
                // needs work
            }
            else if (this.request.StartsWith("GET /tradings"))
            {
                // needs work 
            }
            else if (this.request.StartsWith("POST /tradings"))
            {
                // needs work
            }
            else if (this.request.StartsWith("DELETE /tradings/{tradingdealid}"))
            {
                // needs work
            }
            else if (this.request.StartsWith("POST /tradings/{tradingdealid}"))
            {
                // needs work
            }
            else
            {
                // needs work
            }
        }

        public string GetLastLine(string request)
        {
            if (string.IsNullOrEmpty(request))
            {
                return string.Empty;
            }

            string[] lines = request.Split('\n');
            return lines.LastOrDefault()?.Trim() ?? string.Empty;
        }

        public string GetTokenFromRequest(string request)
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

        public List<string> GetCardIDsFromRequestBody(string requestBody)
        {
            List<string> idValues = new List<string>();

            try
            {
                // JSON-Array parsen
                JArray jsonArray = JArray.Parse(requestBody);

                // Id-Werte extrahieren
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
                Console.WriteLine($"Fehler beim Extrahieren der Id-Werte: {ex.Message}");
            }

            return idValues;
        }

        public string GetUsernameFromToken(string token)
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
    }
}
