using System;
using System.Collections.Generic;
using System.Linq;

namespace Resonance.Assemblies.LobbySystem
{
    public partial struct Lobby : IEquatable<Lobby>
    {
        public bool Equals(Lobby other)
        {
            return Name == other.Name
                && IsValid == other.IsValid
                && LobbyId == other.LobbyId
                && LobbyCode == other.LobbyCode
                && MaxPlayers == other.MaxPlayers
                && IsOwner == other.IsOwner
                && MembersEqual(other.Members)
                && PropertiesEqual(other.UnderlyingProviderProperties);
        }

        public override bool Equals(object obj) => obj is Lobby other && Equals(other);

        public override int GetHashCode() => LobbyId?.GetHashCode() ?? 0;

        private bool MembersEqual(List<LobbyUser> other)
        {
            if (Members == other) return true;
            if (Members == null || other == null) return false;
            return Members.SequenceEqual(other);
        }

        private bool PropertiesEqual(Dictionary<string, string> other)
        {
            if (UnderlyingProviderProperties == other) return true;
            if (UnderlyingProviderProperties == null || other == null) return false;
            if (UnderlyingProviderProperties.Count != other.Count) return false;
            foreach (var kv in UnderlyingProviderProperties)
            {
                if (!other.TryGetValue(kv.Key, out var val) || val != kv.Value) return false;
            }
            return true;
        }
    }

}
