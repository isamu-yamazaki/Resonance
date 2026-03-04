using Resonance.LobbySystem;
using TMPro;
using UnityEngine;

/// <summary>
/// Script which sets the owner state on the object.
/// </summary>
public class GameBootstrapOwnerStateDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text text;

    private LobbyDataHolder lobbyDataHolder;

    private void Awake()
    {
        lobbyDataHolder = FindFirstObjectByType<LobbyDataHolder>();
        if (!lobbyDataHolder)
        {
            Debug.LogError($"Unable to find {nameof(LobbyDataHolder)} component");
        }

        var gameMode = lobbyDataHolder.CurrentLobby.IsOwner;
        text.text = $"Is owner: {gameMode}";
    }

}
