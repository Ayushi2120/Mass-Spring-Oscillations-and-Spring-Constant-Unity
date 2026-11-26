using MassSpringLab.Apparatus;
using MassSpringLab.Interaction;
using MassSpringLab.Robot;
using MassSpringLab.UI;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

namespace MassSpringLab.Core
{
    /// <summary>
    /// Connects experiment state transitions to ATOM-8 instructor voice narration, subtitles,
    /// facing the student when speaking, turning &amp; pointing toward exact targets,
    /// VR Controller vs. Hand-Tracking mode detection, and Replay instruction button.
    /// </summary>
    public class VoiceGuidanceCoordinator : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrapVoiceGuidance()
        {
            if (FindFirstObjectByType<VoiceGuidanceCoordinator>() != null) return;
            GameObject atom8Go = GameObject.Find("ATOM-8");
            if (atom8Go == null) return;

            // Position ATOM-8 beside left-front of workstation facing student
            atom8Go.transform.position = new Vector3(-1.24f, 0f, 0.82f);

            Atom8VoiceNarrator narrator = atom8Go.GetComponent<Atom8VoiceNarrator>();
            if (narrator == null)
            {
                narrator = atom8Go.AddComponent<Atom8VoiceNarrator>();
                CreateRuntimeSubtitlePanel(atom8Go.transform, narrator);
            }

            GameObject vgcGo = new GameObject("VoiceGuidanceCoordinator");
            VoiceGuidanceCoordinator vgc = vgcGo.AddComponent<VoiceGuidanceCoordinator>();
            vgc.narrator = narrator;
            vgc.atom8 = atom8Go.GetComponent<Atom8GuidanceController>();
            vgc.workstationTarget = GameObject.Find("LabTable")?.transform;
            vgc.rulerTarget = GameObject.Find("MeasurementRuler")?.transform;
            vgc.measureButtonTarget = GameObject.Find("MeasureButtonStation/MeasureLengthButton")?.transform;
            vgc.springHookTarget = GameObject.Find("SpringApparatus/SpringBottomHook")?.transform;
            vgc.stopwatchTarget = GameObject.Find("StopwatchDisplayStation")?.transform;
            vgc.calculationsTarget = GameObject.Find("CalculationPanelStation")?.transform;
            vgc.observationTableTarget = GameObject.Find("WallTheoryBoard")?.transform;
            vgc.proceedButtonTarget = GameObject.Find("ObservationConsoleStation")?.transform.Find("ProceedButton");
        }

        private static void CreateRuntimeSubtitlePanel(Transform atom8Transform, Atom8VoiceNarrator narrator)
        {
            if (atom8Transform.Find("Atom8SubtitlePanel") != null) return;

            GameObject subRoot = new GameObject("Atom8SubtitlePanel");
            subRoot.transform.SetParent(atom8Transform, false);
            subRoot.transform.localPosition = new Vector3(0f, 1.66f, -0.05f);

            GameObject textGo = new GameObject("SubtitleText");
            textGo.transform.SetParent(subRoot.transform, false);
            textGo.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            TextMeshPro subTmp = textGo.AddComponent<TextMeshPro>();
            subTmp.rectTransform.sizeDelta = new Vector2(0.85f, 0.22f);
            subTmp.fontSize = 0.24f;
            subTmp.color = Color.white;
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.textWrappingMode = TextWrappingModes.Normal;

            narrator.ConfigureSubtitleUI(subTmp, subRoot);
        }

        [Header("Controllers")]
        [SerializeField] private Atom8VoiceNarrator narrator;
        [SerializeField] private Atom8GuidanceController atom8;
        [SerializeField] private SpringLengthMeasurementController measurementController;
        [SerializeField] private SpringAttachmentPoint springSocket;
        [SerializeField] private SpringPhysicsController springPhysics;
        [SerializeField] private SpringOscillationController oscillationController;
        [SerializeField] private StopwatchController stopwatch;
        [SerializeField] private CalculationSpotlightController spotlight;
        [SerializeField] private XRInputModalityManager modalityManager;
        [SerializeField] private LabActionButton replayButton;

        [Header("Pointing Targets")]
        [SerializeField] private Transform workstationTarget;
        [SerializeField] private Transform rulerTarget;
        [SerializeField] private Transform measureButtonTarget;
        [SerializeField] private Transform springHookTarget;
        [SerializeField] private Transform stopwatchTarget;
        [SerializeField] private Transform calculationsTarget;
        [SerializeField] private Transform observationTableTarget;
        [SerializeField] private Transform proceedButtonTarget;

        private bool _wasNaturalMeasured;
        private bool _wasMassAttached;
        private bool _wasExtendedMeasured;
        private int _currentMassGrams = -1;
        private bool _overrideHandTrackingForTest;
        private bool _hasOverride;

        public bool IsHandTrackingMode
        {
            get
            {
                if (_hasOverride) return _overrideHandTrackingForTest;
                if (modalityManager != null)
                {
                    return XRInputModalityManager.currentInputMode.Value == XRInputModalityManager.InputMode.TrackedHand;
                }
                return false;
            }
        }

        private void Awake()
        {
            if (narrator == null) narrator = FindFirstObjectByType<Atom8VoiceNarrator>();
            if (atom8 == null) atom8 = FindFirstObjectByType<Atom8GuidanceController>();
            if (measurementController == null) measurementController = FindFirstObjectByType<SpringLengthMeasurementController>();
            if (springSocket == null) springSocket = FindFirstObjectByType<SpringAttachmentPoint>();
            if (springPhysics == null) springPhysics = FindFirstObjectByType<SpringPhysicsController>();
            if (oscillationController == null) oscillationController = FindFirstObjectByType<SpringOscillationController>();
            if (stopwatch == null) stopwatch = FindFirstObjectByType<StopwatchController>();
            if (spotlight == null) spotlight = FindFirstObjectByType<CalculationSpotlightController>();
            if (modalityManager == null) modalityManager = FindFirstObjectByType<XRInputModalityManager>();
        }

        private void OnEnable()
        {
            if (oscillationController != null) oscillationController.OnOscillationStarted += HandleOscillationStarted;
            if (stopwatch != null) stopwatch.OnTimingCompleted += HandleTimingCompleted;
            if (spotlight != null) spotlight.OnPresentationCompleted += HandlePresentationCompleted;
            if (replayButton != null) replayButton.Clicked += HandleReplayClicked;
        }

        private void OnDisable()
        {
            if (oscillationController != null) oscillationController.OnOscillationStarted -= HandleOscillationStarted;
            if (stopwatch != null) stopwatch.OnTimingCompleted -= HandleTimingCompleted;
            if (spotlight != null) spotlight.OnPresentationCompleted -= HandlePresentationCompleted;
            if (replayButton != null) replayButton.Clicked -= HandleReplayClicked;
        }

        private void Start()
        {
            if (replayButton != null) replayButton.SetState(true, "REPLAY\nVOICE");

            // 1. Welcome facing student -> Natural Length (face student -> point ruler -> return to student)
            atom8?.FaceStudent();
            narrator?.PlayStateCue("welcome", "01_welcome", true, () =>
            {
                if (_wasNaturalMeasured) return;
                atom8?.FaceStudent();
                narrator?.PlayStateCue("natural_prompt", "02a_natural_length", false, () =>
                {
                    if (_wasNaturalMeasured) return;
                    atom8?.PointAtTarget(rulerTarget);
                    narrator?.EnqueueCue("02a_look_ruler", () => atom8?.FaceStudent());
                });
            });
        }

        private void Update()
        {
            // Transition: Natural Length recorded -> Pick Mass -> Point to attachment point -> Face student
            if (!_wasNaturalMeasured && measurementController != null && measurementController.HasNaturalLength)
            {
                _wasNaturalMeasured = true;
                atom8?.FaceStudent();
                narrator?.PlayStateCue("natural_done", "02b_natural_recorded", true, () =>
                {
                    int firstMass = atom8 != null ? atom8.NextRecommendedMassGrams : 50;
                    atom8?.PointAtMass(firstMass);
                    narrator?.PlayStateCue($"pick_{firstMass}", $"03_pick_{firstMass}g", false, () =>
                    {
                        if (springSocket != null && springSocket.HasAttachedMass) return;
                        atom8?.PointAtTarget(springHookTarget);
                        narrator?.EnqueueCue("03b_attach_point", () => atom8?.FaceStudent());
                    });
                });
            }

            // Transition: Mass attached & settled -> Extended Length (point ruler -> point Measure button -> face student)
            bool hasMass = springSocket != null && springSocket.HasAttachedMass;
            if (hasMass && springPhysics != null && springPhysics.IsStable && !_wasMassAttached)
            {
                _wasMassAttached = true;
                _wasExtendedMeasured = false;
                _currentMassGrams = springSocket.AttachedMass.MassGrams;
                atom8?.PointAtTarget(rulerTarget);
                narrator?.PlayStateCue($"ext_prompt_{_currentMassGrams}", "04a_extended_length", true, () =>
                {
                    if (_wasExtendedMeasured) return;
                    atom8?.PointAtTarget(measureButtonTarget);
                    narrator?.EnqueueCue("04a_click_measure", () => atom8?.FaceStudent());
                });
            }
            else if (!hasMass && _wasMassAttached)
            {
                // Proceed / Detached -> guide next mass
                _wasMassAttached = false;
                _wasExtendedMeasured = false;
                int nextMass = atom8 != null ? atom8.NextRecommendedMassGrams : -1;
                if (nextMass > 0)
                {
                    atom8?.PointAtMass(nextMass);
                    narrator?.PlayStateCue($"next_{nextMass}", $"09_next_{nextMass}g", true, () =>
                    {
                        if (springSocket != null && springSocket.HasAttachedMass) return;
                        atom8?.PointAtTarget(springHookTarget);
                        Invoke(nameof(ReturnRobotToStudentIfUnattached), 1.5f);
                    });
                }
            }

            // Transition: Extended Length measured -> Start Oscillation (Controller vs Hand Tracking)
            if (_wasMassAttached && !_wasExtendedMeasured && measurementController != null &&
                measurementController.TryGetRecordedExtendedLength(_currentMassGrams, out _))
            {
                _wasExtendedMeasured = true;
                atom8?.FaceStudent();
                string modeCue = IsHandTrackingMode ? "05b_osc_hands" : "05a_osc_controller";
                narrator?.PlayStateCue($"ext_done_{_currentMassGrams}", "04b_extension_recorded", true, () =>
                {
                    atom8?.PointAtTarget(springHookTarget, true);
                    narrator?.PlayStateCue($"osc_prompt_{_currentMassGrams}_{modeCue}", modeCue, false, () => atom8?.FaceStudent());
                });
            }
        }

        public void SetInteractionModeOverrideForTest(bool handTracking)
        {
            _hasOverride = true;
            _overrideHandTrackingForTest = handTracking;
        }

        private void ReturnRobotToStudentIfUnattached()
        {
            if (springSocket != null && !springSocket.HasAttachedMass)
            {
                atom8?.FaceStudent();
            }
        }

        private void HandleOscillationStarted(MassObject mass)
        {
            atom8?.PointAtTarget(springHookTarget);
            narrator?.PlayStateCue($"osc_run_{mass.MassGrams}", "06a_oscillating", true, () =>
            {
                atom8?.PointAtTarget(stopwatchTarget);
            });
        }

        private void HandleTimingCompleted(int oscillations, float totalTimeSec, float periodSec)
        {
            int g = _currentMassGrams > 0 ? _currentMassGrams : 50;
            atom8?.FaceStudent();
            narrator?.PlayStateCue($"osc_done_{g}", "06b_osc_done", true, () =>
            {
                atom8?.PointAtTarget(calculationsTarget);
                narrator?.EnqueueCue("07_calculations", () =>
                {
                    atom8?.PointAtTarget(observationTableTarget);
                    narrator?.EnqueueCue("08_observation_table", () => atom8?.FaceStudent());
                });
            });
        }

        private void HandlePresentationCompleted()
        {
            if (atom8 != null && atom8.AllMassesCompleted)
            {
                atom8.FaceStudent();
                narrator?.PlayStateCue("exp_complete", "10_complete", true, () =>
                {
                    atom8?.PointAtTarget(observationTableTarget);
                    Invoke(nameof(FaceStudentAfterCompletion), 2.5f);
                });
            }
            else
            {
                atom8?.PointAtTarget(proceedButtonTarget);
                narrator?.PlayStateCue($"ready_proceed_{_currentMassGrams}", "09_ready_proceed", true);
            }
        }

        private void FaceStudentAfterCompletion()
        {
            atom8?.FaceStudent();
        }

        private void HandleReplayClicked()
        {
            narrator?.ReplayCurrentInstruction();
        }
    }
}
