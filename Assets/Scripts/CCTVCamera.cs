using UnityEngine;

/// <summary>
/// Attaches a RenderTexture to this camera and exposes it for any
/// SecurityMonitor to consume. Place on each CCTV camera GameObject.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CCTVCamera : MonoBehaviour
{
    [Header("Render Texture")]
    [Tooltip("Resolution of the generated render texture (square).")]
    public int textureSize = 512;

    [Tooltip("Optional name shown on security monitors.")]
    public string cameraLabel = "CCTV Camera";

    [Header("Scan Line Overlay")]
    [Tooltip("Enable a subtle noise/scan-line effect on the render.")]
    public bool enableScanLines = true;

    // ── Public readonly access ────────────────────────────────────

    /// <summary>The live render texture this camera writes to.</summary>
    public RenderTexture LiveTexture { get; private set; }

    // ── Private ───────────────────────────────────────────────────
    private Camera _cam;

    void Awake()
    {
        _cam = GetComponent<Camera>();

        LiveTexture = new RenderTexture(textureSize, textureSize, 24, RenderTextureFormat.ARGB32)
        {
            filterMode   = FilterMode.Bilinear,
            antiAliasing = 1
        };
        LiveTexture.Create();

        _cam.targetTexture = LiveTexture;
    }

    void OnDestroy()
    {
        if (LiveTexture == null) return;
        LiveTexture.Release();
        Destroy(LiveTexture);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawFrustum(Vector3.zero, 60f, 15f, 0.1f, 1f);
    }
}
