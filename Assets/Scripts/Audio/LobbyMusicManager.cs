using System.Collections;
using UnityEngine;

public class LobbyMusicManager : MonoBehaviour
{
    [Header("Wwise Events")]
    [SerializeField] private AK.Wwise.Event lobbyMusicStartEvent;
    [SerializeField] private AK.Wwise.Event lobbyMusicStopEvent;

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(0.5f);
        lobbyMusicStartEvent.Post(gameObject);
    }

    private void OnDestroy()
    {
        lobbyMusicStopEvent.Post(gameObject);
    }
}
