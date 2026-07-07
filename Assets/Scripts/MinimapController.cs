using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Creates a RenderTexture at runtime and pipes the overhead minimap camera feed
/// into the RawImage HUD element. Attach to the MinimapManager GameObject.
/// </summary>
public class MinimapController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The orthographic overhead camera that renders the minimap view.")]
    public Camera minimapCamera;

    [Tooltip("The RawImage UI element that displays the minimap render texture.")]
    public RawImage minimapDisplay;

    [Header("Render Texture")]
    [Tooltip("Resolution in pixels for the minimap render texture (square).")]
    public int textureSize = 512;

    private RenderTexture _renderTexture;

    void Awake()
    {
        _renderTexture = new RenderTexture(textureSize, textureSize, 16, RenderTextureFormat.ARGB32)
        {
            antiAliasing = 2,
            filterMode = FilterMode.Bilinear
        };
        _renderTexture.Create();

        if (minimapCamera != null)
            minimapCamera.targetTexture = _renderTexture;

        if (minimapDisplay != null)
            minimapDisplay.texture = _renderTexture;
    }

    void OnDestroy()
    {
        if (_renderTexture != null)
        {
            _renderTexture.Release();
            Destroy(_renderTexture);
        }
    }
}
