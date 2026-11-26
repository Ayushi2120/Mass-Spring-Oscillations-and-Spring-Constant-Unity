using System;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace MassSpringLab.Interaction
{
    /// <summary>
    /// Interactive 3D button supporting XR Controller/Hand-Tracking selection
    /// and Desktop Left-Mouse click.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class LabActionButton : MonoBehaviour
    {
        [SerializeField] private Renderer buttonRenderer;
        [SerializeField] private TextMeshPro buttonLabel;
        [SerializeField] private Color enabledColor = new Color(0.0f, 0.85f, 0.88f);
        [SerializeField] private Color disabledColor = new Color(0.18f, 0.21f, 0.26f);

        private XRSimpleInteractable _interactable;
        private MaterialPropertyBlock _propBlock;
        private bool _isInteractable = true;

        public bool IsInteractable => _isInteractable;
        public event Action Clicked;

        private void Awake()
        {
            _propBlock = new MaterialPropertyBlock();
            if (buttonRenderer == null) buttonRenderer = GetComponent<Renderer>();
            _interactable = GetComponent<XRSimpleInteractable>();
            ApplyVisuals();
        }

        private void OnEnable()
        {
            if (_interactable != null)
            {
                _interactable.selectEntered.AddListener(OnXRSelect);
            }
        }

        private void OnDisable()
        {
            if (_interactable != null)
            {
                _interactable.selectEntered.RemoveListener(OnXRSelect);
            }
        }

        public void SetState(bool interactable, string labelText = null)
        {
            _isInteractable = interactable;
            if (_interactable != null)
            {
                _interactable.enabled = interactable;
            }
            if (buttonLabel != null && !string.IsNullOrEmpty(labelText))
            {
                buttonLabel.text = labelText;
            }
            ApplyVisuals();
        }

        public void TriggerClick()
        {
            if (!_isInteractable) return;
            Clicked?.Invoke();
        }

        private void OnXRSelect(SelectEnterEventArgs args)
        {
            TriggerClick();
        }

        private void ApplyVisuals()
        {
            if (buttonRenderer == null || _propBlock == null) return;
            Color c = _isInteractable ? enabledColor : disabledColor;
            buttonRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor("_BaseColor", c);
            _propBlock.SetColor("_EmissionColor", _isInteractable ? c * 0.55f : Color.black);
            buttonRenderer.SetPropertyBlock(_propBlock);
        }
    }
}
