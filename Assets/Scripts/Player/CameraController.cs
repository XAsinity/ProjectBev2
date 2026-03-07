using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 90f;

    [Header("Head Bob")]
    public float bobHeightMin = 0.02f;
    public float bobHeightMax = 0.08f;
    public float bobSpeedMultiplier = 0.5f;
    [Range(0f, 1f)]
    public float bobIntensity = 1f;

    private float xRotation = 0f;
    private InputAction lookAction;
    private InputAction escapeAction;
    private Vector3 originalCameraPos;
    private float bobTimer = 0f;
    private PlayerController playerController;
    private CharacterController controller;

    void Start()
    {
        var inputMap = InputSystem.actions;
        lookAction = inputMap.FindAction("Look");
        escapeAction = inputMap.FindAction("UI/Cancel"); // ESC key mapped in Input System

        originalCameraPos = transform.localPosition;
        playerController = GetComponentInParent<PlayerController>();
        controller = GetComponentInParent<CharacterController>();

        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        HandleLook();
        HandleEscape();
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

    void HandleEscape()
    {
        // Use Input System for ESC instead of Input.GetKeyDown
        if (escapeAction.WasPressedThisFrame())
        {
            Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? CursorLockMode.None : CursorLockMode.Locked;
        }
    }

    void LateUpdate()
    {
        HandleHeadBob();
    }

    void HandleHeadBob()
    {
        float currentSpeed = playerController.GetCurrentSpeed();
        bool isMoving = playerController.IsMoving();

        if (isMoving && controller.isGrounded && bobIntensity > 0f)
        {
            float maxSpeed = 10.5f;
            float speedRatio = Mathf.Clamp01(currentSpeed / maxSpeed);

            float bobFrequency = 4f + (speedRatio * 4f * bobSpeedMultiplier);
            bobTimer += Time.deltaTime * bobFrequency;

            float currentBobHeight = Mathf.Lerp(bobHeightMin, bobHeightMax, speedRatio);
            float bobHeight = Mathf.Sin(bobTimer) * currentBobHeight * bobIntensity;

            Vector3 newPos = originalCameraPos;
            newPos.y += bobHeight;

            transform.localPosition = newPos;
        }
        else if (!isMoving || !controller.isGrounded)
        {
            bobTimer = 0f;
            transform.localPosition = Vector3.Lerp(transform.localPosition, originalCameraPos, Time.deltaTime * 5f);
        }
    }
}