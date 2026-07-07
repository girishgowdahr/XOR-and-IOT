using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Manages smooth switching between four camera modes:
/// First Person, Car Interior, CCTV (cycled), and Drone/Top View.
/// Press 1-4 on keyboard or call SetMode() from any script.
/// </summary>
public class CameraManager : MonoBehaviour
{
    // ── Camera Mode ───────────────────────────────────────────────
    public enum CameraMode
    {
        FirstPerson  = 0,
        CarInterior  = 1,
        CCTV         = 2,
        Drone        = 3
    }

    // ── References ────────────────────────────────────────────────
    [Header("Cameras")]
    [Tooltip("Player's first-person camera (child of Player rig).")]
    public Camera firstPersonCamera;
    [Tooltip("Camera mounted inside the car at the driver seat.")]
    public Camera carInteriorCamera;
    [Tooltip("Fixed security CCTV cameras in the scene.")]
    public Camera[] cctvCameras;
    [Tooltip("Overhead drone / top-view camera.")]
    public Camera droneCamera;

    [Header("Player Reference")]
    [Tooltip("Player root — movement will be disabled in non-FP modes.")]
    public MonoBehaviour playerMovementScript;

    [Header("Transition")]
    [Tooltip("Seconds for the screen-fade transition between cameras.")]
    public float fadeDuration = 0.35f;

    [Header("CCTV HUD Label (optional)")]
    [Tooltip("TMP label in the overlay canvas that shows the current CCTV name.")]
    public TMP_Text cctvLabel;

    // ── Private state ─────────────────────────────────────────────
    private CameraMode _currentMode  = CameraMode.FirstPerson;
    private int        _cctvIndex    = 0;
    private bool       _transitioning = false;

    // ── Unity lifecycle ───────────────────────────────────────────

    void Start()
    {
        ActivateCamera(CameraMode.FirstPerson, instant: true);
    }

    void Update()
    {
        HandleKeyboardInput();
    }

    // ── Input ─────────────────────────────────────────────────────

    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetMode(CameraMode.FirstPerson);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetMode(CameraMode.CarInterior);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetMode(CameraMode.CCTV);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SetMode(CameraMode.Drone);

        // Cycle CCTV cameras with Q / E while in CCTV mode
        if (_currentMode == CameraMode.CCTV)
        {
            if (Input.GetKeyDown(KeyCode.Q)) CycleCCTV(-1);
            if (Input.GetKeyDown(KeyCode.E)) CycleCCTV(+1);
        }
    }

    // ── Public API ────────────────────────────────────────────────

    /// <summary>Switches to the requested camera mode with a fade transition.</summary>
    public void SetMode(CameraMode mode)
    {
        if (_transitioning || mode == _currentMode) return;
        _currentMode = mode;
        StartCoroutine(TransitionTo(mode));
    }

    /// <summary>Cycles the active CCTV camera by the given direction (+1 / -1).</summary>
    public void CycleCCTV(int direction)
    {
        if (cctvCameras == null || cctvCameras.Length == 0) return;

        DisableAllCameras();
        _cctvIndex = (_cctvIndex + direction + cctvCameras.Length) % cctvCameras.Length;

        if (cctvCameras[_cctvIndex] != null)
        {
            cctvCameras[_cctvIndex].enabled = true;
            UpdateCCTVLabel();
        }
    }

    // ── Transition ────────────────────────────────────────────────

    private IEnumerator TransitionTo(CameraMode mode)
    {
        _transitioning = true;

        // Fade out
        yield return StartCoroutine(Fade(0f, 1f));

        ActivateCamera(mode, instant: false);

        // Fade in
        yield return StartCoroutine(Fade(1f, 0f));
        _transitioning = false;
    }

    private void ActivateCamera(CameraMode mode, bool instant)
    {
        DisableAllCameras();

        Camera target = null;

        switch (mode)
        {
            case CameraMode.FirstPerson:
                target = firstPersonCamera;
                EnablePlayerMovement(true);
                break;

            case CameraMode.CarInterior:
                target = carInteriorCamera;
                EnablePlayerMovement(false);
                break;

            case CameraMode.CCTV:
                if (cctvCameras != null && cctvCameras.Length > 0)
                    target = cctvCameras[_cctvIndex];
                EnablePlayerMovement(false);
                UpdateCCTVLabel();
                break;

            case CameraMode.Drone:
                target = droneCamera;
                EnablePlayerMovement(false);
                break;
        }

        if (target != null) target.enabled = true;
    }

    private void DisableAllCameras()
    {
        if (firstPersonCamera  != null) firstPersonCamera.enabled  = false;
        if (carInteriorCamera  != null) carInteriorCamera.enabled  = false;
        if (droneCamera        != null) droneCamera.enabled        = false;

        if (cctvCameras == null) return;
        foreach (Camera c in cctvCameras)
            if (c != null) c.enabled = false;
    }

    private void EnablePlayerMovement(bool enable)
    {
        if (playerMovementScript != null)
            playerMovementScript.enabled = enable;
    }

    private void UpdateCCTVLabel()
    {
        if (cctvLabel == null || cctvCameras == null || cctvCameras.Length == 0) return;
        Camera active = cctvCameras[_cctvIndex];
        cctvLabel.text = active != null ? $"CCTV — {active.name}" : "CCTV — No Feed";
    }

    // ── Screen fade ───────────────────────────────────────────────

    // Uses a full-screen texture built lazily at runtime so no extra asset is needed.

    private Texture2D _blackTex;
    private float     _fadeAlpha = 0f;
    private bool      _isFading  = false;

    void OnGUI()
    {
        if (!_isFading || _fadeAlpha <= 0f) return;

        if (_blackTex == null)
        {
            _blackTex = new Texture2D(1, 1);
            _blackTex.SetPixel(0, 0, Color.black);
            _blackTex.Apply();
        }

        GUI.color = new Color(0f, 0f, 0f, _fadeAlpha);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _blackTex);
        GUI.color = Color.white;
    }

    private IEnumerator Fade(float from, float to)
    {
        _isFading = true;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            _fadeAlpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }

        _fadeAlpha = to;
        _isFading  = to > 0f;
    }

    // ── Public accessors ──────────────────────────────────────────

    /// <summary>The currently active camera mode.</summary>
    public CameraMode CurrentMode => _currentMode;

    /// <summary>Index of the currently active CCTV camera.</summary>
    public int CurrentCCTVIndex => _cctvIndex;
}
