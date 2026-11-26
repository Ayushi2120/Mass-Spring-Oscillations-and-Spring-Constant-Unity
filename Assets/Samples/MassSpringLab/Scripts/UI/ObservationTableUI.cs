using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MassSpringLab.UI
{
    /// <summary>
    /// Updates the WallTheoryBoard Observation Table rows (50g - 250g) with all 7 columns:
    /// Mass (g), L0 (cm), L (cm), x (cm), N, t (s), and T (s).
    /// Updates existing rows in-place without duplication and preserves L0 and prior trials.
    /// </summary>
    public class ObservationTableUI : MonoBehaviour
    {
        public struct RowData
        {
            public float L0Cm;
            public float LCm;
            public float XCm;
            public int Oscillations;
            public float TotalTimeSec;
            public float TimePeriodSec;
            public bool HasLength;
            public bool HasTiming;
        }

        private readonly Dictionary<int, TextMeshProUGUI> _rowTexts = new Dictionary<int, TextMeshProUGUI>();
        private readonly Dictionary<int, RowData> _recordedData = new Dictionary<int, RowData>();

        private int _lastHighlightedMass = -1;
        private float _savedL0Cm;
        private bool _hasSavedL0;

        private void Awake()
        {
            CacheRows();
        }

        public void CacheRows()
        {
            _rowTexts.Clear();
            int[] masses = { 50, 100, 150, 200, 250 };
            foreach (int m in masses)
            {
                Transform t = transform.Find($"ObsRow_{m}g");
                if (t == null)
                {
                    GameObject go = GameObject.Find($"ObsRow_{m}g");
                    if (go != null) t = go.transform;
                }

                if (t != null)
                {
                    TextMeshProUGUI tmp = t.GetComponent<TextMeshProUGUI>();
                    if (tmp != null) _rowTexts[m] = tmp;
                }
            }
        }

        public void SetBaselineL0ForAllPending(float l0Cm)
        {
            if (_rowTexts.Count == 0) CacheRows();

            _savedL0Cm = l0Cm;
            _hasSavedL0 = true;

            int[] masses = { 50, 100, 150, 200, 250 };
            foreach (int m in masses)
            {
                if (_recordedData.TryGetValue(m, out RowData existing) && existing.HasLength)
                {
                    existing.L0Cm = l0Cm;
                    existing.XCm = existing.LCm - l0Cm;
                    _recordedData[m] = existing;
                    RenderRow(m, existing, m == _lastHighlightedMass);
                    continue;
                }

                if (_rowTexts.TryGetValue(m, out TextMeshProUGUI tmp))
                {
                    tmp.text = FormatRow(m, $"{l0Cm:F2}", "—", "—", "—", "—", "—");
                    tmp.color = Color.white;
                }
            }
        }

        public void RecordLengthObservation(int massGrams, float l0Cm, float lCm)
        {
            if (_rowTexts.Count == 0) CacheRows();

            float effectiveL0 = _hasSavedL0 ? _savedL0Cm : l0Cm;
            float xCm = lCm - effectiveL0;
            RecordObservation(massGrams, effectiveL0, lCm, xCm, 0f);
        }

        public void RecordObservation(int massGrams, float l0Cm, float lCm, float xCm, float kNm)
        {
            if (_rowTexts.Count == 0) CacheRows();

            float effectiveL0 = _hasSavedL0 ? _savedL0Cm : l0Cm;
            RowData data = _recordedData.TryGetValue(massGrams, out RowData prev) ? prev : default;
            data.L0Cm = effectiveL0;
            data.LCm = lCm;
            data.XCm = xCm;
            data.HasLength = true;
            _recordedData[massGrams] = data;

            HighlightRow(massGrams, data);
        }

        public void RecordTimingObservation(int massGrams, int oscillations, float totalTimeSec, float timePeriodSec)
        {
            if (_rowTexts.Count == 0) CacheRows();

            RowData data = _recordedData.TryGetValue(massGrams, out RowData prev) ? prev : default;
            if (!data.HasLength && _hasSavedL0) data.L0Cm = _savedL0Cm;
            data.Oscillations = oscillations;
            data.TotalTimeSec = totalTimeSec;
            data.TimePeriodSec = timePeriodSec;
            data.HasTiming = true;
            _recordedData[massGrams] = data;

            HighlightRow(massGrams, data);
        }

        public bool TryGetRecordedRow(int massGrams, out RowData data)
        {
            return _recordedData.TryGetValue(massGrams, out data);
        }

        private void HighlightRow(int massGrams, RowData data)
        {
            if (_lastHighlightedMass > 0 && _lastHighlightedMass != massGrams &&
                _rowTexts.TryGetValue(_lastHighlightedMass, out TextMeshProUGUI prevTmp))
            {
                prevTmp.color = Color.white;
            }

            RenderRow(massGrams, data, true);
            _lastHighlightedMass = massGrams;
        }

        private void RenderRow(int massGrams, RowData data, bool highlighted)
        {
            if (!_rowTexts.TryGetValue(massGrams, out TextMeshProUGUI tmp)) return;

            string l0Str = data.HasLength || _hasSavedL0 ? $"{data.L0Cm:F2}" : "—";
            string lStr = data.HasLength ? $"{data.LCm:F2}" : "—";
            string xStr = data.HasLength ? $"{data.XCm:F2}" : "—";
            string nStr = data.HasTiming ? $"{data.Oscillations}" : "—";
            string tStr = data.HasTiming ? $"{data.TotalTimeSec:F2}" : "—";
            string periodStr = data.HasTiming ? $"{data.TimePeriodSec:F3}" : "—";

            tmp.text = FormatRow(massGrams, l0Str, lStr, xStr, nStr, tStr, periodStr);
            tmp.color = highlighted ? new Color(0.46f, 1.0f, 0.01f) : Color.white;
        }

        public static string FormatRow(int massGrams, string l0Str, string lStr, string xStr)
        {
            return FormatRow(massGrams, l0Str, lStr, xStr, "—", "—", "—");
        }

        public static string FormatRow(
            int massGrams,
            string l0Str,
            string lStr,
            string xStr,
            string nStr,
            string tStr,
            string periodStr)
        {
            return $"{massGrams,3}g<pos=14%>{l0Str}<pos=30%>{lStr}<pos=45%>{xStr}<pos=60%>{nStr}<pos=70%>{tStr}<pos=85%>{periodStr}";
        }
    }
}
