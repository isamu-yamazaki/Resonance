namespace Resonance.Assemblies.Train
{
    public enum TrainMovementState
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