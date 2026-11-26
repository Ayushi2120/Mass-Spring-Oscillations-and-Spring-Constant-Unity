using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace MassSpringLab.Apparatus
{
    /// <summary>
    /// Allows VR controllers and hand-tracking pinch interactors to pull the attached mass/hook
    /// slightly downward and release it to start vertical oscillation, without modifying MassObject.
    /// </summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class SpringPullHandle : MonoBehaviour
    {
        [SerializeField] private SpringOscillationController oscillationController;

        private XRSimpleInteractable _interactable;
        private IXRSelectInteractor _activeInteractor;
        private float _grabStartY;

        private void Awake()
        {
            _interactable = GetComponent<XRSimpleInteractable>();
            if (oscillationController == null)
            {
                oscillationController = GetComponentInParent<SpringOscillationController>();
            }
        }

        private void OnEnable()
        {
            if (_interactable != null)
            {
                _interactable.selectEntered.AddListener(OnSelectEntered);
                _interactable.selectExited.AddListener(OnSelectExited);
            }
        }

        private void OnDisable()
        {
            if (_interactable != null)
            {
                _interactable.selectEntered.RemoveListener(OnSelectEntered);
                _interactable.selectExited.RemoveListener(OnSelectExited);
            }
        }

        private void Update()
        {
            if (_interactable != null && oscillationController != null)
            {
                bool canPull = oscillationController.CanPullAndOscillate;
                if (_activeInteractor == null && _interactable.enabled != canPull)
                {
                    _interactable.enabled = canPull;
                }
            }

            if (_activeInteractor != null && oscillationController != null)
            {
                float currentY = _activeInteractor.GetAttachTransform(_interactable).position.y;
                float downwardDelta = Mathf.Max(0.01f, _grabStartY - currentY);
                oscillationController.BeginManualPull(downwardDelta);
            }
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (oscillationController == null || !oscillationController.CanPullAndOscillate) return;
            _activeInteractor = args.interactorObject;
            _grabStartY = _activeInteractor.GetAttachTransform(_interactable).position.y;
            oscillationController.BeginManualPull(0.015f);
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            if (_activeInteractor == null) return;
            _activeInteractor = null;
            oscillationController?.ReleaseToOscillate(0.038f);
        }
    }
}
