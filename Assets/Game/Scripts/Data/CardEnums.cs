namespace COYGame
{
    public enum CardType
    {
        Attack,
        Defense,
        Universal,
        Item
    }

    public enum CardTag
    {
        Exhaust,
        Once,
        Recycle,
        Retain,
        Combo
    }

    public enum CardTrigger
    {
        OnPlay,
        OnDraw,
        OnDiscard,
        OnCreate,
        OnRemove,
        OnPhaseEnd,
        OnNextOwnPhaseStart,
        OnEveryOwnPhaseStart,
        OnOwnAttackPhaseStart,
        OnOwnAttackPhaseEnd,
        OnOwnDefensePhaseStart,
        OnOwnDefensePhaseEnd,
        OnAnyOwnPhaseStart,
        OnAnyOwnPhaseEnd,
        OnHoopBroken,
        OnHoopDamaged
    }

    public enum CardConditionType
    {
        None,
        PlayerChose2PT,
        PlayerChose3PT,
        EnemyChose2PT,
        EnemyChose3PT,
        HoopBrokenByThisCard,
        CardWasNotPlayedThisPhase,
        HasCardInHand,
        HasEnoughAP,
        TargetHasStatus,
        StatusStackAtLeast,
        CardCostGreaterThan,
        ActingTeamChose2PT,
        ActingTeamChose3PT
    }

    public enum TargetSide
    {
        Self,
        Opponent,
        Both
    }

    public enum TargetKind
    {
        Team,
        Player,
        Card,
        Hoop,
        Zone
    }

    public enum CardZone
    {
        Hand,
        DrawPile,
        DiscardPile,
        Reserved,
        OutsideGame
    }

    public enum OwnershipScope
    {
        OwnerOnly,
        TeamAll,
        All
    }

    public enum TargetSelectorType
    {
        ThisCard,
        NextCard,
        Random,
        All,
        PlayerChoice,
        HighestAttackPlayer,
        LowestCostCard,
        CardsInZone,
        OwnerPlayer,
        ActingTeam,
        OpponentTeam
    }

    public enum EffectDurationType
    {
        Instant,
        CurrentPhase,
        OwnAttackPhases,
        OwnDefensePhases,
        AnyOwnPhases,
        FullRounds,
        ThisGame,
        UntilUsed,
        UntilTriggered
    }

    public enum EffectActionType
    {
        None,
        DealDamage,
        ModifyDamage,
        RepeatDamage,
        GainShield,
        ModifyShield,
        ModifyAvailableAP,
        ModifyMaxPhaseAP,
        ModifyNextOwnPhaseAP,
        ModifyCardCost,
        DrawCards,
        ModifyDrawCount,
        DiscardCards,
        GenerateCard,
        CopyCard,
        MoveCard,
        RemoveCard,
        PlayCard,
        PlayRandomCards,
        ApplyTeamStatus,
        ApplyPlayerStatus,
        ApplyCardStatus,
        ApplyModifier,
        ClearStatus
    }

    public enum EffectModifierType
    {
        None,
        OutgoingAttackThisPhase,
        NextAttackCard,
        NextIncomingAttack
    }

    public enum StatusModifierType
    {
        None,
        OutgoingAttackDamage,
        IncomingAttackDamage,
        ShieldGain,
        AvailableAP,
        MaxAP,
        DrawCount,
        CardCost
    }

    public enum ModifierValueMode
    {
        FlatAdd,
        PercentAdd,
        Multiplier,
        Override
    }

    public enum CardEffectType
    {
        DealDamage,
        GainShield,
        BuffOutgoingAttackThisTurn,
        ReduceNextIncomingAttack,
        ModifyOpponentNextTurnAp,
        ModifyCurrentTurnAp,
        DrawCards,
        DrawCardsNow,
        ModifyHandCardCostsThisPhase,
        BuffNextAttackCard,
        DealBonusDamageIfStrategy,
        GainBonusShieldIfStrategy,
        ModifyRandomHandCardCostThisPhase
    }

    public enum BattlePhase
    {
        None,
        EnemyDefense,
        PlayerChooseStrategy,
        PlayerAttack,
        PlayerAttackResult,
        PlayerDefense,
        EnemyChooseStrategy,
        EnemyAttack,
        EnemyAttackResult,
        BattleOver
    }

    public enum TeamSide
    {
        Player,
        Enemy
    }

    public enum ScoreStrategy
    {
        TwoPoint = 2,
        ThreePoint = 3
    }
}
