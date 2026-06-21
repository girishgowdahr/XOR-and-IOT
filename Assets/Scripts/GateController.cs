using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class GateController : MonoBehaviour
{
    [Header("Gate Transforms")]
    public Transform leftGate;
    public Transform rightGate;

    [Header("Movement")]
    [Tooltip("Movement speed in units per second.")]
    public float speed = 1f;

    [Tooltip("How far the left gate slides inward (X+) to close.")]
    public float leftCloseOffset = 3.5f;

    [Tooltip("How far the right gate slides inward (X-) to close. Reduce if the right gate overshoots past centre.")]
    public float rightCloseOffset = 3.5f;

    [Header("UI")]
    [Tooltip("TMP text element that displays the current gate status.")]
    public TMP_Text statusLabel;

    private static readonly string OpenText   = "Gate is Open";
    private static readonly string ClosedText = "Gate is Closed";

    private Vector3 leftClosed;
    private Vector3 rightClosed;
    private Vector3 leftOpen;
    private Vector3 rightOpen;

    private bool isOpen = false;

    /// <summary>Returns true when the gate is currently open.</summary>
    public bool IsOpen => isOpen;

    /// <summary>
    /// Stores open positions from the model's starting pose and
    /// derives closed positions by sliding each panel inward.
    /// </summary>
    void Start()
    {
        leftOpen  = leftGate.localPosition;
        rightOpen = rightGate.localPosition;

        leftClosed  = leftOpen  + new Vector3( leftCloseOffset,  0f, 0f);
        rightClosed = rightOpen + new Vector3(-rightCloseOffset, 0f, 0f);

        isOpen = true;
        UpdateStatusLabel();
    }

    void Update()
    {
        HandleKeyboardInput();
        HandleGamepadInput();
        MoveGates();
    }

    // ──────────────────────────────────────────────────────────────
    //  Input
    // ──────────────────────────────────────────────────────────────

    private void HandleKeyboardInput()
    {
        if (Keyboard.current.oKey.wasPressedThisFrame) OpenGate();
        if (Keyboard.current.cKey.wasPressedThisFrame) CloseGate();
    }

    private void HandleGamepadInput()
    {
        if (Gamepad.current == null) return;

        // South button (A on Xbox / Cross on PlayStation) → Open
        if (Gamepad.current.buttonSouth.wasPressedThisFrame) OpenGate();

        // East button (B on Xbox / Circle on PlayStation) → Close
        if (Gamepad.current.buttonEast.wasPressedThisFrame) CloseGate();
    }

    // ──────────────────────────────────────────────────────────────
    //  Gate control (also called by UI buttons)
    // ──────────────────────────────────────────────────────────────

    /// <summary>Slides both panels to their open positions.</summary>
    public void OpenGate()
    {
        if (isOpen) return;
        isOpen = true;
        UpdateStatusLabel();
    }

    /// <summary>Slides both panels to their closed positions.</summary>
    public void CloseGate()
    {
        if (!isOpen) return;
        isOpen = false;
        UpdateStatusLabel();
    }

    // ──────────────────────────────────────────────────────────────
    //  Movement & UI
    // ──────────────────────────────────────────────────────────────

    private void MoveGates()
    {
        float step = speed * Time.deltaTime;
        Vector3 leftTarget  = isOpen ? leftOpen  : leftClosed;
        Vector3 rightTarget = isOpen ? rightOpen : rightClosed;

        leftGate.localPosition  = Vector3.MoveTowards(leftGate.localPosition,  leftTarget,  step);
        rightGate.localPosition = Vector3.MoveTowards(rightGate.localPosition, rightTarget, step);
    }

    private void UpdateStatusLabel()
    {
        if (statusLabel == null) return;
        statusLabel.text = isOpen ? OpenText : ClosedText;
    }
}
