using System;
using System.Collections.Generic;
using UnityEngine;

namespace MassSpringLab.UI
{
    /// <summary>
    /// Smoothly illuminates the Calculations Panel with a dedicated SpotLight
    /// while gently dimming surrounding lab lights (never completely black),
    /// then smoothly restores normal lighting and notifies listeners.
    /// </summary>
    public class CalculationSpotlightController : MonoBehaviour
    {
        private enum PresentationPhase
        {
            Idle,
            RampingUp,
            Holding,
            RampingDown
        }

        [SerializeField] private Light spotlight;
        [SerializeField] private float peakIntensity = 6.5f;
        [SerializeField] private float holdDuration = 4.0f;
        [SerializeField] private float transitionDuration = 0.75f;
        [SerializeField, Range(0.1f, 0.5f)] private float dimmedLightMultiplier = 0.22f;
        [SerializeField, Range(0.1f, 0.6f)] private float dimmedAmbientMultiplier = 0.30f;

        private readonly List<(Light light, float baseIntensity)> _labLights = new List<(Light, float)>();
        private Color _baseAmbientLight;
        private PresentationPhase _phase = PresentationPhase.Idle;
        private float _blend;
        private float _holdTimer;

        public bool IsPresenting => _phase != PresentationPhase.Idle;
        public float CurrentSpotlightIntensity => spotlight != null ? spotlight.intensity : 0f;
        public float CurrentDimBlend => _blend;

        public event Action OnPresentationCompleted;

        private void Awake()
        {
            if (spotlight == null) spotlight = GetComponentInChildren<Light>();
            if (spotlight != null) spotlight.intensity = 0f;

            _baseAmbientLight = RenderSettings.ambientLight;
            Light[] allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light l in allLights)
            {
                if (l != null && l != spotlight)
                {
                    _labLights.Add((l, l.intensity));
                }
            }
        }

        private void Update()
        {
            if (_phase == PresentationPhase.Idle) return;

            float step = transitionDuration > 0.01f ? Time.deltaTime / transitionDuration : 1f;

            switch (_phase)
            {
                case PresentationPhase.RampingUp:
                    _blend = Mathf.MoveTowards(_blend, 1f, step);
                    ApplyLighting(_blend);
                    if (_blend >= 0.999f)
                    {
                        _blend = 1f;
                        _holdTimer = holdDuration;
                        _phase = PresentationPhase.Holding;
                    }
                    break;

                case PresentationPhase.Holding:
                    _holdTimer -= Time.deltaTime;
                    if (_holdTimer <= 0f)
                    {
                        _phase = PresentationPhase.RampingDown;
                    }
                    break;

                case PresentationPhase.RampingDown:
                    _blend = Mathf.MoveTowards(_blend, 0f, step);
                    ApplyLighting(_blend);
                    if (_blend <= 0.001f)
                    {
                        _blend = 0f;
                        ApplyLighting(0f);
                        _phase = PresentationPhase.Idle;
                        OnPresentationCompleted?.Invoke();
                    }
                    break;
            }
        }

        public void TriggerSpotlight()
        {
            _phase = PresentationPhase.RampingUp;
        }

        public void SetTimingForTesting(float customHoldSeconds, float customTransitionSeconds)
        {
            holdDuration = Mathf.Max(0.05f, customHoldSeconds);
            transitionDuration = Mathf.Max(0.05f, customTransitionSeconds);
        }

        private void ApplyLighting(float blend)
        {
            if (spotlight != null)
            {
                spotlight.intensity = Mathf.Lerp(0f, peakIntensity, blend);
            }

            float lightMul = Mathf.Lerp(1f, dimmedLightMultiplier, blend);
            foreach (var entry in _labLights)
            {
                if (entry.light != null)
                {
                    entry.light.intensity = entry.baseIntensity * lightMul;
                }
            }

            float ambMul = Mathf.Lerp(1f, dimmedAmbientMultiplier, blend);
            RenderSettings.ambientLight = _baseAmbientLight * ambMul;
        }
    }
}
