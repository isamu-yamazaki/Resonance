using UnityEngine;

[CreateAssetMenu(fileName = "ClientBuildConfig", menuName = "Resonance/Client Build Configuration")]
public class ClientBuildConfig : ScriptableObject
{
    public bool enableSteamLobby;
    public bool useProductionRelay;
    public bool useRemoteOrchestrator;
    public bool useClientServerMode;
    public bool isProduction;
}
