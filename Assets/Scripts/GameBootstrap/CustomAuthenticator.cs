using System;
using System.Threading.Tasks;
using PurrNet;
using PurrNet.Authentication;
using PurrNet.Transports;

namespace Resonance.GameBootstrap
{
    [RegisterNetworkType(typeof(AuthenticationRequest<string>))]
    public class CustomAuthenticator : AuthenticationBehaviour<string>
    {
        private class AuthenticatorException : Exception
        {
            public AuthenticatorException(string message) : base(message)
            {
            }
        }

        protected override void UnAuthenticateClient(Connection conn)
        {
        }

        protected override Task<AuthenticationRequest<string>> GetClientPayload()
        {
            if (ClientTokenHolder.Instance == null)
            {
                throw new AuthenticatorException("No client token holder found.");
            }

            if (string.IsNullOrEmpty(ClientTokenHolder.Instance.ClientToken))
            {
                throw new AuthenticatorException("No client token found in ClientTokenHolder.");
            }

            return Task.FromResult(new AuthenticationRequest<string>(ClientTokenHolder.Instance.ClientToken));
        }

        protected override Task<AuthenticationResponse> ValidateClientPayload(Connection conn, string payload)
        {
            var networkedMatchDataHolder = FindFirstObjectByType<NetworkedMatchDataHolder>();
            if (networkedMatchDataHolder == null)
            {
                throw new AuthenticatorException("No NetworkedMatchDataHolder found on the server.");
            }

            var identity = networkedMatchDataHolder.ExchangeClientTokenForPlayerIdentity(payload);
            return Task.FromResult(new AuthenticationResponse { success = identity.HasValue });
        }
    }
}