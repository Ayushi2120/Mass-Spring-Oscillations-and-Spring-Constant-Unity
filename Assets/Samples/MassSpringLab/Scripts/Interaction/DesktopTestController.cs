using MassSpringLab.Apparatus;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace MassSpringLab.Interaction
{
    /// <summary>
    /// Enables Desktop Play Mode navigation (WASD + Right-Mouse look),
    /// Left-Mouse button clicking on LabActionButtons, and Left-Mouse click-and-drag
    /// grabbing of MassObjects when no XR HMD is active.
    /// </summary>
    public class DesktopTestController : MonoBehaviour
    {
        [Header("Desktop Movement")]
        [SerializeField] private float moveSpeed = 1.8f;
        [SerializeField] private float lookSensitivity = 0.18f;
        [SerializeField] private float defaultEyeHeight = 1.45f;

        [Header("Desktop Grab & Click")]
        [SerializeField] private float maxGrabRange = 3.5f;
        [SerializeField] private float scrollDepthSpeed = 0.08f;

        [Header("References")]
        [SerializeField] private Transform cameraTransform;

        private float _yaw;
        private float _pitch;
        private bool _xrActive;

        private Camera _cam;
        private MassObject _grabbedMass;
        private SpringAttachmentPoint _springSocket;
        private SpringOscillationController _oscillationCtrl;
        private bool _isPullingAttachedMass;
        private float _pullStartMouseY;
        private float _grabDistance;

        private void Start()
        {
            _xrActive = XRSettings.isDeviceActive;
            if (_xrActive)
            {
                enabled = false;
                return;
            }

            _cam = Camera.main;
            _springSocket = FindFirstObjectByType<SpringAttachmentPoint>();
            _oscillationCtrl = FindFirstObjectByType<SpringOscillationController>();
            if (cameraTransform == null && _cam != null)
            {
                cameraTransform = _cam.transform;
            }

            if (cameraTransform != null)
            {
                Vector3 localPos = cameraTransform.localPosition;
                if (localPos.y < 0.5f)
                {
                    localPos.y = defaultEyeHeight;
                    cameraTransform.localPosition = localPos;
                }

                Vector3 euler = cameraTransform.eulerAngles;
                _yaw = euler.y;
                _pitch = euler.x > 180f ? euler.x - 360f : euler.x;
            }
        }

        private void Update()
        {
            if (_xrActive || cameraTransform == null)
            {
                return;
            }

            HandleLook();
            HandleMovement();
            HandleDesktopPointer();
        }

        private void HandleLook()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.rightButton.isPressed)
            {
                return;
            }

            Vector2 delta = mouse.delta.ReadValue();
            _yaw += delta.x * lookSensitivity;
            _pitch = Mathf.Clamp(_pitch - delta.y * lookSensitivity, -80f, 80f);

            cameraTransform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        private void HandleMovement()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            Vector3 input = Vector3.zero;
            if (keyboard.wKey.isPressed) input.z += 1f;
            if (keyboard.sKey.isPressed) input.z -= 1f;
            if (keyboard.dKey.isPressed) input.x += 1f;
            if (keyboard.aKey.isPressed) input.x -= 1f;

            if (input.sqrMagnitude < 0.001f)
            {
                return;
            }

            Vector3 forward = cameraTransform.forward;
            forward.y = 0f;
            forward.Normalize();

            Vector3 right = cameraTransform.right;
            right.y = 0f;
            right.Normalize();

            Vector3 move = (forward * input.z + right * input.x).normalized;
            transform.position += move * (moveSpeed * Time.deltaTime);
        }

        private void HandleDesktopPointer()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || _cam == null)
            {
                return;
            }

            Vector2 mousePos = mouse.position.ReadValue();
            Ray ray = _cam.ScreenPointToRay(mousePos);

            if (mouse.leftButton.wasPressedThisFrame && _grabbedMass == null && !_isPullingAttachedMass)
            {
                RaycastHit[] hits = Physics.RaycastAll(ray, maxGrabRange);
                float bestDist = float.MaxValue;
                LabActionButton hitButton = null;
                MassObject hitMass = null;
                MassObject hitAttachedMass = null;

                foreach (RaycastHit h in hits)
                {
                    if (h.distance >= bestDist) continue;

                    LabActionButton btn = h.collider.GetComponentInParent<LabActionButton>();
                    if (btn != null && btn.IsInteractable)
                    {
                        bestDist = h.distance;
                        hitButton = btn;
                        hitMass = null;
                        hitAttachedMass = null;
                        continue;
                    }

                    MassObject m = h.collider.GetComponentInParent<MassObject>();
                    if (m != null)
                    {
                        if (!m.IsAttached)
                        {
                            bestDist = h.distance;
                            hitMass = m;
                            hitAttachedMass = null;
                            hitButton = null;
                        }
                        else if (_oscillationCtrl != null && _oscillationCtrl.CanPullAndOscillate)
                        {
                            bestDist = h.distance;
                            hitAttachedMass = m;
                            hitMass = null;
                            hitButton = null;
                        }
                    }
                }

                if (hitButton != null)
                {
                    hitButton.TriggerClick();
                    return;
                }

                if (hitAttachedMass != null)
                {
                    _isPullingAttachedMass = true;
                    _pullStartMouseY = mousePos.y;
                    _oscillationCtrl.BeginManualPull(0.015f);
                    return;
                }

                if (hitMass != null)
                {
                    _grabbedMass = hitMass;
                    _grabDistance = Mathf.Clamp(bestDist, 0.45f, 2.0f);
                    _grabbedMass.BeginDesktopGrab();
                }
            }

            if (_isPullingAttachedMass)
            {
                if (mouse.leftButton.isPressed)
                {
                    float deltaPixels = Mathf.Max(0f, _pullStartMouseY - mousePos.y);
                    float pullMeters = Mathf.Clamp(0.015f + deltaPixels * 0.0003f, 0.01f, 0.055f);
                    _oscillationCtrl?.BeginManualPull(pullMeters);
                }
                else
                {
                    _isPullingAttachedMass = false;
                    _oscillationCtrl?.ReleaseToOscillate(0.038f);
                }
                return;
            }

            if (_grabbedMass != null)
            {
                if (_grabbedMass.IsAttached)
                {
                    _grabbedMass = null;
                    return;
                }

                if (mouse.leftButton.isPressed)
                {
                    float scroll = mouse.scroll.ReadValue().y;
                    if (Mathf.Abs(scroll) > 0.01f)
                    {
                        _grabDistance = Mathf.Clamp(_grabDistance + Mathf.Sign(scroll) * scrollDepthSpeed, 0.45f, 2.0f);
                    }

                    Vector3 targetPos = ray.GetPoint(_grabDistance);

                    if (_springSocket != null && !_springSocket.HasAttachedMass)
                    {
                        Vector3 hookPos = _springSocket.AttachWorldPosition;
                        Vector3 closestOnRay = ray.origin + ray.direction * Vector3.Dot(hookPos - ray.origin, ray.direction);
                        if (Vector3.Distance(closestOnRay, hookPos) < 0.22f)
                        {
                            targetPos = Vector3.Lerp(targetPos, hookPos, 0.65f);
                        }
                    }

                    _grabbedMass.MoveDesktopGrab(targetPos);
                }
                else
                {
                    _grabbedMass.EndDesktopGrab();
                    _grabbedMass = null;
                }
            }
        }
    }
}
