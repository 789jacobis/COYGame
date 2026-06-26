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
        TestHoopTriggers(failures);
        TestKeywordRuntime(failures);
        TestStatusRuntime(failures);
        TestPlayerRuntime(failures);

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

        var context = CreateContext(new List<CardRuntime>());
        Resolve(heatCheck, context);
        Expect(Mathf.Approximately(context.OutgoingAttackMultiplier, 1.75f), failures, "HeatCheck should increase outgoing attack multiplier to 1.75.");

        context = CreateContext(new List<CardRuntime>());
        Resolve(curryHeatCheck, context);
        Expect(Mathf.Approximately(context.NextAttackCardMultiplier, 1.75f), failures, "CurryHeatCheck should increase next attack card multiplier to 1.75.");
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
        Expect(context.OpposingTeam.NextTurnApModifier == -1, failures, "Low Blow should reduce opponent next phase AP by 1.");
    }

    private static void TestDefenseReaction(List<string> failures)
    {
        var contest = LoadCard("Contest");
        var hoop = new HoopState();
        hoop.Reset(500);
        var context = CreateContext(new List<CardRuntime>(), hoop);

        Resolve(contest, context, CreatePlayer("Defender", 30, 100));

        Expect(hoop.Shield == 60, failures, "Contest should gain 60 shield at 100 DEF.");
        Expect(Mathf.Approximately(context.NextIncomingAttackMultiplier, 0.8f), failures, "Contest should reduce next incoming attack to 80% damage.");
    }

    private static void TestV2Triggers(List<string> failures)
    {
        var onDrawCard = CreateV2Card("OnDraw AP", CardTrigger.OnDraw, EffectActionType.ModifyAvailableAP, 1);
        var drawCard = CreateV2Card("Draw Test", CardTrigger.OnPlay, EffectActionType.DrawCards, 1);
        var context = CreateContext(new List<CardRuntime> { new(onDrawCard, CreatePlayer("DrawOwner", 100, 100)) });
        context.ResolveCardTriggers = (cards, trigger) => ResolveTriggers(cards, context, trigger);

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
        context.ResolveCardTriggers = (cards, trigger) => ResolveTriggers(cards, context, trigger);

        Resolve(breakCard, context, CreatePlayer("Breaker", 100, 100));

        Expect(hoop.IsBroken, failures, "Hoop trigger test should break the hoop.");
        Expect(context.Ap == 5, failures, "OnHoopBroken trigger should resolve after a card breaks the hoop.");
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
        var acting = new TeamRuntime(TeamSide.Player, actingPlayers, 1001);
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
            Deck = new DeckRuntime(new List<CardRuntime>(), 3003),
            Hand = new List<CardRuntime>()
        };
    }

    private static string ResolveTriggers(IReadOnlyList<CardRuntime> cards, TurnContext context, CardTrigger trigger)
    {
        var messages = new List<string>();
        foreach (var card in cards)
        {
            var message = CardEffectResolver.Resolve(card, context, trigger);
            if (!string.IsNullOrWhiteSpace(message))
            {
                messages.Add(message);
            }
        }

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
        TargetSelectorType selector = TargetSelectorType.ActingTeam)
    {
        var card = CreateRuntimeCard(statusId, CardType.Universal, 0);
        card.effects.Add(new CardEffectData
        {
            useV2Effect = true,
            trigger = CardTrigger.OnPlay,
            target = new CardTargetData
            {
                side = TargetSide.Self,
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
