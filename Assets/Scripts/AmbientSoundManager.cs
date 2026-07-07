using UnityEngine;

/// <summary>
/// Manages contextual ambient audio zones throughout the residential scene.
/// Each zone has an AudioSource and a radius. The volume of each source is
/// driven by the player's distance from the zone centre.
/// </summary>
public class AmbientSoundManager : MonoBehaviour
{
    [System.Serializable]
    public class AmbientZone
    {
        [Tooltip("Name for inspector clarity.")]
        public string label = "Zone";
        [Tooltip("World-space centre of this ambient zone.")]
        public Transform centre;
        [Tooltip("AudioSource that plays the ambient clip for this zone.")]
        public AudioSource audioSource;
        [Tooltip("Distance at which volume is at full.")]
        public float innerRadius = 5f;
        [Tooltip("Distance at which volume has faded to zero.")]
        public float outerRadius = 20f;
        [Tooltip("Maximum volume for this zone.")]
        [Range(0f, 1f)]
        public float maxVolume = 1f;
    }

    [Header("Zones")]
    public AmbientZone[] zones;

    [Header("Player Reference")]
    [Tooltip("Transform used to measure distance to each zone.")]
    public Transform playerTransform;

    [Header("Footstep Audio")]
    public AudioSource footstepAudio;
    [Tooltip("Minimum speed (m/s) before footsteps trigger.")]
    public float footstepSpeedThreshold = 0.5f;
    [Tooltip("Seconds between footstep sounds at walk speed.")]
    public float footstepInterval = 0.5f;

    // ── Private ───────────────────────────────────────────────────
    private CharacterController _characterController;
    private float _footstepTimer;

    void Start()
    {
        if (playerTransform != null)
            _characterController = playerTransform.GetComponent<CharacterController>();

        foreach (AmbientZone zone in zones)
        {
            if (zone.audioSource == null) continue;
            zone.audioSource.loop   = true;
            zone.audioSource.volume = 0f;
            if (!zone.audioSource.isPlaying) zone.audioSource.Play();
        }
    }

    void Update()
    {
        if (playerTransform == null) return;

        UpdateZoneVolumes();
        HandleFootsteps();
    }

    // ── Zone volume ───────────────────────────────────────────────

    private void UpdateZoneVolumes()
    {
        foreach (AmbientZone zone in zones)
        {
            if (zone.audioSource == null || zone.centre == null) continue;

            float dist = Vector3.Distance(playerTransform.position, zone.centre.position);
            float t    = Mathf.InverseLerp(zone.outerRadius, zone.innerRadius, dist);
            float targetVolume = Mathf.Lerp(0f, zone.maxVolume, t);

            zone.audioSource.volume = Mathf.MoveTowards(
                zone.audioSource.volume, targetVolume, 2f * Time.deltaTime);
        }
    }

    // ── Footsteps ─────────────────────────────────────────────────

    private void HandleFootsteps()
    {
        if (footstepAudio == null || _characterController == null) return;

        float speed = new Vector3(
            _characterController.velocity.x, 0f, _characterController.velocity.z).magnitude;

        if (speed < footstepSpeedThreshold)
        {
            _footstepTimer = 0f;
            return;
        }

        // Speed ratio shortens the interval when running
        float speedRatio = Mathf.Clamp01(speed / 7f);
        float interval   = Mathf.Lerp(footstepInterval, footstepInterval * 0.4f, speedRatio);

        _footstepTimer += Time.deltaTime;
        if (_footstepTimer < interval) return;

        _footstepTimer = 0f;
        footstepAudio.pitch  = Random.Range(0.9f, 1.1f);
        footstepAudio.volume = Random.Range(0.7f, 1f);
        footstepAudio.Play();
    }

    // ── Gizmos ────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        if (zones == null) return;
        foreach (AmbientZone zone in zones)
        {
            if (zone.centre == null) continue;
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.2f);
            Gizmos.DrawSphere(zone.centre.position, zone.innerRadius);
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.05f);
            Gizmos.DrawSphere(zone.centre.position, zone.outerRadius);
        }
    }
}
