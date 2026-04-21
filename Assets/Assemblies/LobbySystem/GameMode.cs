namespace Resonance.Assemblies.LobbySystem
{
    public enum GameMode
    {
        Arena = 0,  // default
        Polarity = 1,
        ArenaShort = 2,
    }

    public static class Extensions
    {
        public static GameMode CycleNext(this GameMode gameMode)
        {
            if (gameMode == GameMode.Arena)
            {
                return GameMode.Polarity;
            }
            if (gameMode == GameMode.Polarity)
            {
                return GameMode.ArenaShort;
            }
            return GameMode.Arena;
        }
    }
}
