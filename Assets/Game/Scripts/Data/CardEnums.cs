namespace COYGame
{
    public enum CardType
    {
        Attack,
        Defense
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
        GainBonusShieldIfStrategy
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
