using System.Collections.Generic;
using UnityEngine;

namespace MassSpringLab.Robot
{
    /// <summary>
    /// Controls ATOM-8's physical instructor presentation: smoothly facing the student while speaking,
    /// extending its RightArm and PointerTip hand directly toward target objects, projecting a subtle
    /// pointer ray from its hand to the exact target, and smoothly returning its arm to rest afterward.
    /// </summary>
    public class Atom8GuidanceController : MonoBehaviour
    {
        private static readonly int[] MassSequence = { 50, 100, 150, 200, 250 };

        [SerializeField] private Transform rightShoulder;
        [SerializeField] private Transform rightElbow;
        [SerializeField] private Transform pointerTip;
        [SerializeField] private Transform headTransform;
        [SerializeField] private Transform studentTransform;
        [SerializeField] private LineRenderer handPointerRay;
        [SerializeField] private float rotateSpeed = 5.0f;

        private readonly HashSet<int> _completedMasses = new HashSet<int>();
        private readonly Dictionary<int, Renderer> _slotPads = new Dictionary<int, Renderer>();
        private readonly Dictionary<int, Transform> _massTransforms = new Dictionary<int, Transform>();
        private readonly Dictionary<int, Renderer> _massBodyRenderers = new Dictionary<int, Renderer>();

        private MaterialPropertyBlock _propBlock;
        private Transform _pointTarget;
        private Quaternion _neutralArmLocalRot;
        private Quaternion _neutralElbowLocalRot = Quaternion.Euler(-22f, 0f, 0f);
        private Quaternion _neutralHeadLocalRot;
        private float _gestureTimer;
        private Vector3 _defaultStudentPos = new Vector3(0f, 1.45f, 0.05f);

        private static readonly Color DefaultPadColor = new Color(0.0f, 0.83f, 0.90f);
        private static readonly Color NextMassColor = new Color(1.0f, 0.65f, 0.16f);
        private static readonly Color CompletedPadColor = new Color(0.18f, 0.25f, 0.30f);

        public int NextRecommendedMassGrams { get; private set; } = 50;
        public bool AllMassesCompleted => _completedMasses.Count >= MassSequence.Length;
        public bool IsPointingAtMass => _pointTarget != null && _massTransforms.ContainsValue(_pointTarget);
        public Transform CurrentPointTarget => _pointTarget;
        public bool IsHandRayActive => handPointerRay != null && handPointerRay.enabled;

        private void Awake()
        {
            _propBlock = new MaterialPropertyBlock();

            if (rightShoulder == null) rightShoulder = transform.Find("Torso/RightArm");
            if (rightElbow == null && rightShoulder != null) rightElbow = rightShoulder.Find("Elbow");
            if (pointerTip == null && rightElbow != null) pointerTip = rightElbow.Find("PointerTip");
            if (headTransform == null) headTransform = transform.Find("Torso/Head");
            if (studentTransform == null && Camera.main != null) studentTransform = Camera.main.transform;

            if (rightShoulder != null) _neutralArmLocalRot = rightShoulder.localRotation;
            if (rightElbow != null) _neutralElbowLocalRot = rightElbow.localRotation;
            if (headTransform != null) _neutralHeadLocalRot = headTransform.localRotation;

            if (handPointerRay == null && pointerTip != null)
            {
                handPointerRay = pointerTip.GetComponent<LineRenderer>();
                if (handPointerRay == null)
                {
                    handPointerRay = pointerTip.gameObject.AddComponent<LineRenderer>();
                    handPointerRay.useWorldSpace = true;
                    handPointerRay.positionCount = 2;
                    handPointerRay.startWidth = 0.007f;
                    handPointerRay.endWidth = 0.002f;
                    handPointerRay.startColor = new Color(0.0f, 0.90f, 0.95f, 0.85f);
                    handPointerRay.endColor = new Color(1.0f, 0.65f, 0.16f, 0.65f);
                }
            }
            if (handPointerRay != null) handPointerRay.enabled = false;

            foreach (int m in MassSequence)
            {
                GameObject pad = GameObject.Find($"SlotPad_{m}g");
                if (pad != null) _slotPads[m] = pad.GetComponent<Renderer>();

                GameObject massGo = GameObject.Find($"Mass_{m}g");
                if (massGo != null)
                {
                    _massTransforms[m] = massGo.transform;
                    Transform body = massGo.transform.Find("Body");
                    if (body != null) _massBodyRenderers[m] = body.GetComponent<Renderer>();
                }
            }
        }

        private void Update()
        {
            // Face target when pointing, or face the student when speaking/resting
            Vector3 lookPos = _pointTarget != null
                ? _pointTarget.position
                : (studentTransform != null ? studentTransform.position : _defaultStudentPos);

            Vector3 flatDir = lookPos - transform.position;
            flatDir.y = 0f;
            if (flatDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetBodyRot = Quaternion.LookRotation(-flatDir.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetBodyRot, rotateSpeed * Time.deltaTime);
            }

            if (rightShoulder == null) return;

            if (_pointTarget != null)
            {
                Vector3 dir = (_pointTarget.position - rightShoulder.position).normalized;
                // Arm geometry extends along local -Y, so map Vector3.down -> dir
                Quaternion worldAim = Quaternion.FromToRotation(Vector3.down, dir);

                if (_gestureTimer > 0f)
                {
                    _gestureTimer -= Time.deltaTime;
                    float pullBounce = Mathf.Sin(_gestureTimer * 6f) * 14f;
                    worldAim *= Quaternion.Euler(pullBounce, 0f, 0f);
                }

                rightShoulder.rotation = Quaternion.Slerp(rightShoulder.rotation, worldAim, rotateSpeed * Time.deltaTime);

                if (rightElbow != null)
                {
                    rightElbow.localRotation = Quaternion.Slerp(rightElbow.localRotation, Quaternion.identity, rotateSpeed * Time.deltaTime);
                }

                if (headTransform != null)
                {
                    Vector3 headDir = (_pointTarget.position - headTransform.position).normalized;
                    Quaternion headLook = Quaternion.LookRotation(-headDir, Vector3.up);
                    headTransform.rotation = Quaternion.Slerp(headTransform.rotation, headLook, rotateSpeed * Time.deltaTime);
                }

                UpdateHandPointerRay(dir);
            }
            else
            {
                rightShoulder.localRotation = Quaternion.Slerp(rightShoulder.localRotation, _neutralArmLocalRot, rotateSpeed * Time.deltaTime);
                if (rightElbow != null)
                {
                    rightElbow.localRotation = Quaternion.Slerp(rightElbow.localRotation, _neutralElbowLocalRot, rotateSpeed * Time.deltaTime);
                }
                if (headTransform != null)
                {
                    headTransform.localRotation = Quaternion.Slerp(headTransform.localRotation, _neutralHeadLocalRot, rotateSpeed * Time.deltaTime);
                }
                if (handPointerRay != null && handPointerRay.enabled)
                {
                    handPointerRay.enabled = false;
                }
            }
        }

        private void UpdateHandPointerRay(Vector3 targetDir)
        {
            if (handPointerRay == null || pointerTip == null || _pointTarget == null) return;

            Vector3 armDir = -rightShoulder.up;
            float angleToTarget = Vector3.Angle(armDir, targetDir);
            if (angleToTarget < 28f)
            {
                handPointerRay.enabled = true;
                handPointerRay.SetPosition(0, pointerTip.position);
                handPointerRay.SetPosition(1, _pointTarget.position);
            }
            else
            {
                handPointerRay.enabled = false;
            }
        }

        public void FaceStudent()
        {
            _pointTarget = null;
            _gestureTimer = 0f;
            if (handPointerRay != null) handPointerRay.enabled = false;
        }

        public void PointAtTarget(Transform target, bool playDownwardGesture = false)
        {
            _pointTarget = target;
            if (playDownwardGesture)
            {
                _gestureTimer = 2.2f;
            }
        }

        public void PointAtMass(int massGrams)
        {
            if (_massTransforms.TryGetValue(massGrams, out Transform t))
            {
                _pointTarget = t;
            }
            NextRecommendedMassGrams = massGrams;
            RefreshHighlights();
        }

        public void ReturnToNeutral()
        {
            FaceStudent();
        }

        public void MarkMassCompletedAndGuideNext(int completedMassGrams)
        {
            _completedMasses.Add(completedMassGrams);

            int nextMass = -1;
            foreach (int m in MassSequence)
            {
                if (!_completedMasses.Contains(m))
                {
                    nextMass = m;
                    break;
                }
            }

            NextRecommendedMassGrams = nextMass;

            if (nextMass > 0 && _massTransforms.TryGetValue(nextMass, out Transform target))
            {
                _pointTarget = target;
            }
            else
            {
                _pointTarget = null;
            }

            RefreshHighlights();
        }

        private void RefreshHighlights()
        {
            foreach (int m in MassSequence)
            {
                bool isCompleted = _completedMasses.Contains(m);
                bool isNext = (m == NextRecommendedMassGrams);

                if (_slotPads.TryGetValue(m, out Renderer padRend) && padRend != null)
                {
                    Color c = isCompleted ? CompletedPadColor : (isNext ? NextMassColor : DefaultPadColor);
                    Color em = isCompleted ? Color.black : (isNext ? NextMassColor * 0.65f : DefaultPadColor * 0.35f);

                    padRend.GetPropertyBlock(_propBlock);
                    _propBlock.SetColor("_BaseColor", c);
                    _propBlock.SetColor("_EmissionColor", em);
                    padRend.SetPropertyBlock(_propBlock);
                }

                if (_massBodyRenderers.TryGetValue(m, out Renderer bodyRend) && bodyRend != null)
                {
                    Color bodyEm = isNext ? new Color(0.28f, 0.18f, 0.02f) : Color.black;
                    bodyRend.GetPropertyBlock(_propBlock);
                    _propBlock.SetColor("_EmissionColor", bodyEm);
                    bodyRend.SetPropertyBlock(_propBlock);
                }
            }
        }
    }
}
