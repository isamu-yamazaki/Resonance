using UnityEngine;

public class AutomaticDoor : MonoBehaviour
{
    [SerializeField] private bool moveLeft;
    [SerializeField] private float slideSpeed = 3f;
    private float slideDistance;
    private Transform _door;
    private Vector3 _closedPosition;
    private Vector3 _openPosition;
    private Vector3 _targetPosition;
    private int _triggerCount = 0;
    private Vector3 slideDirection;
    
    void Start()
    {
        _door = transform.GetChild(0);
        _closedPosition = _door.localPosition;

        //get slide distance from door's width
        Collider doorCollider = _door.GetComponent<Collider>();
        if (doorCollider != null)
        {
            slideDistance = _door.localScale.x;
        }
        
        if (moveLeft)
        {
            slideDirection = Vector3.left;
        }
        else
        {
            slideDirection = Vector3.right;
        }
        
        _openPosition = _closedPosition + slideDirection * slideDistance;
        _targetPosition = _closedPosition;
    }
    
    void Update()
    {
        _door.localPosition = Vector3.MoveTowards(
            _door.localPosition,
            _targetPosition,
            slideSpeed * Time.deltaTime
        );
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // if other is a player, slide the door to the side
        if (other.CompareTag("Player"))
        {
            _triggerCount++;
            _targetPosition = _openPosition;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // if door is moved, put it back
        if (other.CompareTag("Player"))
        {
            _triggerCount--;
            if (_triggerCount <= 0)
            {
                _triggerCount = 0;
                _targetPosition = _closedPosition;
            }
        }
    }
}
