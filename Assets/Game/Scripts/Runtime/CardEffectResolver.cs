using UnityEngine;

namespace COYGame
{
    public static class CardEffectResolver
    {
        public static string Resolve(CardRuntime card, TurnContext context)
        {
            return Resolve(card, context, CardTrigger.OnPlay);
        }

        public static string Resolve(CardRuntime card, TurnContext context, CardTrigger trigger)
        {
            var messages = new System.Collections.Generic.List<string>();
            foreach (var effect in card.Data.Effects)
            {
                var message = effect.useV2Effect
                    ? EffectResolverV2.Resolve(card, context, effect, trigger)
                    : trigger == CardTrigger.OnPlay ? ResolveEffect(card, context, effect) : string.Empty;
                if (!string.IsNullOrWhiteSpace(message))
                {
                    messages.Add(message);
                }
            }

            return string.Join("\n", messages);
        }

        private static string ResolveEffect(CardRuntime card, TurnContext context, CardEffectData effect)
        {
            var data = card.Data;
            var ownerName = card.Owner != null ? card.Owner.playerName : "[Item]";
            var ownerAttack = card.Owner != null ? card.Owner.attack : 0;
            var ownerDefense = card.Owner != null ? card.Owner.defense : 0;
            switch (effect.effectType)
            {
                case CardEffectType.DealDamage:
                {
                    var raw = Mathf.RoundToInt(ownerAttack * effect.powerMultiplier * context.OutgoingAttackMultiplier * context.NextAttackCardMultiplier * context.NextIncomingAttackMultiplier);
                    var dealt = context.TargetHoop.ApplyDamage(raw);
                    context.NextAttackCardMultiplier = 1f;
                    context.NextIncomingAttackMultiplier = 1f;
                    return $"{ownerName} played {data.cardName}: {dealt} damage";
                }
                case CardEffectType.GainShield:
                {
                    var shield = Mathf.RoundToInt(ownerDefense * effect.powerMultiplier);
                    context.TargetHoop.AddShield(shield);
                    return $"{ownerName} played {data.cardName}: +{shield} shield";
                }
                case CardEffectType.BuffOutgoingAttackThisTurn:
                    context.OutgoingAttackMultiplier += effect.percentageValue;
                    return $"{data.cardName}: attack damage +{Mathf.RoundToInt(effect.percentageValue * 100f)}% this phase";
                case CardEffectType.ReduceNextIncomingAttack:
                {
                    var shield = Mathf.RoundToInt(ownerDefense * effect.powerMultiplier);
                    context.TargetHoop.AddShield(shield);
                    context.NextIncomingAttackMultiplier *= Mathf.Clamp01(1f - effect.percentageValue);
                    return $"{data.cardName}: +{shield} shield, next incoming attack -{Mathf.RoundToInt(effect.percentageValue * 100f)}%";
                }
                case CardEffectType.ModifyOpponentNextTurnAp:
                {
                    if (effect.powerMultiplier > 0f)
                    {
                        var shield = Mathf.RoundToInt(ownerDefense * effect.powerMultiplier);
                        context.TargetHoop.AddShield(shield);
                    }

                    context.OpposingTeam.NextTurnApModifier += effect.flatValue;
                    return $"{data.cardName}: opponent next turn AP {effect.flatValue:+#;-#;0}";
                }
                case CardEffectType.ModifyCurrentTurnAp:
                    context.MaxAp = Mathf.Max(0, context.MaxAp + effect.flatValue);
                    context.Ap = Mathf.Clamp(context.Ap + effect.flatValue, 0, context.MaxAp);
                    return $"{data.cardName}: AP {effect.flatValue:+#;-#;0}";
                case CardEffectType.DrawCards:
                    context.ActingTeam.NextTurnDrawModifier += effect.flatValue;
                    return $"{data.cardName}: next draw count {effect.flatValue:+#;-#;0}";
                case CardEffectType.DrawCardsNow:
                {
                    var drawn = context.Deck?.Draw(Mathf.Max(0, effect.flatValue)) ?? new System.Collections.Generic.List<CardRuntime>();
                    context.Hand.AddRange(drawn);
                    return $"{data.cardName}: draw {drawn.Count}";
                }
                case CardEffectType.ModifyHandCardCostsThisPhase:
                {
                    foreach (var handCard in context.Hand)
                    {
                        handCard.CurrentCost = Mathf.Max(0, handCard.CurrentCost + effect.flatValue);
                    }

                    return $"{data.cardName}: hand costs {effect.flatValue:+#;-#;0}";
                }
                case CardEffectType.ModifyRandomHandCardCostThisPhase:
                {
                    var candidates = context.Hand.FindAll(handCard => handCard.CurrentCost > 0);
                    if (candidates.Count == 0)
                    {
                        return $"{data.cardName}: no card to discount";
                    }

                    var handCard = candidates[Random.Range(0, candidates.Count)];
                    handCard.CurrentCost = Mathf.Max(0, handCard.CurrentCost + effect.flatValue);
                    return $"{data.cardName}: {handCard.Data.cardName} cost {effect.flatValue:+#;-#;0}";
                }
                case CardEffectType.BuffNextAttackCard:
                    context.NextAttackCardMultiplier += effect.percentageValue;
                    return $"{data.cardName}: next attack +{Mathf.RoundToInt(effect.percentageValue * 100f)}% damage";
                case CardEffectType.DealBonusDamageIfStrategy:
                {
                    if (context.Strategy != effect.strategy)
                    {
                        return $"{data.cardName}: no strategy bonus";
                    }

                    var raw = Mathf.RoundToInt(ownerAttack * effect.powerMultiplier * context.OutgoingAttackMultiplier * context.NextAttackCardMultiplier * context.NextIncomingAttackMultiplier);
                    var dealt = context.TargetHoop.ApplyDamage(raw);
                    context.NextAttackCardMultiplier = 1f;
                    context.NextIncomingAttackMultiplier = 1f;
                    return $"{data.cardName}: {dealt} bonus damage";
                }
                case CardEffectType.GainBonusShieldIfStrategy:
                {
                    if (context.Strategy != effect.strategy)
                    {
                        return $"{data.cardName}: no strategy shield bonus";
                    }

                    var shield = Mathf.RoundToInt(ownerDefense * effect.powerMultiplier);
                    context.TargetHoop.AddShield(shield);
                    return $"{data.cardName}: +{shield} bonus shield";
                }
                default:
                    return $"{data.cardName}: no effect";
            }
        }
    }
}
