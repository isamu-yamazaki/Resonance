using UnityEngine;

public class HologramRotator : MonoBehaviour
{
    [SerializeField] private Vector3 rotationAxis = Vector3.up;
    [SerializeField] private float rotationSpeed = 45f;
    [SerializeField] private bool rotateInWorldSpace = false;

    private void Update()
    {
        Space space = rotateInWorldSpace ? Space.World : Space.Self;
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, space);
    }
}
