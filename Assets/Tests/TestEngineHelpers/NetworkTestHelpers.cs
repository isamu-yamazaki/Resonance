using System;
using System.Collections;
using PurrNet;
using PurrNet.Transports;
using UnityEditor;
using UnityEngine;

public static class NetworkTestHelpers
{
    public static IEnumerator SetupNetworkManager(Action<NetworkManager> onReady)
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

        nmType.GetField("_networkRules", flags).SetValue(networkManager, networkRules);
        nmType.GetField("_transport", flags).SetValue(networkManager, transport);
        nmType.GetField("_visibilityRules", flags).SetValue(networkManager, visibilityRuleSet);

        networkManagerGO.SetActive(true);

        // yield return new WaitUntil(() => networkManager.isLocalPlayerReady);
        yield return new WaitForSeconds(1);

        onReady(networkManager);
    }

    public static IEnumerator SetupNetworkIdentityOnNewGameObject<T>(NetworkManager networkManager, Action<T> onReady) where T : NetworkIdentity
    {
        var mapGameObject = new GameObject(typeof(T).Name);
        var component = mapGameObject.AddComponent<T>();
        // required for directChildren to populate
        NetworkManager.SetupPrefabInfo(mapGameObject, 0, false);
        networkManager.Spawn(mapGameObject);

        // yield return new WaitUntil(() => component.isSpawned);
        yield return new WaitForSeconds(1);

        onReady(component);
    }
}
