using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Raycasts from the player camera every frame.
/// When an <see cref="IInteractable"/> is in range the player can press E (desktop)
/// or the right grip button (Quest/XR) to interact.
/// Attach this to the Player root (same GameObject as PlayerMovement).
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("Maximum distance at which the player can interact with objects.")]
    public float interactRange = 3f;

    [Tooltip("LayerMask to include in the raycast (leave as Everything if unsure).")]
    public LayerMask interactLayer = ~0;

    [Header("UI")]
    [Tooltip("TextMeshProUGUI element that shows the interaction prompt.")]
    public TextMeshProUGUI promptText;

    [Header("Input")]
    [Tooltip("Assign Player/Interact from InputSystem_Actions. Right-grip on Quest, E on keyboard.")]
    public InputActionReference interactAction;

    // ── Private state ──────────────────────────────────────────────
    private Camera _camera;
    private IInteractable _currentTarget;

    void Start()
    {
        _camera = GetComponentInChildren<Camera>();

        if (_camera == null)
            Debug.LogError("[PlayerInteraction] No Camera found as a child of Player.", this);

        if (promptText != null)
            promptText.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        if (interactAction != null && interactAction.action != null && !interactAction.action.enabled)
            interactAction.action.Enable();
    }

    void Update()
    {
        DetectInteractable();

        bool pressed = (Keyboard.current != null && Keyboard.current[Key.E].wasPressedThisFrame) ||
                       (interactAction?.action?.WasPressedThisFrame() == true);

        if (_currentTarget != null && pressed)
            _currentTarget.Interact();
    }

    /// <summary>Casts a ray from screen center; updates prompt visibility and current target.</summary>
    private void DetectInteractable()
    {
        if (_camera == null) return;

        Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);
        bool hit = Physics.Raycast(ray, out RaycastHit hitInfo, interactRange, interactLayer);

        if (hit && hitInfo.collider.TryGetComponent(out IInteractable interactable))
        {
            _currentTarget = interactable;
            ShowPrompt(interactable.GetInteractionPrompt());
        }
        else
        {
            _currentTarget = null;
            HidePrompt();
        }
    }

    private void ShowPrompt(string message)
    {
        if (promptText == null) return;

        bool isVR = UnityEngine.XR.XRSettings.enabled && UnityEngine.XR.XRSettings.isDeviceActive;
        string keyHint = isVR ? "[Grip]" : "[E]";
        promptText.text = $"{keyHint}  {message}";
        promptText.gameObject.SetActive(true);
    }

    private void HidePrompt()
    {
        if (promptText == null) return;
        promptText.gameObject.SetActive(false);
    }
}
