using Resonance.PlayerController;
using Resonance.UI;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteract : MonoBehaviour
{
    [SerializeField] private GameObject player;
    private IInteractable _currentInteractable;
    private PlayerActionsInput _playerActionsInput;

    private void Awake()
    {
        player = gameObject;
        _playerActionsInput = GetComponent<PlayerActionsInput>();
    }

    private void Update()
    {
        if (_playerActionsInput.InteractPressed)
        {
            _playerActionsInput.SetInteractPressedFalse();
            if (_currentInteractable != null)
                _currentInteractable.Interact(player);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        IInteractable interactable = other.GetComponent<IInteractable>() ?? other.GetComponentInParent<IInteractable>();

        if (interactable != null)
        {
            _currentInteractable = interactable;
            ShowPrompt(interactable);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        IInteractable interactable = other.GetComponent<IInteractable>() ?? other.GetComponentInParent<IInteractable>();

        if (interactable != null && interactable == _currentInteractable)
        {
            _currentInteractable = null;
            InteractPromptUI.Instance?.Hide();
        }
    }

    private void ShowPrompt(IInteractable interactable)
    {
        if (InteractPromptUI.Instance == null) return;

        string keyLabel = GetInteractBindingLabel();
        InteractPromptUI.Instance.Show(keyLabel, "RIDE");
    }

    private string GetInteractBindingLabel()
    {
        var controls = Resonance.PlayerController.PlayerInputManager.Instance?.PlayerControls;
        if (controls == null) return "E";

        InputAction interactAction = controls.PlayerActionMap.Interact;
        if (interactAction == null || interactAction.bindings.Count == 0)
            return "E";

        string displayString = interactAction.GetBindingDisplayString(0);
        return string.IsNullOrEmpty(displayString) ? "E" : displayString;
    }
}
