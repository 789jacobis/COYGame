using System;
using UnityEngine;

namespace COYGame
{
    [Serializable]
    public sealed class CardKeywordData
    {
        public CardTag tag = CardTag.Exhaust;
        [Min(0)] public int value;
    }
}
