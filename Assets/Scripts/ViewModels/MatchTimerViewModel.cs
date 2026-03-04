using UnityEngine;
using Resonance.Helper;

public class MatchTimerViewModel : MonoBehaviour
{
    // Raw seconds remaining
    public ObservableValue<double> SecondsRemaining { get; private set; }

    // Optional: formatted string for UI binding
    public ObservableValue<string> FormattedTime { get; private set; }

    private void Awake()
    {
        SecondsRemaining = new ObservableValue<double>(0);
        FormattedTime = new ObservableValue<string>("00:00");
    }

    public void SetTime(double seconds)
    {
        SecondsRemaining.Value = seconds;
        FormattedTime.Value = FormatTime(seconds);
    }

    private string FormatTime(double time)
    {
        int minutes = Mathf.FloorToInt((float)time / 60f);
        int seconds = Mathf.FloorToInt((float)time % 60f);
        return $"{minutes:00}:{seconds:00}";
    }
}