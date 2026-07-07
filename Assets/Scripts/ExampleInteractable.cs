using UnityEngine;

/// <summary>
/// Drop this on any GameObject with a Collider to make it interactable.
/// Customize <see cref="promptLabel"/> and override <see cref="OnInteract"/> for real behaviour.
/// </summary>
public class ExampleInteractable : MonoBehaviour, IInteractable
{
    [Tooltip("Label shown in the interaction prompt, e.g. 'Open Door'.")]
    public string promptLabel = "Interact";

    private static readonly Color HighlightColor  = new Color(1f, 0.6f, 0f);   // orange
    private static readonly Color DefaultColor     = Color.white;

    private Renderer _renderer;
    private MaterialPropertyBlock _mpb;

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _mpb      = new MaterialPropertyBlock();
    }

    /// <summary>Called by PlayerInteraction when E is pressed.</summary>
    public void Interact()
    {
        Debug.Log($"[ExampleInteractable] '{gameObject.name}' was interacted with.");
        Flash();
    }

    /// <summary>Returns the prompt label shown to the player.</summary>
    public string GetInteractionPrompt() => promptLabel;

    // ── Visual feedback ────────────────────────────────────────────
    private void Flash()
    {
        if (_renderer == null) return;

        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetColor("_BaseColor", HighlightColor);
        _renderer.SetPropertyBlock(_mpb);

        Invoke(nameof(ResetColor), 0.3f);
    }

    private void ResetColor()
    {
        if (_renderer == null) return;

        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetColor("_BaseColor", DefaultColor);
        _renderer.SetPropertyBlock(_mpb);
    }
}
