using UnityEngine;
using UnityEngine.InputSystem;

public class DirtSlideAudio : MonoBehaviour
{
    public AudioSource dirtSlideAudio;
    [Range(0f, 1f)]
    public float slideVolume = 0.8f;
    public float minSpeedToSlide = 5f;

    private InputAction moveAction;
    private InputAction sprintAction;
    private PlayerController playerController;
    private CharacterController controller;
    private bool wasSprintingLastFrame = false;

    void Start()
    {
        var inputMap = InputSystem.actions;
        moveAction = inputMap.FindAction("Move");
        sprintAction = inputMap.FindAction("Sprint");

        playerController = GetComponent<PlayerController>();
        controller = GetComponent<CharacterController>();

        if (dirtSlideAudio == null)
        {
            Debug.LogError("Dirt Slide Audio Source not assigned!");
        }
    }

    void Update()
    {
        HandleDirtSlide();
    }

    void HandleDirtSlide()
    {
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        bool isSprinting = sprintAction.IsPressed();
        float currentSpeed = playerController.GetCurrentSpeed();
        bool isMoving = moveInput.magnitude > 0;

        // Check if we just stopped sprinting while moving
        if (wasSprintingLastFrame && !isSprinting && isMoving && controller.isGrounded && currentSpeed > minSpeedToSlide)
        {
            // Play the dirt slide sound once
            dirtSlideAudio.volume = slideVolume;
            dirtSlideAudio.PlayOneShot(dirtSlideAudio.clip);
        }

        wasSprintingLastFrame = isSprinting;
    }
}