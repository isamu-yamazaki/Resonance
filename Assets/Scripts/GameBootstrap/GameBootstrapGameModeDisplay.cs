using Resonance.LobbySystem;
using TMPro;
using UnityEngine;

/// <summary>
/// Script which sets the selected game mode on the text object.
/// </summary>
public class GameBootstrapGameModeDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text text;

    private LobbyDataHolder lobbyDataHolder;

    private void Awake()
    {
        lobbyDataHolder = FindFirstObjectByType<LobbyDataHolder>();
        if (lobbyDataHolder == null)
        {
            Debug.LogError($"Unable to find {nameof(LobbyDataHolder)} component");
            return;
        }

        var gameMode = lobbyDataHolder.CurrentLobby.GameMode;
        text.text = $"Selected game mode: {gameMode}";
    }

}
