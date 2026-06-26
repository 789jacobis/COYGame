namespace COYGame
{
    public sealed class PlayerRuntime
    {
        public PlayerRuntime(PlayerData data)
        {
            Data = data;
        }

        public PlayerData Data { get; }
        public StatusContainer Statuses { get; } = new();
        public string PlayerName => Data != null ? Data.playerName : string.Empty;
        public int Attack => Data != null ? Data.attack : 0;
        public int Defense => Data != null ? Data.defense : 0;
    }
}
