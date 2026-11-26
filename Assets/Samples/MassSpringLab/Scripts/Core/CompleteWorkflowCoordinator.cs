using MassSpringLab.Apparatus;
using MassSpringLab.Interaction;
using MassSpringLab.Robot;
using MassSpringLab.UI;
using TMPro;
using UnityEngine;

namespace MassSpringLab.Core
{
    /// <summary>
    /// Coordinates calculation presentation, spotlight dimming/restoration,
    /// automatic observation recording, delayed Proceed button reveal, and
    /// ATOM-8 next-mass guidance (or final Experiment Completed state after 250g).
    /// </summary>
    public class CompleteWorkflowCoordinator : MonoBehaviour
    {
        [Header("Existing Apparatus & Measurement")]
        [SerializeField] private SpringAttachmentPoint springSocket;
        [SerializeField] private SpringPhysicsController springPhysics;
        [SerializeField] private SpringLengthMeasurementController measurementController;
        [SerializeField] private SpringOscillationController oscillationController;

        [Header("UI, Lighting & Guidance")]
        [SerializeField] private StopwatchController stopwatch;
        [SerializeField] private ObservationTableUI observationTable;
        [SerializeField] private CalculationDisplayUI calculationDisplay;
        [SerializeField] private CalculationSpotlightController calculationSpotlight;
        [SerializeField] private Atom8GuidanceController atom8Guidance;

        [Header("Console Controls")]
        [SerializeField] private LabActionButton pullOscillateButton;
        [SerializeField] private LabActionButton proceedButton;
        [SerializeField] private TextMeshPro consoleStatusText;

        private bool _trialReadyToProceed;
        private int _pendingCompletedMassGrams = -1;

        public bool IsExperimentCompleted { get; private set; }

        private void Awake()
        {
            if (springSocket == null) springSocket = FindFirstObjectByType<SpringAttachmentPoint>();
            if (springPhysics == null) springPhysics = FindFirstObjectByType<SpringPhysicsController>();
            if (measurementController == null) measurementController = FindFirstObjectByType<SpringLengthMeasurementController>();
            if (oscillationController == null) oscillationController = FindFirstObjectByType<SpringOscillationController>();
            if (stopwatch == null) stopwatch = FindFirstObjectByType<StopwatchController>();
            if (observationTable == null) observationTable = FindFirstObjectByType<ObservationTableUI>();
            if (calculationDisplay == null) calculationDisplay = FindFirstObjectByType<CalculationDisplayUI>();
            if (calculationSpotlight == null) calculationSpotlight = FindFirstObjectByType<CalculationSpotlightController>();
            if (atom8Guidance == null) atom8Guidance = FindFirstObjectByType<Atom8GuidanceController>();
        }

        private void Start()
        {
            SetProceedVisible(false);
        }

        private void OnEnable()
        {
            if (stopwatch != null) stopwatch.OnTimingCompleted += HandleTimingCompleted;
            if (calculationSpotlight != null) calculationSpotlight.OnPresentationCompleted += HandlePresentationCompleted;
            if (pullOscillateButton != null) pullOscillateButton.Clicked += HandlePullOscillateClicked;
            if (proceedButton != null) proceedButton.Clicked += HandleProceedClicked;
        }

        private void OnDisable()
        {
            if (stopwatch != null) stopwatch.OnTimingCompleted -= HandleTimingCompleted;
            if (calculationSpotlight != null) calculationSpotlight.OnPresentationCompleted -= HandlePresentationCompleted;
            if (pullOscillateButton != null) pullOscillateButton.Clicked -= HandlePullOscillateClicked;
            if (proceedButton != null) proceedButton.Clicked -= HandleProceedClicked;
        }

        private void Update()
        {
            if (pullOscillateButton != null)
            {
                bool canPull = oscillationController != null && oscillationController.CanPullAndOscillate;
                pullOscillateButton.SetState(canPull, "PULL &\nOSCILLATE");
            }
        }

        public void HandlePullOscillateClicked()
        {
            if (oscillationController != null && oscillationController.CanPullAndOscillate)
            {
                oscillationController.ReleaseToOscillate(0.038f);
                if (consoleStatusText != null)
                {
                    consoleStatusText.text = "Oscillating...\nCounting 10 cycles";
                }
            }
        }

        public void HandleProceedClicked()
        {
            if (!_trialReadyToProceed) return;

            _trialReadyToProceed = false;
            SetProceedVisible(false);

            oscillationController?.ResetTrialFlag();
            stopwatch?.ResetDisplay();
            springSocket?.DetachCurrentMass();

            if (_pendingCompletedMassGrams > 0)
            {
                atom8Guidance?.MarkMassCompletedAndGuideNext(_pendingCompletedMassGrams);
                _pendingCompletedMassGrams = -1;
            }

            if (atom8Guidance != null && atom8Guidance.NextRecommendedMassGrams > 0)
            {
                if (consoleStatusText != null)
                {
                    consoleStatusText.text = $"Pick up {atom8Guidance.NextRecommendedMassGrams}g\nand attach it to the spring.";
                }
            }
        }

        private void HandleTimingCompleted(int oscillations, float totalTimeSec, float periodSec)
        {
            MassObject mass = springSocket != null ? springSocket.AttachedMass : null;
            if (mass == null || measurementController == null) return;

            float l0Cm = measurementController.HasNaturalLength
                ? measurementController.NaturalLengthCm
                : (springPhysics != null ? springPhysics.NaturalLengthMeters * 100f : 12.0f);

            if (!measurementController.TryGetRecordedExtendedLength(mass.MassGrams, out float lCm))
            {
                lCm = measurementController.MeasureCurrentSpringLengthCm();
            }

            _pendingCompletedMassGrams = mass.MassGrams;
            _trialReadyToProceed = false;
            SetProceedVisible(false);

            // 1. Automatically record observation into existing whiteboard table
            observationTable?.RecordTimingObservation(mass.MassGrams, oscillations, totalTimeSec, periodSec);

            // 2. Display full calculated values on existing Calculations panel
            calculationDisplay?.ShowFullCalculations(mass.MassGrams, l0Cm, lCm, oscillations, totalTimeSec);

            if (consoleStatusText != null)
            {
                consoleStatusText.text = $"Review {mass.MassGrams}g calculations\non the Calculations Board.";
            }

            // 3. Activate spotlight & dim lab smoothly; Proceed/Completion appears after presentation
            if (calculationSpotlight != null)
            {
                calculationSpotlight.TriggerSpotlight();
            }
            else
            {
                HandlePresentationCompleted();
            }

            Debug.Log($"[Workflow] Completed {mass.MassGrams}g: N={oscillations}, t={totalTimeSec:F2}s, T={periodSec:F3}s.");
        }

        private void HandlePresentationCompleted()
        {
            if (_pendingCompletedMassGrams == 250)
            {
                // Final 250g mass: do NOT show Proceed to another mass; show Experiment Completed
                atom8Guidance?.MarkMassCompletedAndGuideNext(250);
                _pendingCompletedMassGrams = -1;
                _trialReadyToProceed = false;
                SetProceedVisible(false);
                IsExperimentCompleted = true;

                if (consoleStatusText != null)
                {
                    consoleStatusText.text = "EXPERIMENT COMPLETED!\nAll 5 masses (50g-250g) recorded.";
                }
                return;
            }

            _trialReadyToProceed = true;
            SetProceedVisible(true);

            if (consoleStatusText != null)
            {
                consoleStatusText.text = "Observation recorded.\nClick PROCEED to continue.";
            }
        }

        private void SetProceedVisible(bool visible)
        {
            if (proceedButton == null) return;
            proceedButton.gameObject.SetActive(visible);
            proceedButton.SetState(visible, "PROCEED");
        }
    }
}
