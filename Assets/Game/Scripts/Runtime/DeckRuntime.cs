using System;
using System.Collections.Generic;
using UnityEngine;

namespace COYGame
{
    public sealed class DeckRuntime
    {
        private readonly List<CardRuntime> deck = new();
        private readonly List<CardRuntime> discard = new();
        private readonly List<CardRuntime> reserved = new();
        private readonly List<CardRuntime> outsideGame = new();
        private readonly System.Random random;

        public DeckRuntime(IEnumerable<CardRuntime> cards, int seed)
        {
            random = new System.Random(seed);
            deck.AddRange(cards);
            Shuffle(deck);
        }

        public IReadOnlyList<CardRuntime> Deck => deck;
        public IReadOnlyList<CardRuntime> DiscardPile => discard;
        public IReadOnlyList<CardRuntime> Reserved => reserved;
        public IReadOnlyList<CardRuntime> OutsideGame => outsideGame;
        public int DeckCount => deck.Count;
        public int DiscardCount => discard.Count;
        public int ReservedCount => reserved.Count;
        public int OutsideGameCount => outsideGame.Count;

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

            if (card.IsExhaust)
            {
                MoveOutsideGame(card);
                return;
            }

            if (card.IsOnce && card.WasPlayed)
            {
                MoveOutsideGame(card);
                return;
            }

            if (card.IsRetain && !card.WasPlayed)
            {
                card.WasPlayed = false;
                reserved.Add(card);
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

        public void ReleaseReservedToHand(List<CardRuntime> hand, Func<CardRuntime, bool> predicate)
        {
            for (var i = reserved.Count - 1; i >= 0; i--)
            {
                var card = reserved[i];
                if (predicate != null && !predicate(card))
                {
                    continue;
                }

                reserved.RemoveAt(i);
                card.WasPlayed = false;
                card.CurrentCost = card.Data.apCost;
                hand.Add(card);
            }
        }

        public void MoveOutsideGame(CardRuntime card)
        {
            if (card == null)
            {
                return;
            }

            card.WasPlayed = false;
            outsideGame.Add(card);
        }

        public bool RemoveFromZone(CardRuntime card, CardZone zone)
        {
            return zone switch
            {
                CardZone.DrawPile => deck.Remove(card),
                CardZone.DiscardPile => discard.Remove(card),
                CardZone.Reserved => reserved.Remove(card),
                CardZone.OutsideGame => outsideGame.Remove(card),
                _ => false
            };
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
