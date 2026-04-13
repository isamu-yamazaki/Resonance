using System.Collections;
using NUnit.Framework;
using PurrNet;
using PurrNet.Transports;
using Resonance.Assemblies.LobbySystem;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public class PlayerIdToLobbyMemberIdMapTests
{
    // A Test behaves as an ordinary method
    [Test]
    public void PlayerIdToLobbyMemberIdMapTestsSimplePasses()
    {
        // Use the Assert class to test conditions
    }

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator PlayerIdToLobbyMemberIdMapTestsWithEnumeratorPasses()
    {
        var networkManagerGO = new GameObject("NetworkManager");
        networkManagerGO.SetActive(false);

        var networkManager = networkManagerGO.AddComponent<NetworkManager>();
        var transport = networkManagerGO.AddComponent<UDPTransport>();

        var networkRules = AssetDatabase.LoadAssetAtPath<NetworkRules>("Packages/dev.purrnet.purrnet/Defaults/NetworkRules/Unsafe.asset");

        var alwaysVisibleRule = ScriptableObject.CreateInstance<AlwaysVisibleRule>();
        var visibilityRuleSet = ScriptableObject.CreateInstance<NetworkVisibilityRuleSet>();
        typeof(NetworkVisibilityRuleSet)
            .GetField("_rules", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(visibilityRuleSet, new NetworkVisibilityRule[] { alwaysVisibleRule });

        var nmType = typeof(NetworkManager);
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        // var networkPrefabs = AssetDatabase.LoadAssetAtPath<NetworkPrefabs>("Assets/Prefabs/NetworkPrefabs.asset");

        nmType.GetField("_networkRules", flags).SetValue(networkManager, networkRules);
        nmType.GetField("_transport", flags).SetValue(networkManager, transport);
        nmType.GetField("_visibilityRules", flags).SetValue(networkManager, visibilityRuleSet);
        // nmType.GetField("_networkPrefabs", flags).SetValue(networkManager, networkPrefabs);

        networkManagerGO.SetActive(true);
        // server is automatically started when set to active with transport

        yield return new WaitForSeconds(2);

        var map = new GameObject("PlayerIdToLobbyMemberIdMap");
        map.AddComponent<PlayerIdToLobbyMemberIdMap>();
        // required for directChildren to populate
        NetworkManager.SetupPrefabInfo(map, 0, false);
        networkManager.Spawn(map);

        yield return new WaitForSeconds(30);

        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
    }
}
