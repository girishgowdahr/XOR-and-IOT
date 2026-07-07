using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays a CCTV camera's live render texture on a world-space monitor.
/// Place on any quad or plane that represents a security screen.
/// Optionally assign a canvas label to show the camera name.
/// </summary>
public class SecurityMonitor : MonoBehaviour
{
    [Header("Feed Source")]
    [Tooltip("The CCTVCamera whose RenderTexture will be displayed.")]
    public CCTVCamera sourceCCTV;

    [Header("Display")]
    [Tooltip("The Renderer (e.g. MeshRenderer on a quad) that shows the feed.")]
    public Renderer monitorRenderer;
    [Tooltip("Material property name for the main texture. Defaults to _BaseMap (URP).")]
    public string texturePropertyName = "_BaseMap";

    [Header("UI Label (optional)")]
    [Tooltip("World-space canvas TMP label showing the camera name.")]
    public TMP_Text cameraNameLabel;

    [Header("Power")]
    [Tooltip("If false, the monitor shows a black screen (powered off).")]
    public bool isPowered = true;

    // ── Private ───────────────────────────────────────────────────
    private MaterialPropertyBlock _propBlock;

    void Start()
    {
        _propBlock = new MaterialPropertyBlock();

        if (cameraNameLabel != null && sourceCCTV != null)
            cameraNameLabel.text = sourceCCTV.cameraLabel;

        ApplyFeed();
    }

    void Update()
    {
        // Re-apply each frame so the live texture stays current
        ApplyFeed();
    }

    /// <summary>Assigns the CCTV render texture to the monitor renderer.</summary>
    private void ApplyFeed()
    {
        if (monitorRenderer == null) return;

        monitorRenderer.GetPropertyBlock(_propBlock);

        if (isPowered && sourceCCTV != null && sourceCCTV.LiveTexture != null)
            _propBlock.SetTexture(texturePropertyName, sourceCCTV.LiveTexture);
        else
            _propBlock.SetTexture(texturePropertyName, Texture2D.blackTexture);

        monitorRenderer.SetPropertyBlock(_propBlock);
    }

    /// <summary>Switches this monitor to display a different CCTV source.</summary>
    public void SetSource(CCTVCamera newSource)
    {
        sourceCCTV = newSource;

        if (cameraNameLabel != null)
            cameraNameLabel.text = newSource != null ? newSource.cameraLabel : "NO FEED";
    }

    /// <summary>Toggles monitor power on/off.</summary>
    public void SetPower(bool powered)
    {
        isPowered = powered;
    }
}
