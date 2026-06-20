using UnityEngine;

namespace COYGame
{
    [CreateAssetMenu(menuName = "COY/Card", fileName = "NewCard")]
    public sealed class CardData : ScriptableObject
    {
        [Header("Identity")]
        public string cardName;
        [TextArea(2, 5)] public string rulesText;
        public CardType cardType;

        [Header("Cost")]
        [Min(0)] public int apCost = 1;

        [Header("Primary Effect")]
        public CardEffectType effectType = CardEffectType.DealDamage;
        public float powerMultiplier = 1f;

        [Header("Secondary Values")]
        public float percentageValue;
        public int flatValue;
    }
}
