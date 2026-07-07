using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float gravity = -9.81f;

    private CharacterController controller;
    private Vector3 velocity;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // VRPlayerController handles all locomotion in XR — skip to avoid double movement and gravity
        if (UnityEngine.XR.XRSettings.enabled && UnityEngine.XR.XRSettings.isDeviceActive) return;

        float horizontal = 0;
        float vertical = 0;

        if (Input.GetKey(KeyCode.W)) vertical = 1;
        if (Input.GetKey(KeyCode.S)) vertical = -1;
        if (Input.GetKey(KeyCode.A)) horizontal = -1;
        if (Input.GetKey(KeyCode.D)) horizontal = 1;

        Vector3 move = transform.right * horizontal + transform.forward * vertical;

        controller.Move(move * moveSpeed * Time.deltaTime);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}