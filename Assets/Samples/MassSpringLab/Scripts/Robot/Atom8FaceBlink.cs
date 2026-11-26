using UnityEngine;

namespace MassSpringLab.Robot
{
    /// <summary>
    /// Visual-only subtle eye-blinking effect for ATOM-8's glowing cyan eyes.
    /// Does not interfere with head rotation, arm pointing, or narration.
    /// </summary>
    public class Atom8FaceBlink : MonoBehaviour
    {
        [SerializeField] private Transform eyeLeft;
        [SerializeField] private Transform eyeRight;
        [SerializeField] private float blinkInterval = 3.8f;
        [SerializeField] private float blinkDuration = 0.14f;

        private Vector3 _baseScaleLeft = Vector3.one;
        private Vector3 _baseScaleRight = Vector3.one;
        private float _timer;
        private float _blinkTimer;
        private bool _isBlinking;

        private void Awake()
        {
            if (eyeLeft != null) _baseScaleLeft = eyeLeft.localScale;
            if (eyeRight != null) _baseScaleRight = eyeRight.localScale;
            _timer = blinkInterval;
        }

        private void Update()
        {
            if (eyeLeft == null || eyeRight == null) return;

            if (!_isBlinking)
            {
                _timer -= Time.deltaTime;
                if (_timer <= 0f)
                {
                    _isBlinking = true;
                    _blinkTimer = 0f;
                }
                return;
            }

            _blinkTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_blinkTimer / blinkDuration);
            float scaleY = 1f - Mathf.Sin(t * Mathf.PI) * 0.88f;

            eyeLeft.localScale = new Vector3(_baseScaleLeft.x, _baseScaleLeft.y * scaleY, _baseScaleLeft.z);
            eyeRight.localScale = new Vector3(_baseScaleRight.x, _baseScaleRight.y * scaleY, _baseScaleRight.z);

            if (t >= 1f)
            {
                _isBlinking = false;
                _timer = blinkInterval + Random.Range(-0.6f, 0.8f);
                eyeLeft.localScale = _baseScaleLeft;
                eyeRight.localScale = _baseScaleRight;
            }
        }
    }
}
