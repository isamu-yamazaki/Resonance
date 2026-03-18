namespace Resonance.Train
{
    public enum TrainState
    {
        StoppedAtStation,
        Accelerating,
        Cruising,
        Braking
    }

    public enum TrainDirection
    {
        Forward = 1,
        Backward = -1
    }
}