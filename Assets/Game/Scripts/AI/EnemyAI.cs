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
                if (card.Data.apCost > ap)
                {
                    continue;
                }

                result.Add(card);
                ap -= card.Data.apCost;
            }

            return result;
        }

        private int EstimateValue(CardRuntime card)
        {
            return card.Data.effectType == CardEffectType.GainShield || card.Data.effectType == CardEffectType.ReduceNextIncomingAttack
                ? Mathf.RoundToInt(card.Owner.defense * card.Data.powerMultiplier)
                : Mathf.RoundToInt(card.Owner.attack * Mathf.Max(0.1f, card.Data.powerMultiplier));
        }
    }
}
