using UnityEngine;

/// <summary>
/// Positions a camera at the driver seat of the vehicle and provides
/// a limited look-around angle (head turn) for the in-car view.
/// Attach to a child GameObject of the car named "DriverSeatCamera".
/// </summary>
[RequireComponent(typeof(Camera))]
public class CarInteriorCamera : MonoBehaviour
{
    [Header("Head Look")]
    [Tooltip("Maximum left/right head-turn angle in degrees.")]
    public float maxYaw   = 60f;
    [Tooltip("Maximum up/down head-tilt angle in degrees.")]
    public float maxPitch = 25f;
    [Tooltip("How smoothly the head turns.")]
    public float lookSmoothing = 8f;

    [Header("Seat Position")]
    [Tooltip("Local offset from the car root to the driver eye position.")]
    public Vector3 seatLocalOffset = new Vector3(-0.35f, 1.1f, 0.15f);

    // ── Private state ─────────────────────────────────────────────
    private float _targetYaw;
    private float _targetPitch;
    private float _currentYaw;
    private float _currentPitch;

    void Start()
    {
        transform.localPosition = seatLocalOffset;
    }

    void Update()
    {
        if (!GetComponent<Camera>().enabled) return;

        ReadInput();
        ApplyRotation();
    }

    private void ReadInput()
    {
        _targetYaw   += Input.GetAxis("Mouse X") * 3f;
        _targetPitch -= Input.GetAxis("Mouse Y") * 3f;

        _targetYaw   = Mathf.Clamp(_targetYaw,   -maxYaw,   maxYaw);
        _targetPitch = Mathf.Clamp(_targetPitch, -maxPitch, maxPitch);
    }

    private void ApplyRotation()
    {
        _currentYaw   = Mathf.Lerp(_currentYaw,   _targetYaw,   lookSmoothing * Time.deltaTime);
        _currentPitch = Mathf.Lerp(_currentPitch, _targetPitch, lookSmoothing * Time.deltaTime);

        transform.localRotation = Quaternion.Euler(_currentPitch, _currentYaw, 0f);
    }
}
