using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds and drives the "Access Denied" world-space HUD panel at runtime.
/// Attach to any GameObject near the gate; call Show() / Hide() directly or
/// wire it through SmartGateController's accessDeniedPanel reference.
/// </summary>
public class AccessDeniedHUD : MonoBehaviour
{
    [Header("World Canvas Settings")]
    [Tooltip("Width of the panel in metres.")]
    public float panelWidth = 1.2f;
    [Tooltip("Height of the panel in metres.")]
    public float panelHeight = 0.5f;
    [Tooltip("Pixels-per-unit for the world canvas.")]
    public float pixelsPerUnit = 200f;

    [Header("Flashing")]
    [Tooltip("Times per second the panel background flashes.")]
    public float flashFrequency = 2f;

    // ── Built references ──────────────────────────────────────────
    private Canvas      _canvas;
    private Image       _background;
    private TMP_Text    _mainLabel;
    private TMP_Text    _subLabel;
    private Coroutine   _flashCoroutine;

    private static readonly Color ColorRed   = new Color(0.85f, 0.10f, 0.10f, 0.95f);
    private static readonly Color ColorDark  = new Color(0.10f, 0.10f, 0.10f, 0.92f);

    void Awake()
    {
        BuildPanel();
        gameObject.SetActive(false);
    }

    void OnEnable()
    {
        _flashCoroutine = StartCoroutine(FlashLoop());
    }

    void OnDisable()
    {
        if (_flashCoroutine != null)
        {
            StopCoroutine(_flashCoroutine);
            _flashCoroutine = null;
        }
    }

    // ── Public API ────────────────────────────────────────────────

    /// <summary>Shows the access denied panel with a flash animation.</summary>
    public void Show()  => gameObject.SetActive(true);

    /// <summary>Hides the access denied panel.</summary>
    public void Hide()  => gameObject.SetActive(false);

    // ── Build ─────────────────────────────────────────────────────

    private void BuildPanel()
    {
        // World-space canvas
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode       = RenderMode.WorldSpace;
        _canvas.worldCamera      = Camera.main;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = pixelsPerUnit;

        gameObject.AddComponent<GraphicRaycaster>();

        RectTransform rt = GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(panelWidth * pixelsPerUnit, panelHeight * pixelsPerUnit);
        transform.localScale = Vector3.one / pixelsPerUnit;

        // Background panel
        GameObject bgGO = new GameObject("Background", typeof(RectTransform));
        bgGO.transform.SetParent(transform, false);
        _background = bgGO.AddComponent<Image>();
        _background.color = ColorRed;
        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        Stretch(bgRT);

        // Main label — "ACCESS DENIED"
        _mainLabel = CreateLabel("MainLabel", bgRT, "ACCESS DENIED", 60f, Color.white,
            TextAlignmentOptions.Center, new Vector2(0f, 0.6f), new Vector2(1f, 1f));

        // Sub label — plate info placeholder
        _subLabel  = CreateLabel("SubLabel", bgRT, "UNAUTHORIZED VEHICLE", 28f,
            new Color(1f, 0.9f, 0.9f), TextAlignmentOptions.Center,
            new Vector2(0f, 0f), new Vector2(1f, 0.5f));
    }

    private IEnumerator FlashLoop()
    {
        while (true)
        {
            _background.color = ColorRed;
            yield return new WaitForSeconds(1f / (flashFrequency * 2f));
            _background.color = ColorDark;
            yield return new WaitForSeconds(1f / (flashFrequency * 2f));
        }
    }

    // ── Helpers ───────────────────────────────────────────────────

    private void Stretch(RectTransform rt)
    {
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.one;
        rt.offsetMin        = Vector2.zero;
        rt.offsetMax        = Vector2.zero;
    }

    private TMP_Text CreateLabel(string name, RectTransform parent, string text,
        float fontSize, Color color, TextAlignmentOptions align,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = color;
        tmp.alignment = align;
        tmp.fontStyle = FontStyles.Bold;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = new Vector2(10f, 0f);
        rt.offsetMax = new Vector2(-10f, 0f);
        return tmp;
    }
}
