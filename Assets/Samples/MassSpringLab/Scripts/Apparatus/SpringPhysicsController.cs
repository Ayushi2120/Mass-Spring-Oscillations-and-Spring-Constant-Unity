using System;
using UnityEngine;

namespace MassSpringLab.Apparatus
{
    /// <summary>
    /// Simulates Hooke's Law (F = m*g = k*x) vertical spring stretching,
    /// damped oscillation, tabletop safety clamping, stability detection,
    /// and manual pull-and-release oscillation for 10-oscillation timing.
    /// </summary>
    public class SpringPhysicsController : MonoBehaviour
    {
        public enum StabilityState
        {
            Unloaded,
            Settling,
            Stable
        }

        [Header("Hooke's Law Parameters")]
        [SerializeField] private float springConstant = 20.0f; // N/m
        [SerializeField] private float gravity = 9.81f;        // m/s^2
        [SerializeField] private float naturalLength = 0.12f;  // m (12 cm)

        [Header("Damping & Settling")]
        [SerializeField, Range(0.05f, 0.95f)] private float dampingRatio = 0.24f;
        [SerializeField, Range(0.005f, 0.2f)] private float oscillationDampingRatio = 0.022f;
        [SerializeField] private float positionTolerance = 0.0005f;
        [SerializeField] private float velocityTolerance = 0.002f;
        [SerializeField] private float settleConfirmDuration = 0.25f;

        [Header("Safety Limits")]
        [SerializeField] private float tableTopY = 0.85f;
        [SerializeField] private float minTableClearance = 0.05f;
        [SerializeField] private float maxAllowedExtension = 0.22f;
        [SerializeField] private float assumedMassDropBelowHook = 0.09f;

        [Header("References")]
        [SerializeField] private Transform springTopAnchor;
        [SerializeField] private Transform springBottomHook;

        private MassObject _attachedMass;
        private float _currentExtension;
        private float _targetExtension;
        private float _velocity;
        private float _settleTimer;
        private StabilityState _state = StabilityState.Unloaded;
        private bool _isManualPulling;
        private bool _isTimedOscillating;

        public float SpringConstant => springConstant;
        public float Gravity => gravity;
        public float NaturalLengthMeters => naturalLength;
        public float CurrentExtensionMeters => _currentExtension;
        public float CurrentLengthMeters => naturalLength + _currentExtension;
        public float TargetExtensionMeters => _targetExtension;
        public StabilityState State => _state;
        public bool IsSettling => _state == StabilityState.Settling;
        public bool IsStable => _state == StabilityState.Stable;
        public bool CanMeasureExtendedLength => _attachedMass != null && _state == StabilityState.Stable;
        public bool IsTimedOscillating => _isTimedOscillating;

        public event Action<StabilityState> OnStabilityChanged;

        private void Awake()
        {
            if (springTopAnchor == null)
            {
                Transform t = transform.Find("SpringTopAnchor");
                if (t != null) springTopAnchor = t;
            }

            if (springBottomHook == null)
            {
                Transform b = transform.Find("SpringBottomHook");
                if (b != null) springBottomHook = b;
            }

            ApplyHookPosition(0f);
        }

        private void FixedUpdate()
        {
            if (_isManualPulling) return;

            if (_state == StabilityState.Unloaded && Mathf.Abs(_currentExtension) < 0.0001f && Mathf.Abs(_velocity) < 0.0001f)
            {
                return;
            }

            StepSpringPhysics(Time.fixedDeltaTime);
        }

        public void OnMassAttached(MassObject mass)
        {
            _attachedMass = mass;
            _isManualPulling = false;
            _isTimedOscillating = false;
            _targetExtension = ComputeSafeExtension(mass != null ? mass.MassKg : 0f);
            _settleTimer = 0f;
            SetState(StabilityState.Settling);
        }

        public void OnMassDetached()
        {
            _attachedMass = null;
            _isManualPulling = false;
            _isTimedOscillating = false;
            _targetExtension = 0f;
            _settleTimer = 0f;
            SetState(StabilityState.Unloaded);
        }

        public void SetManualPullDisplacement(float downwardDisplacementMeters)
        {
            if (_attachedMass == null) return;
            _isManualPulling = true;
            _isTimedOscillating = false;
            _velocity = 0f;
            float clampedDisp = Mathf.Clamp(downwardDisplacementMeters, 0.005f, 0.06f);
            float safeMax = GetSafeMaxExtension();
            _currentExtension = Mathf.Clamp(_targetExtension + clampedDisp, 0f, safeMax);
            ApplyHookPosition(_currentExtension);
        }

        public void ReleaseManualOscillation(float defaultPullMeters = 0.035f)
        {
            if (_attachedMass == null) return;
            if (!_isManualPulling)
            {
                SetManualPullDisplacement(defaultPullMeters);
            }

            _isManualPulling = false;
            _isTimedOscillating = true;
            _velocity = 0f;
            _settleTimer = 0f;
            SetState(StabilityState.Settling);
        }

        public float ComputeSafeExtension(float massKg)
        {
            if (massKg <= 0f || springConstant <= 0.001f) return 0f;

            float force = massKg * gravity;
            float rawExtension = force / springConstant;

            float safeMax = GetSafeMaxExtension();
            return Mathf.Clamp(rawExtension, 0f, safeMax);
        }

        public float GetSafeMaxExtension()
        {
            if (springTopAnchor == null) return maxAllowedExtension;

            float minHookWorldY = tableTopY + minTableClearance + assumedMassDropBelowHook;
            float maxTotalLength = Mathf.Max(naturalLength, springTopAnchor.position.y - minHookWorldY);
            float clearanceBasedMaxExt = Mathf.Max(0f, maxTotalLength - naturalLength);
            return Mathf.Min(maxAllowedExtension, clearanceBasedMaxExt);
        }

        private void StepSpringPhysics(float dt)
        {
            float effectiveMass = _attachedMass != null ? Mathf.Max(0.04f, _attachedMass.MassKg) : 0.08f;
            float activeDamping = _isTimedOscillating ? oscillationDampingRatio : dampingRatio;
            float omega = Mathf.Sqrt(springConstant / effectiveMass);
            float accel = (omega * omega) * (_targetExtension - _currentExtension) - (2f * activeDamping * omega) * _velocity;

            _velocity += accel * dt;
            _currentExtension += _velocity * dt;

            float safeMax = GetSafeMaxExtension();
            if (_currentExtension < 0f)
            {
                _currentExtension = 0f;
                if (_velocity < 0f) _velocity = 0f;
            }
            else if (_currentExtension > safeMax)
            {
                _currentExtension = safeMax;
                if (_velocity > 0f) _velocity = 0f;
            }

            ApplyHookPosition(_currentExtension);

            if (_attachedMass != null && _state == StabilityState.Settling)
            {
                bool withinPos = Mathf.Abs(_currentExtension - _targetExtension) <= positionTolerance;
                bool withinVel = Mathf.Abs(_velocity) <= velocityTolerance;

                if (withinPos && withinVel)
                {
                    _settleTimer += dt;
                    if (_settleTimer >= settleConfirmDuration)
                    {
                        _currentExtension = _targetExtension;
                        _velocity = 0f;
                        _isTimedOscillating = false;
                        ApplyHookPosition(_currentExtension);
                        SetState(StabilityState.Stable);
                    }
                }
                else
                {
                    _settleTimer = 0f;
                }
            }
            else if (_attachedMass == null)
            {
                if (Mathf.Abs(_currentExtension) <= positionTolerance && Mathf.Abs(_velocity) <= velocityTolerance)
                {
                    _currentExtension = 0f;
                    _velocity = 0f;
                    ApplyHookPosition(0f);
                }
            }
        }

        private void ApplyHookPosition(float extension)
        {
            if (springTopAnchor == null || springBottomHook == null) return;

            Vector3 topPos = springTopAnchor.position;
            float targetWorldY = topPos.y - (naturalLength + extension);
            springBottomHook.position = new Vector3(topPos.x, targetWorldY, topPos.z);
        }

        private void SetState(StabilityState next)
        {
            if (_state == next) return;
            _state = next;
            OnStabilityChanged?.Invoke(_state);
        }
    }
}
