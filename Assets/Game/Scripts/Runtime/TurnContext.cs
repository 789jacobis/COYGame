using System;
using System.Collections.Generic;

namespace COYGame
{
    public sealed class TurnContext
    {
        public BattlePhase Phase;
        public TeamRuntime ActingTeam;
        public TeamRuntime OpposingTeam;
        public HoopState TargetHoop;
        public ScoreStrategy Strategy = ScoreStrategy.TwoPoint;
        public int Ap;
        public int MaxAp;
        public int DrawCount = 4;
        public float OutgoingAttackMultiplier = 1f;
        public float NextAttackCardMultiplier = 1f;
        public float NextIncomingAttackMultiplier = 1f;
        public DeckRuntime Deck;
        public List<CardRuntime> Hand = new();
        public Func<IReadOnlyList<CardRuntime>, CardTrigger, string> ResolveCardTriggers;
    }
}
