using UnityEngine;

/// <summary>
/// Makes the gate panel interactable via the existing PlayerInteraction system.
/// Attach to a child collider on the gate or gate panel. The player can press E
/// to manually open/close the gate when in range.
/// </summary>
public class GateInteractable : MonoBehaviour, IInteractable
{
    [Tooltip("The SmartGateController to open/close.")]
    public SmartGateController gateController;

    /// <summary>Toggles the gate open or closed.</summary>
    public void Interact()
    {
        if (gateController == null) return;

        if (gateController.IsOpen)
            gateController.CloseGate();
        else
            gateController.OpenGate();
    }

    /// <summary>Returns the context-sensitive prompt label.</summary>
    public string GetInteractionPrompt()
    {
        if (gateController == null) return "Gate Panel";
        return gateController.IsOpen ? "Close Gate" : "Open Gate";
    }
}
