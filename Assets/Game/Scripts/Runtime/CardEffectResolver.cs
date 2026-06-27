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
                    var raw = CalculateDamage(card, context, ownerAttack, effect.powerMultiplier);
                    var dealt = context.TargetHoop.ApplyDamage(raw);
                    ConsumeDamageStatuses(card, context);
                    return $"{ownerName} played {data.cardName}: {dealt} damage";
                }
                case CardEffectType.GainShield:
                {
                    var shield = CalculateShield(card, context, ownerDefense, effect.powerMultiplier);
                    context.TargetHoop.AddShield(shield);
                    ConsumeShieldStatuses(card, context);
                    return $"{ownerName} played {data.cardName}: +{shield} shield";
                }
                case CardEffectType.BuffOutgoingAttackThisTurn:
                    context.ActingTeam.Statuses.Add(CreatePercentStatus(data.cardName, StatusModifierType.OutgoingAttackDamage, effect.percentageValue, EffectDurationType.CurrentPhase, 1));
                    return $"{data.cardName}: attack damage +{Mathf.RoundToInt(effect.percentageValue * 100f)}% this phase";
                case CardEffectType.ReduceNextIncomingAttack:
                {
                    var shield = CalculateShield(card, context, ownerDefense, effect.powerMultiplier);
                    context.TargetHoop.AddShield(shield);
                    ConsumeShieldStatuses(card, context);
                    context.ActingTeam.Statuses.Add(CreateMultiplierStatus(data.cardName, StatusModifierType.IncomingAttackDamage, Mathf.Clamp01(1f - effect.percentageValue), EffectDurationType.UntilTriggered, 0));
                    return $"{data.cardName}: +{shield} shield, next incoming attack -{Mathf.RoundToInt(effect.percentageValue * 100f)}%";
                }
                case CardEffectType.ModifyOpponentNextTurnAp:
                {
                    if (effect.powerMultiplier > 0f)
                    {
                        var shield = CalculateShield(card, context, ownerDefense, effect.powerMultiplier);
                        context.TargetHoop.AddShield(shield);
                        ConsumeShieldStatuses(card, context);
                    }

                    context.OpposingTeam.Statuses.Add(CreateIntStatus(data.cardName, StatusModifierType.AvailableAP, effect.flatValue, EffectDurationType.UntilTriggered, 0));
                    return $"{data.cardName}: opponent next turn AP {effect.flatValue:+#;-#;0}";
                }
                case CardEffectType.ModifyCurrentTurnAp:
                    context.MaxAp = Mathf.Max(0, context.MaxAp + effect.flatValue);
                    context.Ap = Mathf.Clamp(context.Ap + effect.flatValue, 0, context.MaxAp);
                    return $"{data.cardName}: AP {effect.flatValue:+#;-#;0}";
                case CardEffectType.DrawCards:
                    context.ActingTeam.Statuses.Add(CreateIntStatus(data.cardName, StatusModifierType.DrawCount, effect.flatValue, EffectDurationType.UntilTriggered, 0));
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
                    context.ActingTeam.Statuses.Add(CreatePercentStatus(data.cardName, StatusModifierType.OutgoingAttackDamage, effect.percentageValue, EffectDurationType.UntilTriggered, 0));
                    return $"{data.cardName}: next attack +{Mathf.RoundToInt(effect.percentageValue * 100f)}% damage";
                case CardEffectType.DealBonusDamageIfStrategy:
                {
                    if (context.Strategy != effect.strategy)
                    {
                        return $"{data.cardName}: no strategy bonus";
                    }

                    var raw = CalculateDamage(card, context, ownerAttack, effect.powerMultiplier);
                    var dealt = context.TargetHoop.ApplyDamage(raw);
                    ConsumeDamageStatuses(card, context);
                    return $"{data.cardName}: {dealt} bonus damage";
                }
                case CardEffectType.GainBonusShieldIfStrategy:
                {
                    if (context.Strategy != effect.strategy)
                    {
                        return $"{data.cardName}: no strategy shield bonus";
                    }

                    var shield = CalculateShield(card, context, ownerDefense, effect.powerMultiplier);
                    context.TargetHoop.AddShield(shield);
                    ConsumeShieldStatuses(card, context);
                    return $"{data.cardName}: +{shield} bonus shield";
                }
                default:
                    return $"{data.cardName}: no effect";
            }
        }

        private static int CalculateDamage(CardRuntime card, TurnContext context, int ownerAttack, float multiplier)
        {
            var ownerPlayer = context.ActingTeam.GetPlayerRuntime(card.Owner);
            var outgoingMultiplier = context.ActingTeam.Statuses.Multiplier(StatusModifierType.OutgoingAttackDamage)
                * card.Statuses.Multiplier(StatusModifierType.OutgoingAttackDamage)
                * (ownerPlayer?.Statuses.Multiplier(StatusModifierType.OutgoingAttackDamage) ?? 1f);
            var incomingMultiplier = context.OpposingTeam.Statuses.Multiplier(StatusModifierType.IncomingAttackDamage);
            return Mathf.RoundToInt(ownerAttack * multiplier * outgoingMultiplier * incomingMultiplier);
        }

        private static int CalculateShield(CardRuntime card, TurnContext context, int ownerDefense, float multiplier)
        {
            var ownerPlayer = context.ActingTeam.GetPlayerRuntime(card.Owner);
            var shieldMultiplier = context.ActingTeam.Statuses.Multiplier(StatusModifierType.ShieldGain)
                * card.Statuses.Multiplier(StatusModifierType.ShieldGain)
                * (ownerPlayer?.Statuses.Multiplier(StatusModifierType.ShieldGain) ?? 1f);
            return Mathf.RoundToInt(ownerDefense * multiplier * shieldMultiplier);
        }

        private static void ConsumeDamageStatuses(CardRuntime card, TurnContext context)
        {
            var ownerPlayer = context.ActingTeam.GetPlayerRuntime(card.Owner);
            context.ActingTeam.Statuses.ConsumeTriggered(StatusModifierType.OutgoingAttackDamage);
            card.Statuses.ConsumeTriggered(StatusModifierType.OutgoingAttackDamage);
            ownerPlayer?.Statuses.ConsumeTriggered(StatusModifierType.OutgoingAttackDamage);
            context.OpposingTeam.Statuses.ConsumeTriggered(StatusModifierType.IncomingAttackDamage);
        }

        private static void ConsumeShieldStatuses(CardRuntime card, TurnContext context)
        {
            var ownerPlayer = context.ActingTeam.GetPlayerRuntime(card.Owner);
            context.ActingTeam.Statuses.ConsumeTriggered(StatusModifierType.ShieldGain);
            card.Statuses.ConsumeTriggered(StatusModifierType.ShieldGain);
            ownerPlayer?.Statuses.ConsumeTriggered(StatusModifierType.ShieldGain);
        }

        private static StatusRuntime CreatePercentStatus(string sourceName, StatusModifierType modifierType, float percentageValue, EffectDurationType durationType, int durationCount)
        {
            return CreateStatus(sourceName, modifierType, ModifierValueMode.PercentAdd, percentageValue, 0, durationType, durationCount);
        }

        private static StatusRuntime CreateMultiplierStatus(string sourceName, StatusModifierType modifierType, float multiplierValue, EffectDurationType durationType, int durationCount)
        {
            return CreateStatus(sourceName, modifierType, ModifierValueMode.Multiplier, multiplierValue, 0, durationType, durationCount);
        }

        private static StatusRuntime CreateIntStatus(string sourceName, StatusModifierType modifierType, int intValue, EffectDurationType durationType, int durationCount)
        {
            return CreateStatus(sourceName, modifierType, ModifierValueMode.FlatAdd, 0f, intValue, durationType, durationCount);
        }

        private static StatusRuntime CreateStatus(string sourceName, StatusModifierType modifierType, ModifierValueMode valueMode, float floatValue, int intValue, EffectDurationType durationType, int durationCount)
        {
            return new StatusRuntime(
                $"{sourceName}_{modifierType}_{durationType}",
                sourceName,
                1,
                0,
                durationType,
                durationCount,
                new[]
                {
                    new StatusModifierRuntime(modifierType, valueMode, floatValue, intValue)
                });
        }
    }
}
