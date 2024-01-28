using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MonsterTradingCardGame
{
    public class Battle
    {
        public string Log {  get; set; }
        public Player PlayerA {  get; set; } 
        public Player PlayerB {  get; set; } 
        public Database Database { get; set; }

        public Battle(Player player, Database database)
        {
            this.PlayerA = player;
            Database = database;
        }

        public void AddPlayer(Player player)
        {
            this.PlayerB = player;
        }

        public void StartBattle()
        {
            string log = "";

            User userAStats = Database.GetStatsByUsername(this.PlayerA.Username);
            User userBStats = Database.GetStatsByUsername (this.PlayerB.Username);

            for (int round = 0; round < 100; round++)
            {
                if (this.PlayerA.Deck.Count == 0 || this.PlayerB.Deck.Count == 0)
                {
                    if (this.PlayerA.Deck.Count == 0)
                    {
                        Database.UpdateUserStatsByUsername(this.PlayerB.Username, userBStats.Elo + 3, userBStats.GamesPlayed + 1, userBStats.Wins + 1, userBStats.Draws, userBStats.Losses);
                        Database.UpdateUserStatsByUsername(this.PlayerA.Username, userAStats.Elo - 5, userAStats.GamesPlayed + 1, userAStats.Wins, userAStats.Draws, userAStats.Losses + 1);
                        log += $"PlayerB wins\n";
                    } 
                    else
                    {
                        Database.UpdateUserStatsByUsername(this.PlayerA.Username, userAStats.Elo + 3, userAStats.GamesPlayed + 1, userAStats.Wins + 1, userAStats.Draws, userAStats.Losses);
                        Database.UpdateUserStatsByUsername(this.PlayerB.Username, userBStats.Elo - 5, userBStats.GamesPlayed + 1, userBStats.Wins, userBStats.Draws, userBStats.Losses + 1);
                        log += $"PlayerA wins\n";
                    }
                    break; // End the battle if a player runs out of cards
                }
                    
                Card cardA = this.PlayerA.Deck[new Random().Next(this.PlayerA.Deck.Count)];
                Card cardB = this.PlayerB.Deck[new Random().Next(this.PlayerB.Deck.Count)];

                if (cardA.IsMonster() && cardB.IsMonster())
                {
                    // Monster fight
                    log += MonsterFight(this.PlayerA, cardA, this.PlayerB, cardB);
                }
                else if (!cardA.IsMonster() && !cardB.IsMonster())
                {
                    // Spell fight
                    log += SpellFight(this.PlayerA, cardA, this.PlayerB, cardB);
                }
                else
                {
                    // Mixed fight
                    log += MixedFight(this.PlayerA, cardA, this.PlayerB, cardB);
                }
            }

            Database.UpdateUserStatsByUsername(this.PlayerA.Username, userAStats.Elo, userAStats.GamesPlayed + 1, userAStats.Wins, userAStats.Draws + 1, userAStats.Losses);
            Database.UpdateUserStatsByUsername(this.PlayerB.Username, userBStats.Elo, userBStats.GamesPlayed + 1, userBStats.Wins, userBStats.Draws + 1, userBStats.Losses);
            log += $"Draw\n";

            this.Log = log;
        }

        public string MonsterFight(Player playerA, Card cardA, Player playerB, Card cardB)
        {
            string log = $"Monster Fight: PlayerA: {cardA.Name} ({cardA.Damage} Damage) vs PlayerB: {cardB.Name} ({cardB.Damage} Damage) => ";

            if (cardA.Name == "WaterGoblin" && cardB.Name == "FireTroll")
            {
                log += $"Troll defeats Goblin\n";
                playerA.Deck.Remove(cardA);
                playerB.Deck.Add(cardA);
            }
            else if (cardB.Name == "WaterGoblin" && cardA.Name == "FireTroll")
            {
                log += $"Troll defeats Goblin\n";
                playerB.Deck.Remove(cardB);
                playerA.Deck.Add(cardB);
            } 
            else if (cardA.Name.ToLower().Contains("goblin") && cardB.Name.ToLower().Contains("dragon"))
            {
                log += $"Goblins are too afraid of Dragons to attack.\n";
                playerA.Deck.Remove(cardA);
                playerB.Deck.Add(cardA);
            }
            else if (cardB.Name.ToLower().Contains("goblin") && cardA.Name.ToLower().Contains("dragon"))
            {
                log += $"Goblins are too afraid of Dragons to attack.\n";
                playerB.Deck.Remove(cardB);
                playerA.Deck.Add(cardB);
            }
            else if (cardA.Name.ToLower().Contains("ork") && cardB.Name.ToLower().Contains("wizzard"))
            {
                log += $"Wizzard can control Orks so they are not able to damage them.\n";
                playerA.Deck.Remove(cardA);
                playerB.Deck.Add(cardA);
            }
            else if (cardB.Name.ToLower().Contains("ork") && cardA.Name.ToLower().Contains("wizzard"))
            {
                log += $"Wizzard can control Orks so they are not able to damage them.\n";
                playerB.Deck.Remove(cardB);
                playerA.Deck.Add(cardB);
            }
            else if (cardA.Name.ToLower().Contains("dragon") && cardB.Name.ToLower().Contains("fireelve"))
            {
                log += $"The FireElves know Dragons since they were little and can evade their attacks.\n";
                playerA.Deck.Remove(cardA);
                playerB.Deck.Add(cardA);
            }
            else if (cardB.Name.ToLower().Contains("dragon") && cardA.Name.ToLower().Contains("fireelve"))
            {
                log += $"The FireElves know Dragons since they were little and can evade their attacks.\n";
                playerB.Deck.Remove(cardB);
                playerA.Deck.Add(cardB);
            }
            else
            {
                decimal damageA = cardA.Damage;
                decimal damageB = cardB.Damage;

                if (damageA > damageB)
                {
                    log += $"{cardA.Name} defeats {cardB.Name}\n";
                    playerB.Deck.Remove(cardB);
                    playerA.Deck.Add(cardB);
                }
                else if (damageB > damageA)
                {
                    log += $"{cardB.Name} defeats {cardA.Name}\n";
                    playerA.Deck.Remove(cardA);
                    playerB.Deck.Add(cardA);
                }
                else
                {
                    log += $"Draw  (no action)\n";
                }
            }

            return log;
        }

        public string SpellFight(Player playerA, Card cardA, Player playerB, Card cardB)
        {
            string log = $"Spell Fight: PlayerA: {cardA.Name} ({cardA.Damage} Damage) vs PlayerB: {cardB.Name} ({cardB.Damage} Damage) => ";

            decimal damageA = cardA.Damage;
            decimal damageB = cardB.Damage;

            if (cardA.Element == cardB.Element)
            {
                log += $"{damageA} VS {damageB} -> ";
                if (damageA > damageB)
                {
                    log += $"{damageA / 2} VS {damageB / 2} => {cardA.Name} wins\n";
                    playerB.Deck.Remove(cardB);
                    playerA.Deck.Add(cardB);
                }
                else if (damageB > damageA)
                {
                    log += $"{damageA / 2} VS {damageB / 2} => {cardB.Name} wins\n";
                    playerA.Deck.Remove(cardA);
                    playerB.Deck.Add(cardA);
                }
                else
                {
                    log += "Draw (no action)\n";
                }
            }
            else if ((cardA.Element.ToLower() == "water" && cardB.Element.ToLower() == "fire") ||
                     (cardA.Element.ToLower() == "fire" && cardB.Element.ToLower() == "normal") ||
                     (cardA.Element.ToLower() == "normal" && cardB.Element.ToLower() == "water"))
            {
                log += $"{damageA} VS {damageB} -> {damageA / 2} VS {damageB * 2} => {cardB.Name} wins\n";
                playerA.Deck.Remove(cardA);
                playerB.Deck.Add(cardA);
            }
            else
            {
                log += $"{damageA} VS {damageB} -> {damageA * 2} VS {damageB / 2} => {cardA.Name} wins\n";
                playerB.Deck.Remove(cardB);
                playerA.Deck.Add(cardB);
            }

            return log;
        }

        public string MixedFight(Player playerA, Card cardA, Player playerB, Card cardB)
        {
            string log = $"Mixed Fight: PlayerA: {cardA.Name} ({cardA.Damage} Damage) vs PlayerB: {cardB.Name} ({cardB.Damage} Damage) => ";

            if (cardA.IsMonster())
            {
                // Swap cards
                Card temp = cardA;
                cardA = cardB;
                cardB = temp;
            }

            if (cardA.Name == "FireSpell" && cardB.Name == "WaterGoblin")
            {
                log += $"{cardA.Damage} vs {cardB.Damage} -> {cardA.Damage / 2} vs {cardB.Damage * 2} => {cardB.Name} wins\n";
                playerA.Deck.Remove(cardA);
                playerB.Deck.Add(cardA);
            }
            else if (cardA.Name == "WaterSpell" && cardB.Name == "Knight")
            {
                log += $"{cardA.Damage} vs {cardB.Damage} -> {cardA.Damage / 2} vs {cardB.Damage * 2} => {cardB.Name} wins\n";
                playerA.Deck.Remove(cardA);
                playerB.Deck.Add(cardA);
            }
            else if (cardA.Name == "RegularSpell" && cardB.Name == "WaterGoblin")
            {
                log += $"{cardA.Damage} vs {cardB.Damage} -> {cardA.Damage * 2} vs {cardB.Damage / 2} => {cardA.Name} wins\n";
                playerB.Deck.Remove(cardB);
                playerA.Deck.Add(cardB);
            }
            else if (cardA.Name.ToLower().Contains("knight") && cardB.Name.ToLower().Contains("waterspell"))
            {
                log += $"The armor of Knights is so heavy that WaterSpells make them drown them instantly.\n";
                playerA.Deck.Remove(cardA);
                playerB.Deck.Add(cardA);
            }
            else if (cardB.Name.ToLower().Contains("knight") && cardA.Name.ToLower().Contains("waterspell"))
            {
                log += $"The armor of Knights is so heavy that WaterSpells make them drown them instantly.\n";
                playerB.Deck.Remove(cardB);
                playerA.Deck.Add(cardB);
            }
            else if (cardA.Name.ToLower().Contains("spell") && cardB.Name.ToLower().Contains("kraken"))
            {
                log += $"The Kraken is immune against spells.\n";
                playerA.Deck.Remove(cardA);
                playerB.Deck.Add(cardA);
            }
            else if (cardB.Name.ToLower().Contains("spell") && cardA.Name.ToLower().Contains("kraken"))
            {
                log += $"The Kraken is immune against spells.\n";
                playerB.Deck.Remove(cardB);
                playerA.Deck.Add(cardB);
            }
            else
            {
                log += $"{cardA.Damage} vs {cardB.Damage} -> {cardA.Damage / 2} vs {cardB.Damage * 2} => {cardB.Name} wins\n";
                playerA.Deck.Remove(cardA);
                playerB.Deck.Add(cardA);
            }

            return log;
        }
    }
}