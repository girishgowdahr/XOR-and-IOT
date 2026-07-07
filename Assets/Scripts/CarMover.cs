using UnityEngine;

/// <summary>
/// Drives the car through two stages: to a turn point, then to a final destination.
/// Waits for the gate to open before moving. Notifies PlayerSeatingController
/// when the car starts moving and when it parks.
/// </summary>
public class CarMover : MonoBehaviour
{
    [Header("Path")]
    public GateController gateController;
    public Transform turnPoint;
    public Transform destination;

    [Header("Movement")]
    public float speed     = 5f;
    public float turnSpeed = 2f;

    [Header("Player Seating")]
    [Tooltip("Auto-resolved at Start if left empty.")]
    public PlayerSeatingController playerSeating;

    private int  _stage       = 0;
    private bool _movingFired = false;
    private Rigidbody _rb;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();

        if (playerSeating == null)
            playerSeating = FindFirstObjectByType<PlayerSeatingController>();
    }

    void Update()
    {
        if (gateController == null || !gateController.IsOpen)
            return;

        // Seat the player the first time the car begins moving
        if (!_movingFired)
        {
            _movingFired = true;
            playerSeating?.Seat();
        }

        if (_stage == 0)
        {
            MoveTo(turnPoint.position);

            if (Vector3.Distance(transform.position, turnPoint.position) < 1f)
                _stage = 1;
        }
        else if (_stage == 1)
        {
            MoveTo(destination.position);

            if (Vector3.Distance(transform.position, destination.position) < 1f)
            {
                // Car has parked — unseat the player so they can move freely
                playerSeating?.Unseat();
                enabled = false;
            }
        }
    }

    /// <summary>Moves and rotates the car toward a world-space target position.</summary>
    private void MoveTo(Vector3 target)
    {
        target.y = transform.position.y;

        Vector3 direction = (target - transform.position).normalized;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            Quaternion newRot = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

            if (_rb != null)
                _rb.MoveRotation(newRot);
            else
                transform.rotation = newRot;
        }

        if (_rb != null)
        {
            Vector3 vel = transform.forward * speed;
            vel.y = _rb.linearVelocity.y;
            _rb.linearVelocity = vel;
        }
        else
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                target,
                speed * Time.deltaTime);
        }
    }
}
