using UnityEngine;

namespace COYGame
{
    public static class CardEffectResolver
    {
        public static string Resolve(CardRuntime card, TurnContext context)
        {
            var data = card.Data;
            switch (data.effectType)
            {
                case CardEffectType.DealDamage:
                {
                    var raw = Mathf.RoundToInt(card.Owner.attack * data.powerMultiplier * context.OutgoingAttackMultiplier * context.NextIncomingAttackMultiplier);
                    var dealt = context.TargetHoop.ApplyDamage(raw);
                    context.NextIncomingAttackMultiplier = 1f;
                    return $"{card.Owner.playerName} played {data.cardName}: {dealt} damage";
                }
                case CardEffectType.GainShield:
                {
                    var shield = Mathf.RoundToInt(card.Owner.defense * data.powerMultiplier);
                    context.TargetHoop.AddShield(shield);
                    return $"{card.Owner.playerName} played {data.cardName}: +{shield} shield";
                }
                case CardEffectType.BuffOutgoingAttackThisTurn:
                    context.OutgoingAttackMultiplier += data.percentageValue;
                    return $"{data.cardName}: attack damage +{Mathf.RoundToInt(data.percentageValue * 100f)}% this phase";
                case CardEffectType.ReduceNextIncomingAttack:
                {
                    var shield = Mathf.RoundToInt(card.Owner.defense * data.powerMultiplier);
                    context.TargetHoop.AddShield(shield);
                    context.NextIncomingAttackMultiplier *= Mathf.Clamp01(1f - data.percentageValue);
                    return $"{data.cardName}: +{shield} shield, next incoming attack -{Mathf.RoundToInt(data.percentageValue * 100f)}%";
                }
                case CardEffectType.ModifyOpponentNextTurnAp:
                {
                    if (data.powerMultiplier > 0f)
                    {
                        var shield = Mathf.RoundToInt(card.Owner.defense * data.powerMultiplier);
                        context.TargetHoop.AddShield(shield);
                    }

                    context.OpposingTeam.NextTurnApModifier += data.flatValue;
                    return $"{data.cardName}: opponent next turn AP {data.flatValue:+#;-#;0}";
                }
                case CardEffectType.ModifyCurrentTurnAp:
                    context.MaxAp = Mathf.Max(0, context.MaxAp + data.flatValue);
                    context.Ap = Mathf.Clamp(context.Ap + data.flatValue, 0, context.MaxAp);
                    return $"{data.cardName}: AP {data.flatValue:+#;-#;0}";
                case CardEffectType.DrawCards:
                    context.ActingTeam.NextTurnDrawModifier += data.flatValue;
                    return $"{data.cardName}: next draw count {data.flatValue:+#;-#;0}";
                default:
                    return $"{data.cardName}: no effect";
            }
        }
    }
}
