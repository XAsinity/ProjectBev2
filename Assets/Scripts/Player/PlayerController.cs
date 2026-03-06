using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float forwardSpeed = 7f;
    public float strafeSpeed = 5f;
    public float backwardSpeed = 5.5f;
    public float sprintMultiplier = 1.5f;

    [Header("Inertia")]
    public float accelerationTime = 0.3f;
    public float decelerationTime = 0.8f;

    [Header("Jumping")]
    public float jumpForce = 5f;

    [Header("Gravity")]
    public float gravityScale = 2f;

    private CharacterController controller;
    private Vector3 velocity = Vector3.zero;
    private float gravity = -9.81f;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private float currentSpeedMultiplier = 0f;
    private Vector3 currentVelocity = Vector3.zero;
    private Vector3 lastMoveDirection = Vector3.zero;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        var inputMap = InputSystem.actions;
        moveAction = inputMap.FindAction("Move");
        jumpAction = inputMap.FindAction("Jump");
        sprintAction = inputMap.FindAction("Sprint");
    }

    void Update()
    {
        HandleMovement();
    }

    void HandleMovement()
    {
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        bool isSprinting = sprintAction.IsPressed();

        // Calculate target speed based on direction
        float targetSpeed = CalculateMovementSpeed(moveInput, isSprinting);

        // Get move direction
        Vector3 moveDirection = Vector3.zero;
        if (moveInput.y != 0)
        {
            moveDirection += transform.forward * moveInput.y;
        }
        if (moveInput.x != 0)
        {
            moveDirection += transform.right * moveInput.x;
        }

        if (moveDirection.magnitude > 0)
        {
            moveDirection.Normalize();
            lastMoveDirection = moveDirection;
        }

        // Smoothly interpolate speed for momentum
        float acceleration = isSprinting && moveInput.y > 0 ? 1f / accelerationTime : 1f / decelerationTime;
        currentSpeedMultiplier = Mathf.Lerp(currentSpeedMultiplier, moveInput.magnitude > 0 ? 1f : 0f, acceleration * Time.deltaTime);

        // Apply momentum to horizontal movement
        Vector3 targetVelocity = lastMoveDirection * targetSpeed * currentSpeedMultiplier;
        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, acceleration * Time.deltaTime);

        controller.Move(currentVelocity * Time.deltaTime);

        // Apply gravity
        if (controller.isGrounded)
        {
            velocity.y = -0.5f;

            if (jumpAction.WasPressedThisFrame())
            {
                velocity.y = Mathf.Sqrt(jumpForce * -2f * (gravity * gravityScale));
            }
        }
        else
        {
            velocity.y += gravity * gravityScale * Time.deltaTime;
        }

        controller.Move(velocity * Time.deltaTime);
    }

    float CalculateMovementSpeed(Vector2 input, bool sprinting)
    {
        float speed = 0f;

        if (input.y > 0)
        {
            speed = forwardSpeed;
        }
        else if (input.y < 0)
        {
            speed = backwardSpeed;
        }

        if (input.x != 0)
        {
            if (speed == 0)
                speed = strafeSpeed;
            else
                speed = Mathf.Min(speed, strafeSpeed);
        }

        if (sprinting && input.y > 0)
        {
            speed *= sprintMultiplier;
        }

        return speed;
    }

    public float GetCurrentSpeed()
    {
        return currentVelocity.magnitude;
    }

    public bool IsMoving()
    {
        return controller.isGrounded && currentVelocity.magnitude > 0.1f;
    }
}