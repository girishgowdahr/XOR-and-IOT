using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Procedurally builds the Digital Twin corner HUD overlay at runtime.
/// Attach this to any GameObject in the scene alongside GateHUD.
/// It self-destructs after building to keep the scene clean.
/// </summary>
[RequireComponent(typeof(GateHUD))]
public class GateHUDBuilder : MonoBehaviour
{
    // ── Colors ────────────────────────────────────────────────────
    private static readonly Color BgPanel       = new Color(0.05f, 0.07f, 0.10f, 0.93f);
    private static readonly Color BgHeader      = new Color(0.08f, 0.47f, 0.85f, 1.00f);
    private static readonly Color BgRow         = new Color(0.07f, 0.10f, 0.14f, 0.00f);
    private static readonly Color Divider       = new Color(0.18f, 0.23f, 0.30f, 1.00f);
    private static readonly Color TextLabel     = new Color(0.55f, 0.62f, 0.72f, 1.00f);
    private static readonly Color TextValue     = new Color(0.95f, 0.95f, 0.95f, 1.00f);
    private static readonly Color ColorGreen    = new Color(0.13f, 0.77f, 0.37f, 1.00f);
    private static readonly Color ColorRed      = new Color(0.93f, 0.26f, 0.26f, 1.00f);
    private static readonly Color BattBarBg     = new Color(0.14f, 0.18f, 0.23f, 1.00f);

    private const float PanelW  = 270f;
    private const float HeaderH = 36f;
    private const float RowH    = 50f;
    private const int   RowCount = 5;
    private const float PanelH  = HeaderH + RowH * RowCount + 8f;
    private const float Margin  = 20f;

    void Awake()
    {
        BuildHUD();
    }

    private void BuildHUD()
    {
        GateHUD hud = GetComponent<GateHUD>();

        // ── Canvas ────────────────────────────────────────────────
        GameObject canvasGO = new GameObject("GateHUDCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── HUD Panel (top-right corner) ──────────────────────────
        RectTransform panel = MakeRect("HUDPanel", canvasGO.transform);
        panel.anchorMin        = new Vector2(1f, 1f);
        panel.anchorMax        = new Vector2(1f, 1f);
        panel.pivot            = new Vector2(1f, 1f);
        panel.anchoredPosition = new Vector2(-Margin, -Margin);
        panel.sizeDelta        = new Vector2(PanelW, PanelH);
        AddImage(panel.gameObject, BgPanel);

        // ── Header ────────────────────────────────────────────────
        RectTransform header = MakeRect("Header", panel);
        StretchTop(header, HeaderH);
        AddImage(header.gameObject, BgHeader);

        // Title
        TMP_Text title = MakeText("TitleText", header, "DIGITAL TWIN · GATE MONITOR", 10f, TextValue, TextAlignmentOptions.MidlineLeft);
        RectTransform titleRT = title.rectTransform;
        titleRT.anchorMin = Vector2.zero;
        titleRT.anchorMax = Vector2.one;
        titleRT.offsetMin = new Vector2(12f, 0f);
        titleRT.offsetMax = Vector2.zero;

        // Live dot
        RectTransform liveDotRT = MakeRect("LiveDot", header);
        liveDotRT.anchorMin        = new Vector2(1f, 0.5f);
        liveDotRT.anchorMax        = new Vector2(1f, 0.5f);
        liveDotRT.pivot            = new Vector2(1f, 0.5f);
        liveDotRT.anchoredPosition = new Vector2(-10f, 0f);
        liveDotRT.sizeDelta        = new Vector2(8f, 8f);
        Image liveDotImg = AddImage(liveDotRT.gameObject, ColorGreen);

        // ── Rows ──────────────────────────────────────────────────
        float yBase = -HeaderH;

        // Row 0 — Gate Status
        MakeRow(panel, "GateStatusRow", yBase, out TMP_Text lbl0, out TMP_Text val0);
        lbl0.text = "Gate Status";
        val0.text = "CLOSED";
        val0.color = ColorRed;
        // Status dot
        RectTransform statusDot = MakeRect("StatusDot", panel.Find("GateStatusRow"));
        statusDot.anchorMin        = new Vector2(1f, 0.5f);
        statusDot.anchorMax        = new Vector2(1f, 0.5f);
        statusDot.pivot            = new Vector2(1f, 0.5f);
        statusDot.anchoredPosition = new Vector2(-10f, 0f);
        statusDot.sizeDelta        = new Vector2(8f, 8f);
        Image statusDotImg = AddImage(statusDot.gameObject, ColorRed);

        // Row 1 — Battery
        yBase -= RowH;
        MakeRow(panel, "BatteryRow", yBase, out TMP_Text lbl1, out TMP_Text val1);
        lbl1.text = "Battery";
        val1.text = "87%";
        // Battery bar
        RectTransform battBg = MakeRect("BattBg", panel.Find("BatteryRow"));
        battBg.anchorMin        = new Vector2(0f, 0f);
        battBg.anchorMax        = new Vector2(1f, 0f);
        battBg.pivot            = new Vector2(0.5f, 0f);
        battBg.anchoredPosition = new Vector2(0f, 4f);
        battBg.sizeDelta        = new Vector2(-16f, 4f);
        AddImage(battBg.gameObject, BattBarBg);

        RectTransform battFill = MakeRect("BattFill", battBg);
        battFill.anchorMin = Vector2.zero;
        battFill.anchorMax = Vector2.one;
        battFill.offsetMin = Vector2.zero;
        battFill.offsetMax = Vector2.zero;
        Image battFillImg = AddImage(battFill.gameObject, ColorGreen);
        battFillImg.type       = Image.Type.Filled;
        battFillImg.fillMethod = Image.FillMethod.Horizontal;
        battFillImg.fillAmount = 0.87f;

        // Row 2 — Signal
        yBase -= RowH;
        MakeRow(panel, "SignalRow", yBase, out TMP_Text lbl2, out TMP_Text val2);
        lbl2.text = "Signal Strength";
        val2.text = "-39 dBm";

        // Row 3 — Temperature
        yBase -= RowH;
        MakeRow(panel, "TempRow", yBase, out TMP_Text lbl3, out TMP_Text val3);
        lbl3.text = "Temperature";
        val3.text = "34.0°C";

        // Row 4 — Events Today
        yBase -= RowH;
        MakeRow(panel, "EventsRow", yBase, out TMP_Text lbl4, out TMP_Text val4);
        lbl4.text = "Events Today";
        val4.text = "24";
        val4.color = ColorGreen;

        // ── Wire GateHUD references ───────────────────────────────
        hud.gateStatusValue      = val0;
        hud.gateStatusIndicator  = statusDotImg;
        hud.batteryValue         = val1;
        hud.batteryFill          = battFillImg;
        hud.signalValue          = val2;
        hud.temperatureValue     = val3;
        hud.eventsValue          = val4;
        hud.liveDot              = liveDotImg;

        Debug.Log("[GateHUDBuilder] HUD built successfully.");
        Destroy(this);   // clean up builder after wiring
    }

    // ── Helpers ────────────────────────────────────────────────────

    private void MakeRow(RectTransform parent, string rowName, float yOffset,
                         out TMP_Text labelText, out TMP_Text valueText)
    {
        RectTransform row = MakeRect(rowName, parent);
        row.anchorMin        = new Vector2(0f, 1f);
        row.anchorMax        = new Vector2(1f, 1f);
        row.pivot            = new Vector2(0.5f, 1f);
        row.anchoredPosition = new Vector2(0f, yOffset);
        row.sizeDelta        = new Vector2(-8f, RowH);
        AddImage(row.gameObject, BgRow);

        // Divider line at top of row
        RectTransform div = MakeRect("Divider", row);
        div.anchorMin        = new Vector2(0f, 1f);
        div.anchorMax        = new Vector2(1f, 1f);
        div.pivot            = new Vector2(0.5f, 1f);
        div.anchoredPosition = Vector2.zero;
        div.sizeDelta        = new Vector2(0f, 1f);
        AddImage(div.gameObject, Divider);

        // Label (left side)
        labelText = MakeText("Label", row, "", 9f, TextLabel, TextAlignmentOptions.MidlineLeft);
        RectTransform lblRT = labelText.rectTransform;
        lblRT.anchorMin = new Vector2(0f, 0f);
        lblRT.anchorMax = new Vector2(0.5f, 1f);
        lblRT.offsetMin = new Vector2(8f, 0f);
        lblRT.offsetMax = new Vector2(0f, -2f);

        // Value (right side)
        valueText = MakeText("Value", row, "", 14f, TextValue, TextAlignmentOptions.MidlineRight);
        valueText.fontStyle = FontStyles.Bold;
        RectTransform valRT = valueText.rectTransform;
        valRT.anchorMin = new Vector2(0.5f, 0f);
        valRT.anchorMax = new Vector2(1f, 1f);
        valRT.offsetMin = Vector2.zero;
        valRT.offsetMax = new Vector2(-10f, -2f);
    }

    private RectTransform MakeRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private void StretchTop(RectTransform rt, float height)
    {
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = new Vector2(0f, height);
    }

    private Image AddImage(GameObject go, Color color)
    {
        Image img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private TMP_Text MakeText(string name, RectTransform parent, string content,
                              float size, Color color, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = content;
        tmp.fontSize  = size;
        tmp.color     = color;
        tmp.alignment = align;
        return tmp;
    }
}
