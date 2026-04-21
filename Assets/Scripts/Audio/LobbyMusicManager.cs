using UnityEngine;

public class LobbyMusicManager : MonoBehaviour
{
    [Header("Wwise Events")]
    [SerializeField] private AK.Wwise.Event lobbyMusicStartEvent;
    [SerializeField] private AK.Wwise.Event lobbyMusicStopEvent;

    private void Start()
    {
        lobbyMusicStartEvent.Post(gameObject);
    }

    private void OnDestroy()
    {
        lobbyMusicStopEvent.Post(gameObject);
    }
}
