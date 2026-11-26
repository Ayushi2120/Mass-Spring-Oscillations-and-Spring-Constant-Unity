using System;
using TMPro;
using UnityEngine;

namespace MassSpringLab.Apparatus
{
    /// <summary>
    /// Marks the attachment socket at the bottom of the spring hook where a single MassObject connects.
    /// Detects grabbed masses approaching or entering snapRadius, aligns their top hook directly
    /// to the spring bottom endpoint, notifies SpringPhysicsController, and enforces single-mass attachment.
    /// </summary>
    public class SpringAttachmentPoint : MonoBehaviour
    {
        [Header("Attachment Settings")]
        [SerializeField] private float snapRadius = 0.16f;
        [SerializeField] private float highlightRadius = 0.28f;
        [SerializeField] private bool autoSnapWhenGrabbedNearHook = true;

        [Header("References")]
        [SerializeField] private SpringPhysicsController springPhysics;
        [SerializeField] private Renderer haloRenderer;
        [SerializeField] private TextMeshPro statusLabel;

        private MassObject _attachedMass;
        private MassObject[] _allMasses;
        private MaterialPropertyBlock _propBlock;

        private static readonly Color IdleColor = new Color(0.0f, 0.75f, 0.80f);
        private static readonly Color ReadyColor = new Color(0.46f, 1.0f, 0.01f);
        private static readonly Color SettlingColor = new Color(1.0f, 0.78f, 0.15f);
        private static readonly Color StableColor = new Color(0.20f, 0.95f, 0.35f);
        private static readonly Color OccupiedColor = new Color(1.0f, 0.45f, 0.10f);

        public float SnapRadius => snapRadius;
        public Vector3 AttachWorldPosition => transform.position;
        public MassObject AttachedMass => _attachedMass;
        public bool HasAttachedMass => _attachedMass != null;

        public event Action<MassObject> OnMassAttachedEvent;

        private void Awake()
        {
            _propBlock = new MaterialPropertyBlock();
            _allMasses = FindObjectsByType<MassObject>(FindObjectsSortMode.None);
            if (springPhysics == null)
            {
                springPhysics = GetComponentInParent<SpringPhysicsController>();
            }
            UpdateFeedback(IdleColor, "Attach Mass Here");
        }

        private void Update()
        {
            if (_allMasses == null || _allMasses.Length == 0) return;

            if (_attachedMass != null)
            {
                bool anotherGrabbedNear = false;
                foreach (MassObject m in _allMasses)
                {
                    if (m != null && m != _attachedMass && m.IsGrabbed && DistanceToMass(m) <= highlightRadius)
                    {
                        anotherGrabbedNear = true;
                        break;
                    }
                }

                if (anotherGrabbedNear)
                {
                    UpdateFeedback(OccupiedColor, $"Occupied ({_attachedMass.MassGrams}g)");
                }
                else if (springPhysics != null && springPhysics.IsSettling)
                {
                    UpdateFeedback(SettlingColor, $"{_attachedMass.MassGrams}g Attached\nSpring Settling...");
                }
                else
                {
                    UpdateFeedback(StableColor, $"{_attachedMass.MassGrams}g Attached\nSpring Stable");
                }
                return;
            }

            MassObject closestGrabbed = null;
            float bestDist = float.MaxValue;
            foreach (MassObject m in _allMasses)
            {
                if (m == null || !m.IsGrabbed) continue;
                float d = DistanceToMass(m);
                if (d < bestDist)
                {
                    bestDist = d;
                    closestGrabbed = m;
                }
            }

            if (closestGrabbed != null && bestDist <= snapRadius)
            {
                if (autoSnapWhenGrabbedNearHook)
                {
                    TryAttachMass(closestGrabbed);
                    return;
                }
                UpdateFeedback(ReadyColor, $"Release to Attach {closestGrabbed.MassGrams}g");
            }
            else if (closestGrabbed != null && bestDist <= highlightRadius)
            {
                UpdateFeedback(ReadyColor * 0.75f, "Move Closer to Hook");
            }
            else
            {
                UpdateFeedback(IdleColor, "Attach Mass Here");
            }
        }

        public bool TryAttachMass(MassObject mass)
        {
            if (mass == null || _attachedMass != null)
            {
                return false;
            }

            if (DistanceToMass(mass) > snapRadius)
            {
                return false;
            }

            _attachedMass = mass;
            mass.SetAttachedState(true, transform);

            if (springPhysics != null)
            {
                springPhysics.OnMassAttached(mass);
                UpdateFeedback(SettlingColor, $"{mass.MassGrams}g Attached\nSpring Settling...");
            }
            else
            {
                UpdateFeedback(StableColor, $"{mass.MassGrams}g Attached\nSpring Stable");
            }

            OnMassAttachedEvent?.Invoke(mass);
            return true;
        }

        public void DetachCurrentMass()
        {
            if (_attachedMass == null) return;
            _attachedMass.ReturnToHome();
            _attachedMass = null;
            if (springPhysics != null)
            {
                springPhysics.OnMassDetached();
            }
            UpdateFeedback(IdleColor, "Attach Mass Here");
        }

        public float DistanceToMass(MassObject mass)
        {
            float centerDist = Vector3.Distance(transform.position, mass.transform.position);
            if (mass.HookAttachPoint != null)
            {
                float hookDist = Vector3.Distance(transform.position, mass.HookAttachPoint.position);
                return Mathf.Min(centerDist, hookDist);
            }
            return centerDist;
        }

        private void UpdateFeedback(Color color, string text)
        {
            if (haloRenderer != null && _propBlock != null)
            {
                haloRenderer.GetPropertyBlock(_propBlock);
                _propBlock.SetColor("_BaseColor", color);
                _propBlock.SetColor("_EmissionColor", color * 0.75f);
                haloRenderer.SetPropertyBlock(_propBlock);
            }

            if (statusLabel != null && statusLabel.text != text)
            {
                statusLabel.text = text;
                statusLabel.color = color;
            }
        }
    }
}
