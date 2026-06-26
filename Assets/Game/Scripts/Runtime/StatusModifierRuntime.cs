using UnityEngine;

namespace COYGame
{
    public sealed class StatusModifierRuntime
    {
        public StatusModifierRuntime(StatusModifierType modifierType, ModifierValueMode valueMode, float floatValue, int intValue)
        {
            ModifierType = modifierType;
            ValueMode = valueMode;
            FloatValue = floatValue;
            IntValue = intValue;
        }

        public StatusModifierType ModifierType { get; }
        public ModifierValueMode ValueMode { get; }
        public float FloatValue { get; }
        public int IntValue { get; }

        public int ApplyToInt(int value)
        {
            return ValueMode switch
            {
                ModifierValueMode.FlatAdd => value + IntValue,
                ModifierValueMode.PercentAdd => Mathf.RoundToInt(value * (1f + FloatValue)),
                ModifierValueMode.Multiplier => Mathf.RoundToInt(value * FloatValue),
                ModifierValueMode.Override => IntValue,
                _ => value
            };
        }

        public float ToMultiplier()
        {
            return ValueMode switch
            {
                ModifierValueMode.PercentAdd => 1f + FloatValue,
                ModifierValueMode.Multiplier => FloatValue,
                _ => 1f
            };
        }
    }
}
