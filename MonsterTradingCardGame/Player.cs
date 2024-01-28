using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MonsterTradingCardGame
{
    public class Player
    {
        public TcpClient Client { get; set; }
        public string Username { get; set; }
        public List<Card> Deck { get; set; }

        public Player(TcpClient client, string username, List<Card> deck)
        {
            this.Client = client;
            this.Username = username;
            this.Deck = deck;
        }
    }
}
