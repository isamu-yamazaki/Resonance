using System;
using PurrNet;
using PurrNet.Transports;
using Resonance.Contracts;
using UnityEngine;

namespace Resonance.GameBootstrap
{
    public class PlayerIdToMatchMemberIdMap : NetworkBehaviour
    {
        private NetworkedMatchDataHolder _matchDataHolder;
        public event Action OnDictionaryChanged;
        [SerializeField] private SyncDictionary<PlayerID, PlayerIdentity> matchMemberIdentitiesByPlayerId = new();

        public static PlayerIdToMatchMemberIdMap Instance
        {
            get
            {
                if (InstanceHandler.TryGetInstance<PlayerIdToMatchMemberIdMap>(out var instance))
                {
                    return instance;
                }
                return null;
            }
        }

        private void Awake()
        {
            if (InstanceHandler.TryGetInstance<PlayerIdToMatchMemberIdMap>(out var _))
            {
                Destroy(this);
                return;
            }

            InstanceHandler.RegisterInstance(this);
            _matchDataHolder = FindFirstObjectByType<NetworkedMatchDataHolder>();

            if (_matchDataHolder == null)
            {
                Debug.LogError(
                    $"[{nameof(PlayerIdToMatchMemberIdMap)}] Unable to find {nameof(NetworkedMatchDataHolder)} component.");
            }
        }

        protected override void OnSpawned(bool asServer)
        {
            base.OnSpawned(asServer);

            matchMemberIdentitiesByPlayerId.onChanged += HandleSyncDictionaryChanged;

            if (!asServer)
            {
                networkManager.onClientConnectionState += OnClientConnectionState;
            }
            else
            {
                networkManager.onPlayerLeft += HandlePlayerLeft;
            }
        }


        protected override void OnDespawned(bool asServer)
        {
            base.OnDespawned(asServer);

            matchMemberIdentitiesByPlayerId.onChanged -= HandleSyncDictionaryChanged;

            if (!asServer)
            {
                networkManager.onClientConnectionState -= OnClientConnectionState;
            }
            else
            {
                networkManager.onPlayerLeft -= HandlePlayerLeft;
            }
        }

        private void HandlePlayerLeft(PlayerID player, bool asServer)
        {
            if (isServer)
            {
                UnregisterPlayerId(player);
            }
        }

        private void UnregisterPlayerId(PlayerID playerId)
        {
            matchMemberIdentitiesByPlayerId.Remove(playerId);
        }

        private void OnClientConnectionState(ConnectionState state)
        {
            if (state == ConnectionState.Connected)
            {
                RegisterPlayerIdUsingClientToken();
            }
        }

        private void RegisterPlayerIdUsingClientToken()
        {
            if (ClientTokenHolder.Instance?.ClientToken != null)
            {
                RegisterPlayer(ClientTokenHolder.Instance?.ClientToken);
            }
        }

        [ServerRpc]
        private void RegisterPlayer(string token, RPCInfo info = default)
        {
            if (token == null) return;
            var playerIdentity = _matchDataHolder.ExchangePlayerIdentityForClientToken(token);
            if (playerIdentity != null)
            {
                matchMemberIdentitiesByPlayerId.Add(info.sender, playerIdentity.Value);
            }
        }

        private void HandleSyncDictionaryChanged(SyncDictionaryChange<PlayerID, PlayerIdentity> change)
        {
            OnDictionaryChanged?.Invoke();
        }
    }
}