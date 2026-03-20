using System;

namespace Resonance.Assemblies.LobbySystem
{
    public partial struct LobbyUser : IEquatable<LobbyUser>
    {
        public bool Equals(LobbyUser other)
        {
            return Id == other.Id
                && DisplayName == other.DisplayName
                && IsReady == other.IsReady
                && IsOwner == other.IsOwner;
        }

        public override bool Equals(object obj) => obj is LobbyUser other && Equals(other);

        public override int GetHashCode() => Id?.GetHashCode() ?? 0;
    }
}
