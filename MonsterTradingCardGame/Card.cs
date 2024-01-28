using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MonsterTradingCardGame
{
    public class Card
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public decimal Damage { get; set; }
        public string Element { get; set; }
        public string Type { get; set; }
        [JsonIgnore]
        public int UserID { get; set; }
        [JsonIgnore]
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

        public bool IsMonster()
        {
            if (Type.Equals("Monster"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override string? ToString()
        {
            return $"ID: {ID}, Name: {Name}, Damage: {Damage}\n";
        }
    }
}