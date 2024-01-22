using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonsterTradingCardGame
{
    public class User
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public int Coins { get; set; }
        public int Elo { get; set; }
        public int GamesPlayed { get; set; }
        //public List<Card> Stack { get; set; } = new List<Card>();
        //public List<Card> Deck { get; set; } = new List<Card>();

        public User(string username, string password)
        {
            Username = username;
            Password = password;
        }
    }
}
