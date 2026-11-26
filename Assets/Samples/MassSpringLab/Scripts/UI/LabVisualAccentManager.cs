using UnityEngine;

namespace MassSpringLab.UI
{
    /// <summary>
    /// Visual-only runtime component that adds subtle, comfortable breathing modulation
    /// to decorative architectural accent lights without affecting any experiment logic.
    /// </summary>
    public class LabVisualAccentManager : MonoBehaviour
    {
        [SerializeField] private Light[] accentLights;
        [SerializeField] private float pulseSpeed = 0.8f;
        [SerializeField, Range(0f, 0.25f)] private float pulseAmount = 0.10f;

        private float[] _baseIntensities;

        private void Awake()
        {
            if (accentLights == null || accentLights.Length == 0)
            {
                accentLights = GetComponentsInChildren<Light>();
            }

            _baseIntensities = new float[accentLights.Length];
            for (int i = 0; i < accentLights.Length; i++)
            {
                if (accentLights[i] != null)
                {
                    _baseIntensities[i] = accentLights[i].intensity;
                }
            }
        }

        private void Update()
        {
            if (accentLights == null || _baseIntensities == null) return;

            float wave = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            for (int i = 0; i < accentLights.Length; i++)
            {
                if (accentLights[i] != null)
                {
                    accentLights[i].intensity = _baseIntensities[i] * (1f + wave);
                }
            }
        }
    }
}
