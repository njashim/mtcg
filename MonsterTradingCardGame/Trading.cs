using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonsterTradingCardGame
{
    public class Trading
    {
        public string ID { get; set; }
        public int TraderID { get; set; }
        public string CardToTrade { get; set; }
        public string Type { get; set; }
        public decimal MinimumDamage { get; set; }
        public bool IsClosed { get; set; }
    }
}