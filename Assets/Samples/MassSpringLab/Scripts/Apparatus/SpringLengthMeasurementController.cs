using System.Collections.Generic;
using MassSpringLab.Interaction;
using MassSpringLab.UI;
using TMPro;
using UnityEngine;

namespace MassSpringLab.Apparatus
{
    /// <summary>
    /// Measures actual spring length from SpringTopAnchor and SpringBottomHook transforms
    /// using the existing MeasureLengthButton, without modifying grabbing, attachment, or physics.
    /// </summary>
    public class SpringLengthMeasurementController : MonoBehaviour
    {
        [Header("Spring Endpoints")]
        [SerializeField] private Transform springTopAnchor;
        [SerializeField] private Transform springBottomHook;

        [Header("Existing Apparatus References")]
        [SerializeField] private SpringAttachmentPoint springSocket;
        [SerializeField] private SpringPhysicsController springPhysics;

        [Header("Existing UI References")]
        [SerializeField] private LabActionButton measureLengthButton;
        [SerializeField] private TextMeshPro statusDisplayText;
        [SerializeField] private ObservationTableUI observationTable;

        private bool _hasNaturalLength;
        private float _naturalLengthCm;
        private float _lastExtendedLengthCm;
        private readonly Dictionary<int, float> _recordedExtendedLengthsCm = new Dictionary<int, float>();

        public bool HasNaturalLength => _hasNaturalLength;
        public float NaturalLengthCm => _naturalLengthCm;
        public float LastExtendedLengthCm => _lastExtendedLengthCm;

        public bool TryGetRecordedExtendedLength(int massGrams, out float lengthCm)
        {
            return _recordedExtendedLengthsCm.TryGetValue(massGrams, out lengthCm);
        }

        private void Awake()
        {
            if (springTopAnchor == null)
            {
                GameObject topGo = GameObject.Find("SpringTopAnchor");
                if (topGo != null) springTopAnchor = topGo.transform;
            }
            if (springBottomHook == null)
            {
                GameObject botGo = GameObject.Find("SpringBottomHook");
                if (botGo != null) springBottomHook = botGo.transform;
            }
            if (springSocket == null) springSocket = FindFirstObjectByType<SpringAttachmentPoint>();
            if (springPhysics == null) springPhysics = FindFirstObjectByType<SpringPhysicsController>();
            if (observationTable == null) observationTable = FindFirstObjectByType<ObservationTableUI>();
        }

        private void OnEnable()
        {
            if (measureLengthButton != null)
            {
                measureLengthButton.Clicked += HandleMeasureButtonClicked;
            }
        }

        private void OnDisable()
        {
            if (measureLengthButton != null)
            {
                measureLengthButton.Clicked -= HandleMeasureButtonClicked;
            }
        }

        private void Start()
        {
            RefreshButtonAndPrompt();
        }

        private void Update()
        {
            RefreshButtonAndPrompt();
        }

        public float MeasureCurrentSpringLengthCm()
        {
            if (springTopAnchor == null || springBottomHook == null) return 0f;
            return Vector3.Distance(springTopAnchor.position, springBottomHook.position) * 100f;
        }

        public void HandleMeasureButtonClicked()
        {
            if (springTopAnchor == null || springBottomHook == null) return;

            bool hasMass = springSocket != null && springSocket.HasAttachedMass;
            float currentLengthCm = MeasureCurrentSpringLengthCm();

            if (!hasMass)
            {
                _naturalLengthCm = currentLengthCm;
                _hasNaturalLength = true;
                observationTable?.SetBaselineL0ForAllPending(_naturalLengthCm);
                SetStatus($"Natural Length L0:\n{_naturalLengthCm:F2} cm\n(Attach a mass next)");
                Debug.Log($"[Measurement] Natural Length L0 = {_naturalLengthCm:F2} cm (from endpoints).");
                return;
            }

            if (springPhysics != null && springPhysics.IsSettling)
            {
                SetStatus("Spring Settling...\nWait before measuring L.");
                return;
            }

            MassObject attached = springSocket.AttachedMass;
            if (attached == null) return;

            _lastExtendedLengthCm = currentLengthCm;
            _recordedExtendedLengthsCm[attached.MassGrams] = _lastExtendedLengthCm;

            float l0Display = _hasNaturalLength ? _naturalLengthCm : (springPhysics != null ? springPhysics.NaturalLengthMeters * 100f : 0f);
            observationTable?.RecordLengthObservation(attached.MassGrams, l0Display, _lastExtendedLengthCm);
            SetStatus($"{attached.MassGrams}g Extended Length:\nL = {_lastExtendedLengthCm:F2} cm\n(L0 = {_naturalLengthCm:F2} cm)");
            Debug.Log($"[Measurement] {attached.MassGrams}g Extended Length L = {_lastExtendedLengthCm:F2} cm (L0 preserved = {_naturalLengthCm:F2} cm).");
        }

        private void RefreshButtonAndPrompt()
        {
            if (measureLengthButton == null) return;

            bool hasMass = springSocket != null && springSocket.HasAttachedMass;
            if (!hasMass)
            {
                string label = _hasNaturalLength ? "RE-MEASURE\nL0" : "MEASURE\nL0";
                measureLengthButton.SetState(true, label);
                if (!_hasNaturalLength)
                {
                    SetStatus("Measure Natural Length L0\n(No mass attached)");
                }
                return;
            }

            if (springPhysics != null && springPhysics.IsSettling)
            {
                measureLengthButton.SetState(false, "SETTLING...");
                return;
            }

            measureLengthButton.SetState(true, "MEASURE\nLENGTH");
        }

        private void SetStatus(string text)
        {
            if (statusDisplayText != null && statusDisplayText.text != text)
            {
                statusDisplayText.text = text;
            }
        }
    }
}
