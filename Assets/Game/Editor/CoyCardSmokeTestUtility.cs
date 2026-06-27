using System.Collections.Generic;
using COYGame;
using UnityEditor;
using UnityEngine;

public static class CoyCardSmokeTestUtility
{
    private const string CardDataPath = "Assets/Game/Data/Cards";

    [MenuItem("Tools/COY/Run V2 Card Smoke Tests")]
    public static void RunV2CardSmokeTests()
    {
        var failures = new List<string>();
        TestRebound(failures);
        TestOutletPass(failures);
        TestDamageModifiers(failures);
        TestStrategyBonus(failures);
        TestUtilityEffects(failures);
        TestDefenseReaction(failures);
        TestV2Triggers(failures);
        TestEventContext(failures);
        TestHoopTriggers(failures);
        TestKeywordRuntime(failures);
        TestStatusRuntime(failures);
        TestDurationRuntime(failures);
        TestLegacyStatusBridge(failures);
        TestPlayerRuntime(failures);
        TestTargetResolverRuntime(failures);
        TestActionRuntime(failures);
        TestPlayerChoiceRuntime(failures);
        TestNextCardRuntime(failures);

        if (failures.Count > 0)
        {
            foreach (var failure in failures)
            {
                Debug.LogError("[COY Smoke Test] " + failure);
            }

            Debug.LogError($"COY V2 card smoke tests failed. Failures: {failures.Count}.");
            return;
        }

        Debug.Log("COY V2 card smoke tests passed.");
    }

    private static void TestRebound(List<string> failures)
    {
        var rebound = LoadCard("Rebound");
        var context = CreateContext(new List<CardRuntime>());
        context.Ap = 4;
        context.MaxAp = 4;

        Resolve(rebound, context);

        Expect(context.Ap == 5, failures, "Rebound should increase current AP from 4 to 5.");
        Expect(context.MaxAp == 5, failures, "Rebound should raise max AP to 5 when current AP exceeds the previous max.");
    }

    private static void TestOutletPass(List<string> failures)
    {
        var outletPass = LoadCard("GreenOutletPass");
        var target = new CardRuntime(CreateRuntimeCard("Target", CardType.Attack, 2), CreatePlayer("TargetOwner", 10, 10));
        var drawn = new CardRuntime(CreateRuntimeCard("Drawn", CardType.Attack, 1), CreatePlayer("DrawnOwner", 10, 10));
        var context = CreateContext(new List<CardRuntime> { drawn });
        context.Hand.Add(target);

        Resolve(outletPass, context);

        Expect(context.Hand.Count == 2, failures, "Outlet Pass should draw 1 card into hand.");
        Expect(target.CurrentCost == 1 || drawn.CurrentCost == 0, failures, "Outlet Pass should reduce one card in hand by 1 AP.");
    }

    private static void TestDamageModifiers(List<string> failures)
    {
        var heatCheck = LoadCard("HeatCheck");
        var curryHeatCheck = LoadCard("CurryHeatCheck");
        var attackCard = CreateDamageCard("Damage Modifier Shot", 1f);
        var shooter = CreatePlayer("Shooter", 100, 100);
        var hoop = new HoopState();

        hoop.Reset(500);
        var context = CreateContext(new List<CardRuntime>(), hoop);
        Resolve(heatCheck, context);
        Resolve(attackCard, context, shooter);
        Expect(hoop.Hp == 325, failures, "HeatCheck status should increase 100 damage to 175.");
        Expect(context.ActingTeam.Statuses.Has("Heat Check_OutgoingAttackDamage_CurrentPhase"), failures, "HeatCheck CurrentPhase status should remain until phase end.");

        hoop.Reset(500);
        context = CreateContext(new List<CardRuntime>(), hoop);
        Resolve(curryHeatCheck, context);
        Resolve(attackCard, context, shooter);
        Expect(hoop.Hp == 325, failures, "CurryHeatCheck status should increase the next 100 damage to 175.");
        Expect(!context.ActingTeam.Statuses.Has("Heat Check_OutgoingAttackDamage_UntilTriggered"), failures, "CurryHeatCheck UntilTriggered status should be consumed by the next attack.");
    }

    private static void TestStrategyBonus(List<string> failures)
    {
        var deepThree = LoadCard("CurryDeepThree");
        var curry = CreatePlayer("Curry", 100, 30);
        var hoop = new HoopState();
        hoop.Reset(500);
        var context = CreateContext(new List<CardRuntime>(), hoop);
        context.Strategy = ScoreStrategy.ThreePoint;

        Resolve(deepThree, context, curry);

        Expect(hoop.Hp == 210, failures, "CurryDeepThree at 100 ATK with 3PT strategy should deal 290 total damage.");

        hoop.Reset(500);
        context = CreateContext(new List<CardRuntime>(), hoop);
        context.Strategy = ScoreStrategy.TwoPoint;
        Resolve(deepThree, context, curry);

        Expect(hoop.Hp == 290, failures, "CurryDeepThree at 100 ATK with 2PT strategy should only deal base 210 damage.");
    }

    private static void TestUtilityEffects(List<string> failures)
    {
        var courtVision = LoadCard("CourtVision");
        var context = CreateContext(new List<CardRuntime>());
        context.Ap = 2;
        context.MaxAp = 4;
        Resolve(courtVision, context);

        Expect(context.Ap == 3, failures, "Court Vision should increase current AP from 2 to 3.");
        Expect(context.MaxAp == 5, failures, "Court Vision should increase max AP from 4 to 5.");

        var lowBlow = LoadCard("Kick");
        var hoop = new HoopState();
        hoop.Reset(500);
        context = CreateContext(new List<CardRuntime>(), hoop);
        Resolve(lowBlow, context, CreatePlayer("Defender", 30, 100));

        Expect(hoop.Shield == 200, failures, "Low Blow should gain 200 shield at 100 DEF.");
        Expect(context.OpposingTeam.Statuses.ApplyInt(StatusModifierType.AvailableAP, 4) == 3, failures, "Low Blow should reduce opponent next phase AP by 1 through status.");
    }

    private static void TestDefenseReaction(List<string> failures)
    {
        var contest = LoadCard("Contest");
        var hoop = new HoopState();
        hoop.Reset(500);
        var context = CreateContext(new List<CardRuntime>(), hoop);

        Resolve(contest, context, CreatePlayer("Defender", 30, 100));

        Expect(hoop.Shield == 60, failures, "Contest should gain 60 shield at 100 DEF.");
        Expect(Mathf.Approximately(context.ActingTeam.Statuses.Multiplier(StatusModifierType.IncomingAttackDamage), 0.8f), failures, "Contest should reduce next incoming attack to 80% damage through status.");
    }

    private static void TestV2Triggers(List<string> failures)
    {
        var onDrawCard = CreateV2Card("OnDraw AP", CardTrigger.OnDraw, EffectActionType.ModifyAvailableAP, 1);
        var drawCard = CreateV2Card("Draw Test", CardTrigger.OnPlay, EffectActionType.DrawCards, 1);
        var context = CreateContext(new List<CardRuntime> { new(onDrawCard, CreatePlayer("DrawOwner", 100, 100)) });
        context.ResolveCardTriggers = (cards, trigger, eventContext) => ResolveTriggers(cards, context, trigger, eventContext);

        Resolve(drawCard, context);

        Expect(context.Hand.Count == 1, failures, "Draw trigger test should draw one card into hand.");
        Expect(context.Ap == 5, failures, "OnDraw trigger should increase AP after the card is drawn.");
        Expect(context.MaxAp == 5, failures, "OnDraw AP gain should also raise max AP.");

        var onDiscardCard = CreateV2Card("OnDiscard AP", CardTrigger.OnDiscard, EffectActionType.ModifyAvailableAP, 1);
        context = CreateContext(new List<CardRuntime>());
        var runtimeCard = new CardRuntime(onDiscardCard, CreatePlayer("DiscardOwner", 100, 100));

        CardEffectResolver.Resolve(runtimeCard, context, CardTrigger.OnDiscard);

        Expect(context.Ap == 5, failures, "OnDiscard trigger should resolve when explicitly discarded.");
        Expect(context.MaxAp == 5, failures, "OnDiscard AP gain should also raise max AP.");
    }

    private static void TestHoopTriggers(List<string> failures)
    {
        var breakCard = CreateRuntimeCard("Break Test", CardType.Attack, 0);
        breakCard.effects.Add(new CardEffectData
        {
            useV2Effect = true,
            trigger = CardTrigger.OnPlay,
            target = new CardTargetData
            {
                side = TargetSide.Opponent,
                kind = TargetKind.Hoop,
                selector = TargetSelectorType.OpponentTeam
            },
            action = new EffectActionData
            {
                actionType = EffectActionType.DealDamage,
                multiplier = 1f
            }
        });
        breakCard.effects.Add(new CardEffectData
        {
            useV2Effect = true,
            trigger = CardTrigger.OnHoopBroken,
            target = new CardTargetData
            {
                side = TargetSide.Self,
                kind = TargetKind.Team,
                selector = TargetSelectorType.ActingTeam
            },
            action = new EffectActionData
            {
                actionType = EffectActionType.ModifyAvailableAP,
                intValue = 1
            }
        });

        var hoop = new HoopState();
        hoop.Reset(50);
        var context = CreateContext(new List<CardRuntime>(), hoop);
        context.ResolveCardTriggers = (cards, trigger, eventContext) => ResolveTriggers(cards, context, trigger, eventContext);

        Resolve(breakCard, context, CreatePlayer("Breaker", 100, 100));

        Expect(hoop.IsBroken, failures, "Hoop trigger test should break the hoop.");
        Expect(context.Ap == 5, failures, "OnHoopBroken trigger should resolve after a card breaks the hoop.");
    }

    private static void TestEventContext(List<string> failures)
    {
        var breakCard = CreateRuntimeCard("Event Break Test", CardType.Attack, 0);
        breakCard.effects.Add(new CardEffectData
        {
            useV2Effect = true,
            trigger = CardTrigger.OnPlay,
            target = new CardTargetData
            {
                side = TargetSide.Opponent,
                kind = TargetKind.Hoop,
                selector = TargetSelectorType.OpponentTeam
            },
            action = new EffectActionData
            {
                actionType = EffectActionType.DealDamage,
                multiplier = 1f
            }
        });
        breakCard.effects.Add(new CardEffectData
        {
            useV2Effect = true,
            trigger = CardTrigger.OnHoopBroken,
            conditions = new List<CardConditionData>
            {
                new()
                {
                    conditionType = CardConditionType.HoopBrokenByThisCard
                }
            },
            target = new CardTargetData
            {
                side = TargetSide.Self,
                kind = TargetKind.Team,
                selector = TargetSelectorType.ActingTeam
            },
            action = new EffectActionData
            {
                actionType = EffectActionType.ModifyAvailableAP,
                intValue = 1
            }
        });

        var hoop = new HoopState();
        hoop.Reset(50);
        var context = CreateContext(new List<CardRuntime>(), hoop);
        EffectEventContext capturedEvent = null;
        context.ResolveCardTriggers = (cards, trigger, eventContext) =>
        {
            if (trigger == CardTrigger.OnHoopBroken)
            {
                capturedEvent = eventContext;
            }

            return ResolveTriggers(cards, context, trigger, eventContext);
        };

        Resolve(breakCard, context, CreatePlayer("Breaker", 100, 100));

        Expect(capturedEvent != null, failures, "OnHoopBroken should provide an event context.");
        Expect(capturedEvent?.DamageDealt == 50, failures, "OnHoopBroken event context should include dealt damage.");
        Expect(capturedEvent?.HoopBrokenBySource == true, failures, "OnHoopBroken event context should mark the source card as the breaker.");
        Expect(context.Ap == 5, failures, "HoopBrokenByThisCard should read event context and resolve the AP trigger.");
    }

    private static void TestKeywordRuntime(List<string> failures)
    {
        var recycleCard = CreateRuntimeCard("Recycle Test", CardType.Universal, 0);
        recycleCard.keywordRules.Add(new CardKeywordData
        {
            tag = CardTag.Recycle,
            value = 2
        });

        var runtimeCard = new CardRuntime(recycleCard, CreatePlayer("RecycleOwner", 100, 100));

        Expect(runtimeCard.IsRecycle, failures, "Recycle keyword should be detected from keywordRules.");
        Expect(runtimeCard.RemainingRecycleCount == 2, failures, "Recycle keyword should initialize remaining count from keyword value.");
        Expect(runtimeCard.TryConsumeRecycle(), failures, "Recycle should be consumable while count remains.");
        Expect(runtimeCard.RemainingRecycleCount == 1, failures, "Recycle should decrement after first consume.");
        Expect(runtimeCard.TryConsumeRecycle(), failures, "Recycle should be consumable a second time.");
        Expect(runtimeCard.RemainingRecycleCount == 0, failures, "Recycle should decrement to zero after second consume.");
        Expect(!runtimeCard.TryConsumeRecycle(), failures, "Recycle should stop when remaining count reaches zero.");
    }

    private static void TestStatusRuntime(List<string> failures)
    {
        var statusCard = CreateStatusCard(
            "Hot Hand",
            EffectActionType.ApplyTeamStatus,
            TargetKind.Team,
            StatusModifierType.OutgoingAttackDamage,
            ModifierValueMode.PercentAdd,
            0.5f,
            0,
            EffectDurationType.CurrentPhase,
            0);
        var attackCard = CreateDamageCard("Status Shot", 1f);
        var hoop = new HoopState();
        hoop.Reset(500);
        var context = CreateContext(new List<CardRuntime>(), hoop);

        Resolve(statusCard, context);
        Resolve(attackCard, context, CreatePlayer("Shooter", 100, 100));

        Expect(hoop.Hp == 350, failures, "Team outgoing attack status should increase 100 damage to 150.");
        Expect(context.ActingTeam.Statuses.Has("Hot Hand"), failures, "Applied team status should be present before phase end.");

        context.ActingTeam.Statuses.TickPhaseEnd(BattlePhase.PlayerAttack);

        Expect(!context.ActingTeam.Statuses.Has("Hot Hand"), failures, "CurrentPhase team status should expire at phase end.");

        var targetCard = new CardRuntime(CreateDamageCard("Target Card", 1f), CreatePlayer("TargetOwner", 100, 100));
        var cardStatus = CreateStatusCard(
            "Marked Card",
            EffectActionType.ApplyCardStatus,
            TargetKind.Card,
            StatusModifierType.OutgoingAttackDamage,
            ModifierValueMode.PercentAdd,
            0.25f,
            0,
            EffectDurationType.UntilTriggered,
            0);
        context = CreateContext(new List<CardRuntime>());
        context.Hand.Add(targetCard);

        Resolve(cardStatus, context);

        Expect(targetCard.Statuses.Has("Marked Card"), failures, "ApplyCardStatus should attach a status to the targeted hand card.");
    }

    private static void TestPlayerRuntime(List<string> failures)
    {
        var ownerBuff = CreateStatusCard(
            "Owner Hot Hand",
            EffectActionType.ApplyPlayerStatus,
            TargetKind.Player,
            StatusModifierType.OutgoingAttackDamage,
            ModifierValueMode.PercentAdd,
            0.5f,
            0,
            EffectDurationType.CurrentPhase,
            0,
            TargetSelectorType.OwnerPlayer);
        var attackCard = CreateDamageCard("Owner Shot", 1f);
        var owner = CreatePlayer("Owner", 100, 100);
        var hoop = new HoopState();
        hoop.Reset(500);
        var context = CreateContextWithPlayers(new[] { owner }, hoop);

        Resolve(ownerBuff, context, owner);
        Resolve(attackCard, context, owner);

        Expect(context.ActingTeam.GetPlayerRuntime(owner).Statuses.Has("Owner Hot Hand"), failures, "OwnerPlayer status should attach to the card owner.");
        Expect(hoop.Hp == 350, failures, "OwnerPlayer outgoing attack status should increase 100 damage to 150.");

        var low = CreatePlayer("Low", 40, 100);
        var high = CreatePlayer("High", 120, 100);
        var highestBuff = CreateStatusCard(
            "Highest Hot Hand",
            EffectActionType.ApplyPlayerStatus,
            TargetKind.Player,
            StatusModifierType.OutgoingAttackDamage,
            ModifierValueMode.PercentAdd,
            0.25f,
            0,
            EffectDurationType.CurrentPhase,
            0,
            TargetSelectorType.HighestAttackPlayer);
        context = CreateContextWithPlayers(new[] { low, high });

        Resolve(highestBuff, context, low);

        Expect(!context.ActingTeam.GetPlayerRuntime(low).Statuses.Has("Highest Hot Hand"), failures, "HighestAttackPlayer should not target the lower attack player.");
        Expect(context.ActingTeam.GetPlayerRuntime(high).Statuses.Has("Highest Hot Hand"), failures, "HighestAttackPlayer should target the highest attack player.");
    }

    private static void TestDurationRuntime(List<string> failures)
    {
        var fullRoundStatus = CreateStatusCard(
            "Full Round Boost",
            EffectActionType.ApplyTeamStatus,
            TargetKind.Team,
            StatusModifierType.OutgoingAttackDamage,
            ModifierValueMode.PercentAdd,
            0.25f,
            0,
            EffectDurationType.FullRounds,
            1);
        var context = CreateContext(new List<CardRuntime>());

        Resolve(fullRoundStatus, context);
        context.ActingTeam.Statuses.TickPhaseEnd(BattlePhase.PlayerAttack);
        Expect(context.ActingTeam.Statuses.Has("Full Round Boost"), failures, "FullRounds 1 should survive the first own phase end.");
        context.ActingTeam.Statuses.TickPhaseEnd(BattlePhase.PlayerDefense);
        Expect(!context.ActingTeam.Statuses.Has("Full Round Boost"), failures, "FullRounds 1 should expire after two own phase ends.");

        var gameStatus = CreateStatusCard(
            "Game Long Boost",
            EffectActionType.ApplyTeamStatus,
            TargetKind.Team,
            StatusModifierType.OutgoingAttackDamage,
            ModifierValueMode.PercentAdd,
            0.25f,
            0,
            EffectDurationType.ThisGame,
            0);
        context = CreateContext(new List<CardRuntime>());

        Resolve(gameStatus, context);
        context.ActingTeam.Statuses.TickPhaseEnd(BattlePhase.PlayerAttack);
        context.ActingTeam.Statuses.TickPhaseEnd(BattlePhase.PlayerDefense);
        Expect(context.ActingTeam.Statuses.Has("Game Long Boost"), failures, "ThisGame status should not expire from phase ticks.");

        var untilUsedStatus = CreateStatusCard(
            "Use Once Boost",
            EffectActionType.ApplyTeamStatus,
            TargetKind.Team,
            StatusModifierType.OutgoingAttackDamage,
            ModifierValueMode.PercentAdd,
            0.5f,
            0,
            EffectDurationType.UntilUsed,
            0);
        var attackCard = CreateDamageCard("UntilUsed Shot", 1f);
        var hoop = new HoopState();
        hoop.Reset(500);
        context = CreateContext(new List<CardRuntime>(), hoop);

        Resolve(untilUsedStatus, context);
        Resolve(attackCard, context, CreatePlayer("Shooter", 100, 100));

        Expect(hoop.Hp == 350, failures, "UntilUsed outgoing attack status should apply to one attack.");
        Expect(!context.ActingTeam.Statuses.Has("Use Once Boost"), failures, "UntilUsed outgoing attack status should be consumed after it is used.");
    }

    private static void TestLegacyStatusBridge(List<string> failures)
    {
        var nextAttackBuff = CreateLegacyCard("Legacy Next Attack", CardEffectType.BuffNextAttackCard, 0, 0.5f, 0);
        var attackCard = CreateLegacyCard("Legacy Shot", CardEffectType.DealDamage, 1f, 0, 0);
        var shooter = CreatePlayer("Legacy Shooter", 100, 100);
        var hoop = new HoopState();
        hoop.Reset(500);
        var context = CreateContext(new List<CardRuntime>(), hoop);

        Resolve(nextAttackBuff, context, shooter);
        Resolve(attackCard, context, shooter);

        Expect(hoop.Hp == 350, failures, "Legacy BuffNextAttackCard should route through status and increase one 100 damage attack to 150.");
        Expect(!context.ActingTeam.Statuses.Has("Legacy Next Attack_OutgoingAttackDamage_UntilTriggered"), failures, "Legacy next attack status should be consumed after damage.");

        var nextAp = CreateLegacyCard("Legacy AP Tax", CardEffectType.ModifyOpponentNextTurnAp, 0, 0, -1);
        context = CreateContext(new List<CardRuntime>());
        Resolve(nextAp, context, shooter);
        Expect(context.OpposingTeam.Statuses.ApplyInt(StatusModifierType.AvailableAP, 4) == 3, failures, "Legacy ModifyOpponentNextTurnAp should route through AvailableAP status.");

        var nextDraw = CreateLegacyCard("Legacy Draw Setup", CardEffectType.DrawCards, 0, 0, 1);
        context = CreateContext(new List<CardRuntime>());
        Resolve(nextDraw, context, shooter);
        Expect(context.ActingTeam.Statuses.ApplyInt(StatusModifierType.DrawCount, 4) == 5, failures, "Legacy DrawCards should route through DrawCount status.");
    }

    private static void TestTargetResolverRuntime(List<string> failures)
    {
        var bothTeamStatus = CreateStatusCard(
            "Both Team Boost",
            EffectActionType.ApplyTeamStatus,
            TargetKind.Team,
            StatusModifierType.OutgoingAttackDamage,
            ModifierValueMode.PercentAdd,
            0.25f,
            0,
            EffectDurationType.CurrentPhase,
            0,
            TargetSelectorType.All,
            TargetSide.Both);
        var acting = CreatePlayer("Acting", 100, 100);
        var opposing = CreatePlayer("Opposing", 100, 100);
        var context = CreateContextWithTeams(new[] { acting }, new[] { opposing });

        Resolve(bothTeamStatus, context, acting);

        Expect(context.ActingTeam.Statuses.Has("Both Team Boost"), failures, "Both team target should apply status to acting team.");
        Expect(context.OpposingTeam.Statuses.Has("Both Team Boost"), failures, "Both team target should apply status to opposing team.");

        var opponentPlayerStatus = CreateStatusCard(
            "Opponent Player Mark",
            EffectActionType.ApplyPlayerStatus,
            TargetKind.Player,
            StatusModifierType.IncomingAttackDamage,
            ModifierValueMode.Multiplier,
            0.8f,
            0,
            EffectDurationType.CurrentPhase,
            0,
            TargetSelectorType.All,
            TargetSide.Opponent);
        context = CreateContextWithTeams(new[] { acting }, new[] { opposing });

        Resolve(opponentPlayerStatus, context, acting);

        Expect(!context.ActingTeam.GetPlayerRuntime(acting).Statuses.Has("Opponent Player Mark"), failures, "Opponent player target should not apply to acting player.");
        Expect(context.OpposingTeam.GetPlayerRuntime(opposing).Statuses.Has("Opponent Player Mark"), failures, "Opponent player target should apply to opposing player.");

        var selfCard = new CardRuntime(CreateRuntimeCard("Self Hand Card", CardType.Universal, 1), acting);
        var opponentCard = new CardRuntime(CreateRuntimeCard("Opponent Hand Card", CardType.Universal, 1), opposing);
        var bothCardStatus = CreateStatusCard(
            "Both Card Mark",
            EffectActionType.ApplyCardStatus,
            TargetKind.Card,
            StatusModifierType.CardCost,
            ModifierValueMode.FlatAdd,
            0f,
            -1,
            EffectDurationType.CurrentPhase,
            0,
            TargetSelectorType.All,
            TargetSide.Both);
        context = CreateContextWithTeams(new[] { acting }, new[] { opposing });
        context.Hand.Add(selfCard);
        context.OpposingHand.Add(opponentCard);

        Resolve(bothCardStatus, context, acting);

        Expect(selfCard.Statuses.Has("Both Card Mark"), failures, "Both card target should apply to acting hand card.");
        Expect(opponentCard.Statuses.Has("Both Card Mark"), failures, "Both card target should apply to opposing hand card.");
    }

    private static void TestActionRuntime(List<string> failures)
    {
        var owner = CreatePlayer("Action Owner", 100, 100);
        var hoop = new HoopState();
        hoop.Reset(500);
        var context = CreateContext(new List<CardRuntime>(), hoop);
        var repeatDamage = CreateActionCard("Repeat Damage Test", EffectActionType.RepeatDamage, TargetKind.Hoop);
        repeatDamage.effects[0].action.multiplier = 1f;
        repeatDamage.effects[0].action.intValue = 2;

        Resolve(repeatDamage, context, owner);
        Expect(hoop.Hp == 300, failures, "RepeatDamage should deal damage twice.");

        var modifyShield = CreateActionCard("Modify Shield Test", EffectActionType.ModifyShield, TargetKind.Team);
        modifyShield.effects[0].action.percentageValue = 0.5f;
        modifyShield.effects[0].duration.durationType = EffectDurationType.CurrentPhase;
        var shieldCard = CreateActionCard("Shield Gain Test", EffectActionType.GainShield, TargetKind.Hoop);
        shieldCard.effects[0].action.multiplier = 1f;
        hoop.Reset(500);
        context = CreateContext(new List<CardRuntime>(), hoop);

        Resolve(modifyShield, context, owner);
        Resolve(shieldCard, context, owner);
        Expect(hoop.Shield == 150, failures, "ModifyShield should increase the next shield gain calculation.");

        var targetCard = new CardRuntime(CreateDamageCard("Copy Target", 1f), owner);
        var copyCard = CreateActionCard("Copy Card Test", EffectActionType.CopyCard, TargetKind.Card);
        copyCard.effects[0].action.toZone = CardZone.Hand;
        context = CreateContext(new List<CardRuntime>());
        context.Hand.Add(targetCard);

        Resolve(copyCard, context, owner);
        Expect(context.Hand.Count == 2, failures, "CopyCard should copy the targeted card into hand.");
        Expect(context.Hand.Exists(card => card != targetCard && card.Data == targetCard.Data), failures, "CopyCard should preserve copied CardData.");

        var playTarget = new CardRuntime(CreateDamageCard("Play Target", 1f), owner);
        var playCard = CreateActionCard("Play Card Test", EffectActionType.PlayCard, TargetKind.Card);
        playCard.effects[0].action.consumeAP = false;
        hoop.Reset(500);
        context = CreateContext(new List<CardRuntime>(), hoop);
        context.Hand.Add(playTarget);

        Resolve(playCard, context, owner);
        Expect(hoop.Hp == 400, failures, "PlayCard should resolve the target card's OnPlay effect.");
        Expect(!context.Hand.Contains(playTarget), failures, "PlayCard should remove the played target from hand.");

        var randomTargetA = new CardRuntime(CreateDamageCard("Random Target A", 1f), owner);
        var randomTargetB = new CardRuntime(CreateDamageCard("Random Target B", 1f), owner);
        var playRandom = CreateActionCard("Play Random Test", EffectActionType.PlayRandomCards, TargetKind.Card);
        playRandom.effects[0].target.count = 1;
        playRandom.effects[0].action.consumeAP = false;
        hoop.Reset(500);
        context = CreateContext(new List<CardRuntime>(), hoop);
        context.Hand.Add(randomTargetA);
        context.Hand.Add(randomTargetB);

        Resolve(playRandom, context, owner);
        Expect(hoop.Hp == 400, failures, "PlayRandomCards should play one random target card.");
        Expect(context.Hand.Count == 1, failures, "PlayRandomCards should remove exactly one played card from hand.");
    }

    private static void TestPlayerChoiceRuntime(List<string> failures)
    {
        var owner = CreatePlayer("Choice Owner", 100, 100);
        var firstCard = new CardRuntime(CreateRuntimeCard("First Choice Card", CardType.Universal, 1), owner);
        var secondCard = new CardRuntime(CreateRuntimeCard("Second Choice Card", CardType.Universal, 1), owner);
        var cardChoice = CreateStatusCard(
            "Chosen Card Mark",
            EffectActionType.ApplyCardStatus,
            TargetKind.Card,
            StatusModifierType.CardCost,
            ModifierValueMode.FlatAdd,
            0f,
            -1,
            EffectDurationType.CurrentPhase,
            0);
        var cardChoiceEffect = cardChoice.effects[0];
        cardChoiceEffect.target.selector = TargetSelectorType.PlayerChoice;
        var context = CreateContext(new List<CardRuntime>());
        context.Hand.Add(firstCard);
        context.Hand.Add(secondCard);
        context.ChosenCardTargets[cardChoiceEffect] = new List<CardRuntime> { secondCard };

        Resolve(cardChoice, context, owner);

        Expect(!firstCard.Statuses.Has("Chosen Card Mark"), failures, "PlayerChoice card target should not affect an unchosen candidate.");
        Expect(secondCard.Statuses.Has("Chosen Card Mark"), failures, "PlayerChoice card target should affect the chosen card.");

        var ally = CreatePlayer("Chosen Ally", 80, 100);
        var playerChoice = CreateStatusCard(
            "Chosen Player Mark",
            EffectActionType.ApplyPlayerStatus,
            TargetKind.Player,
            StatusModifierType.OutgoingAttackDamage,
            ModifierValueMode.PercentAdd,
            0.25f,
            0,
            EffectDurationType.CurrentPhase,
            0,
            TargetSelectorType.PlayerChoice);
        var playerChoiceEffect = playerChoice.effects[0];
        context = CreateContextWithPlayers(new[] { owner, ally });
        var chosenPlayer = context.ActingTeam.GetPlayerRuntime(ally);
        context.ChosenPlayerTargets[playerChoiceEffect] = new List<PlayerRuntime> { chosenPlayer };

        Resolve(playerChoice, context, owner);

        Expect(!context.ActingTeam.GetPlayerRuntime(owner).Statuses.Has("Chosen Player Mark"), failures, "PlayerChoice player target should not affect an unchosen player.");
        Expect(chosenPlayer.Statuses.Has("Chosen Player Mark"), failures, "PlayerChoice player target should affect the chosen player.");
    }

    private static void TestNextCardRuntime(List<string> failures)
    {
        var owner = CreatePlayer("Next Owner", 100, 100);
        var nextCardEffectSource = CreateActionCard("Next Card Discount", EffectActionType.ModifyCardCost, TargetKind.Card);
        nextCardEffectSource.effects[0].target.selector = TargetSelectorType.NextCard;
        nextCardEffectSource.effects[0].action.intValue = -1;
        var source = new CardRuntime(nextCardEffectSource, owner);
        var firstCandidate = new CardRuntime(CreateRuntimeCard("First Candidate", CardType.Universal, 2), owner);
        var secondCandidate = new CardRuntime(CreateRuntimeCard("Second Candidate", CardType.Universal, 2), owner);
        var context = CreateContext(new List<CardRuntime>());
        context.Hand.Add(firstCandidate);
        context.Hand.Add(secondCandidate);
        context.LastPlayedCard = source;
        context.LastPlayedCardIndex = 0;

        CardEffectResolver.Resolve(source, context);

        Expect(firstCandidate.CurrentCost == 1, failures, "NextCard should target the card at the played card's previous hand index.");
        Expect(secondCandidate.CurrentCost == 2, failures, "NextCard should not affect later candidates when count is 1.");

        context = CreateContext(new List<CardRuntime>());
        context.Hand.Add(firstCandidate);
        context.Hand.Add(secondCandidate);
        context.LastPlayedCard = source;
        context.LastPlayedCardIndex = 5;
        firstCandidate.CurrentCost = 2;
        secondCandidate.CurrentCost = 2;

        CardEffectResolver.Resolve(source, context);

        Expect(secondCandidate.CurrentCost == 1, failures, "NextCard should clamp to the last candidate when the previous index is beyond the candidate list.");
    }


    private static void Resolve(CardData card, TurnContext context, PlayerData owner = null)
    {
        owner ??= CreatePlayer("Owner", 100, 100);
        var runtimeCard = new CardRuntime(card, owner);
        CardEffectResolver.Resolve(runtimeCard, context);
    }

    private static TurnContext CreateContext(List<CardRuntime> deckCards, HoopState hoop = null)
    {
        var acting = new TeamRuntime(TeamSide.Player, new[] { CreatePlayer("Acting", 100, 100) }, 1001);
        var opposing = new TeamRuntime(TeamSide.Enemy, new[] { CreatePlayer("Opposing", 100, 100) }, 2002);
        if (hoop == null)
        {
            hoop = new HoopState();
            hoop.Reset(500);
        }

        return new TurnContext
        {
            Phase = BattlePhase.PlayerAttack,
            ActingTeam = acting,
            OpposingTeam = opposing,
            TargetHoop = hoop,
            Strategy = ScoreStrategy.TwoPoint,
            Ap = 4,
            MaxAp = 4,
            DrawCount = 4,
            Deck = new DeckRuntime(deckCards, 3003),
            Hand = new List<CardRuntime>()
        };
    }

    private static TurnContext CreateContextWithPlayers(IReadOnlyList<PlayerData> actingPlayers, HoopState hoop = null)
    {
        return CreateContextWithTeams(actingPlayers, new[] { CreatePlayer("Opposing", 100, 100) }, hoop);
    }

    private static TurnContext CreateContextWithTeams(IReadOnlyList<PlayerData> actingPlayers, IReadOnlyList<PlayerData> opposingPlayers, HoopState hoop = null)
    {
        var acting = new TeamRuntime(TeamSide.Player, actingPlayers, 1001);
        var opposing = new TeamRuntime(TeamSide.Enemy, opposingPlayers, 2002);
        if (hoop == null)
        {
            hoop = new HoopState();
            hoop.Reset(500);
        }

        return new TurnContext
        {
            Phase = BattlePhase.PlayerAttack,
            ActingTeam = acting,
            OpposingTeam = opposing,
            TargetHoop = hoop,
            Strategy = ScoreStrategy.TwoPoint,
            Ap = 4,
            MaxAp = 4,
            DrawCount = 4,
            Deck = new DeckRuntime(new List<CardRuntime>(), 3003),
            Hand = new List<CardRuntime>(),
            OpposingDeck = new DeckRuntime(new List<CardRuntime>(), 4004),
            OpposingHand = new List<CardRuntime>()
        };
    }

    private static string ResolveTriggers(IReadOnlyList<CardRuntime> cards, TurnContext context, CardTrigger trigger, EffectEventContext eventContext = null)
    {
        var messages = new List<string>();
        var previousEvent = context.CurrentEvent;
        context.CurrentEvent = eventContext;
        foreach (var card in cards)
        {
            var message = CardEffectResolver.Resolve(card, context, trigger);
            if (!string.IsNullOrWhiteSpace(message))
            {
                messages.Add(message);
            }
        }

        context.CurrentEvent = previousEvent;
        return string.Join("\n", messages);
    }

    private static CardData LoadCard(string assetName)
    {
        var card = AssetDatabase.LoadAssetAtPath<CardData>($"{CardDataPath}/{assetName}.asset");
        if (card == null)
        {
            throw new System.InvalidOperationException($"Missing card asset: {assetName}");
        }

        return card;
    }

    private static PlayerData CreatePlayer(string name, int attack, int defense)
    {
        var player = ScriptableObject.CreateInstance<PlayerData>();
        player.playerName = name;
        player.attack = attack;
        player.defense = defense;
        return player;
    }

    private static CardData CreateRuntimeCard(string name, CardType type, int apCost)
    {
        var card = ScriptableObject.CreateInstance<CardData>();
        card.cardName = name;
        card.cardType = type;
        card.apCost = apCost;
        return card;
    }

    private static CardData CreateV2Card(string name, CardTrigger trigger, EffectActionType actionType, int value)
    {
        var card = CreateRuntimeCard(name, CardType.Universal, 0);
        card.effects.Add(new CardEffectData
        {
            useV2Effect = true,
            trigger = trigger,
            target = new CardTargetData
            {
                side = TargetSide.Self,
                kind = TargetKind.Team,
                zone = CardZone.Hand,
                ownershipScope = OwnershipScope.TeamAll,
                selector = TargetSelectorType.ActingTeam,
                count = 1
            },
            action = new EffectActionData
            {
                actionType = actionType,
                intValue = value,
                multiplier = 1f
            }
        });
        return card;
    }

    private static CardData CreateDamageCard(string name, float multiplier)
    {
        var card = CreateRuntimeCard(name, CardType.Attack, 0);
        card.effects.Add(new CardEffectData
        {
            useV2Effect = true,
            trigger = CardTrigger.OnPlay,
            target = new CardTargetData
            {
                side = TargetSide.Opponent,
                kind = TargetKind.Hoop,
                selector = TargetSelectorType.OpponentTeam,
                count = 1
            },
            action = new EffectActionData
            {
                actionType = EffectActionType.DealDamage,
                multiplier = multiplier
            }
        });
        return card;
    }

    private static CardData CreateActionCard(string name, EffectActionType actionType, TargetKind targetKind)
    {
        var card = CreateRuntimeCard(name, CardType.Universal, 0);
        card.effects.Add(new CardEffectData
        {
            useV2Effect = true,
            trigger = CardTrigger.OnPlay,
            target = new CardTargetData
            {
                side = targetKind == TargetKind.Hoop ? TargetSide.Opponent : TargetSide.Self,
                kind = targetKind,
                zone = CardZone.Hand,
                ownershipScope = OwnershipScope.TeamAll,
                selector = targetKind == TargetKind.Card ? TargetSelectorType.All : TargetSelectorType.ActingTeam,
                count = 1
            },
            action = new EffectActionData
            {
                actionType = actionType,
                multiplier = 1f,
                intValue = 1
            }
        });
        return card;
    }

    private static CardData CreateLegacyCard(string name, CardEffectType effectType, float powerMultiplier, float percentageValue, int flatValue)
    {
        var card = CreateRuntimeCard(name, CardType.Universal, 0);
        card.effects.Add(new CardEffectData
        {
            useV2Effect = false,
            effectType = effectType,
            powerMultiplier = powerMultiplier,
            percentageValue = percentageValue,
            flatValue = flatValue
        });
        return card;
    }

    private static CardData CreateStatusCard(
        string statusId,
        EffectActionType actionType,
        TargetKind targetKind,
        StatusModifierType modifierType,
        ModifierValueMode valueMode,
        float floatValue,
        int intValue,
        EffectDurationType durationType,
        int durationCount,
        TargetSelectorType selector = TargetSelectorType.ActingTeam,
        TargetSide targetSide = TargetSide.Self)
    {
        var card = CreateRuntimeCard(statusId, CardType.Universal, 0);
        card.effects.Add(new CardEffectData
        {
            useV2Effect = true,
            trigger = CardTrigger.OnPlay,
            target = new CardTargetData
            {
                side = targetSide,
                kind = targetKind,
                zone = CardZone.Hand,
                ownershipScope = OwnershipScope.TeamAll,
                selector = targetKind == TargetKind.Card ? TargetSelectorType.All : selector,
                count = 1
            },
            action = new EffectActionData
            {
                actionType = actionType,
                statusId = statusId,
                statusDisplayName = statusId,
                statusStacks = 1,
                modifiers = new List<EffectModifierData>
                {
                    new()
                    {
                        modifierType = modifierType,
                        valueMode = valueMode,
                        floatValue = floatValue,
                        intValue = intValue
                    }
                }
            },
            duration = new EffectDurationData
            {
                durationType = durationType,
                count = durationCount
            }
        });
        return card;
    }

    private static void Expect(bool condition, List<string> failures, string message)
    {
        if (!condition)
        {
            failures.Add(message);
        }
    }
}
