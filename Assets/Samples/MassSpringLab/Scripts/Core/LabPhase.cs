namespace MassSpringLab.Core
{
    /// <summary>
    /// Represents the sequential stages of a single Hooke's Law &amp; oscillation trial.
    /// </summary>
    public enum LabPhase
    {
        BaselineMeasurement,
        SelectMass,
        MassAttached,
        StretchedMeasurement,
        Oscillation,
        Timing,
        Calculation,
        ObservationRecorded,
        ReadyToProceed
    }
}
