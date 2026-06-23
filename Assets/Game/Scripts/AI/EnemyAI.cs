using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace COYGame
{
    public sealed class EnemyAI
    {
        public ScoreStrategy ChooseStrategy(int round, TeamRuntime enemy, TeamRuntime player)
        {
            return round % 3 == 0 ? ScoreStrategy.ThreePoint : ScoreStrategy.TwoPoint;
        }

        public List<CardRuntime> ChooseCards(TurnContext context)
        {
            var result = new List<CardRuntime>();
            var sorted = new List<CardRuntime>(context.Hand);
            sorted.Sort((a, b) => EstimateValue(b).CompareTo(EstimateValue(a)));

            var ap = context.Ap;
            foreach (var card in sorted)
            {
                if (card.CurrentCost > ap)
                {
                    continue;
                }

                result.Add(card);
                ap -= card.CurrentCost;
            }

            return result;
        }

        private int EstimateValue(CardRuntime card)
        {
            var total = 0;
            var ownerAttack = card.Owner != null ? card.Owner.attack : 0;
            var ownerDefense = card.Owner != null ? card.Owner.defense : 0;
            foreach (var effect in card.Data.Effects)
            {
                total += effect.effectType switch
                {
                    CardEffectType.GainShield or CardEffectType.ReduceNextIncomingAttack or CardEffectType.GainBonusShieldIfStrategy =>
                        Mathf.RoundToInt(ownerDefense * Mathf.Max(0.1f, effect.powerMultiplier)),
                    CardEffectType.DrawCardsNow => effect.flatValue * 30,
                    CardEffectType.ModifyCurrentTurnAp => effect.flatValue * 40,
                    CardEffectType.ModifyHandCardCostsThisPhase => Mathf.Abs(effect.flatValue) * 25,
                    CardEffectType.ModifyRandomHandCardCostThisPhase => Mathf.Abs(effect.flatValue) * 25,
                    CardEffectType.BuffNextAttackCard => Mathf.RoundToInt(effect.percentageValue * 100f),
                    CardEffectType.ModifyOpponentNextTurnAp => Mathf.Abs(effect.flatValue) * 35,
                    _ => Mathf.RoundToInt(ownerAttack * Mathf.Max(0.1f, effect.powerMultiplier))
                };
            }

            return total;
        }
    }
}
