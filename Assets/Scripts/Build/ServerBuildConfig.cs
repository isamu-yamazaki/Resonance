using UnityEngine;

[CreateAssetMenu(fileName = "ServerBuildConfig", menuName = "Resonance/Server Build Configuration")]
public class ServerBuildConfig : ScriptableObject
{
    public bool useProductionRelay;
}
