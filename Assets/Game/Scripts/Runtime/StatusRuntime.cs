using System.Collections.Generic;

namespace COYGame
{
    public sealed class StatusRuntime
    {
        public StatusRuntime(
            string id,
            string displayName,
            int stacks,
            int maxStacks,
            EffectDurationType durationType,
            int remainingDuration,
            IEnumerable<StatusModifierRuntime> modifiers)
        {
            Id = id;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? id : displayName;
            Stacks = stacks <= 0 ? 1 : stacks;
            MaxStacks = maxStacks;
            DurationType = durationType;
            RemainingDuration = remainingDuration;
            Modifiers.AddRange(modifiers);
        }

        public string Id { get; }
        public string DisplayName { get; }
        public int Stacks { get; private set; }
        public int MaxStacks { get; }
        public EffectDurationType DurationType { get; }
        public int RemainingDuration { get; private set; }
        public List<StatusModifierRuntime> Modifiers { get; } = new();

        public void AddStacks(int stacks)
        {
            Stacks += stacks <= 0 ? 1 : stacks;
            if (MaxStacks > 0 && Stacks > MaxStacks)
            {
                Stacks = MaxStacks;
            }
        }

        public bool TickPhaseEnd(BattlePhase phase)
        {
            if (!ShouldTickOnPhaseEnd(phase))
            {
                return false;
            }

            RemainingDuration--;
            return RemainingDuration <= 0;
        }

        private bool ShouldTickOnPhaseEnd(BattlePhase phase)
        {
            return DurationType switch
            {
                EffectDurationType.CurrentPhase => true,
                EffectDurationType.OwnAttackPhases => phase is BattlePhase.PlayerAttack or BattlePhase.EnemyAttack,
                EffectDurationType.OwnDefensePhases => phase is BattlePhase.PlayerDefense or BattlePhase.EnemyDefense,
                EffectDurationType.AnyOwnPhases => phase is BattlePhase.PlayerAttack or BattlePhase.EnemyAttack or BattlePhase.PlayerDefense or BattlePhase.EnemyDefense,
                _ => false
            };
        }
    }
}
