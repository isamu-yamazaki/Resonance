using UnityEngine;

[CreateAssetMenu(fileName = "BuildConfig", menuName = "Resonance/Build Configuration")]
public class BuildConfig : ScriptableObject
{
    public bool enableSteamLobby;
    public bool useProductionRelay;
    public bool isProduction;
}
