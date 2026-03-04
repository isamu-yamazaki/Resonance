using System;
using System.Collections.Generic;

namespace Resonance.LobbySystem
{
    public struct Lobby
    {
        public string Name;
        public bool IsValid;
        public string LobbyId;
        public string LobbyCode;
        public int MaxPlayers;
        public bool IsOwner;
        public List<LobbyUser> Members;
        public object ServerObject;
        public readonly GameMode GameMode
        {
            get
            {
                if (UnderlyingProviderProperties == null)
                {
                    return default;
                }

                var gameModeString = UnderlyingProviderProperties.GetValueOrDefault(LobbyMetadataKeys.GameMode);
                if (Enum.TryParse(typeof(GameMode), gameModeString, out object result))
                {
                    return (GameMode)result;
                }
                return default;
            }
        }

        /// <summary>
        /// The scene to move players to after all players are ready.
        /// </summary>
        public readonly string SceneName
        {
            get
            {
                if (UnderlyingProviderProperties == null)
                {
                    return null;
                }

                if (UnderlyingProviderProperties.TryGetValue(LobbyMetadataKeys.SceneName, out string sceneName))
                {
                    return sceneName;
                }
                return null;
            }
        }

        /// <summary>
        /// All additional metadata supplied by the lobby provider.
        /// If no typed property reads the desired metadata from the provider,
        /// use this object to retrieve the metadata.
        /// </summary>
        public Dictionary<string, string> UnderlyingProviderProperties;

        public bool HasChanged(Lobby @new)
        {
            if (!IsValid || Name != @new.Name || LobbyId != @new.LobbyId || LobbyCode != @new.LobbyCode || Members.Count != @new.Members.Count || UnderlyingProviderProperties.Count != @new.UnderlyingProviderProperties.Count || ServerObject != @new.ServerObject)
                return true;

            for (int i = 0; i < @new.Members.Count; i++)
            {
                var newMember = @new.Members[i];
                var oldMember = Members[i];

                if (newMember.Id != oldMember.Id || newMember.IsReady != oldMember.IsReady || newMember.DisplayName != oldMember.DisplayName || newMember.Avatar != oldMember.Avatar)
                    return true;
            }

            foreach (var oldProp in UnderlyingProviderProperties)
            {
                if (!@new.UnderlyingProviderProperties.TryGetValue(oldProp.Key, out var newVal) || oldProp.Value != newVal)
                    return true;
            }

            return false;
        }
    }

    public static class LobbyFactory
    {
        public static Lobby Create(string name, string lobbyId, int maxPlayers, bool isOwner, List<LobbyUser> members, Dictionary<string, string> properties)
        {
            return new Lobby
            {
                Name = name,
                IsValid = true,
                LobbyId = lobbyId,
                MaxPlayers = maxPlayers,
                UnderlyingProviderProperties = properties ?? new Dictionary<string, string>(),
                IsOwner = isOwner,
                Members = members,
            };
        }

        public static Lobby Create(string name, string lobbyId, string lobbyCode, int maxPlayers, bool isOwner, List<LobbyUser> members, Dictionary<string, string> properties, object serverObject = null)
        {
            return new Lobby
            {
                Name = name,
                IsValid = true,
                LobbyId = lobbyId,
                LobbyCode = lobbyCode,
                MaxPlayers = maxPlayers,
                UnderlyingProviderProperties = properties ?? new Dictionary<string, string>(),
                IsOwner = isOwner,
                Members = members,
                ServerObject = serverObject,
            };
        }
    }
}
