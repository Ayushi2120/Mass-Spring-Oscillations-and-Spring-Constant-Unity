using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace MassSpringLab.Apparatus
{
    /// <summary>
    /// Manages identity, VR/Desktop grab state, anti-clipping clamping,
    /// and spring attachment/reset for each experimental mass (50g - 250g).
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class MassObject : MonoBehaviour
    {
        [Header("Mass Specification")]
        [SerializeField] private int massGrams = 50;
        [SerializeField] private Transform hookAttachPoint;

        [Header("Home Transform")]
        [SerializeField] private Vector3 homePosition;
        [SerializeField] private Quaternion homeRotation = Quaternion.identity;

        [Header("Bounds & Feedback")]
        [SerializeField] private float minWorldY = 0.875f;
        [SerializeField] private Renderer bodyRenderer;

        private Rigidbody _rb;
        private XRGrabInteractable _grabInteractable;
        private SpringAttachmentPoint _springSocket;
        private Transform _homeParent;
        private MaterialPropertyBlock _propBlock;
        private Color _baseColor = new Color(0.95f, 0.76f, 0.18f);

        private bool _canGrab = true;
        private bool _isGrabbed;
        private bool _isAttached;

        public int MassGrams => massGrams;
        public float MassKg => massGrams / 1000f;
        public Transform HookAttachPoint => hookAttachPoint;
        public Vector3 HomePosition => homePosition;
        public Quaternion HomeRotation => homeRotation;
        public bool IsGrabbed => _isGrabbed;
        public bool IsAttached => _isAttached;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _grabInteractable = GetComponent<XRGrabInteractable>();
            _homeParent = transform.parent;
            _propBlock = new MaterialPropertyBlock();

            if (bodyRenderer == null)
            {
                Transform body = transform.Find("Body");
                if (body != null) bodyRenderer = body.GetComponent<Renderer>();
            }

            if (bodyRenderer != null && bodyRenderer.sharedMaterial != null)
            {
                _baseColor = bodyRenderer.sharedMaterial.GetColor("_BaseColor");
            }

            _springSocket = FindFirstObjectByType<SpringAttachmentPoint>();
        }

        private void OnEnable()
        {
            if (_grabInteractable != null)
            {
                _grabInteractable.selectEntered.AddListener(OnXRSelectEntered);
                _grabInteractable.selectExited.AddListener(OnXRSelectExited);
                _grabInteractable.hoverEntered.AddListener(OnXRHoverEntered);
                _grabInteractable.hoverExited.AddListener(OnXRHoverExited);
            }
        }

        private void OnDisable()
        {
            if (_grabInteractable != null)
            {
                _grabInteractable.selectEntered.RemoveListener(OnXRSelectEntered);
                _grabInteractable.selectExited.RemoveListener(OnXRSelectExited);
                _grabInteractable.hoverEntered.RemoveListener(OnXRHoverEntered);
                _grabInteractable.hoverExited.RemoveListener(OnXRHoverExited);
            }
        }

        private void FixedUpdate()
        {
            if (_isGrabbed && !_isAttached)
            {
                ClampAboveTabletop();
            }
        }

        public void Configure(int grams, Transform hookPoint)
        {
            massGrams = grams;
            hookAttachPoint = hookPoint;
            RecordHomeTransform();
        }

        public void RecordHomeTransform()
        {
            homePosition = transform.position;
            homeRotation = transform.rotation;
        }

        public void SetGrabbable(bool grabbable)
        {
            _canGrab = grabbable;
            if (_grabInteractable != null && !_isAttached)
            {
                _grabInteractable.enabled = grabbable;
            }
        }

        public void BeginDesktopGrab()
        {
            if (!_canGrab || _isAttached)
            {
                return;
            }

            _isGrabbed = true;
            if (_rb != null)
            {
                _rb.constraints = RigidbodyConstraints.None;
                _rb.interpolation = RigidbodyInterpolation.None;
                _rb.isKinematic = true;
                _rb.useGravity = false;
            }
            SetHighlight(new Color(0.15f, 0.25f, 0.05f));
        }

        public void MoveDesktopGrab(Vector3 targetWorldPos)
        {
            if (!_isGrabbed || _isAttached) return;

            targetWorldPos.y = Mathf.Max(minWorldY, targetWorldPos.y);
            targetWorldPos.z = Mathf.Clamp(targetWorldPos.z, 0.35f, 2.40f);
            targetWorldPos.x = Mathf.Clamp(targetWorldPos.x, -1.35f, 1.35f);
            transform.position = Vector3.Lerp(transform.position, targetWorldPos, Time.deltaTime * 25f);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.identity, Time.deltaTime * 15f);
        }

        public void EndDesktopGrab()
        {
            if (!_isGrabbed) return;
            _isGrabbed = false;
            HandleRelease();
        }

        public void SetAttachedState(bool attached, Transform socketParent)
        {
            _isAttached = attached;
            _isGrabbed = false;

            if (attached && socketParent != null)
            {
                if (_grabInteractable != null)
                {
                    if (_grabInteractable.isSelected && _grabInteractable.interactionManager != null)
                    {
                        _grabInteractable.interactionManager.CancelInteractableSelection((IXRSelectInteractable)_grabInteractable);
                    }
                    _grabInteractable.enabled = false;
                }

                if (_rb != null)
                {
                    _rb.linearVelocity = Vector3.zero;
                    _rb.angularVelocity = Vector3.zero;
                    _rb.useGravity = false;
                    _rb.isKinematic = true;
                    _rb.constraints = RigidbodyConstraints.FreezeAll;
                }

                transform.SetParent(socketParent, false);
                transform.localRotation = Quaternion.identity;
                transform.localPosition = hookAttachPoint != null
                    ? -hookAttachPoint.localPosition
                    : Vector3.zero;

                SetHighlight(new Color(0.08f, 0.30f, 0.12f));
            }
            else
            {
                if (_homeParent != null)
                {
                    transform.SetParent(_homeParent, true);
                }

                if (_rb != null)
                {
                    _rb.constraints = RigidbodyConstraints.None;
                    _rb.isKinematic = true;
                    _rb.useGravity = false;
                    _rb.linearVelocity = Vector3.zero;
                    _rb.angularVelocity = Vector3.zero;
                }

                if (_grabInteractable != null)
                {
                    _grabInteractable.enabled = _canGrab;
                }

                SetHighlight(Color.black);
            }
        }

        public void ReturnToHome()
        {
            SetAttachedState(false, null);
            transform.position = homePosition;
            transform.rotation = homeRotation;
        }

        private void OnXRSelectEntered(SelectEnterEventArgs args)
        {
            if (!_canGrab || _isAttached) return;

            _isGrabbed = true;
            if (_rb != null)
            {
                _rb.constraints = RigidbodyConstraints.None;
                _rb.interpolation = RigidbodyInterpolation.None;
                _rb.isKinematic = true;
                _rb.useGravity = false;
            }
            SetHighlight(new Color(0.15f, 0.25f, 0.05f));
        }

        private void OnXRSelectExited(SelectExitEventArgs args)
        {
            if (_isAttached)
            {
                _isGrabbed = false;
                return;
            }

            if (!_isGrabbed) return;
            _isGrabbed = false;
            HandleRelease();
        }

        private void OnXRHoverEntered(HoverEnterEventArgs args)
        {
            if (_canGrab && !_isGrabbed && !_isAttached)
            {
                SetHighlight(new Color(0.0f, 0.18f, 0.22f));
            }
        }

        private void OnXRHoverExited(HoverExitEventArgs args)
        {
            if (!_isGrabbed && !_isAttached)
            {
                SetHighlight(Color.black);
            }
        }

        private void HandleRelease()
        {
            if (_springSocket == null)
            {
                _springSocket = FindFirstObjectByType<SpringAttachmentPoint>();
            }

            if (_springSocket != null && _springSocket.TryAttachMass(this))
            {
                return;
            }

            ReturnToHome();
        }

        private void ClampAboveTabletop()
        {
            Vector3 pos = transform.position;
            if (pos.y < minWorldY)
            {
                pos.y = minWorldY;
                transform.position = pos;
            }
        }

        private void SetHighlight(Color emissionColor)
        {
            if (bodyRenderer == null || _propBlock == null) return;
            bodyRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor("_BaseColor", _baseColor);
            _propBlock.SetColor("_EmissionColor", emissionColor);
            bodyRenderer.SetPropertyBlock(_propBlock);
        }
    }
}
