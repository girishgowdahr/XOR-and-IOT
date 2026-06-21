using UnityEngine;

/// <summary>
/// Holds live IoT telemetry values displayed on the Gate HUD overlay.
/// Values can be set from real IoT data sources or simulated in the editor.
/// </summary>
[CreateAssetMenu(fileName = "GateHUDData", menuName = "XOR/Gate HUD Data")]
public class GateHUDData : ScriptableObject
{
    [Header("Power")]
    [Range(0f, 100f)]
    public float batteryPercent = 87f;

    [Header("Connectivity")]
    [Range(-120f, 0f)]
    public float signalStrengthDbm = -39f;

    [Header("Environment")]
    public float temperatureCelsius = 34f;

    [Header("Activity")]
    public int eventsToday = 24;

    [Header("Simulation")]
    [Tooltip("Randomly fluctuate values to simulate live IoT data feed.")]
    public bool simulateLiveData = true;

    [Tooltip("How often (in seconds) simulated values update.")]
    public float simulationInterval = 2f;
}
