using UnityEngine;

/// <summary>
/// Optional controller for the overhead drone/top-view camera.
/// Allows panning and zooming while the player is in Drone camera mode.
/// Works with both keyboard (Arrow Keys / +/-) and gamepad (right stick + triggers).
/// </summary>
[RequireComponent(typeof(Camera))]
public class DroneCamera : MonoBehaviour
{
    [Header("Pan")]
    public float panSpeed = 10f;
    [Tooltip("World-space bounds for horizontal panning.")]
    public Rect panBounds = new Rect(-50f, -50f, 100f, 100f);

    [Header("Altitude")]
    public float minAltitude = 8f;
    public float maxAltitude = 60f;
    public float zoomSpeed   = 10f;

    [Header("Smoothing")]
    public float smoothing = 8f;

    // ── Private ───────────────────────────────────────────────────
    private Vector3 _targetPosition;

    void Start()
    {
        _targetPosition = transform.position;
    }

    void Update()
    {
        HandleInput();
        transform.position = Vector3.Lerp(transform.position, _targetPosition, smoothing * Time.deltaTime);
    }

    private void HandleInput()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        float zoom = 0f;

        if (Input.GetKey(KeyCode.KeypadPlus)  || Input.GetKey(KeyCode.E)) zoom = -1f;
        if (Input.GetKey(KeyCode.KeypadMinus) || Input.GetKey(KeyCode.Q)) zoom = +1f;

        Vector3 pan = new Vector3(h, 0f, v) * panSpeed * Time.deltaTime;
        _targetPosition += pan;

        float newY = Mathf.Clamp(_targetPosition.y + zoom * zoomSpeed * Time.deltaTime,
                                 minAltitude, maxAltitude);
        _targetPosition.y = newY;

        // Clamp XZ to pan bounds
        _targetPosition.x = Mathf.Clamp(_targetPosition.x, panBounds.xMin, panBounds.xMax);
        _targetPosition.z = Mathf.Clamp(_targetPosition.z, panBounds.yMin, panBounds.yMax);
    }
}
