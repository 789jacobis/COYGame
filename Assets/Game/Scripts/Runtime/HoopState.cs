using UnityEngine;

namespace COYGame
{
    public sealed class HoopState
    {
        public int BaseHp { get; private set; }
        public int Hp { get; private set; }
        public int Shield { get; private set; }
        public int MaxTarget { get; private set; }

        public void Reset(int baseHp)
        {
            BaseHp = baseHp;
            Hp = baseHp;
            Shield = 0;
            MaxTarget = baseHp;
        }

        public void AddShield(int amount)
        {
            Shield += Mathf.Max(0, amount);
            MaxTarget = Mathf.Max(MaxTarget, Hp + Shield);
        }

        public void ApplyThreePointPressure(float multiplier)
        {
            var total = Mathf.CeilToInt((Hp + Shield) * multiplier);
            var extra = Mathf.Max(0, total - Hp - Shield);
            Shield += extra;
            MaxTarget = Mathf.Max(MaxTarget, total);
        }

        public int ApplyDamage(int amount)
        {
            var remaining = Mathf.Max(0, amount);
            var shieldDamage = Mathf.Min(Shield, remaining);
            Shield -= shieldDamage;
            remaining -= shieldDamage;

            var hpDamage = Mathf.Min(Hp, remaining);
            Hp -= hpDamage;
            return shieldDamage + hpDamage;
        }

        public bool IsBroken => Hp <= 0;
    }
}
