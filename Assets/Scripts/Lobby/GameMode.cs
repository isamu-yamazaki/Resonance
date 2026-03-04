namespace Resonance.LobbySystem
{
    public enum GameMode
    {
        Arena = 0,  // default
        Polarity = 1,
    }

    public static class Extensions
    {
        public static GameMode CycleNext(this GameMode gameMode)
        {
            if (gameMode == GameMode.Arena)
            {
                return GameMode.Polarity;
            }
            return GameMode.Arena;
        }
    }
}
