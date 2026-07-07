using UnityEngine;

/// <summary>
/// Parents the Player rig to the car's DriverSeatAnchor so they physically ride
/// with the vehicle. Works for both desktop (PlayerMovement) and VR (VRPlayerController).
/// All references are auto-resolved at Awake if not assigned manually.
/// </summary>
public class PlayerSeatingController : MonoBehaviour
{
    [Header("References — leave empty to auto-resolve")]
    [Tooltip("Root transform of the Player rig.")]
    public Transform playerRoot;

    [Tooltip("Empty child of the car at the driver eye position.")]
    public Transform driverSeatAnchor;

    [Tooltip("VRPlayerController on the Player — disabled while seated.")]
    public VRPlayerController vrController;

    [Tooltip("Desktop PlayerMovement on the Player — disabled while seated.")]
    public PlayerMovement desktopMovement;

    [Tooltip("CharacterController on the Player — disabled while seated.")]
    public CharacterController characterController;

    [Header("Settings")]
    [Tooltip("Automatically seat the player when the scene starts.")]
    public bool seatOnStart = false;

    /// <summary>True while the player is parented to the car seat.</summary>
    public bool IsSeated { get; private set; }

    private Transform _originalParent;
    private Vector3   _originalWorldPos;

    void Awake() => AutoResolveReferences();

    void Start()
    {
        if (seatOnStart) Seat();
    }

    private void AutoResolveReferences()
    {
        if (vrController    == null) vrController    = FindFirstObjectByType<VRPlayerController>();
        if (desktopMovement == null) desktopMovement = FindFirstObjectByType<PlayerMovement>();

        if (playerRoot == null)
        {
            if      (vrController    != null) playerRoot = vrController.transform;
            else if (desktopMovement != null) playerRoot = desktopMovement.transform;
        }

        if (characterController == null && playerRoot != null)
            characterController = playerRoot.GetComponent<CharacterController>();

        if (driverSeatAnchor == null)
        {
            GameObject anchor = GameObject.Find("DriverSeatAnchor");
            if (anchor != null) driverSeatAnchor = anchor.transform;
        }

        if (playerRoot       == null) Debug.LogError("[PlayerSeating] playerRoot not found.");
        if (driverSeatAnchor == null) Debug.LogError("[PlayerSeating] DriverSeatAnchor not found — name a child of the car 'DriverSeatAnchor'.");
    }

    /// <summary>Parents the player to the driver seat and disables all locomotion.</summary>
    public void Seat()
    {
        if (IsSeated) return;
        if (playerRoot == null || driverSeatAnchor == null)
        {
            Debug.LogWarning("[PlayerSeating] Cannot seat — references missing.");
            return;
        }

        _originalParent   = playerRoot.parent;
        _originalWorldPos = playerRoot.position;

        SetMovementEnabled(false);

        playerRoot.SetParent(driverSeatAnchor, worldPositionStays: false);
        playerRoot.localPosition = Vector3.zero;
        playerRoot.localRotation = Quaternion.identity;

        IsSeated = true;
        Debug.Log("[PlayerSeating] Player seated.");
    }

    /// <summary>Unparents the player from the seat and re-enables all locomotion.</summary>
    public void Unseat()
    {
        if (!IsSeated || playerRoot == null) return;

        playerRoot.SetParent(_originalParent, worldPositionStays: true);
        SetMovementEnabled(true);

        IsSeated = false;
        Debug.Log("[PlayerSeating] Player unseated.");
    }

    private void SetMovementEnabled(bool enable)
    {
        if (characterController != null) characterController.enabled = enable;
        if (vrController        != null) vrController.enabled        = enable;
        if (desktopMovement     != null) desktopMovement.enabled     = enable;
    }
}
