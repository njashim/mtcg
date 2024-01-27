using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MonsterTradingCardGame
{
    public class User
    {
        public string Username { get; set; }
        public string Name { get; set; }
        [JsonIgnore]
        public string Password { get; set; }
        [JsonIgnore]
        public int Coins { get; set; }
        public int Elo { get; set; }
        public string Image { get; set; }
        public string Bio { get; set; }
        public int GamesPlayed { get; set; }
        public int Wins { get; set; }
        public int Draws { get; set; }
        public int Losses { get; set; }

        /*public User(string username, string password)
        {
            Username = username;
            Password = password;
        }*/
    }
}