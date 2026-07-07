using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manual car controller driven by Quest (or any XR) thumbsticks.
///
/// Controls:
///   Left stick  Y  →  throttle (forward / reverse)
///   Right stick X  →  steering
///   Keyboard fallback: WASD works automatically (same Input Action)
///
/// Automatically disables CarMover when seated and restores it on exit.
/// Attach to the car root GameObject.
/// </summary>
public class QuestCarController : MonoBehaviour
{
    [Header("Input Actions")]
    [Tooltip("Left thumbstick — Y = throttle, X = steering fallback.")]
    public InputActionReference moveAction;

    [Tooltip("Right thumbstick — X = steering (preferred over left stick X).")]
    public InputActionReference lookAction;

    [Header("Driving")]
    [Tooltip("Maximum forward / reverse speed in units per second.")]
    public float maxSpeed = 8f;

    [Tooltip("How quickly the car reaches max speed.")]
    public float acceleration = 6f;

    [Tooltip("How quickly the car slows with no throttle input.")]
    public float deceleration = 10f;

    [Tooltip("Degrees per second the car turns at full stick deflection.")]
    public float turnSpeed = 70f;

    [Tooltip("Minimum speed before steering activates.")]
    public float minSpeedToSteer = 0.2f;

    [Header("Wheel Visuals (optional)")]
    public Transform frontLeftWheel;
    public Transform frontRightWheel;
    public Transform rearLeftWheel;
    public Transform rearRightWheel;

    [Tooltip("Degrees per second wheels spin at max speed.")]
    public float wheelSpinSpeed = 400f;

    [Header("References — leave empty to auto-resolve")]
    public PlayerSeatingController seatingController;
    public CarMover carMover;

    private float _currentSpeed;
    private bool  _wasSeated;
    private Rigidbody _rb;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();

        if (seatingController == null)
            seatingController = FindFirstObjectByType<PlayerSeatingController>();

        if (carMover == null)
            carMover = GetComponent<CarMover>();

        EnableAction(moveAction);
        EnableAction(lookAction);
    }

    void Update()
    {
        bool seated = seatingController != null && seatingController.IsSeated;

        // Toggle CarMover so it doesn't fight manual input
        if (seated != _wasSeated)
        {
            if (carMover != null) carMover.enabled = !seated;
            if (!seated) _currentSpeed = 0f;
            _wasSeated = seated;
        }

        if (!seated) return;

        Drive();
        SpinWheels();
    }

    private void Drive()
    {
        Vector2 moveInput = ReadVector2(moveAction);
        Vector2 lookInput = ReadVector2(lookAction);

        float throttle = moveInput.y;
        // Right stick X preferred for steering; left stick X as fallback
        float steer = lookInput.x != 0f ? lookInput.x : moveInput.x;

        // Accelerate / coast
        float targetSpeed = Mathf.Abs(throttle) > 0.05f ? throttle * maxSpeed : 0f;
        float rate        = Mathf.Abs(throttle) > 0.05f ? acceleration : deceleration;
        _currentSpeed     = Mathf.MoveTowards(_currentSpeed, targetSpeed, rate * Time.deltaTime);

        // Steer — flip direction in reverse so it feels natural
        if (Mathf.Abs(_currentSpeed) > minSpeedToSteer)
        {
            float steerDir = _currentSpeed > 0f ? 1f : -1f;
            float yaw = steer * turnSpeed * steerDir * Time.deltaTime;

            if (_rb != null)
                _rb.MoveRotation(_rb.rotation * Quaternion.Euler(0f, yaw, 0f));
            else
                transform.Rotate(Vector3.up, yaw);
        }

        // Move — use Rigidbody velocity so colliders stop the car
        if (_rb != null)
        {
            Vector3 vel = transform.forward * _currentSpeed;
            vel.y = _rb.linearVelocity.y;
            _rb.linearVelocity = vel;
        }
        else
        {
            Vector3 move = transform.forward * _currentSpeed * Time.deltaTime;
            move.y = 0f;
            transform.position += move;
        }
    }

    private void SpinWheels()
    {
        float delta = (_currentSpeed / maxSpeed) * wheelSpinSpeed * Time.deltaTime;
        RotateWheel(frontLeftWheel,  delta);
        RotateWheel(frontRightWheel, delta);
        RotateWheel(rearLeftWheel,   delta);
        RotateWheel(rearRightWheel,  delta);
    }

    private static void RotateWheel(Transform w, float delta)
    {
        if (w != null) w.Rotate(Vector3.right, delta, Space.Self);
    }

    private Vector2 ReadVector2(InputActionReference actionRef)
    {
        if (actionRef == null || actionRef.action == null) return Vector2.zero;
        return actionRef.action.ReadValue<Vector2>();
    }

    private static void EnableAction(InputActionReference actionRef)
    {
        if (actionRef == null || actionRef.action == null) return;
        if (!actionRef.action.enabled) actionRef.action.Enable();
    }
}
