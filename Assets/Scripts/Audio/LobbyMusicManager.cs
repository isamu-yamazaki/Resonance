using System.Collections;
using UnityEngine;

public class LobbyMusicManager : MonoBehaviour
{
    [Header("Wwise Events")]
    [SerializeField] private AK.Wwise.Event lobbyMusicStartEvent;
    [SerializeField] private AK.Wwise.Event lobbyMusicStopEvent;

    private void Start()
    {
        StartCoroutine(PostMusicDelayed());
    }

    private IEnumerator PostMusicDelayed()
    {
        yield return null; // wait one frame for Wwise to finish initializing
        lobbyMusicStartEvent.Post(gameObject);
    }

    private void OnDestroy()
    {
        lobbyMusicStopEvent.Post(gameObject);
    }
}
