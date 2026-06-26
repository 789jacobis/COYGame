using System.Collections.Generic;
using UnityEngine;

namespace COYGame
{
    public static class EffectResolverV2
    {
        public static string Resolve(CardRuntime card, TurnContext context, CardEffectData effect, CardTrigger trigger = CardTrigger.OnPlay)
        {
            if (effect.trigger != trigger || !ConditionsMet(card, context, effect))
            {
                return string.Empty;
            }

            var action = effect.action;
            var data = card.Data;
            return action.actionType switch
            {
                EffectActionType.DealDamage => DealDamage(card, context, effect),
                EffectActionType.ModifyDamage => ModifyDamage(data, context, action),
                EffectActionType.GainShield => GainShield(card, context, effect),
                EffectActionType.ModifyAvailableAP => ModifyAvailableAP(data, context, action.intValue),
                EffectActionType.ModifyMaxPhaseAP => ModifyMaxPhaseAP(data, context, action.intValue),
                EffectActionType.ModifyNextOwnPhaseAP => ModifyNextPhaseAP(data, context, effect),
                EffectActionType.ModifyCardCost => ModifyCardCost(data, context, card, effect),
                EffectActionType.DrawCards => DrawCards(data, context, action.intValue),
                EffectActionType.ModifyDrawCount => ModifyDrawCount(data, context, effect),
                EffectActionType.DiscardCards => DiscardCards(data, context, card, effect),
                EffectActionType.GenerateCard => GenerateCard(data, context, action),
                EffectActionType.MoveCard => MoveCard(data, context, card, effect),
                EffectActionType.RemoveCard => RemoveCard(data, context, card, effect),
                _ => $"{data.cardName}: unsupported V2 action {action.actionType}"
            };
        }

        private static bool ConditionsMet(CardRuntime card, TurnContext context, CardEffectData effect)
        {
            if (effect.conditions == null || effect.conditions.Count == 0)
            {
                return true;
            }

            foreach (var condition in effect.conditions)
            {
                if (!ConditionMet(card, context, effect, condition))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ConditionMet(CardRuntime card, TurnContext context, CardEffectData effect, CardConditionData condition)
        {
            return condition.conditionType switch
            {
                CardConditionType.None => true,
                CardConditionType.PlayerChose2PT => context.ActingTeam.Side == TeamSide.Player && context.Strategy == ScoreStrategy.TwoPoint,
                CardConditionType.PlayerChose3PT => context.ActingTeam.Side == TeamSide.Player && context.Strategy == ScoreStrategy.ThreePoint,
                CardConditionType.EnemyChose2PT => context.ActingTeam.Side == TeamSide.Enemy && context.Strategy == ScoreStrategy.TwoPoint,
                CardConditionType.EnemyChose3PT => context.ActingTeam.Side == TeamSide.Enemy && context.Strategy == ScoreStrategy.ThreePoint,
                CardConditionType.HoopBrokenByThisCard => context.TargetHoop.IsBroken,
                CardConditionType.CardWasNotPlayedThisPhase => !card.WasPlayed,
                CardConditionType.HasCardInHand => context.Hand.Count > 0,
                CardConditionType.HasEnoughAP => card.CurrentCost <= context.Ap,
                CardConditionType.CardCostGreaterThan => TargetResolver.ResolveCards(card, effect.target, context).Exists(target => target.CurrentCost > condition.intValue),
                CardConditionType.ActingTeamChose2PT => context.Strategy == ScoreStrategy.TwoPoint,
                CardConditionType.ActingTeamChose3PT => context.Strategy == ScoreStrategy.ThreePoint,
                _ => false
            };
        }

        private static string DealDamage(CardRuntime card, TurnContext context, CardEffectData effect)
        {
            var ownerAttack = card.Owner != null ? card.Owner.attack : 0;
            var raw = Mathf.RoundToInt(ownerAttack * effect.action.multiplier * context.OutgoingAttackMultiplier * context.NextAttackCardMultiplier * context.NextIncomingAttackMultiplier);
            var targetHoop = TargetResolver.ResolveHoop(effect.target, context);
            var wasBroken = targetHoop.IsBroken;
            var dealt = targetHoop.ApplyDamage(raw);
            context.NextAttackCardMultiplier = 1f;
            context.NextIncomingAttackMultiplier = 1f;

            var triggerMessages = new List<string>();
            if (dealt > 0 && effect.trigger != CardTrigger.OnHoopDamaged)
            {
                triggerMessages.Add(context.ResolveCardTriggers?.Invoke(new List<CardRuntime> { card }, CardTrigger.OnHoopDamaged));
            }

            if (!wasBroken && targetHoop.IsBroken && effect.trigger != CardTrigger.OnHoopBroken)
            {
                triggerMessages.Add(context.ResolveCardTriggers?.Invoke(new List<CardRuntime> { card }, CardTrigger.OnHoopBroken));
            }

            return AppendMessage($"{card.Data.cardName}: {dealt} damage", JoinMessages(triggerMessages));
        }

        private static string GainShield(CardRuntime card, TurnContext context, CardEffectData effect)
        {
            var ownerDefense = card.Owner != null ? card.Owner.defense : 0;
            var shield = Mathf.RoundToInt(ownerDefense * effect.action.multiplier);
            TargetResolver.ResolveHoop(effect.target, context).AddShield(shield);
            return $"{card.Data.cardName}: +{shield} shield";
        }

        private static string ModifyDamage(CardData data, TurnContext context, EffectActionData action)
        {
            switch (action.modifierType)
            {
                case EffectModifierType.OutgoingAttackThisPhase:
                    context.OutgoingAttackMultiplier += action.percentageValue;
                    return $"{data.cardName}: attack damage +{Mathf.RoundToInt(action.percentageValue * 100f)}% this phase";
                case EffectModifierType.NextAttackCard:
                    context.NextAttackCardMultiplier += action.percentageValue;
                    return $"{data.cardName}: next attack +{Mathf.RoundToInt(action.percentageValue * 100f)}% damage";
                case EffectModifierType.NextIncomingAttack:
                    context.NextIncomingAttackMultiplier *= Mathf.Clamp01(1f - action.percentageValue);
                    return $"{data.cardName}: next incoming attack -{Mathf.RoundToInt(action.percentageValue * 100f)}%";
                default:
                    return $"{data.cardName}: no damage modifier";
            }
        }

        private static string ModifyAvailableAP(CardData data, TurnContext context, int value)
        {
            if (value > 0)
            {
                context.MaxAp += value;
            }

            context.Ap = Mathf.Max(0, context.Ap + value);
            if (context.Ap > context.MaxAp)
            {
                context.MaxAp = context.Ap;
            }

            return $"{data.cardName}: AP {value:+#;-#;0}";
        }

        private static string ModifyMaxPhaseAP(CardData data, TurnContext context, int value)
        {
            context.MaxAp = Mathf.Max(0, context.MaxAp + value);
            context.Ap = Mathf.Clamp(context.Ap, 0, context.MaxAp);
            return $"{data.cardName}: max AP {value:+#;-#;0}";
        }

        private static string ModifyNextPhaseAP(CardData data, TurnContext context, CardEffectData effect)
        {
            var team = TargetResolver.ResolveTeam(effect.target, context);
            team.NextTurnApModifier += effect.action.intValue;
            return $"{data.cardName}: next phase AP {effect.action.intValue:+#;-#;0}";
        }

        private static string ModifyCardCost(CardData data, TurnContext context, CardRuntime card, CardEffectData effect)
        {
            var targets = TargetResolver.ResolveCards(card, effect.target, context, effect.conditions);
            foreach (var target in targets)
            {
                target.CurrentCost = Mathf.Max(0, target.CurrentCost + effect.action.intValue);
            }

            return $"{data.cardName}: modified {targets.Count} card cost";
        }

        private static string DrawCards(CardData data, TurnContext context, int count)
        {
            var drawn = context.Deck?.Draw(Mathf.Max(0, count)) ?? new List<CardRuntime>();
            context.Hand.AddRange(drawn);
            var triggerMessage = context.ResolveCardTriggers?.Invoke(drawn, CardTrigger.OnDraw);
            return AppendMessage($"{data.cardName}: draw {drawn.Count}", triggerMessage);
        }

        private static string ModifyDrawCount(CardData data, TurnContext context, CardEffectData effect)
        {
            var team = TargetResolver.ResolveTeam(effect.target, context);
            team.NextTurnDrawModifier += effect.action.intValue;
            return $"{data.cardName}: next draw count {effect.action.intValue:+#;-#;0}";
        }

        private static string DiscardCards(CardData data, TurnContext context, CardRuntime card, CardEffectData effect)
        {
            var targets = TargetResolver.ResolveCards(card, effect.target, context);
            foreach (var target in targets)
            {
                context.Hand.Remove(target);
                context.ResolveCardTriggers?.Invoke(new List<CardRuntime> { target }, CardTrigger.OnDiscard);
                context.Deck?.DiscardCard(target);
            }

            return $"{data.cardName}: discard {targets.Count}";
        }

        private static string GenerateCard(CardData data, TurnContext context, EffectActionData action)
        {
            if (action.cardToGenerate == null || action.intValue <= 0)
            {
                return $"{data.cardName}: no card generated";
            }

            var generated = 0;
            for (var i = 0; i < action.intValue; i++)
            {
                var runtimeCard = new CardRuntime(action.cardToGenerate, null);
                if (action.toZone == CardZone.Hand)
                {
                    context.Hand.Add(runtimeCard);
                    context.ResolveCardTriggers?.Invoke(new List<CardRuntime> { runtimeCard }, CardTrigger.OnCreate);
                    generated++;
                }
                else if (action.toZone == CardZone.DiscardPile)
                {
                    context.ResolveCardTriggers?.Invoke(new List<CardRuntime> { runtimeCard }, CardTrigger.OnCreate);
                    context.ResolveCardTriggers?.Invoke(new List<CardRuntime> { runtimeCard }, CardTrigger.OnDiscard);
                    context.Deck?.DiscardCard(runtimeCard);
                    generated++;
                }
            }

            return $"{data.cardName}: generate {generated}";
        }

        private static string MoveCard(CardData data, TurnContext context, CardRuntime card, CardEffectData effect)
        {
            var targets = TargetResolver.ResolveCards(card, effect.target, context);
            foreach (var target in targets)
            {
                if (effect.action.fromZone == CardZone.Hand)
                {
                    context.Hand.Remove(target);
                }
                else
                {
                    context.Deck?.RemoveFromZone(target, effect.action.fromZone);
                }

                if (effect.action.toZone == CardZone.DiscardPile)
                {
                    context.ResolveCardTriggers?.Invoke(new List<CardRuntime> { target }, CardTrigger.OnDiscard);
                    context.Deck?.DiscardCard(target);
                }
                else if (effect.action.toZone == CardZone.OutsideGame)
                {
                    context.ResolveCardTriggers?.Invoke(new List<CardRuntime> { target }, CardTrigger.OnRemove);
                    context.Deck?.MoveOutsideGame(target);
                }
            }

            return $"{data.cardName}: move {targets.Count}";
        }

        private static string RemoveCard(CardData data, TurnContext context, CardRuntime card, CardEffectData effect)
        {
            var targets = TargetResolver.ResolveCards(card, effect.target, context);
            foreach (var target in targets)
            {
                context.Hand.Remove(target);
                context.ResolveCardTriggers?.Invoke(new List<CardRuntime> { target }, CardTrigger.OnRemove);
                context.Deck?.MoveOutsideGame(target);
            }

            return $"{data.cardName}: remove {targets.Count}";
        }

        private static string AppendMessage(string primary, string secondary)
        {
            return string.IsNullOrWhiteSpace(secondary) ? primary : $"{primary}\n{secondary}";
        }

        private static string JoinMessages(IEnumerable<string> messages)
        {
            var results = new List<string>();
            foreach (var message in messages)
            {
                if (!string.IsNullOrWhiteSpace(message))
                {
                    results.Add(message);
                }
            }

            return string.Join("\n", results);
        }
    }
}
