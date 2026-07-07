using UnityEngine;

/// <summary>
/// Applies immersive in-car effects to the player camera while seated:
///   - Road vibration shake (Perlin noise, speed-scaled)
///   - FOV pump on acceleration and high speed
///   - Subtle camera roll lean on turns (desktop only; VR uses HMD tracking)
/// Attach to the Camera child of the Player GameObject.
/// Uses LateUpdate to apply after all movement scripts have run.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CarImmersionController : MonoBehaviour
{
    [Header("References — leave empty to auto-resolve")]
    [Tooltip("The AutoCarController driving the car.")]
    public AutoCarController carController;

    [Tooltip("Tracks whether the player is currently seated.")]
    public PlayerSeatingController seatingController;

    [Header("FOV")]
    [Tooltip("Field of view at rest. Captured from the Camera at Start if left at 0.")]
    public float baseFOV = 0f;

    [Tooltip("Extra degrees added at full speed — gives a sense of velocity.")]
    public float maxFOVBoost = 8f;

    [Tooltip("Lerp speed for FOV transitions.")]
    public float fovSmoothSpeed = 3f;

    [Header("Road Shake")]
    [Tooltip("Maximum displacement per axis in metres.")]
    public float shakeAmplitude = 0.005f;

    [Tooltip("Oscillation rate of the Perlin noise.")]
    public float shakeFrequency = 14f;

    [Header("Turn Lean  (skipped in VR)")]
    [Tooltip("Maximum camera Z-roll in degrees on hard turns.")]
    public float maxLeanDegrees = 4f;

    [Tooltip("Lerp speed for lean in/out.")]
    public float leanSmoothSpeed = 4f;

    // ── Private ───────────────────────────────────────────────────

    private Camera     _cam;
    private Vector3    _baseLocalPos;
    private Quaternion _baseLocalRot;

    private float _noiseOffsetX;
    private float _noiseOffsetY;

    private float _prevCarYaw;
    private float _angularVelocity;
    private float _currentLean;

    private const float AngularVelocitySmoothing = 0.12f;

    // ── Unity lifecycle ───────────────────────────────────────────

    void Awake()
    {
        _cam          = GetComponent<Camera>();
        _baseLocalPos = transform.localPosition;
        _baseLocalRot = transform.localRotation;
        _noiseOffsetX = Random.Range(0f, 100f);
        _noiseOffsetY = Random.Range(0f, 100f);
    }

    void Start()
    {
        if (carController    == null) carController    = FindFirstObjectByType<AutoCarController>();
        if (seatingController == null) seatingController = FindFirstObjectByType<PlayerSeatingController>();

        // Capture the camera's current FOV as the base if not set manually
        if (baseFOV < 1f) baseFOV = _cam.fieldOfView;

        if (carController != null)
            _prevCarYaw = carController.transform.eulerAngles.y;
    }

    // LateUpdate ensures effects are applied after all movement scripts finish
    void LateUpdate()
    {
        bool seated = seatingController != null && seatingController.IsSeated;

        if (!seated)
        {
            ResetEffects();
            return;
        }

        TrackAngularVelocity();

        float speedRatio = carController != null ? carController.SpeedRatio : 0f;

        ApplyFOV(speedRatio);
        ApplyRoadShake(speedRatio);

        if (!IsVR()) ApplyTurnLean(speedRatio);
    }

    // ── Effects ───────────────────────────────────────────────────

    private void ApplyFOV(float speedRatio)
    {
        float targetFOV = baseFOV + speedRatio * maxFOVBoost;
        _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, targetFOV, fovSmoothSpeed * Time.deltaTime);
    }

    private void ApplyRoadShake(float speedRatio)
    {
        if (speedRatio < 0.01f)
        {
            transform.localPosition = Vector3.Lerp(
                transform.localPosition, _baseLocalPos, 10f * Time.deltaTime);
            return;
        }

        float t      = Time.time * shakeFrequency;
        float shakeX = (Mathf.PerlinNoise(t + _noiseOffsetX, 0f) - 0.5f) * 2f * shakeAmplitude * speedRatio;
        float shakeY = (Mathf.PerlinNoise(0f, t + _noiseOffsetY) - 0.5f) * 2f * shakeAmplitude * speedRatio;

        transform.localPosition = _baseLocalPos + new Vector3(shakeX, shakeY, 0f);
    }

    private void ApplyTurnLean(float speedRatio)
    {
        // Negative angular velocity = turning right → lean right (positive roll in Unity)
        float targetLean = Mathf.Clamp(-_angularVelocity / 90f, -1f, 1f)
                           * maxLeanDegrees * speedRatio;

        _currentLean = Mathf.Lerp(_currentLean, targetLean, leanSmoothSpeed * Time.deltaTime);

        // Compose lean on top of the camera's base local rotation
        transform.localRotation = _baseLocalRot * Quaternion.Euler(0f, 0f, _currentLean);
    }

    private void ResetEffects()
    {
        float dt = Time.deltaTime;
        _cam.fieldOfView        = Mathf.Lerp(_cam.fieldOfView, baseFOV, fovSmoothSpeed * dt);
        transform.localPosition = Vector3.Lerp(transform.localPosition, _baseLocalPos, 8f * dt);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, _baseLocalRot, 8f * dt);
    }

    // ── Helpers ───────────────────────────────────────────────────

    private void TrackAngularVelocity()
    {
        if (carController == null) return;

        float currentYaw     = carController.transform.eulerAngles.y;
        float delta          = Mathf.DeltaAngle(_prevCarYaw, currentYaw) / Time.deltaTime;
        _angularVelocity     = Mathf.Lerp(_angularVelocity, delta, AngularVelocitySmoothing);
        _prevCarYaw          = currentYaw;
    }

    /// <summary>Returns true when the app is running in XR mode.</summary>
    private static bool IsVR() =>
        UnityEngine.XR.XRSettings.enabled && UnityEngine.XR.XRSettings.isDeviceActive;
}
