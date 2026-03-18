using UnityEngine;

public interface IInteractable
{
    Collider InteractRange { get; set; }
    void Interact(GameObject interactor);
}
