using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Autonomous vehicle that follows an ordered waypoint path, stops before a
/// closed smart gate, waits for it to open, then continues to the parking spot.
/// Fires C# events at key journey milestones consumed by ExperienceManager.
/// </summary>
public class AutoCarController : MonoBehaviour
{
    // ── Path ──────────────────────────────────────────────────────
    [Header("Path Waypoints")]
    [Tooltip("Ordered transforms the car drives through.")]
    public Transform[] waypoints;
    [Tooltip("Distance (metres) within which a waypoint counts as reached.")]
    public float arrivalThreshold = 0.8f;

    // ── Movement ──────────────────────────────────────────────────
    [Header("Movement")]
    public float driveSpeed   = 5f;
    public float acceleration = 3f;
    [Tooltip("Angular speed multiplier for Slerp rotation.")]
    public float turnSpeed    = 3f;

    // ── Gate ──────────────────────────────────────────────────────
    [Header("Gate")]
    [Tooltip("The SmartGateController this vehicle interacts with.")]
    public SmartGateController gateController;
    [Tooltip("The waypoint at which the car stops and waits for the gate.")]
    public Transform gateStopWaypoint;
    [Tooltip("Seconds to pause after the gate finishes opening before moving.")]
    public float gateOpenPauseSeconds = 0.8f;

    // ── Wheels ────────────────────────────────────────────────────
    [Header("Wheel Visuals (optional)")]
    public Transform frontLeftWheel;
    public Transform frontRightWheel;
    public Transform rearLeftWheel;
    public Transform rearRightWheel;
    [Tooltip("Degrees per second the wheel spins at full speed.")]
    public float wheelRotationSpeed = 360f;

    // ── Engine Audio ──────────────────────────────────────────────
    [Header("Engine Audio")]
    public AudioSource engineAudio;
    [Tooltip("Pitch at zero speed.")]
    public float idlePitch = 0.6f;
    [Tooltip("Pitch at full driveSpeed.")]
    public float maxPitch  = 1.6f;

    // ── Events ────────────────────────────────────────────────────

    /// <summary>Fired on the first Update tick the car is in motion.</summary>
    public event Action OnCarApproaching;

    /// <summary>Fired when the car decelerates to a stop at the gate waypoint.</summary>
    public event Action OnCarStoppedAtGate;

    /// <summary>Fired when the car resumes after the gate opens.</summary>
    public event Action OnCarEntering;

    /// <summary>Fired once the car reaches its final parking waypoint.</summary>
    public event Action OnCarParked;

    // ── Private state ─────────────────────────────────────────────
    private int   _waypointIndex   = 0;
    private float _currentSpeed    = 0f;
    private bool  _waitingAtGate   = false;
    private bool  _parked          = false;
    private bool  _approachFired   = false;
    private bool  _stoppedFired    = false;

    // ── Public state ──────────────────────────────────────────────

    /// <summary>True after the car reaches its last waypoint.</summary>
    public bool IsParked => _parked;

    /// <summary>Normalised 0-1 speed ratio.</summary>
    public float SpeedRatio => driveSpeed > 0f ? _currentSpeed / driveSpeed : 0f;

    /// <summary>True while the car is waiting for the gate.</summary>
    public bool IsWaitingAtGate => _waitingAtGate;

    // ── Unity lifecycle ───────────────────────────────────────────

    void Update()
    {
        if (_parked) return;
        DriveAlongPath();
        SpinWheels();
        UpdateEngineAudio();
    }

    // ── Driving ───────────────────────────────────────────────────

    private void DriveAlongPath()
    {
        if (_waypointIndex >= waypoints.Length)
        {
            Park();
            return;
        }

        // First motion
        if (!_approachFired && _currentSpeed < 0.05f)
        {
            _approachFired = true;
            OnCarApproaching?.Invoke();
        }

        Transform target = waypoints[_waypointIndex];
        bool isAtGateStop = gateStopWaypoint != null && target == gateStopWaypoint;

        if (isAtGateStop && gateController != null && !gateController.IsOpen)
        {
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, 0f, acceleration * 2f * Time.deltaTime);
            MoveTowards(target.position, _currentSpeed);

            if (!_stoppedFired && _currentSpeed < 0.05f)
            {
                _stoppedFired = true;
                OnCarStoppedAtGate?.Invoke();
            }

            if (!_waitingAtGate)
            {
                _waitingAtGate = true;
                StartCoroutine(WaitForGateToOpen());
            }
            return;
        }

        _currentSpeed = Mathf.MoveTowards(_currentSpeed, driveSpeed, acceleration * Time.deltaTime);
        MoveTowards(target.position, _currentSpeed);

        if (Vector3.Distance(transform.position, target.position) < arrivalThreshold)
        {
            _waypointIndex++;
            _waitingAtGate = false;
            _stoppedFired  = false;
        }
    }

    private IEnumerator WaitForGateToOpen()
    {
        while (gateController != null && !gateController.IsOpen)
            yield return null;

        OnCarEntering?.Invoke();
        yield return new WaitForSeconds(gateOpenPauseSeconds);
        _waitingAtGate = false;
        _waypointIndex++;
    }

    private void MoveTowards(Vector3 targetPos, float speed)
    {
        targetPos.y = transform.position.y;
        Vector3 direction = targetPos - transform.position;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        }

        transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
    }

    private void Park()
    {
        if (_parked) return;
        _parked       = true;
        _currentSpeed = 0f;
        if (engineAudio != null) engineAudio.Stop();
        OnCarParked?.Invoke();
    }

    // ── Wheels ────────────────────────────────────────────────────

    private void SpinWheels()
    {
        float delta = SpeedRatio * wheelRotationSpeed * Time.deltaTime;
        RotateWheel(frontLeftWheel,  delta);
        RotateWheel(frontRightWheel, delta);
        RotateWheel(rearLeftWheel,   delta);
        RotateWheel(rearRightWheel,  delta);
    }

    private void RotateWheel(Transform w, float delta)
    {
        if (w != null) w.Rotate(Vector3.right, delta, Space.Self);
    }

    // ── Engine audio ──────────────────────────────────────────────

    private void UpdateEngineAudio()
    {
        if (engineAudio == null) return;
        if (!engineAudio.isPlaying) engineAudio.Play();
        engineAudio.pitch = Mathf.Lerp(idlePitch, maxPitch, SpeedRatio);
    }

    // ── Gizmos ────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        if (waypoints == null) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            Gizmos.DrawSphere(waypoints[i].position, 0.3f);
            if (i < waypoints.Length - 1 && waypoints[i + 1] != null)
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
        }
        if (gateStopWaypoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(gateStopWaypoint.position, 0.6f);
        }
    }
}
