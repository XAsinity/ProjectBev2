using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PerformanceOptimizer : MonoBehaviour
{
    [System.Serializable]
    public enum QualityPreset
    {
        AbsoluteZero,
        VeryLow,
        Low,
        Medium,
        High,
        VeryHigh,
        AbsoluteBest
    }

    [Header("Quality Preset")]
    public QualityPreset qualityPreset = QualityPreset.Medium;
    private QualityPreset lastPreset = QualityPreset.Medium;

    [Header("LOD Settings")]
    [Range(0.1f, 2f)]
    public float lodBias = 0.5f;
    private float lastLodBias = 0.5f;

    [Header("Shadow Settings")]
    public bool enableShadows = true;
    private bool lastEnableShadows = true;

    [Range(10f, 500f)]
    public float shadowDistance = 100f;
    private float lastShadowDistance = 100f;

    [Header("Terrain Settings")]
    [Range(50f, 1000f)]
    public float terrainBasemapDistance = 500f;
    private float lastTerrainBasemapDistance = 500f;

    [Range(10f, 200f)]
    public float terrainDetailDistance = 100f;
    private float lastTerrainDetailDistance = 100f;

    [Range(0.1f, 1f)]
    public float terrainDetailDensity = 0.5f;
    private float lastTerrainDetailDensity = 0.5f;

    [Header("Camera Settings")]
    [Range(50f, 2000f)]
    public float cameraFarClip = 500f;
    private float lastCameraFarClip = 500f;

    [Header("AntiAliasing")]
    public bool enableMSAA = true;
    private bool lastEnableMSAA = true;

    [Range(1, 8)]
    public int msaaQuality = 4;
    private int lastMsaaQuality = 4;

    [Header("Physics Settings")]
    public bool optimizePhysics = true;
    private bool lastOptimizePhysics = true;

    [Header("Render Bubble Settings")]
    public bool enableRenderBubble = true;
    private bool lastEnableRenderBubble = true;

    [Range(50f, 1000f)]
    public float renderDistance = 300f;
    private float lastRenderDistance = 300f;

    [Range(20f, 500f)]
    public float aggressiveDetailDistance = 100f;
    private float lastAggressiveDetailDistance = 100f;

    [Range(0.1f, 2f)]
    public float bubbleUpdateFrequency = 0.5f;

    [Range(0, 100)]
    public int batchSize = 50;
    private int lastBatchSize = 50;

    public string[] excludeTags = { "Player", "Hunter", "Important" };

    [Header("UI Display")]
    public bool showDebugUI = true;
    public bool showRenderBubbleGizmo = true;
    private TextMeshProUGUI debugText;

    private float updateTimer = 0f;
    private Renderer[] allRenderers;
    private Transform playerTransform;
    private float renderDistanceSqr;
    private float aggressiveDetailDistanceSqr;
    private int rendererIndex = 0;
    private int activeRendererCount = 0;
    private int totalRendererCount = 0;

    void Start()
    {
        ApplyPreset();
        CreateDebugUI();
        InitializeRenderBubble();
    }

    void InitializeRenderBubble()
    {
        if (!enableRenderBubble)
            return;

        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            Debug.Log("[PERF] Render Bubble: Player found");
        }
        else
        {
            Debug.LogWarning("[PERF] Render Bubble: Player not found! Make sure player has 'Player' tag.");
            enableRenderBubble = false;
            return;
        }

        // Get all renderers in scene
        allRenderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        totalRendererCount = allRenderers.Length;
        Debug.Log($"[PERF] Render Bubble: Found {totalRendererCount} renderers");

        UpdateRenderBubbleDistances();
    }

    void CreateDebugUI()
    {
        // Create Canvas
        GameObject canvasGO = new GameObject("DebugUICanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<GraphicRaycaster>();
        canvasGO.AddComponent<CanvasScaler>();

        // Create Text
        GameObject textGO = new GameObject("DebugText");
        textGO.transform.SetParent(canvasGO.transform, false);
        debugText = textGO.AddComponent<TextMeshProUGUI>();
        debugText.fontSize = 40;
        debugText.color = Color.white;

        RectTransform rect = textGO.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = new Vector2(0.5f, 1);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Debug.Log("[PERF] Debug UI created!");
    }

    void Update()
    {
        // Check if preset changed
        if (qualityPreset != lastPreset)
        {
            ApplyPreset();
            lastPreset = qualityPreset;
        }

        // Check for changes and apply
        if (lodBias != lastLodBias)
        {
            QualitySettings.lodBias = lodBias;
            lastLodBias = lodBias;
        }

        if (enableShadows != lastEnableShadows)
        {
            QualitySettings.shadows = enableShadows ? ShadowQuality.All : ShadowQuality.Disable;
            lastEnableShadows = enableShadows;
        }

        if (shadowDistance != lastShadowDistance)
        {
            QualitySettings.shadowDistance = shadowDistance;
            lastShadowDistance = shadowDistance;
        }

        if (enableMSAA != lastEnableMSAA)
        {
            QualitySettings.antiAliasing = enableMSAA ? msaaQuality : 0;
            lastEnableMSAA = enableMSAA;
        }

        if (msaaQuality != lastMsaaQuality && enableMSAA)
        {
            QualitySettings.antiAliasing = msaaQuality;
            lastMsaaQuality = msaaQuality;
        }

        if (cameraFarClip != lastCameraFarClip)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
                mainCamera.farClipPlane = cameraFarClip;
            lastCameraFarClip = cameraFarClip;
        }

        if (terrainBasemapDistance != lastTerrainBasemapDistance)
        {
            Terrain[] terrains = FindObjectsByType<Terrain>(FindObjectsSortMode.None);
            foreach (Terrain terrain in terrains)
                terrain.basemapDistance = terrainBasemapDistance;
            lastTerrainBasemapDistance = terrainBasemapDistance;
        }

        if (terrainDetailDistance != lastTerrainDetailDistance)
        {
            Terrain[] terrains = FindObjectsByType<Terrain>(FindObjectsSortMode.None);
            foreach (Terrain terrain in terrains)
                terrain.detailObjectDistance = terrainDetailDistance;
            lastTerrainDetailDistance = terrainDetailDistance;
        }

        if (terrainDetailDensity != lastTerrainDetailDensity)
        {
            Terrain[] terrains = FindObjectsByType<Terrain>(FindObjectsSortMode.None);
            foreach (Terrain terrain in terrains)
                terrain.detailObjectDensity = terrainDetailDensity;
            lastTerrainDetailDensity = terrainDetailDensity;
        }

        if (optimizePhysics != lastOptimizePhysics)
        {
            if (optimizePhysics)
            {
                Physics.defaultSolverIterations = 6;
                Physics.defaultSolverVelocityIterations = 1;
            }
            lastOptimizePhysics = optimizePhysics;
        }

        // Render Bubble
        if (enableRenderBubble != lastEnableRenderBubble)
        {
            lastEnableRenderBubble = enableRenderBubble;
            if (enableRenderBubble && allRenderers == null)
                InitializeRenderBubble();
        }

        if (renderDistance != lastRenderDistance || aggressiveDetailDistance != lastAggressiveDetailDistance || batchSize != lastBatchSize)
        {
            lastRenderDistance = renderDistance;
            lastAggressiveDetailDistance = aggressiveDetailDistance;
            lastBatchSize = batchSize;
            UpdateRenderBubbleDistances();
        }

        if (enableRenderBubble && playerTransform != null)
        {
            updateTimer += Time.deltaTime;
            if (updateTimer >= bubbleUpdateFrequency)
            {
                if (batchSize > 0)
                    UpdateBubbleBatched();
                else
                    UpdateBubble();
                updateTimer = 0f;
            }
        }

        // Update UI
        if (showDebugUI && debugText != null)
            UpdateDebugUI();
    }

    void UpdateBubble()
    {
        if (playerTransform == null || allRenderers == null)
            return;

        Vector3 playerPos = playerTransform.position;
        activeRendererCount = 0;

        foreach (Renderer renderer in allRenderers)
        {
            if (renderer == null || ShouldExclude(renderer.gameObject))
                continue;

            float distanceSqr = Vector3.SqrMagnitude(renderer.transform.position - playerPos);
            bool shouldRender = distanceSqr <= renderDistanceSqr;

            if (renderer.enabled != shouldRender)
            {
                renderer.enabled = shouldRender;
            }

            if (shouldRender)
                activeRendererCount++;

            // Aggressive detail culling for far objects
            if (distanceSqr > aggressiveDetailDistanceSqr)
            {
                LODGroup lodGroup = renderer.GetComponent<LODGroup>();
                if (lodGroup != null && lodGroup.enabled)
                    lodGroup.enabled = false;
            }
            else
            {
                LODGroup lodGroup = renderer.GetComponent<LODGroup>();
                if (lodGroup != null && !lodGroup.enabled)
                    lodGroup.enabled = true;
            }
        }
    }

    void UpdateBubbleBatched()
    {
        if (playerTransform == null || allRenderers == null)
            return;

        Vector3 playerPos = playerTransform.position;
        int endIndex = Mathf.Min(rendererIndex + batchSize, allRenderers.Length);

        for (int i = rendererIndex; i < endIndex; i++)
        {
            Renderer renderer = allRenderers[i];
            if (renderer == null || ShouldExclude(renderer.gameObject))
                continue;

            float distanceSqr = Vector3.SqrMagnitude(renderer.transform.position - playerPos);
            bool shouldRender = distanceSqr <= renderDistanceSqr;

            if (renderer.enabled != shouldRender)
            {
                renderer.enabled = shouldRender;
            }

            if (shouldRender)
                activeRendererCount++;

            // Aggressive detail culling
            if (distanceSqr > aggressiveDetailDistanceSqr)
            {
                LODGroup lodGroup = renderer.GetComponent<LODGroup>();
                if (lodGroup != null && lodGroup.enabled)
                    lodGroup.enabled = false;
            }
            else
            {
                LODGroup lodGroup = renderer.GetComponent<LODGroup>();
                if (lodGroup != null && !lodGroup.enabled)
                    lodGroup.enabled = true;
            }
        }

        rendererIndex = endIndex;
        if (rendererIndex >= allRenderers.Length)
        {
            rendererIndex = 0;
            activeRendererCount = 0;
        }
    }

    bool ShouldExclude(GameObject obj)
    {
        foreach (string tag in excludeTags)
        {
            if (obj.CompareTag(tag))
                return true;
        }
        return false;
    }

    void UpdateRenderBubbleDistances()
    {
        renderDistanceSqr = renderDistance * renderDistance;
        aggressiveDetailDistanceSqr = aggressiveDetailDistance * aggressiveDetailDistance;
    }

    void UpdateDebugUI()
    {
        float fps = 1f / Time.deltaTime;

        string text = $"<color=green><b>PERFORMANCE SETTINGS (LIVE)</b></color>\n\n";
        text += $"<color={(fps > 60 ? "green" : fps > 30 ? "yellow" : "red")}>FPS: {fps:F0}</color>\n";
        text += $"Frame Time: {Time.deltaTime * 1000f:F2}ms\n\n";

        text += $"<b>Current Preset:</b> {qualityPreset}\n\n";

        text += $"<b>LOD Bias:</b> {lodBias:F2}\n";
        text += $"<b>Shadows:</b> {(enableShadows ? "<color=green>ON" : "<color=red>OFF")}</color>\n";
        text += $"<b>Shadow Distance:</b> {shadowDistance:F0}m\n";
        text += $"<b>Shadow Res:</b> {QualitySettings.shadowResolution}\n\n";

        text += $"<b>Terrain Basemap Dist:</b> {terrainBasemapDistance:F0}m\n";
        text += $"<b>Terrain Detail Dist:</b> {terrainDetailDistance:F0}m\n";
        text += $"<b>Terrain Detail Density:</b> {terrainDetailDensity:F2}\n\n";

        text += $"<b>Camera Far Clip:</b> {cameraFarClip:F0}m\n";
        text += $"<b>MSAA:</b> {(enableMSAA ? $"<color=green>{msaaQuality}x" : "<color=red>OFF")}</color>\n";
        text += $"<b>Physics Opt:</b> {(optimizePhysics ? "<color=green>ON" : "<color=red>OFF")}</color>\n\n";

        if (enableRenderBubble)
        {
            text += $"<color=cyan><b>RENDER BUBBLE</b></color>\n";
            text += $"<b>Status:</b> <color=green>ACTIVE</color>\n";
            text += $"<b>Render Distance:</b> {renderDistance:F0}m\n";
            text += $"<b>Aggressive Detail:</b> {aggressiveDetailDistance:F0}m\n";
            text += $"<b>Active Renderers:</b> {activeRendererCount}/{totalRendererCount}\n";
        }

        text += $"\n<i><color=gray>Adjust settings in Inspector\nto see changes in real-time</color></i>";

        debugText.text = text;
    }

    void ApplyPreset()
    {
        switch (qualityPreset)
        {
            case QualityPreset.AbsoluteZero:
                lodBias = 0.1f;
                shadowDistance = 15f;
                terrainBasemapDistance = 100f;
                terrainDetailDistance = 25f;
                terrainDetailDensity = 0.1f;
                cameraFarClip = 100f;
                enableMSAA = false;
                enableShadows = false;
                renderDistance = 150f;
                aggressiveDetailDistance = 50f;
                Debug.Log("[PERF] Applied: ABSOLUTE ZERO");
                break;

            case QualityPreset.VeryLow:
                lodBias = 0.2f;
                shadowDistance = 30f;
                terrainBasemapDistance = 150f;
                terrainDetailDistance = 50f;
                terrainDetailDensity = 0.2f;
                cameraFarClip = 200f;
                enableMSAA = false;
                enableShadows = true;
                renderDistance = 200f;
                aggressiveDetailDistance = 75f;
                Debug.Log("[PERF] Applied: VERY LOW");
                break;

            case QualityPreset.Low:
                lodBias = 0.35f;
                shadowDistance = 50f;
                terrainBasemapDistance = 250f;
                terrainDetailDistance = 75f;
                terrainDetailDensity = 0.35f;
                cameraFarClip = 300f;
                enableMSAA = false;
                enableShadows = true;
                renderDistance = 250f;
                aggressiveDetailDistance = 100f;
                Debug.Log("[PERF] Applied: LOW");
                break;

            case QualityPreset.Medium:
                lodBias = 0.5f;
                shadowDistance = 75f;
                terrainBasemapDistance = 400f;
                terrainDetailDistance = 100f;
                terrainDetailDensity = 0.5f;
                cameraFarClip = 400f;
                enableMSAA = true;
                msaaQuality = 2;
                enableShadows = true;
                renderDistance = 300f;
                aggressiveDetailDistance = 120f;
                Debug.Log("[PERF] Applied: MEDIUM");
                break;

            case QualityPreset.High:
                lodBias = 0.75f;
                shadowDistance = 125f;
                terrainBasemapDistance = 600f;
                terrainDetailDistance = 150f;
                terrainDetailDensity = 0.75f;
                cameraFarClip = 600f;
                enableMSAA = true;
                msaaQuality = 4;
                enableShadows = true;
                renderDistance = 400f;
                aggressiveDetailDistance = 150f;
                Debug.Log("[PERF] Applied: HIGH");
                break;

            case QualityPreset.VeryHigh:
                lodBias = 1f;
                shadowDistance = 200f;
                terrainBasemapDistance = 800f;
                terrainDetailDistance = 200f;
                terrainDetailDensity = 0.9f;
                cameraFarClip = 1000f;
                enableMSAA = true;
                msaaQuality = 8;
                enableShadows = true;
                renderDistance = 500f;
                aggressiveDetailDistance = 250f;
                Debug.Log("[PERF] Applied: VERY HIGH");
                break;

            case QualityPreset.AbsoluteBest:
                lodBias = 1.5f;
                shadowDistance = 500f;
                terrainBasemapDistance = 1000f;
                terrainDetailDistance = 250f;
                terrainDetailDensity = 1f;
                cameraFarClip = 2000f;
                enableMSAA = true;
                msaaQuality = 8;
                enableShadows = true;
                renderDistance = 750f;
                aggressiveDetailDistance = 400f;
                Debug.Log("[PERF] Applied: ABSOLUTE BEST");
                break;
        }
    }

    void OnValidate()
    {
        UpdateRenderBubbleDistances();
    }

    void OnDrawGizmos()
    {
        if (!showRenderBubbleGizmo)
            return;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
            return;

        // Draw render bubble (green)
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawWireSphere(playerObj.transform.position, renderDistance);

        // Draw aggressive detail distance (yellow)
        Gizmos.color = new Color(1, 1, 0, 0.2f);
        Gizmos.DrawWireSphere(playerObj.transform.position, aggressiveDetailDistance);
    }
}