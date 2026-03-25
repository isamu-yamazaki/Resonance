using System;

namespace Resonance.Assemblies.LobbySystem
{
    [Serializable]
    public partial struct LobbyUser
    {
        public string Id;
        public string DisplayName;
        public bool IsReady;
        public bool IsOwner;

    }
}
