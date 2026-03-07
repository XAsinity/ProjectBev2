using UnityEngine;

public class DebugController : MonoBehaviour
{
    private CharacterController controller;
    private PlayerController playerController;
    private float lastLogTime = 0f;
    private float logThrottle = 0.5f; // Log every 0.5 seconds
    private bool wasGroundedLastFrame = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerController = GetComponent<PlayerController>();
    }

    void Update()
    {
        // Prioritize state changes (grounded → airborne immediately)
        if (controller.isGrounded != wasGroundedLastFrame)
        {
            if (controller.isGrounded)
            {
                Debug.Log($"[LANDED] Speed: {playerController.GetCurrentSpeed():F2}");
            }
            else
            {
                Debug.Log("[AIRBORNE]");
            }
            wasGroundedLastFrame = controller.isGrounded;
            lastLogTime = Time.time; // Reset throttle on state change
        }

        // Regular throttled logging
        if (Time.time - lastLogTime >= logThrottle)
        {
            if (controller.isGrounded)
            {
                Debug.Log($"Grounded | Speed: {playerController.GetCurrentSpeed():F2}");
            }
            lastLogTime = Time.time;
        }
    }
}