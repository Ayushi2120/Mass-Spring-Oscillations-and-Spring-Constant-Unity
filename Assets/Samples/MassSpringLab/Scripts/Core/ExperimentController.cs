using MassSpringLab.Apparatus;
using MassSpringLab.Interaction;
using MassSpringLab.UI;
using TMPro;
using UnityEngine;

namespace MassSpringLab.Core
{
    /// <summary>
    /// Orchestrates the practical Mass-Spring experiment sequence:
    /// 1. Measure L0 (no mass) -> Record -> Proceed to Load Mass
    /// 2. Grab &amp; attach mass -> Stretch &amp; oscillate -> Settle
    /// 3. Measure L -> Record &amp; display calculation -> Proceed to next mass
    /// </summary>
    public class ExperimentController : MonoBehaviour
    {
        public enum WorkflowStep
        {
            MeasureNaturalLength,
            ReadyToLoadMass,
            SelectAndAttachMass,
            SpringSettling,
            MeasureExtendedLength,
            TrialCompleted
        }

        [Header("Apparatus")]
        [SerializeField] private SpringPhysicsController springPhysics;
        [SerializeField] private SpringAttachmentPoint springSocket;

        [Header("Buttons & Displays")]
        [SerializeField] private LabActionButton measureLengthButton;
        [SerializeField] private LabActionButton proceedButton;
        [SerializeField] private TextMeshPro consoleStatusText;
        [SerializeField] private ObservationTableUI observationTable;
        [SerializeField] private CalculationDisplayUI calculationDisplay;

        private WorkflowStep _step = WorkflowStep.MeasureNaturalLength;
        private MassObject[] _allMasses;
        private float _measuredL0Cm;
        private bool _hasMeasuredL0;

        public WorkflowStep CurrentStep => _step;
        public float MeasuredL0Cm => _measuredL0Cm;
        public bool HasMeasuredL0 => _hasMeasuredL0;

        private void Awake()
        {
            if (springPhysics == null) springPhysics = FindFirstObjectByType<SpringPhysicsController>();
            if (springSocket == null) springSocket = FindFirstObjectByType<SpringAttachmentPoint>();
            if (observationTable == null) observationTable = FindFirstObjectByType<ObservationTableUI>();
            if (calculationDisplay == null) calculationDisplay = FindFirstObjectByType<CalculationDisplayUI>();
            _allMasses = FindObjectsByType<MassObject>(FindObjectsSortMode.None);
        }

        private void OnEnable()
        {
            if (measureLengthButton != null) measureLengthButton.Clicked += OnMeasureLengthClicked;
            if (proceedButton != null) proceedButton.Clicked += OnProceedClicked;
            if (springPhysics != null) springPhysics.OnStabilityChanged += OnSpringStabilityChanged;
            if (springSocket != null) springSocket.OnMassAttachedEvent += OnMassAttachedToSpring;
        }

        private void OnDisable()
        {
            if (measureLengthButton != null) measureLengthButton.Clicked -= OnMeasureLengthClicked;
            if (proceedButton != null) proceedButton.Clicked -= OnProceedClicked;
            if (springPhysics != null) springPhysics.OnStabilityChanged -= OnSpringStabilityChanged;
            if (springSocket != null) springSocket.OnMassAttachedEvent -= OnMassAttachedToSpring;
        }

        private void Start()
        {
            EnterStep(WorkflowStep.MeasureNaturalLength);
        }

        public void OnMeasureLengthClicked()
        {
            if (_step == WorkflowStep.MeasureNaturalLength)
            {
                _measuredL0Cm = springPhysics.NaturalLengthMeters * 100f;
                _hasMeasuredL0 = true;
                observationTable?.SetBaselineL0ForAllPending(_measuredL0Cm);
                calculationDisplay?.ShowIdle($"Natural Length L0 = {_measuredL0Cm:F2} cm recorded.\nClick 'PROCEED TO LOAD MASS'.");
                EnterStep(WorkflowStep.ReadyToLoadMass);
                Debug.Log($"[Experiment] Measured natural length L0 = {_measuredL0Cm:F2} cm.");
            }
            else if (_step == WorkflowStep.MeasureExtendedLength && springPhysics != null && springPhysics.IsStable)
            {
                MassObject mass = springSocket != null ? springSocket.AttachedMass : null;
                if (mass == null) return;

                float lCm = springPhysics.CurrentLengthMeters * 100f;
                float xCm = lCm - _measuredL0Cm;
                float xMeters = xCm / 100f;
                float forceN = mass.MassKg * springPhysics.Gravity;
                float kNm = xMeters > 0.0001f ? forceN / xMeters : 0f;

                observationTable?.RecordObservation(mass.MassGrams, _measuredL0Cm, lCm, xCm, kNm);
                calculationDisplay?.ShowCalculation(mass.MassGrams, _measuredL0Cm, lCm, xCm, forceN, kNm);
                EnterStep(WorkflowStep.TrialCompleted);
                Debug.Log($"[Experiment] Recorded {mass.MassGrams}g: L0={_measuredL0Cm:F2}cm, L={lCm:F2}cm, x={xCm:F2}cm, k={kNm:F2}N/m.");
            }
        }

        public void OnProceedClicked()
        {
            if (_step == WorkflowStep.ReadyToLoadMass)
            {
                EnterStep(WorkflowStep.SelectAndAttachMass);
                calculationDisplay?.ShowIdle("Grab any mass (50g - 250g) and\nattach its top hook to the spring.");
            }
            else if (_step == WorkflowStep.TrialCompleted)
            {
                if (springSocket != null)
                {
                    springSocket.DetachCurrentMass();
                }
                calculationDisplay?.ShowIdle("Select and attach the next mass\n(50g - 250g) to the spring.");
                EnterStep(WorkflowStep.SelectAndAttachMass);
            }
        }

        private void OnMassAttachedToSpring(MassObject mass)
        {
            if (_step == WorkflowStep.SelectAndAttachMass)
            {
                EnterStep(WorkflowStep.SpringSettling);
            }
        }

        private void OnSpringStabilityChanged(SpringPhysicsController.StabilityState state)
        {
            if (_step == WorkflowStep.SpringSettling && state == SpringPhysicsController.StabilityState.Stable)
            {
                EnterStep(WorkflowStep.MeasureExtendedLength);
            }
        }

        private void EnterStep(WorkflowStep next)
        {
            _step = next;
            bool allowGrab = (next == WorkflowStep.SelectAndAttachMass);
            SetMassesGrabbable(allowGrab);

            switch (next)
            {
                case WorkflowStep.MeasureNaturalLength:
                    measureLengthButton?.SetState(true, "MEASURE\nL0");
                    proceedButton?.SetState(false, "PROCEED");
                    SetConsoleStatus("STEP 1: Measure natural length L0\n(No mass attached)");
                    calculationDisplay?.ShowIdle("Step 1: Click MEASURE L0\nto record unloaded spring length.");
                    break;

                case WorkflowStep.ReadyToLoadMass:
                    measureLengthButton?.SetState(false, "L0 SAVED");
                    proceedButton?.SetState(true, "PROCEED TO\nLOAD MASS");
                    SetConsoleStatus($"L0 = {_measuredL0Cm:F2} cm saved.\nClick PROCEED TO LOAD MASS.");
                    break;

                case WorkflowStep.SelectAndAttachMass:
                    measureLengthButton?.SetState(false, "MEASURE\nLENGTH");
                    proceedButton?.SetState(false, "PROCEED");
                    SetConsoleStatus("Attach any mass (50g - 250g)\nto the spring bottom hook.");
                    break;

                case WorkflowStep.SpringSettling:
                    measureLengthButton?.SetState(false, "SETTLING...");
                    proceedButton?.SetState(false, "PROCEED");
                    SetConsoleStatus("Spring stretching & oscillating...\nWait until Spring Stable.");
                    break;

                case WorkflowStep.MeasureExtendedLength:
                    measureLengthButton?.SetState(true, "MEASURE\nLENGTH");
                    proceedButton?.SetState(false, "PROCEED");
                    SetConsoleStatus("Spring Stable!\nClick MEASURE LENGTH to record L.");
                    break;

                case WorkflowStep.TrialCompleted:
                    measureLengthButton?.SetState(false, "RECORDED");
                    proceedButton?.SetState(true, "PROCEED");
                    SetConsoleStatus("Observation recorded!\nClick PROCEED for next mass.");
                    break;
            }
        }

        private void SetMassesGrabbable(bool grabbable)
        {
            if (_allMasses == null) return;
            foreach (MassObject m in _allMasses)
            {
                if (m != null && !m.IsAttached)
                {
                    m.SetGrabbable(grabbable);
                }
            }
        }

        private void SetConsoleStatus(string text)
        {
            if (consoleStatusText != null)
            {
                consoleStatusText.text = text;
            }
        }
    }
}
