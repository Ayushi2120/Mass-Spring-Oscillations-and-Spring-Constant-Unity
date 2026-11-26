using System;
using TMPro;
using UnityEngine;

namespace MassSpringLab.UI
{
    /// <summary>
    /// Drives the existing StopwatchDisplayStation automatically during 10-oscillation timing.
    /// Displays Oscillation Count (0/10 -> 10/10), Status (Waiting for Release / Oscillating /
    /// Measurement Complete), Elapsed Time, Total Time, and Time Period (T = t / 10).
    /// </summary>
    public class StopwatchController : MonoBehaviour
    {
        [SerializeField] private TextMeshPro titleText;
        [SerializeField] private TextMeshPro digitsText;
        [SerializeField] private int targetOscillations = 10;

        private bool _isRunning;
        private float _elapsedSeconds;
        private int _currentOscillations;

        public bool IsRunning => _isRunning;
        public float ElapsedSeconds => _elapsedSeconds;
        public int TargetOscillations => targetOscillations;

        public event Action<int, float, float> OnTimingCompleted;

        private void Awake()
        {
            if (titleText == null) titleText = transform.Find("Title")?.GetComponent<TextMeshPro>();
            if (digitsText == null) digitsText = transform.Find("Digits")?.GetComponent<TextMeshPro>();
            ResetDisplay();
        }

        private void Update()
        {
            if (!_isRunning) return;

            _elapsedSeconds += Time.deltaTime;
            if (digitsText != null)
            {
                digitsText.text = $"{_elapsedSeconds:F2} s";
            }
        }

        public void ShowWaitingForRelease()
        {
            if (_isRunning) return;
            UpdateHeader($"Waiting for Release | 0/{targetOscillations}", new Color(1.0f, 0.78f, 0.15f));
            if (digitsText != null)
            {
                digitsText.text = "0.00 s";
                digitsText.color = Color.white;
            }
        }

        public void StartTiming()
        {
            _isRunning = true;
            _elapsedSeconds = 0f;
            _currentOscillations = 0;
            UpdateHeader($"Oscillating | 0/{targetOscillations}", new Color(0.46f, 1.0f, 0.01f));
            if (digitsText != null)
            {
                digitsText.text = "0.00 s";
                digitsText.color = Color.white;
            }
        }

        public void ReportOscillationCount(int count)
        {
            if (!_isRunning) return;
            _currentOscillations = Mathf.Clamp(count, 0, targetOscillations);
            UpdateHeader($"Oscillating | {_currentOscillations}/{targetOscillations}", new Color(0.46f, 1.0f, 0.01f));

            if (_currentOscillations >= targetOscillations)
            {
                StopAndComplete();
            }
        }

        public void StopAndComplete()
        {
            if (!_isRunning) return;
            _isRunning = false;

            float period = targetOscillations > 0 ? _elapsedSeconds / targetOscillations : 0f;
            UpdateHeader($"Measurement Complete | {targetOscillations}/{targetOscillations}", new Color(0.0f, 0.90f, 0.92f));
            if (digitsText != null)
            {
                digitsText.text = $"t={_elapsedSeconds:F2}s\nT={period:F3}s";
                digitsText.color = new Color(0.46f, 1.0f, 0.01f);
            }

            OnTimingCompleted?.Invoke(targetOscillations, _elapsedSeconds, period);
        }

        public void ResetDisplay()
        {
            _isRunning = false;
            _elapsedSeconds = 0f;
            _currentOscillations = 0;
            UpdateHeader($"STOPWATCH (0/{targetOscillations})", new Color(0.0f, 0.90f, 0.92f));
            if (digitsText != null)
            {
                digitsText.text = "00.00 s";
                digitsText.color = Color.white;
            }
        }

        private void UpdateHeader(string text, Color color)
        {
            if (titleText != null)
            {
                titleText.text = text;
                titleText.color = color;
            }
        }
    }
}
