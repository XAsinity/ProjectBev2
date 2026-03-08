using UnityEngine;
using System.Collections;

public class HunterAI : MonoBehaviour
{
    [Header("Hunter Settings")]
    public AudioSource antlerAudioSource;
    public AudioClip antlerBashSound;

    public AudioSource doeCallAudioSource;
    public AudioClip doeCallSound;
    public float doeCallCutoffTime = 10f;

    [Header("Death Settings")]
    public AudioSource gunshotAudioSource;
    public AudioClip gunshotSound;
    public float gunshotCutoffTime = 1.8f;
    public CanvasGroup deathScreenCanvasGroup;
    public float deathScreenFadeDuration = 2f;
    public float fadeToBlackDelay = 2.4f;

    [Header("Luring Behavior (Heard from Far Away)")]
    [Range(0f, 120f)]
    public float minTimeBetweenLures = 60f;

    [Range(0f, 120f)]
    public float maxTimeBetweenLures = 120f;

    [Range(0.3f, 1f)]
    public float lureVolume = 0.6f;

    [Header("Hunting Behavior (When Player is Close)")]
    [Range(5f, 50f)]
    public float huntingDetectionRange = 20f;

    [Range(0f, 60f)]
    public float minTimeBetweenHunts = 15f;

    [Range(0f, 60f)]
    public float maxTimeBetweenHunts = 40f;

    [Range(0.5f, 1f)]
    public float huntingVolume = 0.8f;

    [Header("Movement (Hunting)")]
    public float huntingMoveSpeed = 8f;
    public float killDistance = 2f;

    [Header("Spawning")]
    public Terrain terrain;
    public float spawnHeightAboveTerrain = 20f;

    [Header("Performance")]
    public float terrainSampleInterval = 0.5f;

    public bool isActive = true;
    public bool debugMode = true;

    private Transform playerTransform;
    private float lureSoundTimer = 0f;
    private float huntSoundTimer = 0f;
    private float nextLureTime;
    private float nextHuntTime;
    private bool isPlayerInHuntingRange = false;
    private bool hasPlayedWarningSound = false;
    private bool isHunting = false;
    private Coroutine doeCallCutoffCoroutine;
    private Coroutine gunshotCutoffCoroutine;
    private bool isSoundPlaying = false;
    private float soundDuration = 0f;
    private float soundPlayTime = 0f;
    private bool playerDead = false;
    private Rigidbody hunterRigidbody;
    private float terrainSampleTimer = 0f;

    void Start()
    {
        // Find the player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            if (debugMode)
                Debug.Log("[HUNTER] Player found! Beginning to lure...");
        }
        else
        {
            Debug.LogError("Hunter: Player not found! Make sure player has 'Player' tag.");
        }

        if (antlerAudioSource == null)
        {
            antlerAudioSource = GetComponent<AudioSource>();
            if (debugMode)
                Debug.Log("[HUNTER] Antler Audio Source acquired.");
        }

        if (doeCallAudioSource == null)
        {
            Debug.LogError("Hunter: Doe Call Audio Source not assigned!");
        }

        if (gunshotAudioSource == null)
        {
            Debug.LogError("Hunter: Gunshot Audio Source not assigned!");
        }

        if (antlerBashSound == null || doeCallSound == null)
        {
            Debug.LogError("Hunter: One or more sounds not assigned!");
        }
        else
        {
            if (debugMode)
                Debug.Log("[HUNTER] All sounds loaded successfully!");
        }

        // Setup death screen
        if (deathScreenCanvasGroup == null)
        {
            CanvasGroup[] canvasGroups = FindObjectsByType<CanvasGroup>(FindObjectsSortMode.None);
            if (canvasGroups.Length > 0)
            {
                deathScreenCanvasGroup = canvasGroups[0];
                deathScreenCanvasGroup.alpha = 0f;
                if (debugMode)
                    Debug.Log("[HUNTER] Death Screen Canvas Group found automatically.");
            }
            else
            {
                Debug.LogWarning("[HUNTER] Death Screen Canvas Group not found!");
            }
        }
        else
        {
            deathScreenCanvasGroup.alpha = 0f;
        }

        // Get rigidbody for movement
        hunterRigidbody = GetComponent<Rigidbody>();
        if (hunterRigidbody == null)
        {
            hunterRigidbody = gameObject.AddComponent<Rigidbody>();
            hunterRigidbody.isKinematic = true;
            if (debugMode)
                Debug.Log("[HUNTER] Rigidbody added (kinematic)");
        }

        // Find terrain if not assigned
        if (terrain == null)
        {
            terrain = Terrain.activeTerrain;
            if (terrain == null)
            {
                Debug.LogWarning("[HUNTER] No terrain found! Hunter won't spawn properly.");
            }
        }

        // Spawn hunter at random location above terrain
        SpawnHunter();

        SetNextLureTime();
        SetNextHuntingTime();

        if (debugMode)
            Debug.Log($"[HUNTER] First lure will play in {nextLureTime:F1} seconds");
    }

    void SpawnHunter()
    {
        if (terrain == null)
        {
            if (debugMode)
                Debug.Log("[HUNTER] Spawning at origin (no terrain)");
            return;
        }

        // Get random position on terrain
        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainSize = terrainData.size;
        Vector3 terrainPos = terrain.transform.position;

        // Random X, Z within terrain bounds
        float randomX = Random.Range(0f, terrainSize.x);
        float randomZ = Random.Range(0f, terrainSize.z);

        // Get terrain height at that position
        float terrainHeight = terrain.SampleHeight(new Vector3(randomX, 0, randomZ) + terrainPos);

        // Spawn above terrain
        Vector3 spawnPos = terrainPos + new Vector3(randomX, terrainHeight + spawnHeightAboveTerrain, randomZ);
        transform.position = spawnPos;

        if (debugMode)
            Debug.Log($"[HUNTER] Spawned at: {spawnPos}");
    }

    void Update()
    {
        if (!isActive || playerTransform == null || playerDead)
            return;

        // Update sound playing status based on duration
        if (isSoundPlaying)
        {
            float soundElapsed = Time.time - soundPlayTime;
            if (soundElapsed >= soundDuration)
            {
                isSoundPlaying = false;
                if (debugMode)
                    Debug.Log($"[HUNTER] Sound finished playing (duration was {soundDuration:F2}s)");
            }
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // Check if player entered hunting range
        bool playerWasInRange = isPlayerInHuntingRange;
        isPlayerInHuntingRange = (distanceToPlayer <= huntingDetectionRange);

        // Player entered hunting range
        if (isPlayerInHuntingRange && !playerWasInRange)
        {
            isHunting = true;
            huntSoundTimer = 0f;
            SetNextHuntingTime();
            if (debugMode)
                Debug.Log($"[HUNTER] ⚠️ PLAYER ENTERED HUNTING RANGE! Distance: {distanceToPlayer:F2}m");
        }
        // Player left hunting range
        else if (!isPlayerInHuntingRange && playerWasInRange)
        {
            isHunting = false;
            hasPlayedWarningSound = false;
            lureSoundTimer = 0f;
            SetNextLureTime();
            if (debugMode)
                Debug.Log($"[HUNTER] Player left hunting range. Resuming luring...");
        }

        if (isHunting)
        {
            // Move toward player when hunting (with terrain sample optimization)
            terrainSampleTimer += Time.deltaTime;
            if (terrainSampleTimer >= terrainSampleInterval)
            {
                MoveTowardPlayer();
                terrainSampleTimer = 0f;
            }

            // HUNTING MODE - sounds (but NOT during aggressive phase)
            if (!hasPlayedWarningSound)
            {
                huntSoundTimer += Time.deltaTime;

                if (huntSoundTimer >= nextHuntTime && !isSoundPlaying)
                {
                    PlayHuntingSound(distanceToPlayer);
                    SetNextHuntingTime();
                    huntSoundTimer = 0f;
                }
            }

            // KILL when close enough
            if (distanceToPlayer < killDistance && !playerDead)
            {
                if (debugMode)
                    Debug.Log($"[HUNTER] ☠️ PLAYER IN KILL RANGE ({distanceToPlayer:F2}m)!");
                KillPlayer();
            }

            // Critical range trigger
            if (distanceToPlayer < 5f && !hasPlayedWarningSound)
            {
                hasPlayedWarningSound = true;
                if (debugMode)
                    Debug.Log("[HUNTER] ☠️ CRITICAL RANGE - FINAL ATTACK PHASE!");
                StartCoroutine(AggressiveHuntingPhase());
            }
        }
        else
        {
            // LURING MODE - don't move, just make sounds
            lureSoundTimer += Time.deltaTime;

            if (lureSoundTimer >= nextLureTime && !isSoundPlaying)
            {
                PlayLuringSound();
                SetNextLureTime();
                lureSoundTimer = 0f;
            }
        }
    }

    void MoveTowardPlayer()
    {
        if (playerTransform == null)
            return;

        Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
        Vector3 newPos = transform.position + directionToPlayer * huntingMoveSpeed * Time.deltaTime;

        // Keep hunter above terrain
        if (terrain != null)
        {
            float terrainHeight = terrain.SampleHeight(newPos) + terrain.transform.position.y;
            newPos.y = Mathf.Max(newPos.y, terrainHeight + 5f);
        }

        transform.position = newPos;
    }

    void PlayLuringSound()
    {
        bool useDoeCall = Random.value > 0.5f;

        if (useDoeCall && doeCallSound != null && doeCallAudioSource != null)
        {
            PlayDoeCall();
        }
        else if (antlerBashSound != null && antlerAudioSource != null)
        {
            PlayAntlerBash();
        }
    }

    void PlayAntlerBash()
    {
        isSoundPlaying = true;
        soundPlayTime = Time.time;
        soundDuration = antlerBashSound.length;

        float pitchVariation = Random.Range(0.85f, 1.0f);
        antlerAudioSource.pitch = pitchVariation;
        antlerAudioSource.volume = lureVolume;

        antlerAudioSource.PlayOneShot(antlerBashSound);

        if (debugMode)
            Debug.Log($"[HUNTER] 🎺 ANTLER LURE (duration: {soundDuration:F2}s) - Next lure in: {nextLureTime:F1}s");
    }

    void PlayDoeCall()
    {
        isSoundPlaying = true;
        soundPlayTime = Time.time;
        soundDuration = doeCallCutoffTime + 0.5f;

        float pitchVariation = Random.Range(0.85f, 1.0f);
        doeCallAudioSource.pitch = pitchVariation;
        doeCallAudioSource.volume = lureVolume;

        doeCallAudioSource.PlayOneShot(doeCallSound);

        if (doeCallCutoffCoroutine != null)
        {
            StopCoroutine(doeCallCutoffCoroutine);
        }
        doeCallCutoffCoroutine = StartCoroutine(CutoffDoeCall());

        if (debugMode)
            Debug.Log($"[HUNTER] 🎵 DOE CALL LURE - Will cut off in {doeCallCutoffTime}s - Next lure in: {nextLureTime:F1}s");
    }

    IEnumerator CutoffDoeCall()
    {
        yield return new WaitForSeconds(doeCallCutoffTime);

        if (doeCallAudioSource.isPlaying)
        {
            doeCallAudioSource.Stop();
            if (debugMode)
                Debug.Log("[HUNTER] 🎵 DOE CALL - CUT SHORT");
        }
    }

    void PlayHuntingSound(float distanceToPlayer)
    {
        isSoundPlaying = true;
        soundPlayTime = Time.time;

        bool useDoeCall = Random.value > 0.5f;

        if (useDoeCall && doeCallSound != null && doeCallAudioSource != null)
        {
            soundDuration = 5.5f;

            float pitchVariation = Random.Range(0.9f, 1.2f);
            doeCallAudioSource.pitch = pitchVariation;
            doeCallAudioSource.volume = huntingVolume;

            doeCallAudioSource.PlayOneShot(doeCallSound);

            if (doeCallCutoffCoroutine != null)
            {
                StopCoroutine(doeCallCutoffCoroutine);
            }
            doeCallCutoffCoroutine = StartCoroutine(CutoffDoeCallHunting());

            if (debugMode)
                Debug.Log($"[HUNTER] 🦌 AGGRESSIVE DOE CALL! Distance: {distanceToPlayer:F2}m | Next sound in: {nextHuntTime:F1}s");
        }
        else if (antlerBashSound != null && antlerAudioSource != null)
        {
            soundDuration = antlerBashSound.length;

            float pitchVariation = Random.Range(0.9f, 1.2f);
            antlerAudioSource.pitch = pitchVariation;
            antlerAudioSource.volume = huntingVolume;

            antlerAudioSource.PlayOneShot(antlerBashSound);

            if (debugMode)
                Debug.Log($"[HUNTER] 🦌 AGGRESSIVE ANTLER BASH! Distance: {distanceToPlayer:F2}m | Next sound in: {nextHuntTime:F1}s");
        }
    }

    IEnumerator CutoffDoeCallHunting()
    {
        yield return new WaitForSeconds(5f);

        if (doeCallAudioSource.isPlaying)
        {
            doeCallAudioSource.Stop();
        }
    }

    void SetNextLureTime()
    {
        nextLureTime = Random.Range(minTimeBetweenLures, maxTimeBetweenLures);
    }

    void SetNextHuntingTime()
    {
        nextHuntTime = Random.Range(minTimeBetweenHunts, maxTimeBetweenHunts);
    }

    IEnumerator AggressiveHuntingPhase()
    {
        if (debugMode)
            Debug.Log("[HUNTER] Playing rapid final attack sounds!");

        // Play 5 rapid sounds with proper spacing
        for (int i = 0; i < 5; i++)
        {
            // Only play if not already playing a sound
            if (!isSoundPlaying)
            {
                PlayHuntingSound(0f);
            }
            yield return new WaitForSeconds(1.5f);
        }

        if (debugMode)
            Debug.Log("[HUNTER] ☠️ ATTACK COMPLETE - KILLING PLAYER!");

        yield return new WaitForSeconds(1f);

        if (!playerDead)
        {
            KillPlayer();
        }
    }

    public void KillPlayer()
    {
        if (playerDead)
            return;

        playerDead = true;
        isActive = false;

        Debug.Log("[HUNTER] ☠️ PLAYER KILLED!");

        // Stop ALL player movement and audio
        StartCoroutine(PlayDeathAudio());

        // Disable player controls
        PlayerController playerController = playerTransform.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        // Stop any animator
        Animator animator = playerTransform.GetComponent<Animator>();
        if (animator != null)
        {
            animator.enabled = false;
        }

        // Stop any CharacterController/Rigidbody movement
        CharacterController charController = playerTransform.GetComponent<CharacterController>();
        if (charController != null)
        {
            charController.enabled = false;
        }

        Rigidbody playerRb = playerTransform.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.isKinematic = true;
        }
    }

    IEnumerator PlayDeathAudio()
    {
        // Pause ALL audio temporarily
        AudioListener.pause = true;
        yield return new WaitForSecondsRealtime(0.05f);

        // Resume audio
        AudioListener.pause = false;

        // Find and stop ALL audio sources except gunshot
        AudioSource[] allAudioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        foreach (AudioSource audio in allAudioSources)
        {
            if (audio != gunshotAudioSource)
            {
                audio.Stop();
                audio.enabled = false; // Disable to prevent any playback
                if (debugMode)
                    Debug.Log($"[HUNTER] Stopped & disabled audio: {audio.gameObject.name}");
            }
        }

        // Play gunshot
        PlayGunshotSound();

        // Start death sequence
        StartCoroutine(DeathSequence());
    }

    void PlayGunshotSound()
    {
        if (gunshotAudioSource == null || gunshotSound == null)
        {
            Debug.LogWarning("[HUNTER] Gunshot audio source or sound not assigned!");
            return;
        }

        gunshotAudioSource.enabled = true;
        gunshotAudioSource.volume = 1f;
        gunshotAudioSource.PlayOneShot(gunshotSound);

        // Cut off gunshot after specified time
        if (gunshotCutoffCoroutine != null)
        {
            StopCoroutine(gunshotCutoffCoroutine);
        }
        gunshotCutoffCoroutine = StartCoroutine(CutoffGunshotSound());

        if (debugMode)
            Debug.Log($"[HUNTER] 🔫 GUNSHOT! Will cut off in {gunshotCutoffTime}s");
    }

    IEnumerator CutoffGunshotSound()
    {
        yield return new WaitForSeconds(gunshotCutoffTime);

        if (gunshotAudioSource.isPlaying)
        {
            gunshotAudioSource.Stop();
            if (debugMode)
                Debug.Log("[HUNTER] 🔫 GUNSHOT - CUT SHORT");
        }
    }

    IEnumerator DeathSequence()
    {
        // Wait for gunshot to finish before fading to black
        yield return new WaitForSeconds(fadeToBlackDelay);

        // Now fade to black
        StartCoroutine(FadeToBlack());
    }

    IEnumerator FadeToBlack()
    {
        if (deathScreenCanvasGroup == null)
        {
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < deathScreenFadeDuration)
        {
            elapsedTime += Time.deltaTime;
            deathScreenCanvasGroup.alpha = Mathf.Clamp01(elapsedTime / deathScreenFadeDuration);
            yield return null;
        }

        deathScreenCanvasGroup.alpha = 1f;

        if (debugMode)
            Debug.Log("[HUNTER] Screen faded to black - Player is dead");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, huntingDetectionRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, killDistance);

        if (Application.isPlaying)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                Gizmos.color = isHunting ? Color.red : Color.yellow;
                Gizmos.DrawLine(transform.position, playerObj.transform.position);
            }
        }
    }
}