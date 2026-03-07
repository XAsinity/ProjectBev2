using UnityEngine;
using UnityEngine.InputSystem;

public class WolfMovementAudio : MonoBehaviour
{
    [Header("Run Audio")]
    public AudioSource runAudio;
    [Range(0f, 1f)]
    public float runMasterVolume = 0.03f;
    public float runFadeInTime = 0.2f;
    public float runFadeOutTime = 0.3f;
    [Range(0f, 1f)]
    public float runMaxVolumeGate = 0.033f;

    [Header("Slide Audio")]
    public AudioSource slideAudio;
    [Range(0f, 1f)]
    public float slideMasterVolume = 0.2f;
    public float slideMinSpeedToTrigger = 7f;
    public float slideDuration = 1f;
    public float slideFadeOutTime = 0.2f;
    public bool debugSlide = false;

    private PlayerController playerController;
    private CharacterController controller;
    private InputAction moveAction;
    private InputAction sprintAction;

    private float runTargetVolume = 0f;
    private float runCurrentVolume = 0f;
    private float slideCurrentVolume = 0f;
    private bool wasSprintingLastFrame = false;
    private bool isSliding = false;
    private float slideTimer = 0f;
    private bool hadInputDuringSlide = false;

    void Start()
    {
        var inputMap = InputSystem.actions;
        moveAction = inputMap.FindAction("Move");
        sprintAction = inputMap.FindAction("Sprint");

        playerController = GetComponent<PlayerController>();
        controller = GetComponent<CharacterController>();

        if (runAudio == null)
            Debug.LogError("Run Audio Source not assigned!");
        if (slideAudio == null)
            Debug.LogError("Slide Audio Source not assigned!");
    }

    void Update()
    {
        HandleMovementAudio();
    }

    void HandleMovementAudio()
    {
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        bool isSprinting = sprintAction.IsPressed();
        bool hasInput = moveInput.magnitude > 0;
        float currentSpeed = playerController.GetCurrentSpeed();
        bool isMoving = playerController.IsMoving();

        // Trigger slide only once when releasing sprint with high speed and input
        if (wasSprintingLastFrame && !isSprinting && hasInput && controller.isGrounded && currentSpeed > slideMinSpeedToTrigger)
        {
            if (debugSlide)
                Debug.Log($"[SLIDE TRIGGERED] Speed: {currentSpeed:F2}");

            isSliding = true;
            slideTimer = 0f;
            hadInputDuringSlide = false;
            slideAudio.PlayOneShot(slideAudio.clip);
        }

        // Track if player pressed input during slide
        if (isSliding && hasInput && slideTimer > 0.1f) // Small buffer to avoid instant interrupt
        {
            hadInputDuringSlide = true;
            if (debugSlide)
                Debug.Log("[INPUT DETECTED - Slide interrupt triggered]");
        }

        // Update slide - play for duration OR interrupt if input pressed
        if (isSliding)
        {
            slideTimer += Time.deltaTime;

            // Check if we should interrupt (new input, or came to stop)
            bool shouldInterrupt = hadInputDuringSlide || currentSpeed < 1f;

            if (shouldInterrupt)
            {
                // Fade out slide
                float fadeProgress = slideTimer / slideFadeOutTime;
                slideCurrentVolume = Mathf.Lerp(slideMasterVolume, 0f, fadeProgress);
                slideAudio.volume = slideCurrentVolume;

                if (fadeProgress >= 1f)
                {
                    if (debugSlide)
                        Debug.Log("[SLIDE ENDED - Interrupted]");
                    isSliding = false;
                }
            }
            else if (slideTimer >= slideDuration)
            {
                // Natural fade out after duration expires
                float fadeProgress = (slideTimer - slideDuration) / slideFadeOutTime;
                slideCurrentVolume = Mathf.Lerp(slideMasterVolume, 0f, fadeProgress);
                slideAudio.volume = slideCurrentVolume;

                if (fadeProgress >= 1f)
                {
                    if (debugSlide)
                        Debug.Log("[SLIDE ENDED - Duration expired]");
                    isSliding = false;
                }
            }
            else
            {
                // Slide is playing at full volume
                slideCurrentVolume = slideMasterVolume;
                slideAudio.volume = slideCurrentVolume;
            }
        }

        // Handle running audio
        if (isMoving && currentSpeed > 0.5f)
        {
            if (!runAudio.isPlaying)
            {
                runAudio.Play();
            }

            float maxSpeed = 10.5f;
            float speedRatio = Mathf.Clamp01(currentSpeed / maxSpeed);
            runTargetVolume = Mathf.Min(runMasterVolume * speedRatio, runMaxVolumeGate);

            // Fade in to target volume
            float runFadeSpeed = 1f / runFadeInTime;
            runCurrentVolume = Mathf.Lerp(runCurrentVolume, runTargetVolume, runFadeSpeed * Time.deltaTime);
        }
        else
        {
            // Fade out running audio
            float runFadeSpeed = 1f / runFadeOutTime;
            runCurrentVolume = Mathf.Lerp(runCurrentVolume, 0f, runFadeSpeed * Time.deltaTime);

            if (runCurrentVolume < 0.01f)
            {
                runAudio.Stop();
                runCurrentVolume = 0f;
            }
        }

        runAudio.volume = runCurrentVolume;
        wasSprintingLastFrame = isSprinting;
    }
}