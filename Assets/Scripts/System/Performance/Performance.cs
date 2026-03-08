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

    [Header("CUSTOM TREE CULLING - RADIUS AROUND PLAYER")]
    [Range(0f, 500f)]
    public float treeRenderRadius = 150f;
    private float lastTreeRenderRadius = 150f;

    [Range(0f, 100f)]
    public float treeBillboardRadius = 50f;
    private float lastTreeBillboardRadius = 50f;

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

    [Header("Object Culling")]
    public bool enableObjectCulling = true;
    private bool lastEnableObjectCulling = true;

    [Range(50f, 1000f)]
    public float objectRenderRadius = 300f;
    private float lastObjectRenderRadius = 300f;

    public string[] excludeTags = { "Player", "Hunter", "Important" };

    [Header("UI Display")]
    public bool showDebugUI = true;
    public bool showRenderBubbleGizmo = true;
    private TextMeshProUGUI debugText;

    private Renderer[] allRenderers;
    private Transform playerTransform;
    private int enabledRendererCount = 0;
    private int totalRendererCount = 0;
    private Terrain[] terrains;
    private float treeRenderRadiusSqr;
    private float treeBillboardRadiusSqr;
    private float objectRenderRadiusSqr;
    private int culledTreeCount = 0;
    private int totalTreeCount = 0;

    void Start()
    {
        ApplyPreset();
        CreateDebugUI();
        InitializeCulling();
    }

    void InitializeCulling()
    {
        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            Debug.Log("[PERF] Player found");
        }
        else
        {
            Debug.LogWarning("[PERF] Player not found! Make sure player has 'Player' tag.");
            return;
        }

        // Get all renderers
        allRenderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        System.Collections.Generic.List<Renderer> validRenderers = new System.Collections.Generic.List<Renderer>();

        foreach (Renderer r in allRenderers)
        {
            if (r.GetComponent<Image>() != null || r.GetComponent<Text>() != null || r.GetComponent<TextMeshProUGUI>() != null)
                continue;
            validRenderers.Add(r);
        }

        allRenderers = validRenderers.ToArray();
        totalRendererCount = allRenderers.Length;

        // Get terrain
        terrains = FindObjectsByType<Terrain>(FindObjectsSortMode.None);

        // Count total trees
        totalTreeCount = 0;
        foreach (Terrain terrain in terrains)
        {
            if (terrain != null && terrain.terrainData != null)
            {
                totalTreeCount += terrain.terrainData.treePrototypes.Length;
            }
        }

        Debug.Log($"[PERF] Found {totalRendererCount} renderers, {terrains.Length} terrains, and ~{totalTreeCount} tree prototypes");
        UpdateDistances();
    }

    void CreateDebugUI()
    {
        GameObject canvasGO = new GameObject("DebugUICanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<GraphicRaycaster>();
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

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
        if (qualityPreset != lastPreset)
        {
            ApplyPreset();
            lastPreset = qualityPreset;
        }

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
            if (terrains != null)
            {
                foreach (Terrain terrain in terrains)
                    terrain.basemapDistance = terrainBasemapDistance;
            }
            lastTerrainBasemapDistance = terrainBasemapDistance;
        }

        if (terrainDetailDistance != lastTerrainDetailDistance)
        {
            if (terrains != null)
            {
                foreach (Terrain terrain in terrains)
                    terrain.detailObjectDistance = terrainDetailDistance;
            }
            lastTerrainDetailDistance = terrainDetailDistance;
        }

        if (terrainDetailDensity != lastTerrainDetailDensity)
        {
            if (terrains != null)
            {
                foreach (Terrain terrain in terrains)
                    terrain.detailObjectDensity = terrainDetailDensity;
            }
            lastTerrainDetailDensity = terrainDetailDensity;
        }

        // Custom tree culling radius
        if (treeRenderRadius != lastTreeRenderRadius || treeBillboardRadius != lastTreeBillboardRadius)
        {
            lastTreeRenderRadius = treeRenderRadius;
            lastTreeBillboardRadius = treeBillboardRadius;
            UpdateDistances();
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

        if (objectRenderRadius != lastObjectRenderRadius || enableObjectCulling != lastEnableObjectCulling)
        {
            lastObjectRenderRadius = objectRenderRadius;
            lastEnableObjectCulling = enableObjectCulling;
            UpdateDistances();
        }

        // Update culling every frame
        if (playerTransform != null)
        {
            UpdateTerrainTreeDistance();
            if (enableObjectCulling && allRenderers != null)
                UpdateObjectCulling();
        }

        if (showDebugUI && debugText != null)
            UpdateDebugUI();
    }

    void UpdateTerrainTreeDistance()
    {
        if (terrains == null || terrains.Length == 0)
            return;

        Vector3 playerPos = playerTransform.position;

        // Simply update the terrain's tree distance based on player position
        // This is the most effective way to cull trees
        foreach (Terrain terrain in terrains)
        {
            if (terrain == null)
                continue;

            // Set tree distance - controls how far from camera trees render
            terrain.treeDistance = treeRenderRadius * 2f; // Multiply by 2 for better results

            // Set billboard distance - where trees become 2D
            terrain.treeBillboardDistance = treeBillboardRadius * 2f;
        }
    }

    void UpdateObjectCulling()
    {
        Vector3 playerPos = playerTransform.position;
        enabledRendererCount = 0;
        float radiusSqr = objectRenderRadius * objectRenderRadius;

        for (int i = 0; i < allRenderers.Length; i++)
        {
            if (allRenderers[i] == null)
                continue;

            if (ShouldExclude(allRenderers[i].gameObject))
            {
                if (!allRenderers[i].enabled)
                    allRenderers[i].enabled = true;
                enabledRendererCount++;
                continue;
            }

            float distanceSqr = Vector3.SqrMagnitude(allRenderers[i].transform.position - playerPos);
            bool shouldRender = distanceSqr <= radiusSqr;

            if (allRenderers[i].enabled != shouldRender)
            {
                allRenderers[i].enabled = shouldRender;
            }

            if (shouldRender)
                enabledRendererCount++;
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

    void UpdateDistances()
    {
        treeRenderRadiusSqr = treeRenderRadius * treeRenderRadius;
        treeBillboardRadiusSqr = treeBillboardRadius * treeBillboardRadius;
        objectRenderRadiusSqr = objectRenderRadius * objectRenderRadius;
    }

    void UpdateDebugUI()
    {
        float fps = 1f / Time.deltaTime;

        string text = $"<color=green><b>PERFORMANCE SETTINGS (LIVE)</b></color>\n\n";
        text += $"<color={(fps > 60 ? "green" : fps > 30 ? "yellow" : "red")}>FPS: {fps:F0}</color>\n";
        text += $"Frame Time: {Time.deltaTime * 1000f:F2}ms\n\n";

        text += $"<b>Current Preset:</b> {qualityPreset}\n\n";

        text += $"<color=orange><b>⚠️ TREE RENDERING RADIUS</b></color>\n";
        text += $"<b>Tree Render Radius:</b> {treeRenderRadius:F0}m\n";
        text += $"<b>Billboard Radius:</b> {treeBillboardRadius:F0}m\n\n";

        text += $"<color=orange><b>⚠️ QUALITY SETTINGS</b></color>\n";
        text += $"<b>LOD Bias:</b> {lodBias:F2}\n";
        text += $"<b>Terrain Detail Dist:</b> {terrainDetailDistance:F0}m\n";
        text += $"<b>Terrain Detail Density:</b> {terrainDetailDensity:F2}\n\n";

        text += $"<b>Shadows:</b> {(enableShadows ? "<color=green>ON" : "<color=red>OFF")}</color>\n";
        text += $"<b>Shadow Distance:</b> {shadowDistance:F0}m\n";
        text += $"<b>Camera Far Clip:</b> {cameraFarClip:F0}m\n";
        text += $"<b>MSAA:</b> {(enableMSAA ? $"<color=green>{msaaQuality}x" : "<color=red>OFF")}</color>\n\n";

        if (enableObjectCulling)
        {
            float cullingPercent = totalRendererCount > 0 ? ((float)(totalRendererCount - enabledRendererCount) / totalRendererCount * 100f) : 0f;
            text += $"<b>Objects:</b> {enabledRendererCount}/{totalRendererCount}\n";
        }

        text += $"\n<i><color=yellow>Try 75-150m tree radius\nfor best performance!</color></i>";

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
                treeRenderRadius = 25f;
                treeBillboardRadius = 10f;
                cameraFarClip = 100f;
                enableMSAA = false;
                enableShadows = false;
                objectRenderRadius = 150f;
                Debug.Log("[PERF] Applied: ABSOLUTE ZERO");
                break;

            case QualityPreset.VeryLow:
                lodBias = 0.2f;
                shadowDistance = 30f;
                terrainBasemapDistance = 150f;
                terrainDetailDistance = 50f;
                terrainDetailDensity = 0.2f;
                treeRenderRadius = 50f;
                treeBillboardRadius = 25f;
                cameraFarClip = 200f;
                enableMSAA = false;
                enableShadows = true;
                objectRenderRadius = 200f;
                Debug.Log("[PERF] Applied: VERY LOW");
                break;

            case QualityPreset.Low:
                lodBias = 0.35f;
                shadowDistance = 50f;
                terrainBasemapDistance = 250f;
                terrainDetailDistance = 75f;
                terrainDetailDensity = 0.35f;
                treeRenderRadius = 75f;
                treeBillboardRadius = 40f;
                cameraFarClip = 300f;
                enableMSAA = false;
                enableShadows = true;
                objectRenderRadius = 250f;
                Debug.Log("[PERF] Applied: LOW");
                break;

            case QualityPreset.Medium:
                lodBias = 0.5f;
                shadowDistance = 75f;
                terrainBasemapDistance = 400f;
                terrainDetailDistance = 100f;
                terrainDetailDensity = 0.5f;
                treeRenderRadius = 100f;
                treeBillboardRadius = 50f;
                cameraFarClip = 400f;
                enableMSAA = true;
                msaaQuality = 2;
                enableShadows = true;
                objectRenderRadius = 300f;
                Debug.Log("[PERF] Applied: MEDIUM");
                break;

            case QualityPreset.High:
                lodBias = 0.75f;
                shadowDistance = 125f;
                terrainBasemapDistance = 600f;
                terrainDetailDistance = 150f;
                terrainDetailDensity = 0.75f;
                treeRenderRadius = 150f;
                treeBillboardRadius = 75f;
                cameraFarClip = 600f;
                enableMSAA = true;
                msaaQuality = 4;
                enableShadows = true;
                objectRenderRadius = 400f;
                Debug.Log("[PERF] Applied: HIGH");
                break;

            case QualityPreset.VeryHigh:
                lodBias = 1f;
                shadowDistance = 200f;
                terrainBasemapDistance = 800f;
                terrainDetailDistance = 200f;
                terrainDetailDensity = 0.9f;
                treeRenderRadius = 250f;
                treeBillboardRadius = 125f;
                cameraFarClip = 1000f;
                enableMSAA = true;
                msaaQuality = 8;
                enableShadows = true;
                objectRenderRadius = 500f;
                Debug.Log("[PERF] Applied: VERY HIGH");
                break;

            case QualityPreset.AbsoluteBest:
                lodBias = 1.5f;
                shadowDistance = 500f;
                terrainBasemapDistance = 1000f;
                terrainDetailDistance = 250f;
                terrainDetailDensity = 1f;
                treeRenderRadius = 400f;
                treeBillboardRadius = 200f;
                cameraFarClip = 2000f;
                enableMSAA = true;
                msaaQuality = 8;
                enableShadows = true;
                objectRenderRadius = 750f;
                Debug.Log("[PERF] Applied: ABSOLUTE BEST");
                break;
        }
    }

    void OnValidate()
    {
        UpdateDistances();
    }

    void OnDrawGizmos()
    {
        if (!showRenderBubbleGizmo)
            return;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
            return;

        // Draw tree render radius (green)
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawWireSphere(playerObj.transform.position, treeRenderRadius);

        // Draw billboard radius (yellow)
        Gizmos.color = new Color(1, 1, 0, 0.3f);
        Gizmos.DrawWireSphere(playerObj.transform.position, treeBillboardRadius);

        // Draw object culling radius (red)
        Gizmos.color = new Color(1, 0, 0, 0.2f);
        Gizmos.DrawWireSphere(playerObj.transform.position, objectRenderRadius);
    }
}