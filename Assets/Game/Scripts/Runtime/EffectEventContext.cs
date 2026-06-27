using System.Collections.Generic;

namespace COYGame
{
    public sealed class EffectEventContext
    {
        public CardTrigger Trigger;
        public CardRuntime SourceCard;
        public IReadOnlyList<CardRuntime> AffectedCards = new List<CardRuntime>();
        public HoopState TargetHoop;
        public int DamageDealt;
        public bool HoopWasBroken;
        public bool HoopBrokenBySource;
    }
}
