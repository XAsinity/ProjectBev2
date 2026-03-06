using UnityEngine;
using UnityEngine.InputSystem;

public class FootstepAudio : MonoBehaviour
{
    public AudioSource footstepAudio;
    [Range(0f, 1f)]
    public float masterVolume = 0.7f;
    public float fadeInTime = 0.2f;
    public float fadeOutTime = 0.3f;
    [Range(0f, 1f)]
    public float maxVolumeGate = 0.8f; // Maximum volume limit

    private PlayerController playerController;
    private CharacterController controller;
    private float targetVolume = 0f;
    private float currentVolumeSmoothing = 0f;

    void Start()
    {
        playerController = GetComponent<PlayerController>();
        controller = GetComponent<CharacterController>();

        if (footstepAudio == null)
        {
            Debug.LogError("Footstep Audio Source not assigned!");
        }
    }

    void Update()
    {
        HandleFootstepAudio();
    }

    void HandleFootstepAudio()
    {
        float currentSpeed = playerController.GetCurrentSpeed();
        float maxSpeed = 10.5f;
        float speedRatio = Mathf.Clamp01(currentSpeed / maxSpeed);

        if (playerController.IsMoving() && currentSpeed > 0.5f)
        {
            if (!footstepAudio.isPlaying)
            {
                footstepAudio.Play();
            }

            // Calculate target volume based on speed, then clamp to max gate
            targetVolume = Mathf.Min(masterVolume * speedRatio, maxVolumeGate);

            // Smoothly fade in to target volume
            float fadeSpeed = 1f / fadeInTime;
            currentVolumeSmoothing = Mathf.Lerp(currentVolumeSmoothing, targetVolume, fadeSpeed * Time.deltaTime);
        }
        else
        {
            // Smoothly fade out
            float fadeSpeed = 1f / fadeOutTime;
            currentVolumeSmoothing = Mathf.Lerp(currentVolumeSmoothing, 0f, fadeSpeed * Time.deltaTime);

            // Stop audio when fully faded
            if (currentVolumeSmoothing < 0.01f)
            {
                footstepAudio.Stop();
                currentVolumeSmoothing = 0f;
            }
        }

        footstepAudio.volume = currentVolumeSmoothing;
    }
}