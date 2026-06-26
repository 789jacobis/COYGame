using System.Collections.Generic;

namespace COYGame
{
    public sealed class StatusContainer
    {
        private readonly List<StatusRuntime> statuses = new();

        public IReadOnlyList<StatusRuntime> Statuses => statuses;

        public void Add(StatusRuntime status)
        {
            if (status == null || string.IsNullOrWhiteSpace(status.Id))
            {
                return;
            }

            var existing = statuses.Find(candidate => candidate.Id == status.Id);
            if (existing != null)
            {
                existing.AddStacks(status.Stacks);
                return;
            }

            statuses.Add(status);
        }

        public void Clear(string statusId)
        {
            if (string.IsNullOrWhiteSpace(statusId))
            {
                statuses.Clear();
                return;
            }

            statuses.RemoveAll(status => status.Id == statusId);
        }

        public bool Has(string statusId)
        {
            return statuses.Exists(status => status.Id == statusId);
        }

        public int StackCount(string statusId)
        {
            var status = statuses.Find(candidate => candidate.Id == statusId);
            return status?.Stacks ?? 0;
        }

        public int ApplyInt(StatusModifierType modifierType, int value)
        {
            var result = value;
            foreach (var status in statuses)
            {
                foreach (var modifier in status.Modifiers)
                {
                    if (modifier.ModifierType == modifierType)
                    {
                        result = modifier.ApplyToInt(result);
                    }
                }
            }

            return result;
        }

        public float Multiplier(StatusModifierType modifierType)
        {
            var result = 1f;
            foreach (var status in statuses)
            {
                foreach (var modifier in status.Modifiers)
                {
                    if (modifier.ModifierType == modifierType)
                    {
                        result *= modifier.ToMultiplier();
                    }
                }
            }

            return result;
        }

        public void TickPhaseEnd(BattlePhase phase)
        {
            for (var i = statuses.Count - 1; i >= 0; i--)
            {
                if (statuses[i].TickPhaseEnd(phase))
                {
                    statuses.RemoveAt(i);
                }
            }
        }

        public void ConsumeTriggered(StatusModifierType modifierType)
        {
            for (var i = statuses.Count - 1; i >= 0; i--)
            {
                if (statuses[i].DurationType != EffectDurationType.UntilTriggered)
                {
                    continue;
                }

                if (statuses[i].Modifiers.Exists(modifier => modifier.ModifierType == modifierType))
                {
                    statuses.RemoveAt(i);
                }
            }
        }
    }
}
