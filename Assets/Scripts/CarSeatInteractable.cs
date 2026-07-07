using UnityEngine;

/// <summary>
/// Attach to the DriverSeatAnchor (or any collider near the driver door).
/// The player looks at it and presses E to enter or exit the car.
/// Implements IInteractable so PlayerInteraction picks it up automatically.
/// </summary>
public class CarSeatInteractable : MonoBehaviour, IInteractable
{
    [Header("References — leave empty to auto-resolve")]
    [Tooltip("Manages parent/unparent and locomotion toggle.")]
    public PlayerSeatingController seatingController;

    [Tooltip("CameraManager — kept in FirstPerson mode while seated.")]
    public CameraManager cameraManager;

    void Start()
    {
        if (seatingController == null)
            seatingController = FindFirstObjectByType<PlayerSeatingController>();

        if (cameraManager == null)
            cameraManager = FindFirstObjectByType<CameraManager>();
    }

    /// <summary>Toggles seated state when the player presses E.</summary>
    public void Interact()
    {
        if (seatingController == null) return;

        if (seatingController.IsSeated)
        {
            seatingController.Unseat();
            // Return to free first-person walking
            cameraManager?.SetMode(CameraManager.CameraMode.FirstPerson);
        }
        else
        {
            seatingController.Seat();
            // Player's own FP camera now rides with the car — stay in FirstPerson mode
        }
    }

    public string GetInteractionPrompt() =>
        seatingController != null && seatingController.IsSeated
            ? "Exit Car"
            : "Enter Car";
}
