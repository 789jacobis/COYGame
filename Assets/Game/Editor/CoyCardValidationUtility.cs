using System.Collections.Generic;
using COYGame;
using UnityEditor;
using UnityEngine;

public static class CoyCardValidationUtility
{
    private const string CardDataPath = "Assets/Game/Data/Cards";

    [MenuItem("Tools/COY/Validate Card Data")]
    public static void ValidateCardData()
    {
        var issues = new List<CardValidationIssue>();
        var cardCount = 0;
        var v2EffectCount = 0;
        var legacyEffectCount = 0;

        foreach (var guid in AssetDatabase.FindAssets("t:CardData", new[] { CardDataPath }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var card = AssetDatabase.LoadAssetAtPath<CardData>(path);
            if (card == null)
            {
                continue;
            }

            cardCount++;
            ValidateCard(card, path, issues, ref v2EffectCount, ref legacyEffectCount);
        }

        foreach (var issue in issues)
        {
            if (issue.IsError)
            {
                Debug.LogError(issue.Message, issue.Context);
            }
            else
            {
                Debug.LogWarning(issue.Message, issue.Context);
            }
        }

        var errorCount = issues.FindAll(issue => issue.IsError).Count;
        var warningCount = issues.Count - errorCount;
        Debug.Log($"COY card validation complete. Cards: {cardCount}, V2 effects: {v2EffectCount}, legacy effects: {legacyEffectCount}, errors: {errorCount}, warnings: {warningCount}.");
    }

    private static void ValidateCard(CardData card, string path, List<CardValidationIssue> issues, ref int v2EffectCount, ref int legacyEffectCount)
    {
        if (string.IsNullOrWhiteSpace(card.cardName))
        {
            Error(issues, card, path, "Card name is empty.");
        }

        if (card.apCost < 0)
        {
            Error(issues, card, path, "AP cost cannot be negative.");
        }

        if (card.cardType == CardType.Item && card.usablePhase == BattlePhase.None)
        {
            Warn(issues, card, path, "Item card has usablePhase = None. This is allowed as 'any phase', but set a specific phase if the item should be restricted.");
        }

        ValidateKeywords(card, path, issues);

        if (card.effects == null || card.effects.Count == 0)
        {
            Warn(issues, card, path, "Card has no explicit effects and will rely on legacy single-effect fallback.");
            legacyEffectCount++;
            return;
        }

        for (var i = 0; i < card.effects.Count; i++)
        {
            var effect = card.effects[i];
            if (effect == null)
            {
                Error(issues, card, path, $"Effect {i} is null.");
                continue;
            }

            if (effect.useV2Effect)
            {
                v2EffectCount++;
                ValidateV2Effect(card, path, effect, i, issues);
            }
            else
            {
                legacyEffectCount++;
                ValidateLegacyEffect(card, path, effect, i, issues);
            }
        }
    }

    private static void ValidateKeywords(CardData card, string path, List<CardValidationIssue> issues)
    {
        if (card.tags == null || card.keywordRules == null)
        {
            return;
        }

        foreach (var keyword in card.keywordRules)
        {
            if (keyword == null)
            {
                Error(issues, card, path, "Keyword rule is null.");
                continue;
            }

            if (keyword.tag == CardTag.Recycle && keyword.value <= 0)
            {
                Error(issues, card, path, "Recycle keyword must have a value greater than 0.");
            }

            if (keyword.tag != CardTag.Recycle && keyword.value != 0)
            {
                Warn(issues, card, path, $"{keyword.tag} keyword has a value, but this keyword currently ignores numeric values.");
            }
        }
    }

    private static void ValidateLegacyEffect(CardData card, string path, CardEffectData effect, int index, List<CardValidationIssue> issues)
    {
        switch (effect.effectType)
        {
            case CardEffectType.DealDamage:
            case CardEffectType.GainShield:
            case CardEffectType.ReduceNextIncomingAttack:
            case CardEffectType.DealBonusDamageIfStrategy:
            case CardEffectType.GainBonusShieldIfStrategy:
                if (effect.powerMultiplier < 0f)
                {
                    Error(issues, card, path, $"Legacy effect {index} has a negative multiplier.");
                }
                break;
        }
    }

    private static void ValidateV2Effect(CardData card, string path, CardEffectData effect, int index, List<CardValidationIssue> issues)
    {
        if (effect.target == null)
        {
            Error(issues, card, path, $"V2 effect {index} has no target.");
            return;
        }

        if (effect.action == null)
        {
            Error(issues, card, path, $"V2 effect {index} has no action.");
            return;
        }

        if (effect.duration == null)
        {
            Error(issues, card, path, $"V2 effect {index} has no duration.");
            return;
        }

        ValidateConditions(card, path, effect, index, issues);
        ValidateTarget(card, path, effect, index, issues);
        ValidateAction(card, path, effect, index, issues);
        ValidateDuration(card, path, effect, index, issues);
    }

    private static void ValidateConditions(CardData card, string path, CardEffectData effect, int index, List<CardValidationIssue> issues)
    {
        if (effect.conditions == null)
        {
            return;
        }

        foreach (var condition in effect.conditions)
        {
            if (condition == null)
            {
                Error(issues, card, path, $"V2 effect {index} has a null condition.");
                continue;
            }

            if (condition.conditionType == CardConditionType.CardCostGreaterThan && effect.target.kind != TargetKind.Card)
            {
                Warn(issues, card, path, $"V2 effect {index} uses CardCostGreaterThan, but the target kind is not Card.");
            }
        }
    }

    private static void ValidateTarget(CardData card, string path, CardEffectData effect, int index, List<CardValidationIssue> issues)
    {
        var target = effect.target;
        if (target.count <= 0)
        {
            Error(issues, card, path, $"V2 effect {index} target count must be greater than 0.");
        }

        if (target.side == TargetSide.Both)
        {
            Warn(issues, card, path, $"V2 effect {index} targets Both, but V2 resolver does not fully support Both yet.");
        }
    }

    private static void ValidateAction(CardData card, string path, CardEffectData effect, int index, List<CardValidationIssue> issues)
    {
        var action = effect.action;
        var target = effect.target;
        switch (action.actionType)
        {
            case EffectActionType.None:
                Error(issues, card, path, $"V2 effect {index} action is None.");
                break;
            case EffectActionType.DealDamage:
                RequireTargetKind(card, path, index, target, TargetKind.Hoop, issues);
                RequirePositiveMultiplier(card, path, index, action, issues);
                break;
            case EffectActionType.GainShield:
                RequireTargetKind(card, path, index, target, TargetKind.Hoop, issues);
                RequirePositiveMultiplier(card, path, index, action, issues);
                break;
            case EffectActionType.ModifyDamage:
                if (action.modifierType == EffectModifierType.None)
                {
                    Error(issues, card, path, $"V2 effect {index} ModifyDamage requires a modifierType.");
                }

                if (Mathf.Approximately(action.percentageValue, 0f))
                {
                    Warn(issues, card, path, $"V2 effect {index} ModifyDamage has percentageValue = 0.");
                }
                break;
            case EffectActionType.ModifyAvailableAP:
            case EffectActionType.ModifyMaxPhaseAP:
            case EffectActionType.ModifyNextOwnPhaseAP:
            case EffectActionType.ModifyCardCost:
            case EffectActionType.ModifyDrawCount:
                if (action.intValue == 0)
                {
                    Warn(issues, card, path, $"V2 effect {index} {action.actionType} has intValue = 0.");
                }
                break;
            case EffectActionType.DrawCards:
            case EffectActionType.DiscardCards:
                if (action.intValue <= 0)
                {
                    Error(issues, card, path, $"V2 effect {index} {action.actionType} requires intValue greater than 0.");
                }
                break;
            case EffectActionType.GenerateCard:
                if (action.cardToGenerate == null)
                {
                    Error(issues, card, path, $"V2 effect {index} GenerateCard requires cardToGenerate.");
                }

                if (action.intValue <= 0)
                {
                    Error(issues, card, path, $"V2 effect {index} GenerateCard requires intValue greater than 0.");
                }
                break;
            case EffectActionType.MoveCard:
                if (action.fromZone == action.toZone)
                {
                    Warn(issues, card, path, $"V2 effect {index} MoveCard moves from and to the same zone.");
                }
                break;
            case EffectActionType.ApplyTeamStatus:
            case EffectActionType.ApplyCardStatus:
            case EffectActionType.ApplyPlayerStatus:
            case EffectActionType.ApplyModifier:
                ValidateStatusAction(card, path, effect, index, issues);
                break;
            case EffectActionType.ClearStatus:
                if (string.IsNullOrWhiteSpace(action.statusId))
                {
                    Warn(issues, card, path, $"V2 effect {index} ClearStatus has empty statusId and will clear all statuses on the target.");
                }
                break;
            case EffectActionType.CopyCard:
            case EffectActionType.PlayCard:
            case EffectActionType.PlayRandomCards:
            case EffectActionType.ModifyShield:
            case EffectActionType.RepeatDamage:
                Warn(issues, card, path, $"V2 effect {index} uses {action.actionType}, but runtime support is not implemented yet.");
                break;
        }
    }

    private static void ValidateStatusAction(CardData card, string path, CardEffectData effect, int index, List<CardValidationIssue> issues)
    {
        var action = effect.action;
        if (string.IsNullOrWhiteSpace(action.statusId))
        {
            Warn(issues, card, path, $"V2 effect {index} {action.actionType} has no statusId. Runtime will use action name as the status id.");
        }

        if (action.modifiers == null || action.modifiers.Count == 0)
        {
            if (action.modifierType == EffectModifierType.None)
            {
                Warn(issues, card, path, $"V2 effect {index} {action.actionType} has no status modifiers.");
            }

            return;
        }

        for (var i = 0; i < action.modifiers.Count; i++)
        {
            var modifier = action.modifiers[i];
            if (modifier == null)
            {
                Error(issues, card, path, $"V2 effect {index} status modifier {i} is null.");
                continue;
            }

            if (modifier.modifierType == StatusModifierType.None)
            {
                Error(issues, card, path, $"V2 effect {index} status modifier {i} has modifierType None.");
            }
        }
    }

    private static void ValidateDuration(CardData card, string path, CardEffectData effect, int index, List<CardValidationIssue> issues)
    {
        var duration = effect.duration;
        if (duration.durationType is EffectDurationType.OwnAttackPhases or EffectDurationType.OwnDefensePhases or EffectDurationType.AnyOwnPhases or EffectDurationType.FullRounds
            && duration.count <= 0)
        {
            Error(issues, card, path, $"V2 effect {index} duration {duration.durationType} requires count greater than 0.");
        }

        if (duration.durationType != EffectDurationType.Instant && effect.action.actionType is EffectActionType.DealDamage or EffectActionType.GainShield or EffectActionType.DrawCards or EffectActionType.ModifyAvailableAP)
        {
            Warn(issues, card, path, $"V2 effect {index} has duration {duration.durationType}, but {effect.action.actionType} currently resolves immediately.");
        }
    }

    private static void RequireTargetKind(CardData card, string path, int index, CardTargetData target, TargetKind expected, List<CardValidationIssue> issues)
    {
        if (target.kind != expected)
        {
            Error(issues, card, path, $"V2 effect {index} expected target kind {expected}, got {target.kind}.");
        }
    }

    private static void RequirePositiveMultiplier(CardData card, string path, int index, EffectActionData action, List<CardValidationIssue> issues)
    {
        if (action.multiplier <= 0f)
        {
            Error(issues, card, path, $"V2 effect {index} requires multiplier greater than 0.");
        }
    }

    private static void Error(List<CardValidationIssue> issues, Object context, string path, string message)
    {
        issues.Add(new CardValidationIssue(true, $"[COY Card Error] {path}: {message}", context));
    }

    private static void Warn(List<CardValidationIssue> issues, Object context, string path, string message)
    {
        issues.Add(new CardValidationIssue(false, $"[COY Card Warning] {path}: {message}", context));
    }

    private readonly struct CardValidationIssue
    {
        public CardValidationIssue(bool isError, string message, Object context)
        {
            IsError = isError;
            Message = message;
            Context = context;
        }

        public bool IsError { get; }
        public string Message { get; }
        public Object Context { get; }
    }
}
