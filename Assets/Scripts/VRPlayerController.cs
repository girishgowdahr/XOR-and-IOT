using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// First-person player controller designed for Meta Quest VR.
/// - In VR mode: reads XR head tracking and joystick locomotion from the Input System.
/// - In Desktop mode: reads WASD + mouse look (fallback for Editor testing).
/// Attach to the Player root. The Camera/XR Rig should be a child.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class VRPlayerController : MonoBehaviour
{
    // ── Movement ──────────────────────────────────────────────────
    [Header("Movement")]
    public float walkSpeed   = 4f;
    public float sprintSpeed = 7f;
    public float gravity     = -19.62f;
    [Tooltip("Human eye height from the floor in metres.")]
    public float eyeHeight   = 1.7f;

    // ── Smooth Turning ────────────────────────────────────────────
    [Header("Smooth Turning")]
    [Tooltip("Degrees per second when rotating with the right joystick (VR).")]
    public float smoothTurnSpeed = 80f;
    [Tooltip("If true, turning snaps in fixed degree increments (snap-turn for comfort).")]
    public bool useSnapTurn = false;
    [Tooltip("Degrees per snap-turn step.")]
    public float snapTurnDegrees = 30f;
    [Tooltip("Dead-zone threshold for triggering a snap turn.")]
    private const float SnapTurnDeadzone = 0.5f;

    // ── Desktop Fallback ──────────────────────────────────────────
    [Header("Desktop Fallback (Editor / Non-VR)")]
    public float mouseSensitivity = 0.2f;
    public Transform desktopCameraTransform;

    // ── Input Actions ─────────────────────────────────────────────
    [Header("Input Actions")]
    [Tooltip("Left joystick 2D axis action — locomotion.")]
    public InputActionReference moveAction;
    [Tooltip("Right joystick 2D axis action — turning.")]
    public InputActionReference turnAction;
    [Tooltip("Left grip / Left Shift — sprint modifier.")]
    public InputActionReference sprintAction;

    // ── Private state ─────────────────────────────────────────────
    private CharacterController _controller;
    private Vector3 _velocity;
    private float   _xRotation;
    private bool    _snapReady = true;

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _controller.height = eyeHeight;
        _controller.center = new Vector3(0f, eyeHeight * 0.5f, 0f);
    }

    void OnEnable()
    {
        EnableAction(moveAction);
        EnableAction(turnAction);
        EnableAction(sprintAction);

        if (!IsVR()) Cursor.lockState = CursorLockMode.Locked;
    }

    void OnDisable()
    {
        if (!IsVR()) Cursor.lockState = CursorLockMode.None;
    }

    void Update()
    {
        HandleGravity();
        HandleLocomotion();
        HandleTurning();
    }

    // ── Gravity ───────────────────────────────────────────────────

    private void HandleGravity()
    {
        if (_controller.isGrounded && _velocity.y < 0f)
            _velocity.y = -2f;

        _velocity.y += gravity * Time.deltaTime;
        _controller.Move(_velocity * Time.deltaTime);
    }

    // ── Locomotion ────────────────────────────────────────────────

    private void HandleLocomotion()
    {
        Vector2 input = ReadVector2(moveAction);

        // Desktop fallback
        if (!IsVR() && input == Vector2.zero)
        {
            input.x = Input.GetAxis("Horizontal");
            input.y = Input.GetAxis("Vertical");
        }

        bool sprinting = ReadBool(sprintAction) ||
                         (!IsVR() && Input.GetKey(KeyCode.LeftShift));

        float speed = sprinting ? sprintSpeed : walkSpeed;

        // In VR the character moves in the direction the headset faces
        Transform forward = IsVR()
            ? (desktopCameraTransform != null ? desktopCameraTransform : transform)
            : (desktopCameraTransform != null ? desktopCameraTransform : transform);

        Vector3 flatForward = Vector3.ProjectOnPlane(forward.forward, Vector3.up).normalized;
        Vector3 flatRight   = Vector3.ProjectOnPlane(forward.right,   Vector3.up).normalized;
        Vector3 move        = flatForward * input.y + flatRight * input.x;

        _controller.Move(move * speed * Time.deltaTime);
    }

    // ── Turning ───────────────────────────────────────────────────

    private void HandleTurning()
    {
        Vector2 turnInput = ReadVector2(turnAction);

        if (IsVR())
        {
            HandleVRTurn(turnInput.x);
        }
        else
        {
            HandleDesktopMouseLook();
        }
    }

    private void HandleVRTurn(float horizontal)
    {
        if (useSnapTurn)
        {
            if (Mathf.Abs(horizontal) > SnapTurnDeadzone && _snapReady)
            {
                float sign = horizontal > 0f ? 1f : -1f;
                transform.Rotate(Vector3.up, sign * snapTurnDegrees);
                _snapReady = false;
            }
            else if (Mathf.Abs(horizontal) <= SnapTurnDeadzone)
            {
                _snapReady = true;
            }
        }
        else
        {
            transform.Rotate(Vector3.up, horizontal * smoothTurnSpeed * Time.deltaTime);
        }
    }

    private void HandleDesktopMouseLook()
    {
        if (desktopCameraTransform == null) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * 100f * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * 100f * Time.deltaTime;

        _xRotation -= mouseY;
        _xRotation  = Mathf.Clamp(_xRotation, -85f, 85f);

        desktopCameraTransform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    // ── Helpers ───────────────────────────────────────────────────

    /// <summary>Returns true when the application is running in XR mode.</summary>
    private bool IsVR() =>
        UnityEngine.XR.XRSettings.enabled && UnityEngine.XR.XRSettings.isDeviceActive;

    private Vector2 ReadVector2(InputActionReference actionRef)
    {
        if (actionRef == null || actionRef.action == null) return Vector2.zero;
        return actionRef.action.ReadValue<Vector2>();
    }

    private bool ReadBool(InputActionReference actionRef)
    {
        if (actionRef == null || actionRef.action == null) return false;
        return actionRef.action.ReadValue<float>() > 0.5f;
    }

    private void EnableAction(InputActionReference actionRef)
    {
        if (actionRef == null || actionRef.action == null) return;
        if (!actionRef.action.enabled) actionRef.action.Enable();
    }
}
