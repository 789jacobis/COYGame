using System;
using UnityEngine;

namespace COYGame
{
    [Serializable]
    public sealed class CardEffectData
    {
        public CardEffectType effectType = CardEffectType.DealDamage;
        public float powerMultiplier = 1f;
        public float percentageValue;
        public int flatValue;
        public ScoreStrategy strategy = ScoreStrategy.TwoPoint;
    }
}
