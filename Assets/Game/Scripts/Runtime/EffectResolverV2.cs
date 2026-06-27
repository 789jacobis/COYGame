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
                EffectActionType.RepeatDamage => RepeatDamage(card, context, effect),
                EffectActionType.ModifyDamage => ModifyDamage(data, context, action),
                EffectActionType.GainShield => GainShield(card, context, effect),
                EffectActionType.ModifyShield => ModifyShield(data, context, effect),
                EffectActionType.ModifyAvailableAP => ModifyAvailableAP(data, context, action.intValue),
                EffectActionType.ModifyMaxPhaseAP => ModifyMaxPhaseAP(data, context, action.intValue),
                EffectActionType.ModifyNextOwnPhaseAP => ModifyNextPhaseAP(data, context, effect),
                EffectActionType.ModifyCardCost => ModifyCardCost(data, context, card, effect),
                EffectActionType.DrawCards => DrawCards(data, context, card, action.intValue),
                EffectActionType.ModifyDrawCount => ModifyDrawCount(data, context, effect),
                EffectActionType.DiscardCards => DiscardCards(data, context, card, effect),
                EffectActionType.GenerateCard => GenerateCard(data, context, card, action),
                EffectActionType.CopyCard => CopyCard(data, context, card, effect),
                EffectActionType.MoveCard => MoveCard(data, context, card, effect),
                EffectActionType.RemoveCard => RemoveCard(data, context, card, effect),
                EffectActionType.PlayCard => PlayCards(data, context, card, effect, false),
                EffectActionType.PlayRandomCards => PlayCards(data, context, card, effect, true),
                EffectActionType.ApplyTeamStatus => ApplyTeamStatus(data, context, effect),
                EffectActionType.ApplyPlayerStatus => ApplyPlayerStatus(data, context, card, effect),
                EffectActionType.ApplyCardStatus => ApplyCardStatus(data, context, card, effect),
                EffectActionType.ApplyModifier => ApplyModifier(data, context, card, effect),
                EffectActionType.ClearStatus => ClearStatus(data, context, card, effect),
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
                CardConditionType.HoopBrokenByThisCard => context.CurrentEvent?.HoopBrokenBySource == true && context.CurrentEvent.SourceCard == card,
                CardConditionType.CardWasNotPlayedThisPhase => !card.WasPlayed,
                CardConditionType.HasCardInHand => context.Hand.Count > 0,
                CardConditionType.HasEnoughAP => card.CurrentCost <= context.Ap,
                CardConditionType.CardCostGreaterThan => TargetResolver.ResolveCards(card, effect.target, context).Exists(target => target.CurrentCost > condition.intValue),
                CardConditionType.TargetHasStatus => TargetHasStatus(card, context, effect, condition.statusId),
                CardConditionType.StatusStackAtLeast => StatusStackAtLeast(card, context, effect, condition.statusId, condition.intValue),
                CardConditionType.ActingTeamChose2PT => context.Strategy == ScoreStrategy.TwoPoint,
                CardConditionType.ActingTeamChose3PT => context.Strategy == ScoreStrategy.ThreePoint,
                _ => false
            };
        }

        private static string DealDamage(CardRuntime card, TurnContext context, CardEffectData effect)
        {
            var ownerAttack = card.Owner != null ? card.Owner.attack : 0;
            var targetTeam = TargetResolver.ResolveTeam(effect.target, context);
            var ownerPlayer = context.ActingTeam.GetPlayerRuntime(card.Owner);
            var outgoingMultiplier = context.ActingTeam.Statuses.Multiplier(StatusModifierType.OutgoingAttackDamage)
                * card.Statuses.Multiplier(StatusModifierType.OutgoingAttackDamage)
                * (ownerPlayer?.Statuses.Multiplier(StatusModifierType.OutgoingAttackDamage) ?? 1f);
            var incomingMultiplier = targetTeam.Statuses.Multiplier(StatusModifierType.IncomingAttackDamage);
            var raw = Mathf.RoundToInt(ownerAttack * effect.action.multiplier * outgoingMultiplier * incomingMultiplier);
            var targetHoop = TargetResolver.ResolveHoop(effect.target, context);
            var wasBroken = targetHoop.IsBroken;
            var dealt = targetHoop.ApplyDamage(raw);
            context.ActingTeam.Statuses.ConsumeTriggered(StatusModifierType.OutgoingAttackDamage);
            card.Statuses.ConsumeTriggered(StatusModifierType.OutgoingAttackDamage);
            ownerPlayer?.Statuses.ConsumeTriggered(StatusModifierType.OutgoingAttackDamage);
            targetTeam.Statuses.ConsumeTriggered(StatusModifierType.IncomingAttackDamage);

            var triggerMessages = new List<string>();
            var damageEvent = new EffectEventContext
            {
                Trigger = CardTrigger.OnHoopDamaged,
                SourceCard = card,
                AffectedCards = new List<CardRuntime> { card },
                TargetHoop = targetHoop,
                DamageDealt = dealt,
                HoopWasBroken = wasBroken,
                HoopBrokenBySource = !wasBroken && targetHoop.IsBroken
            };

            if (dealt > 0 && effect.trigger != CardTrigger.OnHoopDamaged)
            {
                triggerMessages.Add(context.ResolveCardTriggers?.Invoke(new List<CardRuntime> { card }, CardTrigger.OnHoopDamaged, damageEvent));
            }

            if (!wasBroken && targetHoop.IsBroken && effect.trigger != CardTrigger.OnHoopBroken)
            {
                triggerMessages.Add(context.ResolveCardTriggers?.Invoke(new List<CardRuntime> { card }, CardTrigger.OnHoopBroken, new EffectEventContext
                {
                    Trigger = CardTrigger.OnHoopBroken,
                    SourceCard = card,
                    AffectedCards = new List<CardRuntime> { card },
                    TargetHoop = targetHoop,
                    DamageDealt = dealt,
                    HoopWasBroken = wasBroken,
                    HoopBrokenBySource = true
                }));
            }

            return AppendMessage($"{card.Data.cardName}: {dealt} damage", JoinMessages(triggerMessages));
        }

        private static string GainShield(CardRuntime card, TurnContext context, CardEffectData effect)
        {
            var ownerDefense = card.Owner != null ? card.Owner.defense : 0;
            var ownerPlayer = context.ActingTeam.GetPlayerRuntime(card.Owner);
            var shieldMultiplier = context.ActingTeam.Statuses.Multiplier(StatusModifierType.ShieldGain)
                * card.Statuses.Multiplier(StatusModifierType.ShieldGain)
                * (ownerPlayer?.Statuses.Multiplier(StatusModifierType.ShieldGain) ?? 1f);
            var shield = Mathf.RoundToInt(ownerDefense * effect.action.multiplier * shieldMultiplier);
            TargetResolver.ResolveHoop(effect.target, context).AddShield(shield);
            context.ActingTeam.Statuses.ConsumeTriggered(StatusModifierType.ShieldGain);
            card.Statuses.ConsumeTriggered(StatusModifierType.ShieldGain);
            ownerPlayer?.Statuses.ConsumeTriggered(StatusModifierType.ShieldGain);
            return $"{card.Data.cardName}: +{shield} shield";
        }

        private static string RepeatDamage(CardRuntime card, TurnContext context, CardEffectData effect)
        {
            var count = Mathf.Max(1, effect.action.intValue);
            var messages = new List<string>();
            for (var i = 0; i < count; i++)
            {
                messages.Add(DealDamage(card, context, effect));
                if (effect.action.stopIfHoopBroken && TargetResolver.ResolveHoop(effect.target, context).IsBroken)
                {
                    break;
                }
            }

            return AppendMessage($"{card.Data.cardName}: repeat damage {messages.Count}", JoinMessages(messages));
        }

        private static string ModifyShield(CardData data, TurnContext context, CardEffectData effect)
        {
            var teams = TargetResolver.ResolveTeams(effect.target, context);
            var durationType = effect.duration.durationType == EffectDurationType.Instant
                ? EffectDurationType.CurrentPhase
                : effect.duration.durationType;
            var durationCount = durationType == EffectDurationType.CurrentPhase
                ? 1
                : DurationCount(effect.duration);
            var valueMode = !Mathf.Approximately(effect.action.percentageValue, 0f)
                ? ModifierValueMode.PercentAdd
                : ModifierValueMode.Multiplier;
            var floatValue = valueMode == ModifierValueMode.PercentAdd
                ? effect.action.percentageValue
                : Mathf.Approximately(effect.action.multiplier, 0f) ? 1f : effect.action.multiplier;

            foreach (var team in teams)
            {
                team.Statuses.Add(new StatusRuntime(
                    $"{data.cardName}_{StatusModifierType.ShieldGain}_{durationType}",
                    data.cardName,
                    1,
                    0,
                    durationType,
                    durationCount,
                    new[]
                    {
                        new StatusModifierRuntime(StatusModifierType.ShieldGain, valueMode, floatValue, 0)
                    }));
            }

            return $"{data.cardName}: shield gain modifier applied to {teams.Count} team";
        }

        private static string ModifyDamage(CardData data, TurnContext context, EffectActionData action)
        {
            switch (action.modifierType)
            {
                case EffectModifierType.OutgoingAttackThisPhase:
                    context.ActingTeam.Statuses.Add(CreateDamageStatus(data.cardName, StatusModifierType.OutgoingAttackDamage, ModifierValueMode.PercentAdd, action.percentageValue, EffectDurationType.CurrentPhase, 1));
                    return $"{data.cardName}: attack damage +{Mathf.RoundToInt(action.percentageValue * 100f)}% this phase";
                case EffectModifierType.NextAttackCard:
                    context.ActingTeam.Statuses.Add(CreateDamageStatus(data.cardName, StatusModifierType.OutgoingAttackDamage, ModifierValueMode.PercentAdd, action.percentageValue, EffectDurationType.UntilTriggered, 0));
                    return $"{data.cardName}: next attack +{Mathf.RoundToInt(action.percentageValue * 100f)}% damage";
                case EffectModifierType.NextIncomingAttack:
                    context.ActingTeam.Statuses.Add(CreateDamageStatus(data.cardName, StatusModifierType.IncomingAttackDamage, ModifierValueMode.Multiplier, Mathf.Clamp01(1f - action.percentageValue), EffectDurationType.UntilTriggered, 0));
                    return $"{data.cardName}: next incoming attack -{Mathf.RoundToInt(action.percentageValue * 100f)}%";
                default:
                    return $"{data.cardName}: no damage modifier";
            }
        }

        private static string ModifyAvailableAP(CardData data, TurnContext context, int value)
        {
            value = context.ActingTeam.Statuses.ApplyInt(StatusModifierType.AvailableAP, value);
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
            value = context.ActingTeam.Statuses.ApplyInt(StatusModifierType.MaxAP, value);
            context.MaxAp = Mathf.Max(0, context.MaxAp + value);
            context.Ap = Mathf.Clamp(context.Ap, 0, context.MaxAp);
            return $"{data.cardName}: max AP {value:+#;-#;0}";
        }

        private static string ModifyNextPhaseAP(CardData data, TurnContext context, CardEffectData effect)
        {
            var teams = TargetResolver.ResolveTeams(effect.target, context);
            foreach (var team in teams)
            {
                team.Statuses.Add(CreateIntStatus(data.cardName, StatusModifierType.AvailableAP, effect.action.intValue, EffectDurationType.UntilTriggered, 0));
            }

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

        private static string DrawCards(CardData data, TurnContext context, CardRuntime sourceCard, int count)
        {
            count = context.ActingTeam.Statuses.ApplyInt(StatusModifierType.DrawCount, count);
            var drawn = context.Deck?.Draw(Mathf.Max(0, count)) ?? new List<CardRuntime>();
            context.Hand.AddRange(drawn);
            var triggerMessage = context.ResolveCardTriggers?.Invoke(drawn, CardTrigger.OnDraw, new EffectEventContext
            {
                Trigger = CardTrigger.OnDraw,
                SourceCard = sourceCard,
                AffectedCards = drawn
            });
            return AppendMessage($"{data.cardName}: draw {drawn.Count}", triggerMessage);
        }

        private static string ModifyDrawCount(CardData data, TurnContext context, CardEffectData effect)
        {
            var teams = TargetResolver.ResolveTeams(effect.target, context);
            foreach (var team in teams)
            {
                team.Statuses.Add(CreateIntStatus(data.cardName, StatusModifierType.DrawCount, effect.action.intValue, EffectDurationType.UntilTriggered, 0));
            }

            return $"{data.cardName}: next draw count {effect.action.intValue:+#;-#;0}";
        }

        private static string DiscardCards(CardData data, TurnContext context, CardRuntime card, CardEffectData effect)
        {
            var targets = TargetResolver.ResolveCards(card, effect.target, context);
            foreach (var target in targets)
            {
                context.Hand.Remove(target);
                context.ResolveCardTriggers?.Invoke(new List<CardRuntime> { target }, CardTrigger.OnDiscard, new EffectEventContext
                {
                    Trigger = CardTrigger.OnDiscard,
                    SourceCard = card,
                    AffectedCards = new List<CardRuntime> { target }
                });
                context.Deck?.DiscardCard(target);
            }

            return $"{data.cardName}: discard {targets.Count}";
        }

        private static string GenerateCard(CardData data, TurnContext context, CardRuntime sourceCard, EffectActionData action)
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
                    context.ResolveCardTriggers?.Invoke(new List<CardRuntime> { runtimeCard }, CardTrigger.OnCreate, new EffectEventContext
                    {
                        Trigger = CardTrigger.OnCreate,
                        SourceCard = sourceCard,
                        AffectedCards = new List<CardRuntime> { runtimeCard }
                    });
                    generated++;
                }
                else if (action.toZone == CardZone.DiscardPile)
                {
                    context.ResolveCardTriggers?.Invoke(new List<CardRuntime> { runtimeCard }, CardTrigger.OnCreate, new EffectEventContext
                    {
                        Trigger = CardTrigger.OnCreate,
                        SourceCard = sourceCard,
                        AffectedCards = new List<CardRuntime> { runtimeCard }
                    });
                    context.ResolveCardTriggers?.Invoke(new List<CardRuntime> { runtimeCard }, CardTrigger.OnDiscard, new EffectEventContext
                    {
                        Trigger = CardTrigger.OnDiscard,
                        SourceCard = sourceCard,
                        AffectedCards = new List<CardRuntime> { runtimeCard }
                    });
                    context.Deck?.DiscardCard(runtimeCard);
                    generated++;
                }
            }

            return $"{data.cardName}: generate {generated}";
        }

        private static string CopyCard(CardData data, TurnContext context, CardRuntime card, CardEffectData effect)
        {
            var targets = TargetResolver.ResolveCards(card, effect.target, context);
            var copied = 0;
            foreach (var target in targets)
            {
                var copy = new CardRuntime(target.Data, target.Owner);
                if (effect.action.toZone == CardZone.Hand)
                {
                    context.Hand.Add(copy);
                    copied++;
                }
                else if (effect.action.toZone == CardZone.DiscardPile)
                {
                    context.Deck?.DiscardCard(copy);
                    copied++;
                }
                else if (effect.action.toZone == CardZone.OutsideGame)
                {
                    context.Deck?.MoveOutsideGame(copy);
                    copied++;
                }
            }

            return $"{data.cardName}: copied {copied} card";
        }

        private static string PlayCards(CardData data, TurnContext context, CardRuntime sourceCard, CardEffectData effect, bool randomOnly)
        {
            var targets = TargetResolver.ResolveCards(sourceCard, effect.target, context)
                .FindAll(target => target != sourceCard && context.Hand.Contains(target));
            if (randomOnly)
            {
                targets = PickRandomCards(targets, Mathf.Max(1, effect.target.count));
            }

            var messages = new List<string>();
            var played = 0;
            foreach (var target in targets)
            {
                if (effect.action.consumeAP && target.CurrentCost > context.Ap)
                {
                    continue;
                }

                if (effect.action.consumeAP)
                {
                    context.Ap = Mathf.Max(0, context.Ap - target.CurrentCost);
                }

                context.Hand.Remove(target);
                target.WasPlayed = true;
                if (effect.action.triggerOnPlay)
                {
                    messages.Add(CardEffectResolver.Resolve(target, context, CardTrigger.OnPlay, new EffectEventContext
                    {
                        Trigger = CardTrigger.OnPlay,
                        SourceCard = sourceCard,
                        AffectedCards = new List<CardRuntime> { target }
                    }));
                }

                context.ResolveCardTriggers?.Invoke(new List<CardRuntime> { target }, CardTrigger.OnDiscard, new EffectEventContext
                {
                    Trigger = CardTrigger.OnDiscard,
                    SourceCard = sourceCard,
                    AffectedCards = new List<CardRuntime> { target }
                });
                context.Deck?.DiscardCard(target);
                played++;

                if (effect.action.stopIfHoopBroken && TargetResolver.ResolveHoop(effect.target, context).IsBroken)
                {
                    break;
                }
            }

            return AppendMessage($"{data.cardName}: played {played} card", JoinMessages(messages));
        }

        private static List<CardRuntime> PickRandomCards(List<CardRuntime> candidates, int count)
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
                    context.ResolveCardTriggers?.Invoke(new List<CardRuntime> { target }, CardTrigger.OnDiscard, new EffectEventContext
                    {
                        Trigger = CardTrigger.OnDiscard,
                        SourceCard = card,
                        AffectedCards = new List<CardRuntime> { target }
                    });
                    context.Deck?.DiscardCard(target);
                }
                else if (effect.action.toZone == CardZone.OutsideGame)
                {
                    context.ResolveCardTriggers?.Invoke(new List<CardRuntime> { target }, CardTrigger.OnRemove, new EffectEventContext
                    {
                        Trigger = CardTrigger.OnRemove,
                        SourceCard = card,
                        AffectedCards = new List<CardRuntime> { target }
                    });
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
                context.ResolveCardTriggers?.Invoke(new List<CardRuntime> { target }, CardTrigger.OnRemove, new EffectEventContext
                {
                    Trigger = CardTrigger.OnRemove,
                    SourceCard = card,
                    AffectedCards = new List<CardRuntime> { target }
                });
                context.Deck?.MoveOutsideGame(target);
            }

            return $"{data.cardName}: remove {targets.Count}";
        }

        private static string ApplyTeamStatus(CardData data, TurnContext context, CardEffectData effect)
        {
            var teams = TargetResolver.ResolveTeams(effect.target, context);
            foreach (var team in teams)
            {
                team.Statuses.Add(CreateStatus(effect));
            }

            return $"{data.cardName}: applied {StatusName(effect)} to {teams.Count} team";
        }

        private static string ApplyCardStatus(CardData data, TurnContext context, CardRuntime card, CardEffectData effect)
        {
            var targets = TargetResolver.ResolveCards(card, effect.target, context);
            foreach (var target in targets)
            {
                target.Statuses.Add(CreateStatus(effect));
            }

            return $"{data.cardName}: applied {StatusName(effect)} to {targets.Count} card";
        }

        private static string ApplyPlayerStatus(CardData data, TurnContext context, CardRuntime card, CardEffectData effect)
        {
            var targets = TargetResolver.ResolvePlayers(card, effect.target, context);
            foreach (var target in targets)
            {
                target.Statuses.Add(CreateStatus(effect));
            }

            return $"{data.cardName}: applied {StatusName(effect)} to {targets.Count} player";
        }

        private static string ApplyModifier(CardData data, TurnContext context, CardRuntime card, CardEffectData effect)
        {
            return effect.target.kind switch
            {
                TargetKind.Card => ApplyCardStatus(data, context, card, effect),
                TargetKind.Player => ApplyPlayerStatus(data, context, card, effect),
                _ => ApplyTeamStatus(data, context, effect)
            };
        }

        private static string ClearStatus(CardData data, TurnContext context, CardRuntime card, CardEffectData effect)
        {
            if (effect.target.kind == TargetKind.Card)
            {
                var targets = TargetResolver.ResolveCards(card, effect.target, context);
                foreach (var target in targets)
                {
                    target.Statuses.Clear(effect.action.statusId);
                }

                return $"{data.cardName}: cleared status from {targets.Count} card";
            }

            if (effect.target.kind == TargetKind.Player)
            {
                var targets = TargetResolver.ResolvePlayers(card, effect.target, context);
                foreach (var target in targets)
                {
                    target.Statuses.Clear(effect.action.statusId);
                }

                return $"{data.cardName}: cleared status from {targets.Count} player";
            }

            var teams = TargetResolver.ResolveTeams(effect.target, context);
            foreach (var team in teams)
            {
                team.Statuses.Clear(effect.action.statusId);
            }

            return $"{data.cardName}: cleared status from {teams.Count} team";
        }

        private static StatusRuntime CreateStatus(CardEffectData effect)
        {
            return new StatusRuntime(
                StatusName(effect),
                string.IsNullOrWhiteSpace(effect.action.statusDisplayName) ? StatusName(effect) : effect.action.statusDisplayName,
                effect.action.statusStacks,
                effect.action.maxStatusStacks,
                effect.duration.durationType,
                DurationCount(effect.duration),
                CreateModifiers(effect));
        }

        private static string StatusName(CardEffectData effect)
        {
            return string.IsNullOrWhiteSpace(effect.action.statusId)
                ? effect.action.actionType.ToString()
                : effect.action.statusId;
        }

        private static int DurationCount(EffectDurationData duration)
        {
            return duration.durationType switch
            {
                EffectDurationType.Instant => 0,
                EffectDurationType.CurrentPhase => 1,
                EffectDurationType.UntilUsed => 0,
                EffectDurationType.UntilTriggered => 0,
                EffectDurationType.ThisGame => 0,
                EffectDurationType.FullRounds => Mathf.Max(1, duration.count) * 2,
                _ => Mathf.Max(1, duration.count)
            };
        }

        private static StatusRuntime CreateDamageStatus(string sourceName, StatusModifierType modifierType, ModifierValueMode valueMode, float floatValue, EffectDurationType durationType, int durationCount)
        {
            var statusId = $"{sourceName}_{modifierType}_{durationType}";
            return new StatusRuntime(
                statusId,
                sourceName,
                1,
                0,
                durationType,
                durationCount,
                new[]
                {
                    new StatusModifierRuntime(modifierType, valueMode, floatValue, 0)
                });
        }

        private static StatusRuntime CreateIntStatus(string sourceName, StatusModifierType modifierType, int intValue, EffectDurationType durationType, int durationCount)
        {
            var statusId = $"{sourceName}_{modifierType}_{durationType}";
            return new StatusRuntime(
                statusId,
                sourceName,
                1,
                0,
                durationType,
                durationCount,
                new[]
                {
                    new StatusModifierRuntime(modifierType, ModifierValueMode.FlatAdd, 0f, intValue)
                });
        }

        private static List<StatusModifierRuntime> CreateModifiers(CardEffectData effect)
        {
            var modifiers = new List<StatusModifierRuntime>();
            if (effect.action.modifiers != null)
            {
                foreach (var modifier in effect.action.modifiers)
                {
                    if (modifier == null || modifier.modifierType == StatusModifierType.None)
                    {
                        continue;
                    }

                    modifiers.Add(new StatusModifierRuntime(modifier.modifierType, modifier.valueMode, modifier.floatValue, modifier.intValue));
                }
            }

            if (modifiers.Count == 0)
            {
                var modifierType = MapLegacyModifier(effect.action.modifierType);
                if (modifierType != StatusModifierType.None)
                {
                    modifiers.Add(new StatusModifierRuntime(
                        modifierType,
                        ModifierValueMode.PercentAdd,
                        effect.action.percentageValue,
                        effect.action.intValue));
                }
            }

            return modifiers;
        }

        private static StatusModifierType MapLegacyModifier(EffectModifierType modifierType)
        {
            return modifierType switch
            {
                EffectModifierType.OutgoingAttackThisPhase => StatusModifierType.OutgoingAttackDamage,
                EffectModifierType.NextAttackCard => StatusModifierType.OutgoingAttackDamage,
                EffectModifierType.NextIncomingAttack => StatusModifierType.IncomingAttackDamage,
                _ => StatusModifierType.None
            };
        }

        private static bool TargetHasStatus(CardRuntime card, TurnContext context, CardEffectData effect, string statusId)
        {
            if (effect.target.kind == TargetKind.Card)
            {
                return TargetResolver.ResolveCards(card, effect.target, context).Exists(target => target.Statuses.Has(statusId));
            }

            if (effect.target.kind == TargetKind.Player)
            {
                return TargetResolver.ResolvePlayers(card, effect.target, context).Exists(target => target.Statuses.Has(statusId));
            }

            return TargetResolver.ResolveTeams(effect.target, context).Exists(team => team.Statuses.Has(statusId));
        }

        private static bool StatusStackAtLeast(CardRuntime card, TurnContext context, CardEffectData effect, string statusId, int stackCount)
        {
            if (effect.target.kind == TargetKind.Card)
            {
                return TargetResolver.ResolveCards(card, effect.target, context).Exists(target => target.Statuses.StackCount(statusId) >= stackCount);
            }

            if (effect.target.kind == TargetKind.Player)
            {
                return TargetResolver.ResolvePlayers(card, effect.target, context).Exists(target => target.Statuses.StackCount(statusId) >= stackCount);
            }

            return TargetResolver.ResolveTeams(effect.target, context).Exists(team => team.Statuses.StackCount(statusId) >= stackCount);
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
