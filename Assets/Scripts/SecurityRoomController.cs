using TMPro;
using UnityEngine;

/// <summary>
/// Controls the security monitoring room. Cycles the four monitor feeds
/// on demand and allows the player to interact with the control terminal
/// via the existing IInteractable / PlayerInteraction system.
/// </summary>
public class SecurityRoomController : MonoBehaviour, IInteractable
{
    [Header("Monitors")]
    [Tooltip("The four security monitors (quads) in the security room.")]
    public SecurityMonitor[] monitors;

    [Header("CCTV Sources")]
    [Tooltip("All CCTVCamera sources in the scene — mapped to monitors in order.")]
    public CCTVCamera[] cctvSources;

    [Header("Room HUD")]
    [Tooltip("TMP text element on the control terminal.")]
    public TMP_Text terminalLabel;

    [Header("Gate Control")]
    [Tooltip("Allows opening/closing the gate from the security room terminal.")]
    public SmartGateController gateController;

    // ── Private state ─────────────────────────────────────────────
    private int _cycleOffset = 0;
    private bool _gateOverride = false;

    void Start()
    {
        AssignDefaultFeeds();
        UpdateTerminalLabel();
    }

    // ── IInteractable ─────────────────────────────────────────────

    /// <summary>Player presses E on the terminal — cycle monitor feeds.</summary>
    public void Interact()
    {
        CycleFeeds();
    }

    public string GetInteractionPrompt() => "Security Terminal  [E] Cycle Feeds  [F] Gate Override";

    // ── Public API ────────────────────────────────────────────────

    /// <summary>Cycles all monitor feeds one position forward through the CCTV source list.</summary>
    public void CycleFeeds()
    {
        if (monitors == null || cctvSources == null) return;

        _cycleOffset = (_cycleOffset + 1) % cctvSources.Length;
        AssignDefaultFeeds();
        UpdateTerminalLabel();
    }

    /// <summary>Toggles the gate from the security room terminal.</summary>
    public void ToggleGateOverride()
    {
        if (gateController == null) return;

        _gateOverride = !_gateOverride;
        if (_gateOverride) gateController.OpenGate();
        else               gateController.CloseGate();

        UpdateTerminalLabel();
    }

    // ── Private ───────────────────────────────────────────────────

    private void AssignDefaultFeeds()
    {
        if (monitors == null || cctvSources == null) return;

        for (int i = 0; i < monitors.Length; i++)
        {
            if (monitors[i] == null) continue;
            int sourceIndex = (i + _cycleOffset) % cctvSources.Length;
            monitors[i].SetSource(cctvSources[sourceIndex]);
        }
    }

    private void UpdateTerminalLabel()
    {
        if (terminalLabel == null) return;

        string gateStatus = gateController != null
            ? (gateController.IsOpen ? "OPEN" : "CLOSED")
            : "N/A";

        terminalLabel.text =
            $"SECURITY TERMINAL\n" +
            $"Gate Status: {gateStatus}\n" +
            $"Feed Offset: {_cycleOffset}\n" +
            $"[E] Cycle Feeds  [F] Gate Override";
    }

    void Update()
    {
        // Allow pressing F while looking at the terminal to toggle gate
        if (Input.GetKeyDown(KeyCode.F)) ToggleGateOverride();
        UpdateTerminalLabel();
    }
}
