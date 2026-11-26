using System;
using MassSpringLab.UI;
using UnityEngine;

namespace MassSpringLab.Apparatus
{
    /// <summary>
    /// Handles pull-and-release vertical oscillation after Extended Length is measured,
    /// counting 10 complete oscillation cycles via equilibrium crossings with hysteresis
    /// and automatically driving StopwatchController.
    /// </summary>
    public class SpringOscillationController : MonoBehaviour
    {
        [SerializeField] private SpringPhysicsController springPhysics;
        [SerializeField] private SpringAttachmentPoint springSocket;
        [SerializeField] private SpringLengthMeasurementController measurementController;
        [SerializeField] private StopwatchController stopwatch;
        [SerializeField] private int targetOscillations = 10;
        [SerializeField] private float minCycleIntervalSeconds = 0.12f;
        [SerializeField] private float minAmplitudeMeters = 0.002f;

        private bool _isOscillatingForTiming;
        private int _completedCycles;
        private float _prevDisplacement;
        private float _maxAbsDispInCycle;
        private float _lastCycleTime;
        private bool _hasPrevSample;
        private bool _completedThisTrial;
        private float _currentPullDisplacement;

        public bool IsOscillatingForTiming => _isOscillatingForTiming;
        public int CompletedCycles => _completedCycles;
        public bool CanPullAndOscillate
        {
            get
            {
                if (_isOscillatingForTiming || _completedThisTrial) return false;
                if (springSocket == null || !springSocket.HasAttachedMass) return false;
                if (springPhysics == null || !springPhysics.IsStable) return false;
                if (measurementController == null) return false;
                return measurementController.TryGetRecordedExtendedLength(springSocket.AttachedMass.MassGrams, out _);
            }
        }

        public event Action<MassObject> OnOscillationStarted;

        private void Awake()
        {
            if (springPhysics == null) springPhysics = FindFirstObjectByType<SpringPhysicsController>();
            if (springSocket == null) springSocket = FindFirstObjectByType<SpringAttachmentPoint>();
            if (measurementController == null) measurementController = FindFirstObjectByType<SpringLengthMeasurementController>();
            if (stopwatch == null) stopwatch = FindFirstObjectByType<StopwatchController>();
        }

        private void FixedUpdate()
        {
            if (!_isOscillatingForTiming || springPhysics == null) return;

            float disp = springPhysics.CurrentExtensionMeters - springPhysics.TargetExtensionMeters;
            _maxAbsDispInCycle = Mathf.Max(_maxAbsDispInCycle, Mathf.Abs(disp));

            if (_hasPrevSample)
            {
                float now = Time.time;
                // Count complete oscillation cycles on downward crossing through equilibrium with hysteresis
                if (_prevDisplacement <= 0f && disp > 0f &&
                    (now - _lastCycleTime) >= minCycleIntervalSeconds &&
                    _maxAbsDispInCycle >= minAmplitudeMeters)
                {
                    _completedCycles++;
                    _lastCycleTime = now;
                    _maxAbsDispInCycle = 0f;
                    stopwatch?.ReportOscillationCount(_completedCycles);

                    if (_completedCycles >= targetOscillations)
                    {
                        _isOscillatingForTiming = false;
                        _completedThisTrial = true;
                    }
                }
            }

            _prevDisplacement = disp;
            _hasPrevSample = true;
        }

        public void ResetTrialFlag()
        {
            _isOscillatingForTiming = false;
            _completedThisTrial = false;
            _completedCycles = 0;
            _hasPrevSample = false;
            _currentPullDisplacement = 0f;
        }

        public void BeginManualPull(float pullDisplacementMeters)
        {
            if (!CanPullAndOscillate || springPhysics == null) return;
            _currentPullDisplacement = Mathf.Clamp(pullDisplacementMeters, 0.005f, 0.06f);
            springPhysics.SetManualPullDisplacement(_currentPullDisplacement);
            stopwatch?.ShowWaitingForRelease();
        }

        public void ReleaseToOscillate(float defaultPullMeters = 0.038f)
        {
            if (!CanPullAndOscillate || springPhysics == null) return;

            float releasePull = _currentPullDisplacement >= 0.008f ? _currentPullDisplacement : defaultPullMeters;
            _currentPullDisplacement = 0f;
            _completedCycles = 0;
            _maxAbsDispInCycle = releasePull;
            _lastCycleTime = Time.time;
            _hasPrevSample = false;
            _isOscillatingForTiming = true;

            springPhysics.ReleaseManualOscillation(releasePull);
            stopwatch?.StartTiming();
            OnOscillationStarted?.Invoke(springSocket.AttachedMass);
            Debug.Log($"[Oscillation] Started 10-oscillation timing for {springSocket.AttachedMass.MassGrams}g.");
        }
    }
}
