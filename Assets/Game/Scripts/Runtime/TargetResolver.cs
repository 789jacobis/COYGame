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

        public static List<PlayerRuntime> ResolvePlayers(CardRuntime sourceCard, CardTargetData target, TurnContext context)
        {
            var candidates = GetPlayers(sourceCard, target, context);
            return target.selector switch
            {
                TargetSelectorType.OwnerPlayer => sourceCard.Owner != null
                    ? candidates.FindAll(player => player.Data == sourceCard.Owner)
                    : new List<PlayerRuntime>(),
                TargetSelectorType.HighestAttackPlayer => PickHighestAttack(candidates, target.count),
                TargetSelectorType.Random => PickRandom(candidates, target.count),
                TargetSelectorType.All => candidates,
                _ => candidates
            };
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

        private static List<PlayerRuntime> GetPlayers(CardRuntime sourceCard, CardTargetData target, TurnContext context)
        {
            var teams = new List<TeamRuntime>();
            if (target.side is TargetSide.Self or TargetSide.Both)
            {
                teams.Add(context.ActingTeam);
            }

            if (target.side is TargetSide.Opponent or TargetSide.Both)
            {
                teams.Add(context.OpposingTeam);
            }

            var players = new List<PlayerRuntime>();
            foreach (var team in teams)
            {
                if (team == null)
                {
                    continue;
                }

                players.AddRange(team.RuntimePlayers);
            }

            if (target.ownershipScope == OwnershipScope.OwnerOnly && sourceCard.Owner != null)
            {
                players = players.FindAll(player => player.Data == sourceCard.Owner);
            }

            return players;
        }

        private static List<PlayerRuntime> PickRandom(List<PlayerRuntime> candidates, int count)
        {
            var results = new List<PlayerRuntime>();
            var pool = new List<PlayerRuntime>(candidates);
            var take = Mathf.Min(Mathf.Max(0, count), pool.Count);
            for (var i = 0; i < take; i++)
            {
                var index = Random.Range(0, pool.Count);
                results.Add(pool[index]);
                pool.RemoveAt(index);
            }

            return results;
        }

        private static List<PlayerRuntime> PickHighestAttack(List<PlayerRuntime> candidates, int count)
        {
            candidates.Sort((left, right) => right.Attack.CompareTo(left.Attack));
            return candidates.GetRange(0, Mathf.Min(Mathf.Max(0, count), candidates.Count));
        }
    }
}
