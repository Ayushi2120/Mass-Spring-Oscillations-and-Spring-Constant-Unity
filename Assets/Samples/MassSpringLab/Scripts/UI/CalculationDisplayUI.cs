using TMPro;
using UnityEngine;

namespace MassSpringLab.UI
{
    /// <summary>
    /// Displays step-by-step Hooke's Law and Time Period calculations on CalculationPanelStation
    /// using actual measured L0, L, t, N, and g = 9.8 m/s^2.
    /// </summary>
    public class CalculationDisplayUI : MonoBehaviour
    {
        [SerializeField] private TextMeshPro headerText;
        [SerializeField] private TextMeshPro bodyText;

        private void Awake()
        {
            if (headerText == null)
            {
                Transform h = transform.Find("CalcHeader");
                if (h != null) headerText = h.GetComponent<TextMeshPro>();
            }
            if (bodyText == null)
            {
                Transform b = transform.Find("CalcPlaceholder");
                if (b != null) bodyText = b.GetComponent<TextMeshPro>();
            }
        }

        public void ShowIdle(string prompt)
        {
            if (headerText != null) headerText.text = "CALCULATIONS";
            if (bodyText != null) bodyText.text = prompt;
        }

        public void ShowCalculation(int massGrams, float l0Cm, float lCm, float xCm, float forceN, float kNm)
        {
            float massKg = massGrams / 1000f;
            float xMeters = xCm / 100f;

            if (headerText != null)
            {
                headerText.text = $"CALCULATIONS — {massGrams} g";
            }

            if (bodyText != null)
            {
                bodyText.text =
                    $"A. x = L - L0 = {lCm:F2} - {l0Cm:F2} = {xCm:F2} cm ({xMeters:F4} m)\n" +
                    $"B. F = m*g = {massKg:F3} kg x 9.81 = {forceN:F4} N\n" +
                    $"<color=#76FF03><b>C. k = F/x = {kNm:F2} N/m</b></color>";
            }
        }

        public void ShowFullCalculations(
            int massGrams,
            float l0Cm,
            float lCm,
            int oscillations,
            float totalTimeSec)
        {
            float massKg = massGrams / 1000f;
            float xCm = lCm - l0Cm;
            float xMeters = xCm / 100f;
            const float g = 9.8f;
            float forceN = massKg * g;
            float kNm = xMeters > 0.0001f ? forceN / xMeters : 0f;
            float tExp = oscillations > 0 ? totalTimeSec / oscillations : 0f;
            float tTheory = kNm > 0.0001f ? 2f * Mathf.PI * Mathf.Sqrt(massKg / kNm) : 0f;

            if (headerText != null)
            {
                headerText.text = $"CALCULATIONS — {massGrams} g";
            }

            if (bodyText != null)
            {
                bodyText.text =
                    $"A. x = L - L0 = {lCm:F2} - {l0Cm:F2} = {xCm:F2} cm ({xMeters:F4} m)\n" +
                    $"B. F = m*g = {massKg:F3} kg x 9.8 = {forceN:F3} N\n" +
                    $"<color=#76FF03><b>C. k = F/x = {kNm:F2} N/m</b></color>\n" +
                    $"D. T(exp) = t/N = {totalTimeSec:F2}/{oscillations} = <b>{tExp:F3} s</b>\n" +
                    $"<color=#00E5FF><b>E. T(theo) = 2*pi*sqrt(m/k) = {tTheory:F3} s</b></color>";
            }
        }
    }
}
