using System;
using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Smart automated gate controller with vehicle detection, authentication delay,
/// authorization checking, auto-open/close, access-denied HUD, and alarm.
/// Fires C# events consumed by ExperienceManager and StatusHUD.
/// </summary>
public class SmartGateController : MonoBehaviour
{
    // ── Gate Transforms ───────────────────────────────────────────
    [Header("Gate Transforms")]
    public Transform leftGate;
    public Transform rightGate;

    [Header("Movement")]
    [Tooltip("Slide speed in units per second.")]
    public float speed = 2f;
    [Tooltip("Distance the left panel slides inward to close.")]
    public float leftCloseOffset = 3.5f;
    [Tooltip("Distance the right panel slides inward to close.")]
    public float rightCloseOffset = 3.5f;

    // ── Authorization ─────────────────────────────────────────────
    [Header("Authorization")]
    [Tooltip("Unity tags that identify authorized vehicles.")]
    public string[] authorizedTags = { "AuthorizedVehicle" };
    [Tooltip("Simulated authentication scan duration in seconds.")]
    public float authenticationDelay = 2.5f;
    [Tooltip("Seconds to wait after the vehicle clears before auto-closing.")]
    public float autoCloseDelay = 4f;

    // ── UI ────────────────────────────────────────────────────────
    [Header("UI")]
    [Tooltip("TMP label showing current gate status.")]
    public TMP_Text statusLabel;
    [Tooltip("World-space panel shown when access is denied.")]
    public GameObject accessDeniedPanel;
    [Tooltip("Seconds the access-denied panel stays visible.")]
    public float accessDeniedDuration = 3f;

    // ── Alarm ─────────────────────────────────────────────────────
    [Header("Alarm")]
    [Tooltip("Point light that flashes red during an alarm.")]
    public Light warningLight;
    [Tooltip("AudioSource that plays the alarm clip.")]
    public AudioSource alarmAudio;
    [Tooltip("Seconds per warning-light toggle.")]
    public float alarmFlashRate = 0.4f;

    [Header("Gate Motor Audio")]
    [Tooltip("AudioSource that plays while the gate slides.")]
    public AudioSource motorAudio;

    // ── Events ────────────────────────────────────────────────────

    /// <summary>Fired when an authorized vehicle enters the detection trigger.</summary>
    public event Action OnVehicleDetected;

    /// <summary>Fired after the authentication delay — gate is about to open.</summary>
    public event Action OnAccessGranted;

    /// <summary>Fired when an unauthorized vehicle is detected.</summary>
    public event Action OnAccessDenied;

    /// <summary>Fired once the gate panels reach the fully-open position.</summary>
    public event Action OnGateFullyOpen;

    /// <summary>Fired once the gate panels reach the fully-closed position.</summary>
    public event Action OnGateFullyClosed;

    /// <summary>Fired when the authorized vehicle exits the trigger zone.</summary>
    public event Action OnVehicleCleared;

    // ── Private state ─────────────────────────────────────────────
    private Vector3 _leftClosed, _rightClosed;
    private Vector3 _leftOpen,   _rightOpen;

    private bool _isOpen    = false;
    private bool _isMoving  = false;
    private bool _wasOpen   = false;

    private int  _vehiclesInsideTrigger = 0;
    private bool _alarmActive           = false;
    private float _flashTimer;

    private Coroutine _authCoroutine;
    private Coroutine _autoCloseCoroutine;
    private Coroutine _accessDeniedCoroutine;

    private const string StatusOpen   = "ACCESS GRANTED — GATE OPEN";
    private const string StatusClosed = "GATE CLOSED";
    private const float  FullyMovedEpsilon = 0.01f;

    // ── Public state ──────────────────────────────────────────────

    /// <summary>True when the gate is currently set to the open state.</summary>
    public bool IsOpen => _isOpen;

    /// <summary>True while the gate panels are in motion.</summary>
    public bool IsMoving => _isMoving;

    // ── Unity lifecycle ───────────────────────────────────────────

    void Start()
    {
        _leftOpen  = leftGate.localPosition;
        _rightOpen = rightGate.localPosition;

        _leftClosed  = _leftOpen  + new Vector3( leftCloseOffset,  0f, 0f);
        _rightClosed = _rightOpen + new Vector3(-rightCloseOffset, 0f, 0f);

        leftGate.localPosition  = _leftClosed;
        rightGate.localPosition = _rightClosed;
        _isOpen  = false;
        _wasOpen = false;

        if (accessDeniedPanel != null) accessDeniedPanel.SetActive(false);
        if (warningLight      != null) warningLight.enabled = false;

        UpdateStatusLabel();
    }

    void Update()
    {
        HandleDebugKeyboard();

        bool wasMoving = _isMoving;
        _isMoving = MoveGates();
        HandleMotorAudio(wasMoving);
        DetectFullyOpenClosed();

        if (_alarmActive) FlashWarningLight();
    }

    // ── Trigger detection ─────────────────────────────────────────

    void OnTriggerEnter(Collider other)
    {
        if (IsAuthorized(other.tag))
        {
            _vehiclesInsideTrigger++;
            CancelAutoClose();

            if (_authCoroutine == null)
                _authCoroutine = StartCoroutine(AuthenticateAndOpen());
        }
        else if (IsKnownVehicleTag(other.tag))
        {
            TriggerAccessDenied();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsAuthorized(other.tag)) return;

        _vehiclesInsideTrigger = Mathf.Max(0, _vehiclesInsideTrigger - 1);

        if (_vehiclesInsideTrigger == 0)
        {
            OnVehicleCleared?.Invoke();
            _autoCloseCoroutine = StartCoroutine(AutoCloseAfterDelay());
        }
    }

    // ── Public control API ────────────────────────────────────────

    /// <summary>Slides both gate panels to the open position.</summary>
    public void OpenGate()
    {
        if (_isOpen) return;
        _isOpen = true;
        UpdateStatusLabel();
    }

    /// <summary>Slides both gate panels to the closed position.</summary>
    public void CloseGate()
    {
        if (!_isOpen) return;
        _isOpen = false;
        UpdateStatusLabel();
    }

    // ── Auth sequence ─────────────────────────────────────────────

    private IEnumerator AuthenticateAndOpen()
    {
        OnVehicleDetected?.Invoke();

        yield return new WaitForSeconds(authenticationDelay);

        OnAccessGranted?.Invoke();
        OpenGate();
        _authCoroutine = null;
    }

    // ── Internal helpers ──────────────────────────────────────────

    private bool IsAuthorized(string tag)
    {
        foreach (string t in authorizedTags)
            if (tag == t) return true;
        return false;
    }

    private bool IsKnownVehicleTag(string tag) =>
        tag == "Vehicle" || tag == "UnauthorizedVehicle";

    private void CancelAutoClose()
    {
        if (_autoCloseCoroutine == null) return;
        StopCoroutine(_autoCloseCoroutine);
        _autoCloseCoroutine = null;
    }

    private IEnumerator AutoCloseAfterDelay()
    {
        yield return new WaitForSeconds(autoCloseDelay);
        if (_vehiclesInsideTrigger == 0) CloseGate();
        _autoCloseCoroutine = null;
    }

    private void TriggerAccessDenied()
    {
        OnAccessDenied?.Invoke();
        if (_accessDeniedCoroutine != null) StopCoroutine(_accessDeniedCoroutine);
        _accessDeniedCoroutine = StartCoroutine(AccessDeniedSequence());
    }

    private IEnumerator AccessDeniedSequence()
    {
        if (accessDeniedPanel != null) accessDeniedPanel.SetActive(true);
        StartAlarm();
        yield return new WaitForSeconds(accessDeniedDuration);
        if (accessDeniedPanel != null) accessDeniedPanel.SetActive(false);
        StopAlarm();
        _accessDeniedCoroutine = null;
    }

    private void StartAlarm()
    {
        _alarmActive = true;
        if (alarmAudio != null && !alarmAudio.isPlaying) alarmAudio.Play();
    }

    private void StopAlarm()
    {
        _alarmActive = false;
        if (alarmAudio != null) alarmAudio.Stop();
        if (warningLight != null) warningLight.enabled = false;
    }

    private void FlashWarningLight()
    {
        if (warningLight == null) return;
        _flashTimer += Time.deltaTime;
        if (_flashTimer < alarmFlashRate) return;
        _flashTimer = 0f;
        warningLight.enabled = !warningLight.enabled;
    }

    private bool MoveGates()
    {
        float step        = speed * Time.deltaTime;
        Vector3 leftTarget  = _isOpen ? _leftOpen  : _leftClosed;
        Vector3 rightTarget = _isOpen ? _rightOpen : _rightClosed;

        leftGate.localPosition  = Vector3.MoveTowards(leftGate.localPosition,  leftTarget,  step);
        rightGate.localPosition = Vector3.MoveTowards(rightGate.localPosition, rightTarget, step);

        return Vector3.Distance(leftGate.localPosition,  leftTarget)  > FullyMovedEpsilon ||
               Vector3.Distance(rightGate.localPosition, rightTarget) > FullyMovedEpsilon;
    }

    /// <summary>Fires OnGateFullyOpen / OnGateFullyClosed on the frame the panels settle.</summary>
    private void DetectFullyOpenClosed()
    {
        if (_isMoving) { _wasOpen = _isOpen; return; }

        if (_isOpen && !_wasOpen)
        {
            _wasOpen = true;
            OnGateFullyOpen?.Invoke();
        }
        else if (!_isOpen && _wasOpen)
        {
            _wasOpen = false;
            OnGateFullyClosed?.Invoke();
        }
    }

    private void HandleMotorAudio(bool wasMoving)
    {
        if (motorAudio == null) return;
        if ( _isMoving && !wasMoving) motorAudio.Play();
        if (!_isMoving &&  wasMoving) motorAudio.Stop();
    }

    private void UpdateStatusLabel()
    {
        if (statusLabel == null) return;
        statusLabel.text = _isOpen ? StatusOpen : StatusClosed;
    }

    private void HandleDebugKeyboard()
    {
#if UNITY_EDITOR
        if (UnityEngine.InputSystem.Keyboard.current == null) return;
        if (UnityEngine.InputSystem.Keyboard.current.oKey.wasPressedThisFrame) OpenGate();
        if (UnityEngine.InputSystem.Keyboard.current.cKey.wasPressedThisFrame) CloseGate();
#endif
    }
}
