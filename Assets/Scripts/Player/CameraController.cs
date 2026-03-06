using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 90f;

    [Header("Head Bob")]
    public float bobHeightMin = 0.02f; // Bob height when walking
    public float bobHeightMax = 0.08f; // Bob height when sprinting
    public float bobSpeedMultiplier = 0.5f; // How much speed affects bob frequency
    [Range(0f, 1f)]
    public float bobIntensity = 1f; // Motion sickness control (0 = disabled, 1 = full)

    private float xRotation = 0f;
    private InputAction lookAction;
    private Vector3 originalCameraPos;
    private float bobTimer = 0f;
    private PlayerController playerController;

    void Start()
    {
        var inputMap = InputSystem.actions;
        lookAction = inputMap.FindAction("Look");

        // Get the camera's original position
        originalCameraPos = transform.localPosition;

        // Get reference to player controller
        playerController = GetComponentInParent<PlayerController>();

        // Lock cursor to center of screen
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        HandleLook();
        HandleHeadBob();

        // Unlock cursor with ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? CursorLockMode.None : CursorLockMode.Locked;
        }
    }

    void HandleLook()
    {
        Vector2 lookInput = lookAction.ReadValue<Vector2>();

        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.parent.Rotate(Vector3.up * mouseX);
    }

    void HandleHeadBob()
    {
        CharacterController controller = GetComponentInParent<CharacterController>();
        float currentSpeed = playerController.GetCurrentSpeed();
        bool isMoving = playerController.IsMoving();

        // Only bob if grounded and moving
        if (controller.isGrounded && isMoving && bobIntensity > 0f)
        {
            // Normalize speed (0 to 1, where 1 is max sprint speed of 10.5)
            float maxSpeed = 10.5f;
            float speedRatio = Mathf.Clamp01(currentSpeed / maxSpeed);

            // Bob frequency increases slightly with speed
            float bobFrequency = 4f + (speedRatio * 4f * bobSpeedMultiplier);
            bobTimer += Time.deltaTime * bobFrequency;

            // Bob height increases with speed
            float currentBobHeight = Mathf.Lerp(bobHeightMin, bobHeightMax, speedRatio);
            float bobHeight = Mathf.Sin(bobTimer) * currentBobHeight * bobIntensity;

            Vector3 newPos = originalCameraPos;
            newPos.y += bobHeight;

            transform.localPosition = newPos;
        }
        else
        {
            // Return to original position when not moving
            bobTimer = 0f;
            transform.localPosition = originalCameraPos;
        }
    }
}