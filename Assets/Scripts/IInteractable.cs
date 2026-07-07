/// <summary>
/// Implement this interface on any MonoBehaviour that should be interactable by the player.
/// </summary>
public interface IInteractable
{
    /// <summary>Called when the player presses the interact key while looking at this object.</summary>
    void Interact();

    /// <summary>Returns the label shown in the interaction prompt, e.g. "Open Door".</summary>
    string GetInteractionPrompt();
}
