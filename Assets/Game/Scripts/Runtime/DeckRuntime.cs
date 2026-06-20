using System;
using System.Collections.Generic;
using UnityEngine;

namespace COYGame
{
    public sealed class DeckRuntime
    {
        private readonly List<CardRuntime> deck = new();
        private readonly List<CardRuntime> discard = new();
        private readonly System.Random random;

        public DeckRuntime(IEnumerable<CardRuntime> cards, int seed)
        {
            random = new System.Random(seed);
            deck.AddRange(cards);
            Shuffle(deck);
        }

        public IReadOnlyList<CardRuntime> Deck => deck;
        public IReadOnlyList<CardRuntime> DiscardPile => discard;
        public int DeckCount => deck.Count;
        public int DiscardCount => discard.Count;

        public List<CardRuntime> Draw(int count)
        {
            var hand = new List<CardRuntime>();
            for (var i = 0; i < count; i++)
            {
                if (deck.Count == 0)
                {
                    RecycleDiscard();
                }

                if (deck.Count == 0)
                {
                    break;
                }

                var card = deck[0];
                deck.RemoveAt(0);
                hand.Add(card);
            }

            return hand;
        }

        public void DiscardCard(CardRuntime card)
        {
            if (card == null)
            {
                return;
            }

            card.WasPlayed = false;
            discard.Add(card);
        }

        public void DiscardMany(IEnumerable<CardRuntime> cards)
        {
            foreach (var card in cards)
            {
                DiscardCard(card);
            }
        }

        private void RecycleDiscard()
        {
            if (discard.Count == 0)
            {
                return;
            }

            deck.AddRange(discard);
            discard.Clear();
            Shuffle(deck);
        }

        private void Shuffle<T>(IList<T> list)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
