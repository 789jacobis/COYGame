namespace COYGame
{
    public sealed class CardRuntime
    {
        public CardRuntime(CardData data, PlayerData owner)
        {
            Data = data;
            Owner = owner;
        }

        public CardData Data { get; }
        public PlayerData Owner { get; }
        public bool WasPlayed { get; set; }
    }
}
