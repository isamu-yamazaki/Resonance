using UnityEngine;

public interface IInteractable
{
    Collider InteractRange { get; }
    void Interact(GameObject interactor);
}
