using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Minimal floating status HUD displaying:
///   • Gate Status       (CLOSED / OPENING / OPEN / CLOSING)
///   • Vehicle Status    (IDLE / APPROACHING / AT GATE / ENTERING / PARKED)
///   • Authentication    (— / SCANNING... / AUTHORIZED / DENIED)
///   • Access            (— / GRANTED / DENIED)
///
/// Built entirely at runtime. Renders on a world-space canvas so it works in VR.
/// Attach to a child of the player camera; set the localPosition and localRotation
/// to place it comfortably in view (e.g. 1.5 m forward, 0.25 m down).
/// </summary>
public class StatusHUD : MonoBehaviour
{
    // ── Data sources ──────────────────────────────────────────────
    [Header("Data Sources")]
    public ExperienceManager experienceManager;

    [Header("Canvas Layout")]
    [Tooltip("Width of the HUD panel in world-space metres.")]
    public float panelWidth  = 0.36f;
    [Tooltip("Height of the HUD panel in world-space metres.")]
    public float panelHeight = 0.22f;
    [Tooltip("Dynamic pixels per unit for text sharpness.")]
    public float pixelsPerUnit = 400f;

    [Header("Scan Animation")]
    public float scanDotInterval = 0.4f;

    // ── Design tokens ─────────────────────────────────────────────
    private static readonly Color BgColor       = new Color(0.04f, 0.06f, 0.09f, 0.88f);
    private static readonly Color HeaderColor   = new Color(0.07f, 0.44f, 0.82f, 1.00f);
    private static readonly Color DividerColor  = new Color(0.15f, 0.20f, 0.28f, 1.00f);
    private static readonly Color LabelColor    = new Color(0.50f, 0.58f, 0.70f, 1.00f);
    private static readonly Color ValueWhite    = new Color(0.95f, 0.95f, 0.95f, 1.00f);
    private static readonly Color Green         = new Color(0.13f, 0.82f, 0.42f, 1.00f);
    private static readonly Color Red           = new Color(0.92f, 0.22f, 0.22f, 1.00f);
    private static readonly Color Amber         = new Color(0.95f, 0.68f, 0.10f, 1.00f);

    // Built text references
    private TMP_Text _gateValue;
    private TMP_Text _vehicleValue;
    private TMP_Text _authValue;
    private TMP_Text _accessValue;
    private Image    _liveDot;

    // Dot blink
    private bool  _liveDotVisible = true;
    private float _liveDotTimer;
    private const float LiveDotInterval = 0.7f;

    // Auth scan dots
    private int   _scanDotCount = 0;
    private float _scanTimer;

    // Current display state
    private ExperienceManager.State _lastState = (ExperienceManager.State)(-1);
    private bool _isScanning = false;
    private bool _isFlashingAccess = false;

    // ── Unity lifecycle ───────────────────────────────────────────

    void Awake()
    {
        BuildCanvas();
    }

    void OnEnable()
    {
        if (experienceManager != null)
            experienceManager.OnStateChanged += HandleStateChanged;
    }

    void OnDisable()
    {
        if (experienceManager != null)
            experienceManager.OnStateChanged -= HandleStateChanged;
    }

    void Update()
    {
        BlinkLiveDot();
        if (_isScanning) AnimateScanDots();
    }

    // ── State listener ────────────────────────────────────────────

    private void HandleStateChanged(ExperienceManager.State prev, ExperienceManager.State next)
    {
        _lastState = next;
        RefreshAll(next);
    }

    private void RefreshAll(ExperienceManager.State state)
    {
        UpdateGateRow(state);
        UpdateVehicleRow(state);
        UpdateAuthRow(state);
        UpdateAccessRow(state);
    }

    // ── Row updaters ──────────────────────────────────────────────

    private void UpdateGateRow(ExperienceManager.State state)
    {
        switch (state)
        {
            case ExperienceManager.State.Idle:
            case ExperienceManager.State.CarApproaching:
            case ExperienceManager.State.VehicleDetected:
            case ExperienceManager.State.Authenticating:
                SetValue(_gateValue, "CLOSED", Red);
                break;
            case ExperienceManager.State.AccessGranted:
            case ExperienceManager.State.GateOpening:
                SetValue(_gateValue, "OPENING", Amber);
                break;
            case ExperienceManager.State.CarEntering:
                SetValue(_gateValue, "OPEN", Green);
                break;
            case ExperienceManager.State.GateClosing:
                SetValue(_gateValue, "CLOSING", Amber);
                break;
            default:
                SetValue(_gateValue, "CLOSED", Red);
                break;
        }
    }

    private void UpdateVehicleRow(ExperienceManager.State state)
    {
        switch (state)
        {
            case ExperienceManager.State.Idle:
                SetValue(_vehicleValue, "IDLE", LabelColor);
                break;
            case ExperienceManager.State.CarApproaching:
                SetValue(_vehicleValue, "APPROACHING", Amber);
                break;
            case ExperienceManager.State.VehicleDetected:
            case ExperienceManager.State.Authenticating:
            case ExperienceManager.State.AccessGranted:
            case ExperienceManager.State.GateOpening:
                SetValue(_vehicleValue, "AT GATE", Amber);
                break;
            case ExperienceManager.State.CarEntering:
            case ExperienceManager.State.GateClosing:
                SetValue(_vehicleValue, "ENTERING", Green);
                break;
            case ExperienceManager.State.CarParking:
                SetValue(_vehicleValue, "PARKING", Amber);
                break;
            case ExperienceManager.State.CarParked:
            case ExperienceManager.State.PlayerFree:
                SetValue(_vehicleValue, "PARKED", Green);
                break;
        }
    }

    private void UpdateAuthRow(ExperienceManager.State state)
    {
        _isScanning = false;
        switch (state)
        {
            case ExperienceManager.State.Idle:
            case ExperienceManager.State.CarApproaching:
                SetValue(_authValue, "—", LabelColor);
                break;
            case ExperienceManager.State.VehicleDetected:
            case ExperienceManager.State.Authenticating:
                _isScanning = true;
                _scanDotCount = 0;
                _scanTimer = 0f;
                SetValue(_authValue, "SCANNING", Amber);
                break;
            case ExperienceManager.State.AccessGranted:
            case ExperienceManager.State.GateOpening:
            case ExperienceManager.State.CarEntering:
            case ExperienceManager.State.GateClosing:
            case ExperienceManager.State.CarParking:
            case ExperienceManager.State.CarParked:
            case ExperienceManager.State.PlayerFree:
                SetValue(_authValue, "AUTHORIZED", Green);
                break;
        }
    }

    private void UpdateAccessRow(ExperienceManager.State state)
    {
        switch (state)
        {
            case ExperienceManager.State.Idle:
            case ExperienceManager.State.CarApproaching:
            case ExperienceManager.State.VehicleDetected:
            case ExperienceManager.State.Authenticating:
                SetValue(_accessValue, "—", LabelColor);
                break;
            case ExperienceManager.State.AccessGranted:
            case ExperienceManager.State.GateOpening:
            case ExperienceManager.State.CarEntering:
            case ExperienceManager.State.GateClosing:
            case ExperienceManager.State.CarParking:
            case ExperienceManager.State.CarParked:
            case ExperienceManager.State.PlayerFree:
                if (!_isFlashingAccess)
                    StartCoroutine(FlashAccess(granted: true));
                break;
        }
    }

    // ── Animations ────────────────────────────────────────────────

    private void BlinkLiveDot()
    {
        if (_liveDot == null) return;
        _liveDotTimer += Time.deltaTime;
        if (_liveDotTimer < LiveDotInterval) return;
        _liveDotTimer   = 0f;
        _liveDotVisible = !_liveDotVisible;
        _liveDot.color  = _liveDotVisible
            ? Green
            : new Color(Green.r, Green.g, Green.b, 0.15f);
    }

    private void AnimateScanDots()
    {
        _scanTimer += Time.deltaTime;
        if (_scanTimer < scanDotInterval) return;
        _scanTimer = 0f;
        _scanDotCount = (_scanDotCount + 1) % 4;
        string dots = new string('.', _scanDotCount);
        if (_authValue != null)
        {
            _authValue.text  = "SCANNING" + dots;
            _authValue.color = Amber;
        }
    }

    private IEnumerator FlashAccess(bool granted)
    {
        _isFlashingAccess = true;
        Color flashCol = granted ? Green : Red;
        string text    = granted ? "GRANTED" : "DENIED";

        for (int i = 0; i < 4; i++)
        {
            SetValue(_accessValue, text, i % 2 == 0 ? flashCol : ValueWhite);
            yield return new WaitForSeconds(0.18f);
        }

        SetValue(_accessValue, text, flashCol);
        _isFlashingAccess = false;
    }

    // ── Helpers ───────────────────────────────────────────────────

    private void SetValue(TMP_Text label, string text, Color color)
    {
        if (label == null) return;
        label.text  = text;
        label.color = color;
    }

    // ── Canvas builder ────────────────────────────────────────────

    private void BuildCanvas()
    {
        // World-space canvas
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        gameObject.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = pixelsPerUnit;
        gameObject.AddComponent<GraphicRaycaster>();

        float pxW = panelWidth  * pixelsPerUnit;
        float pxH = panelHeight * pixelsPerUnit;

        RectTransform rt = GetComponent<RectTransform>();
        rt.sizeDelta   = new Vector2(pxW, pxH);
        transform.localScale = Vector3.one / pixelsPerUnit;

        // ── Background ────────────────────────────────────────────
        GameObject bg = Quad("Background", transform);
        StretchFill(bg.GetComponent<RectTransform>());
        AddImage(bg, BgColor);

        // ── Header bar ────────────────────────────────────────────
        const float headerH = 38f;
        GameObject header = Quad("Header", transform);
        RectTransform headerRT = header.GetComponent<RectTransform>();
        headerRT.anchorMin        = new Vector2(0f, 1f);
        headerRT.anchorMax        = new Vector2(1f, 1f);
        headerRT.pivot            = new Vector2(0.5f, 1f);
        headerRT.anchoredPosition = Vector2.zero;
        headerRT.sizeDelta        = new Vector2(0f, headerH);
        AddImage(header, HeaderColor);

        // Header title
        TMP_Text title = MakeText("Title", headerRT, "SMART GATE SYSTEM", 11f, ValueWhite,
            TextAlignmentOptions.MidlineLeft);
        RectFill(title.rectTransform, new Vector2(12f, 0f), new Vector2(-44f, 0f));

        // Live dot
        GameObject dotGO = Quad("LiveDot", headerRT);
        RectTransform dotRT = dotGO.GetComponent<RectTransform>();
        dotRT.anchorMin = dotRT.anchorMax = new Vector2(1f, 0.5f);
        dotRT.pivot     = new Vector2(1f, 0.5f);
        dotRT.anchoredPosition = new Vector2(-12f, 0f);
        dotRT.sizeDelta        = new Vector2(9f, 9f);
        _liveDot = AddImage(dotGO, Green);

        // ── Rows ──────────────────────────────────────────────────
        float rowH    = (pxH - headerH) / 4f;
        float yOffset = -headerH;

        (_gateValue,   _) = MakeRow(transform, "GateRow",    yOffset,          rowH, "GATE",           "CLOSED",  Red);
        (_vehicleValue,_) = MakeRow(transform, "VehicleRow", yOffset - rowH,   rowH, "VEHICLE",        "IDLE",    LabelColor);
        (_authValue,   _) = MakeRow(transform, "AuthRow",    yOffset - rowH*2, rowH, "AUTHENTICATION", "—",       LabelColor);
        (_accessValue, _) = MakeRow(transform, "AccessRow",  yOffset - rowH*3, rowH, "ACCESS",         "—",       LabelColor);
    }

    private (TMP_Text value, TMP_Text label) MakeRow(Transform parent, string name,
        float yOffset, float rowH, string labelStr, string valueStr, Color valueColor)
    {
        float pxW = panelWidth * pixelsPerUnit;

        // Row container
        GameObject rowGO = Quad(name, parent);
        RectTransform rowRT = rowGO.GetComponent<RectTransform>();
        rowRT.anchorMin        = new Vector2(0f, 1f);
        rowRT.anchorMax        = new Vector2(1f, 1f);
        rowRT.pivot            = new Vector2(0.5f, 1f);
        rowRT.anchoredPosition = new Vector2(0f, yOffset);
        rowRT.sizeDelta        = new Vector2(0f, rowH);

        // Divider at top
        GameObject div = Quad("Divider", rowRT);
        RectTransform divRT = div.GetComponent<RectTransform>();
        divRT.anchorMin = new Vector2(0f, 1f);
        divRT.anchorMax = new Vector2(1f, 1f);
        divRT.pivot     = new Vector2(0.5f, 1f);
        divRT.anchoredPosition = Vector2.zero;
        divRT.sizeDelta        = new Vector2(-16f, 1f);
        AddImage(div, DividerColor);

        // Dot indicator
        GameObject dotGO = Quad("Dot", rowRT);
        RectTransform dotRT = dotGO.GetComponent<RectTransform>();
        dotRT.anchorMin = dotRT.anchorMax = new Vector2(0f, 0.5f);
        dotRT.pivot     = new Vector2(0f, 0.5f);
        dotRT.anchoredPosition = new Vector2(10f, 0f);
        dotRT.sizeDelta        = new Vector2(5f, 5f);
        AddImage(dotGO, valueColor);

        // Label
        TMP_Text lbl = MakeText("Label", rowRT, labelStr, 8.5f, LabelColor, TextAlignmentOptions.MidlineLeft);
        lbl.rectTransform.anchorMin = new Vector2(0f, 0f);
        lbl.rectTransform.anchorMax = new Vector2(0.52f, 1f);
        lbl.rectTransform.offsetMin = new Vector2(20f, 0f);
        lbl.rectTransform.offsetMax = new Vector2(0f, -2f);

        // Value
        TMP_Text val = MakeText("Value", rowRT, valueStr, 11.5f, valueColor, TextAlignmentOptions.MidlineRight);
        val.fontStyle = FontStyles.Bold;
        val.rectTransform.anchorMin = new Vector2(0.48f, 0f);
        val.rectTransform.anchorMax = new Vector2(1f, 1f);
        val.rectTransform.offsetMin = Vector2.zero;
        val.rectTransform.offsetMax = new Vector2(-12f, -2f);

        return (val, lbl);
    }

    // ── Layout helpers ────────────────────────────────────────────

    private GameObject Quad(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private void StretchFill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private void RectFill(RectTransform rt, Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    private Image AddImage(GameObject go, Color color)
    {
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private TMP_Text MakeText(string name, RectTransform parent, string content,
        float size, Color color, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = content;
        tmp.fontSize  = size;
        tmp.color     = color;
        tmp.alignment = align;
        return tmp;
    }
}
