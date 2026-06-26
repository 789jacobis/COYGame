using System;
using System.Collections.Generic;
using UnityEngine;

namespace COYGame
{
    [Serializable]
    public sealed class CardEffectData
    {
        [Header("V2 Effect")]
        public bool useV2Effect;
        public CardTrigger trigger = CardTrigger.OnPlay;
        public List<CardConditionData> conditions = new();
        public CardTargetData target = new();
        public EffectActionData action = new();
        public EffectDurationData duration = new();

        [Header("Legacy Effect")]
        public CardEffectType effectType = CardEffectType.DealDamage;
        public float powerMultiplier = 1f;
        public float percentageValue;
        public int flatValue;
        public ScoreStrategy strategy = ScoreStrategy.TwoPoint;
    }

    [Serializable]
    public sealed class CardConditionData
    {
        public CardConditionType conditionType = CardConditionType.None;
        public ScoreStrategy strategy = ScoreStrategy.TwoPoint;
        public string statusId;
        public int intValue;
    }

    [Serializable]
    public sealed class CardTargetData
    {
        public TargetSide side = TargetSide.Opponent;
        public TargetKind kind = TargetKind.Hoop;
        public CardZone zone = CardZone.Hand;
        public OwnershipScope ownershipScope = OwnershipScope.TeamAll;
        public TargetSelectorType selector = TargetSelectorType.All;
        [Min(1)] public int count = 1;
    }

    [Serializable]
    public sealed class EffectActionData
    {
        public EffectActionType actionType = EffectActionType.DealDamage;
        public EffectModifierType modifierType = EffectModifierType.None;
        public float multiplier = 1f;
        public float percentageValue;
        public int intValue;
        public string statusId;
        public CardData cardToGenerate;
        public CardZone fromZone = CardZone.Hand;
        public CardZone toZone = CardZone.DiscardPile;

        [Header("Auto Play")]
        public bool consumeAP = true;
        public bool triggerOnPlay = true;
        public bool allowFrozen;
        public bool stopIfHoopBroken = true;
    }

    [Serializable]
    public sealed class EffectDurationData
    {
        public EffectDurationType durationType = EffectDurationType.Instant;
        [Min(0)] public int count;
    }
}
