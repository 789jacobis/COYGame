namespace COYGame
{
    public sealed class CardRuntime
    {
        public CardRuntime(CardData data, PlayerData owner)
        {
            Data = data;
            Owner = owner;
            CurrentCost = data.apCost;
            RemainingRecycleCount = GetKeywordValue(CardTag.Recycle);
        }

        public CardData Data { get; }
        public PlayerData Owner { get; }
        public int CurrentCost { get; set; }
        public int RemainingRecycleCount { get; private set; }
        public bool WasPlayed { get; set; }
        public bool IsExhaust => HasTag(CardTag.Exhaust);
        public bool IsOnce => HasTag(CardTag.Once);
        public bool IsRecycle => HasTag(CardTag.Recycle);
        public bool IsRetain => HasTag(CardTag.Retain);
        public bool IsCombo => HasTag(CardTag.Combo);

        public bool TryConsumeRecycle()
        {
            if (!IsRecycle || RemainingRecycleCount <= 0)
            {
                return false;
            }

            RemainingRecycleCount--;
            WasPlayed = false;
            return true;
        }

        public bool HasTag(CardTag tag)
        {
            if (Data.tags != null && Data.tags.Contains(tag))
            {
                return true;
            }

            if (Data.keywordRules == null)
            {
                return false;
            }

            foreach (var keyword in Data.keywordRules)
            {
                if (keyword != null && keyword.tag == tag)
                {
                    return true;
                }
            }

            return false;
        }

        public int GetKeywordValue(CardTag tag)
        {
            if (Data.keywordRules == null)
            {
                return 0;
            }

            foreach (var keyword in Data.keywordRules)
            {
                if (keyword != null && keyword.tag == tag)
                {
                    return keyword.value;
                }
            }

            return 0;
        }
    }
}
