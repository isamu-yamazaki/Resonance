using Resonance.PlayerController;
using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    //update check for interact press
    //check if colliding with tagged interact collider
    //assume interactable object, call Interact and pass in player

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
            Debug.Log("Interact pressed, _currentInteractable is: " + _currentInteractable);
            _playerActionsInput.SetInteractPressedFalse();
            if (_currentInteractable != null)
            {
                Debug.Log("Interactable attempted interacted");
                _currentInteractable.Interact(player);
                Debug.Log("Interactable interacted");
            }
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Check if object has an IInteractable component
        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable == null)
        {
            interactable = other.GetComponentInParent<IInteractable>();
        }

        if (interactable != null)
        {
            _currentInteractable = interactable;
            Debug.Log("Interactable in range: " + other.gameObject.name);
        }
    }

    void OnTriggerExit(Collider other)
    {
        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable == null)
        {
            interactable = other.GetComponentInParent<IInteractable>();
        }

        if (interactable != null && interactable == _currentInteractable)
        {
            _currentInteractable = null;
            Debug.Log("Left interactable range");
        }
    }
}
