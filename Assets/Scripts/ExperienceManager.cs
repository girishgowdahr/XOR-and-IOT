using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Orchestrates the ten-step smart gate VR experience:
///   1  Idle          — player standing outside, car visible at road
///   2  Approaching   — car driving toward gate
///   3  Detected      — gate sensor fires on the vehicle
///   4  Authenticating — 2-3 s biometric/RFID scan animation
///   5  AccessGranted  — gate begins opening
///   6  GateOpening   — panels sliding, car waiting
///   7  CarEntering   — car drives through
///   8  GateClosing   — gate closes behind the car
///   9  CarParking    — car navigates to parking spot
///  10  PlayerFree    — player exits and explores freely
///
/// Subscribes to events from SmartGateController and AutoCarController.
/// Broadcasts its own OnStateChanged event consumed by StatusHUD.
/// </summary>
public class ExperienceManager : MonoBehaviour
{
    // ── State ─────────────────────────────────────────────────────

    public enum State
    {
        Idle,
        CarApproaching,
        VehicleDetected,
        Authenticating,
        AccessGranted,
        GateOpening,
        CarEntering,
        GateClosing,
        CarParking,
        CarParked,
        PlayerFree
    }

    // ── References ────────────────────────────────────────────────
    [Header("System References")]
    public SmartGateController gateController;
    public AutoCarController   carController;
    public CameraManager       cameraManager;

    [Header("Player")]
    [Tooltip("Root transform of the player rig (used for camera / position reference only).")]
    public Transform playerRoot;

    [Header("Timing")]
    [Tooltip("Seconds after parking before player-free mode begins.")]
    public float postParkDelay = 2f;
    [Tooltip("Seconds the 'Access Granted' message stays before transitioning.")]
    public float accessGrantedHoldSeconds = 1.2f;

    [Header("Optional: Switch to FP on park")]
    [Tooltip("If true, camera automatically switches to first-person when the car parks.")]
    public bool autoSwitchToFPOnPark = true;
[Header("Player Seating (optional)")]
[Tooltip("When assigned, the player is physically seated at CarEntering and freed at PlayerFree.")]
public PlayerSeatingController seatingController;

    // ── Events ────────────────────────────────────────────────────

    /// <summary>Fired whenever the experience state changes.</summary>
    public event Action<State, State> OnStateChanged;   // (prev, next)

    // ── Public state ──────────────────────────────────────────────

    public State CurrentState { get; private set; } = State.Idle;

    // ── Unity lifecycle ───────────────────────────────────────────

    void Start()
    {
        SubscribeToEvents();
        EnterState(State.Idle);
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    // ── Event wiring ──────────────────────────────────────────────

    private void SubscribeToEvents()
    {
        if (carController  != null)
        {
            carController.OnCarApproaching    += HandleCarApproaching;
            carController.OnCarStoppedAtGate  += HandleCarStopped;
            carController.OnCarEntering       += HandleCarEntering;
            carController.OnCarParked         += HandleCarParked;
        }

        if (gateController != null)
        {
            gateController.OnVehicleDetected  += HandleVehicleDetected;
            gateController.OnAccessGranted    += HandleAccessGranted;
            gateController.OnAccessDenied     += HandleAccessDenied;
            gateController.OnGateFullyOpen    += HandleGateFullyOpen;
            gateController.OnGateFullyClosed  += HandleGateFullyClosed;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (carController  != null)
        {
            carController.OnCarApproaching    -= HandleCarApproaching;
            carController.OnCarStoppedAtGate  -= HandleCarStopped;
            carController.OnCarEntering       -= HandleCarEntering;
            carController.OnCarParked         -= HandleCarParked;
        }

        if (gateController != null)
        {
            gateController.OnVehicleDetected  -= HandleVehicleDetected;
            gateController.OnAccessGranted    -= HandleAccessGranted;
            gateController.OnAccessDenied     -= HandleAccessDenied;
            gateController.OnGateFullyOpen    -= HandleGateFullyOpen;
            gateController.OnGateFullyClosed  -= HandleGateFullyClosed;
        }
    }

    // ── Event handlers ────────────────────────────────────────────

    private void HandleCarApproaching()    => TryTransition(State.CarApproaching);
    private void HandleCarStopped()        => TryTransition(State.VehicleDetected);
    private void HandleVehicleDetected()   => TryTransition(State.Authenticating);
    private void HandleAccessGranted()     => StartCoroutine(AccessGrantedSequence());
    private void HandleAccessDenied()      => Debug.Log("[Experience] Access denied — sequence unchanged.");
    private void HandleGateFullyOpen()     => TryTransition(State.CarEntering);
    private void HandleGateFullyClosed()   => TryTransition(State.CarParking);
    private void HandleCarEntering()       => TryTransition(State.GateOpening);
    private void HandleCarParked()         => StartCoroutine(PostParkSequence());

    // ── Transitions ───────────────────────────────────────────────

    /// <summary>Advances the state machine if the transition is valid from the current state.</summary>
    private void TryTransition(State next)
    {
        if (!IsValidTransition(CurrentState, next))
        {
            Debug.LogWarning($"[ExperienceManager] Invalid transition: {CurrentState} → {next}");
            return;
        }
        EnterState(next);
    }

    private bool IsValidTransition(State from, State to)
    {
        return (from, to) switch
        {
            (State.Idle,            State.CarApproaching)  => true,
            (State.CarApproaching,  State.VehicleDetected) => true,
            (State.VehicleDetected, State.Authenticating)  => true,
            (State.Authenticating,  State.AccessGranted)   => true,
            (State.AccessGranted,   State.GateOpening)     => true,
            (State.GateOpening,     State.CarEntering)     => true,
            (State.CarEntering,     State.GateClosing)     => true,
            (State.GateClosing,     State.CarParking)      => true,
            (State.CarParking,      State.CarParked)       => true,
            (State.CarParked,       State.PlayerFree)      => true,
            _ => false
        };
    }

    private void EnterState(State next)
    {
        State prev    = CurrentState;
        CurrentState  = next;
        OnStateChanged?.Invoke(prev, next);
        Debug.Log($"[ExperienceManager] {prev} → {next}");
        ExecuteStateEntry(next);
    }

    private void ExecuteStateEntry(State state)
{
    switch (state)
    {
        case State.Idle:
            break;

        case State.CarApproaching:
            break;

        case State.VehicleDetected:
            break;

        case State.Authenticating:
            break;

        case State.AccessGranted:
            break;

        case State.GateOpening:
            break;

        case State.CarEntering:
            if (seatingController != null)
            {
                // Player's own FP camera rides with the car — no separate camera switch needed
                seatingController.Seat();
            }
            else if (cameraManager != null)
            {
                cameraManager.SetMode(CameraManager.CameraMode.CarInterior);
            }
            break;

        case State.GateClosing:
            // Only switch camera if the player is NOT physically seated in the car
            if ((seatingController == null || !seatingController.IsSeated) && cameraManager != null)
                cameraManager.SetMode(CameraManager.CameraMode.FirstPerson);
            break;

        case State.CarParking:
            break;

        case State.CarParked:
            break;

        case State.PlayerFree:
            // Unseat the player now the car has parked
            if (seatingController != null && seatingController.IsSeated)
                seatingController.Unseat();

            if (autoSwitchToFPOnPark && cameraManager != null)
                cameraManager.SetMode(CameraManager.CameraMode.FirstPerson);
            break;
    }
}


    // ── Coroutine helpers ─────────────────────────────────────────

    private IEnumerator AccessGrantedSequence()
    {
        TryTransition(State.AccessGranted);
        yield return new WaitForSeconds(accessGrantedHoldSeconds);
        TryTransition(State.GateOpening);
    }

    private IEnumerator PostParkSequence()
    {
        TryTransition(State.CarParked);
        yield return new WaitForSeconds(postParkDelay);
        TryTransition(State.PlayerFree);
    }

    /// <summary>Skip ahead to PlayerFree from any state (debug / security room terminal).</summary>
    public void ForcePlayerFree()
    {
        CurrentState = State.CarParked;
        EnterState(State.PlayerFree);
    }
}
