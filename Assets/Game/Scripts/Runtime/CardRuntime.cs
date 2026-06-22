namespace COYGame
{
    public sealed class CardRuntime
    {
        public CardRuntime(CardData data, PlayerData owner)
        {
            Data = data;
            Owner = owner;
            CurrentCost = data.apCost;
        }

        public CardData Data { get; }
        public PlayerData Owner { get; }
        public int CurrentCost { get; set; }
        public bool WasPlayed { get; set; }
    }
}
