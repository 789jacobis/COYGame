using System.Collections.Generic;
using UnityEngine;

namespace COYGame
{
    public static class TargetResolver
    {
        public static TeamRuntime ResolveTeam(CardTargetData target, TurnContext context)
        {
            return target.side == TargetSide.Opponent ? context.OpposingTeam : context.ActingTeam;
        }

        public static HoopState ResolveHoop(CardTargetData target, TurnContext context)
        {
            return context.TargetHoop;
        }

        public static List<CardRuntime> ResolveCards(CardRuntime sourceCard, CardTargetData target, TurnContext context)
        {
            return ResolveCards(sourceCard, target, context, null);
        }

        public static List<CardRuntime> ResolveCards(CardRuntime sourceCard, CardTargetData target, TurnContext context, IReadOnlyList<CardConditionData> conditions)
        {
            var candidates = GetCardsInZone(sourceCard, target, context);
            if (target.ownershipScope == OwnershipScope.OwnerOnly && sourceCard.Owner != null)
            {
                candidates = candidates.FindAll(card => card.Owner == sourceCard.Owner);
            }

            candidates = ApplyCardConditions(candidates, conditions);

            return target.selector switch
            {
                TargetSelectorType.ThisCard => new List<CardRuntime> { sourceCard },
                TargetSelectorType.Random => PickRandom(candidates, target.count),
                TargetSelectorType.LowestCostCard => PickLowestCost(candidates, target.count),
                TargetSelectorType.All or TargetSelectorType.CardsInZone => candidates,
                _ => candidates
            };
        }

        private static List<CardRuntime> ApplyCardConditions(List<CardRuntime> candidates, IReadOnlyList<CardConditionData> conditions)
        {
            if (conditions == null)
            {
                return candidates;
            }

            foreach (var condition in conditions)
            {
                if (condition == null)
                {
                    continue;
                }

                if (condition.conditionType == CardConditionType.CardCostGreaterThan)
                {
                    candidates = candidates.FindAll(card => card.CurrentCost > condition.intValue);
                }
            }

            return candidates;
        }

        private static List<CardRuntime> GetCardsInZone(CardRuntime sourceCard, CardTargetData target, TurnContext context)
        {
            if (target.selector == TargetSelectorType.ThisCard)
            {
                return new List<CardRuntime> { sourceCard };
            }

            if (target.side == TargetSide.Opponent)
            {
                return new List<CardRuntime>();
            }

            return target.zone switch
            {
                CardZone.Hand => new List<CardRuntime>(context.Hand),
                CardZone.DrawPile => new List<CardRuntime>(context.Deck?.Deck ?? new List<CardRuntime>()),
                CardZone.DiscardPile => new List<CardRuntime>(context.Deck?.DiscardPile ?? new List<CardRuntime>()),
                CardZone.Reserved => new List<CardRuntime>(context.Deck?.Reserved ?? new List<CardRuntime>()),
                CardZone.OutsideGame => new List<CardRuntime>(context.Deck?.OutsideGame ?? new List<CardRuntime>()),
                _ => new List<CardRuntime>()
            };
        }

        private static List<CardRuntime> PickRandom(List<CardRuntime> candidates, int count)
        {
            var results = new List<CardRuntime>();
            var pool = new List<CardRuntime>(candidates);
            var take = Mathf.Min(Mathf.Max(0, count), pool.Count);
            for (var i = 0; i < take; i++)
            {
                var index = Random.Range(0, pool.Count);
                results.Add(pool[index]);
                pool.RemoveAt(index);
            }

            return results;
        }

        private static List<CardRuntime> PickLowestCost(List<CardRuntime> candidates, int count)
        {
            candidates.Sort((left, right) => left.CurrentCost.CompareTo(right.CurrentCost));
            return candidates.GetRange(0, Mathf.Min(Mathf.Max(0, count), candidates.Count));
        }
    }
}
