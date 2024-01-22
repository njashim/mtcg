using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonsterTradingCardGame
{
    public class Card
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public decimal Damage { get; set; }
        public string Element { get; set; }
        public string Type { get; set; }
        public int UserID { get; set; }
        public int PackID { get; set; }

        /*public enum ElementType
        {
            Water,
            Fire,
            Normal
        }

        public enum CardType
        {
            Monster,
            Spell
        }*/

        public override string? ToString()
        {
            return $"ID: {ID}, Name: {Name}, Damage: {Damage}\n";
        }
    }
}
