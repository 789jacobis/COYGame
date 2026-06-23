using System.Collections.Generic;

namespace COYGame
{
    public sealed class TeamRuntime
    {
        public TeamRuntime(TeamSide side, IReadOnlyList<PlayerData> players, int seed)
        {
            Side = side;
            Players = players;

            var attackCards = new List<CardRuntime>();
            var defenseCards = new List<CardRuntime>();
            foreach (var player in players)
            {
                foreach (var card in player.attackCards)
                {
                    attackCards.Add(new CardRuntime(card, player));
                }

                foreach (var card in player.defenseCards)
                {
                    defenseCards.Add(new CardRuntime(card, player));
                }
            }

            AttackDeck = new DeckRuntime(attackCards, seed);
            DefenseDeck = new DeckRuntime(defenseCards, seed + 97);
        }

        public TeamSide Side { get; }
        public IReadOnlyList<PlayerData> Players { get; }
        public DeckRuntime AttackDeck { get; }
        public DeckRuntime DefenseDeck { get; }
        public int Score { get; set; }
        public int NextTurnApModifier { get; set; }
        public int NextTurnDrawModifier { get; set; }
        public int PendingReboundCards { get; set; }
    }
}
