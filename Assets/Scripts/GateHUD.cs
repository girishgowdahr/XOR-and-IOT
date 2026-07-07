using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the corner HUD overlay displaying Digital Twin gate telemetry.
/// Reads gate state from GateController and IoT metrics from GateHUDData.
/// </summary>
public class GateHUD : MonoBehaviour
{
    [Header("Data Sources")]
    [Tooltip("Assign either a GateController or a SmartGateController here.")]
    public GateController gateController;
    [Tooltip("Assign a SmartGateController if using the enhanced gate system.")]
    public SmartGateController smartGateController;
    public GateHUDData hudData;

    [Header("Status Row")]
    public TMP_Text gateStatusValue;
    public Image gateStatusIndicator;

    [Header("Battery Row")]
    public TMP_Text batteryValue;
    public Image batteryFill;

    [Header("Signal Row")]
    public TMP_Text signalValue;

    [Header("Temperature Row")]
    public TMP_Text temperatureValue;

    [Header("Events Row")]
    public TMP_Text eventsValue;

    [Header("Live Dot")]
    public Image liveDot;

    // Colors
    private static readonly Color ColorOpen      = new Color(0.13f, 0.77f, 0.37f);   // green
    private static readonly Color ColorClosed    = new Color(0.93f, 0.26f, 0.26f);   // red
    private static readonly Color ColorBattGood  = new Color(0.13f, 0.77f, 0.37f);
    private static readonly Color ColorBattLow   = new Color(0.93f, 0.60f, 0.13f);
    private static readonly Color ColorBattCrit  = new Color(0.93f, 0.26f, 0.26f);

    private float _simulationTimer;
    private bool _liveDotVisible = true;
    private float _liveDotTimer;
    private const float LiveDotBlinkInterval = 0.8f;

    // Cached gate state read via reflection-free public property
    private bool _isGateOpen = true;

    void Start()
    {
        RefreshAll();
    }

    void Update()
    {
        if (hudData == null) return;

        // Simulate live data fluctuation
        if (hudData.simulateLiveData)
        {
            _simulationTimer += Time.deltaTime;
            if (_simulationTimer >= hudData.simulationInterval)
            {
                _simulationTimer = 0f;
                SimulateValues();
                RefreshAll();
            }
        }

        // Blink live dot
        _liveDotTimer += Time.deltaTime;
        if (_liveDotTimer >= LiveDotBlinkInterval)
        {
            _liveDotTimer = 0f;
            _liveDotVisible = !_liveDotVisible;
            if (liveDot != null)
                liveDot.color = _liveDotVisible
                    ? ColorOpen
                    : new Color(ColorOpen.r, ColorOpen.g, ColorOpen.b, 0.2f);
        }
    }

    /// <summary>Call this whenever gate state changes externally.</summary>
    public void RefreshAll()
    {
        RefreshGateStatus();
        RefreshBattery();
        RefreshSignal();
        RefreshTemperature();
        RefreshEvents();
    }

    // ──────────────────────────────────────────────────────────────
    //  Refresh methods
    // ──────────────────────────────────────────────────────────────

    private void RefreshGateStatus()
    {
        // Prefer SmartGateController if assigned; fall back to legacy GateController
        bool hasController = smartGateController != null || gateController != null;
        if (!hasController) return;

        _isGateOpen = smartGateController != null
            ? smartGateController.IsOpen
            : gateController.IsOpen;

        if (gateStatusValue != null)
        {
            gateStatusValue.text  = _isGateOpen ? "OPEN" : "CLOSED";
            gateStatusValue.color = _isGateOpen ? ColorOpen : ColorClosed;
        }

        if (gateStatusIndicator != null)
            gateStatusIndicator.color = _isGateOpen ? ColorOpen : ColorClosed;
    }

    private void RefreshBattery()
    {
        if (hudData == null) return;
        float pct = hudData.batteryPercent;

        if (batteryValue != null)
            batteryValue.text = $"{pct:F0}%";

        if (batteryFill != null)
        {
            batteryFill.fillAmount = pct / 100f;
            batteryFill.color = pct > 50f ? ColorBattGood
                              : pct > 20f ? ColorBattLow
                                          : ColorBattCrit;
        }
    }

    private void RefreshSignal()
    {
        if (hudData == null || signalValue == null) return;
        signalValue.text = $"{hudData.signalStrengthDbm:F0} dBm";
    }

    private void RefreshTemperature()
    {
        if (hudData == null || temperatureValue == null) return;
        temperatureValue.text = $"{hudData.temperatureCelsius:F1}°C";
    }

    private void RefreshEvents()
    {
        if (hudData == null || eventsValue == null) return;
        eventsValue.text = hudData.eventsToday.ToString();
    }

    // ──────────────────────────────────────────────────────────────
    //  Simulation
    // ──────────────────────────────────────────────────────────────

    private void SimulateValues()
    {
        hudData.batteryPercent     = Mathf.Clamp(hudData.batteryPercent     + Random.Range(-0.3f, 0.1f), 0f, 100f);
        hudData.signalStrengthDbm  = Mathf.Clamp(hudData.signalStrengthDbm  + Random.Range(-2f,   2f),  -120f, 0f);
        hudData.temperatureCelsius = hudData.temperatureCelsius + Random.Range(-0.2f, 0.3f);

        // Refresh gate status on every simulation tick
        RefreshGateStatus();
    }
}
