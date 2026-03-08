using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Performance : MonoBehaviour
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

    [Header("TREE DISTANCE - THIS IS THE KEY SETTING")]
    [Range(0f, 1000f)]
    public float treeDistance = 250f;
    private float lastTreeDistance = 250f;

    [Range(0f, 500f)]
    public float treeBillboardDistance = 50f;
    private float lastTreeBillboardDistance = 50f;

    [Header("Terrain Detail Settings")]
    [Range(50f, 1000f)]
    public float terrainBasemapDistance = 500f;
    private float lastTerrainBasemapDistance = 500f;

    [Range(10f, 200f)]
    public float terrainDetailDistance = 100f;
    private float lastTerrainDetailDistance = 100f;

    [Range(0.01f, 1f)]
    public float terrainDetailDensity = 0.5f;
    private float lastTerrainDetailDensity = 0.5f;

    [Header("CTI WIND CONTROL")]
    public bool enableWind = true;
    private bool lastEnableWind = true;

    [Range(0f, 10f)]
    public float windStrength = 1f;
    private float lastWindStrength = 1f;

    [Range(0f, 10f)]
    public float windTurbulence = 1f;
    private float lastWindTurbulence = 1f;

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

    [Header("OBJECT CULLING")]
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

    private System.Collections.Generic.List<Renderer> culledRenderers = new System.Collections.Generic.List<Renderer>();
    private Transform playerTransform;
    private int enabledRendererCount = 0;
    private Terrain[] terrains;
    private float objectRenderRadiusSqr;

    // CTI Wind shader property IDs
    private int ctiWindPropertyID;
    private int ctiTurbulencePropertyID;
    private Vector4 windVector = Vector4.zero;

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

        // Get CTI wind shader property IDs
        ctiWindPropertyID = Shader.PropertyToID("_CTI_SRP_Wind");
        ctiTurbulencePropertyID = Shader.PropertyToID("_CTI_SRP_Turbulence");
        Debug.Log("[PERF] CTI Wind property IDs registered");

        // Get all renderers - SKIP UI ELEMENTS
        Renderer[] allRenderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        culledRenderers.Clear();

        foreach (Renderer r in allRenderers)
        {
            // SKIP UI elements (Image, Text, etc)
            if (r.GetComponent<Image>() != null || r.GetComponent<Text>() != null || r.GetComponent<TextMeshProUGUI>() != null)
                continue;

            culledRenderers.Add(r);
        }

        // Get terrain
        terrains = FindObjectsByType<Terrain>(FindObjectsSortMode.None);

        Debug.Log($"[PERF] Found {culledRenderers.Count} cullable renderers and {terrains.Length} terrains");
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
        debugText.fontSize = 32;
        debugText.color = Color.white;

        RectTransform rect = textGO.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = new Vector2(0.7f, 1);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Debug.Log("[PERF] Debug UI created!");
    }

    void Update()
    {
        if (playerTransform == null)
            return;

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

        // TREE DISTANCE - THE KEY SETTING FOR CULLING
        if (treeDistance != lastTreeDistance || treeBillboardDistance != lastTreeBillboardDistance)
        {
            if (terrains != null)
            {
                foreach (Terrain terrain in terrains)
                {
                    if (terrain == null)
                        continue;
                    terrain.treeDistance = treeDistance;
                    terrain.treeBillboardDistance = treeBillboardDistance;
                }
            }
            lastTreeDistance = treeDistance;
            lastTreeBillboardDistance = treeBillboardDistance;
        }

        if (terrainBasemapDistance != lastTerrainBasemapDistance)
        {
            if (terrains != null)
            {
                foreach (Terrain terrain in terrains)
                {
                    if (terrain == null)
                        continue;
                    terrain.basemapDistance = terrainBasemapDistance;
                }
            }
            lastTerrainBasemapDistance = terrainBasemapDistance;
        }

        if (terrainDetailDistance != lastTerrainDetailDistance)
        {
            if (terrains != null)
            {
                foreach (Terrain terrain in terrains)
                {
                    if (terrain == null)
                        continue;
                    terrain.detailObjectDistance = terrainDetailDistance;
                }
            }
            lastTerrainDetailDistance = terrainDetailDistance;
        }

        if (terrainDetailDensity != lastTerrainDetailDensity)
        {
            if (terrains != null)
            {
                foreach (Terrain terrain in terrains)
                {
                    if (terrain == null)
                        continue;
                    terrain.detailObjectDensity = terrainDetailDensity;
                }
            }
            lastTerrainDetailDensity = terrainDetailDensity;
        }

        // CTI Wind control
        if (enableWind != lastEnableWind || windStrength != lastWindStrength || windTurbulence != lastWindTurbulence)
        {
            lastEnableWind = enableWind;
            lastWindStrength = windStrength;
            lastWindTurbulence = windTurbulence;
            UpdateCTIWind();
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

        // Update CTI wind every frame
        UpdateCTIWind();

        // UPDATE OBJECT CULLING EVERY FRAME - THIS IS CRITICAL
        if (enableObjectCulling)
        {
            UpdateObjectCulling();
        }

        if (showDebugUI && debugText != null)
            UpdateDebugUI();
    }

    void UpdateCTIWind()
    {
        if (!enableWind)
        {
            Shader.SetGlobalVector(ctiWindPropertyID, Vector4.zero);
            Shader.SetGlobalFloat(ctiTurbulencePropertyID, 0f);
        }
        else
        {
            windVector = Vector4.zero;
            windVector.w = windStrength;

            Shader.SetGlobalVector(ctiWindPropertyID, windVector);
            Shader.SetGlobalFloat(ctiTurbulencePropertyID, windTurbulence);
        }
    }

    void UpdateObjectCulling()
    {
        Vector3 playerPos = playerTransform.position;
        enabledRendererCount = 0;
        float radiusSqr = objectRenderRadiusSqr;

        // Cull based on distance
        for (int i = 0; i < culledRenderers.Count; i++)
        {
            Renderer r = culledRenderers[i];

            if (r == null)
                continue;

            if (ShouldExclude(r.gameObject))
            {
                r.enabled = true;
                enabledRendererCount++;
                continue;
            }

            float distanceSqr = Vector3.SqrMagnitude(r.bounds.center - playerPos);
            bool shouldRender = distanceSqr <= radiusSqr;

            r.enabled = shouldRender;

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
        objectRenderRadiusSqr = objectRenderRadius * objectRenderRadius;
    }

    void UpdateDebugUI()
    {
        float fps = 1f / Time.deltaTime;

        string text = $"<color=green><b>PERFORMANCE SETTINGS (LIVE)</b></color>\n\n";
        text += $"<color={(fps > 60 ? "green" : fps > 30 ? "yellow" : "red")}>FPS: {fps:F0}</color>\n";
        text += $"Frame Time: {Time.deltaTime * 1000f:F2}ms\n\n";

        text += $"<b>Current Preset:</b> {qualityPreset}\n\n";

        text += $"<color=orange><b>⚠️ TREE CULLING (CRITICAL)</b></color>\n";
        text += $"<b>Tree Distance:</b> {treeDistance:F0}m ✅ WORKS\n";
        text += $"<b>Billboard Distance:</b> {treeBillboardDistance:F0}m ✅ WORKS\n\n";

        text += $"<color=orange><b>⚠️ CTI WIND</b></color>\n";
        text += $"<b>Enable Wind:</b> {(enableWind ? "<color=green>ON" : "<color=red>OFF")}</color> ✅ WORKS\n";
        text += $"<b>Wind Strength:</b> {windStrength:F2}x\n";
        text += $"<b>Wind Turbulence:</b> {windTurbulence:F2}x\n\n";

        text += $"<color=orange><b>LOD SETTINGS</b></color>\n";
        text += $"<b>LOD Bias:</b> {lodBias:F2} ✅ WORKS\n";
        text += $"<b>Shadows:</b> {(enableShadows ? "<color=green>ON" : "<color=red>OFF")}</color>\n\n";

        text += $"<color=orange><b>OBJECT CULLING</b></color>\n";
        text += $"<b>Enable:</b> {(enableObjectCulling ? "<color=green>ON" : "<color=red>OFF")}</color>\n";
        text += $"<b>Radius:</b> {objectRenderRadius:F0}m ✅ NOW WORKS\n";
        text += $"<b>Rendered Objects:</b> {enabledRendererCount}/{culledRenderers.Count}\n\n";

        text += $"<i><color=yellow>📌 Focus on Tree Distance\n";
        text += $"for best cross-map FPS!</color></i>";

        debugText.text = text;
    }

    void ApplyPreset()
    {
        switch (qualityPreset)
        {
            case QualityPreset.AbsoluteZero:
                lodBias = 0.1f;
                shadowDistance = 15f;
                treeDistance = 50f;
                treeBillboardDistance = 25f;
                terrainBasemapDistance = 100f;
                terrainDetailDistance = 25f;
                terrainDetailDensity = 0.1f;
                enableWind = false;
                windStrength = 0f;
                windTurbulence = 0f;
                cameraFarClip = 100f;
                enableMSAA = false;
                enableShadows = false;
                objectRenderRadius = 150f;
                break;

            case QualityPreset.VeryLow:
                lodBias = 0.2f;
                shadowDistance = 30f;
                treeDistance = 100f;
                treeBillboardDistance = 50f;
                terrainBasemapDistance = 150f;
                terrainDetailDistance = 50f;
                terrainDetailDensity = 0.2f;
                enableWind = true;
                windStrength = 0.2f;
                windTurbulence = 0.1f;
                cameraFarClip = 200f;
                enableMSAA = false;
                enableShadows = true;
                objectRenderRadius = 200f;
                break;

            case QualityPreset.Low:
                lodBias = 0.35f;
                shadowDistance = 50f;
                treeDistance = 150f;
                treeBillboardDistance = 75f;
                terrainBasemapDistance = 250f;
                terrainDetailDistance = 75f;
                terrainDetailDensity = 0.35f;
                enableWind = true;
                windStrength = 0.4f;
                windTurbulence = 0.2f;
                cameraFarClip = 300f;
                enableMSAA = false;
                enableShadows = true;
                objectRenderRadius = 250f;
                break;

            case QualityPreset.Medium:
                lodBias = 0.5f;
                shadowDistance = 75f;
                treeDistance = 250f;
                treeBillboardDistance = 100f;
                terrainBasemapDistance = 400f;
                terrainDetailDistance = 100f;
                terrainDetailDensity = 0.5f;
                enableWind = true;
                windStrength = 0.6f;
                windTurbulence = 0.4f;
                cameraFarClip = 400f;
                enableMSAA = true;
                msaaQuality = 2;
                enableShadows = true;
                objectRenderRadius = 300f;
                break;

            case QualityPreset.High:
                lodBias = 0.75f;
                shadowDistance = 125f;
                treeDistance = 400f;
                treeBillboardDistance = 150f;
                terrainBasemapDistance = 600f;
                terrainDetailDistance = 150f;
                terrainDetailDensity = 0.75f;
                enableWind = true;
                windStrength = 0.8f;
                windTurbulence = 0.6f;
                cameraFarClip = 600f;
                enableMSAA = true;
                msaaQuality = 4;
                enableShadows = true;
                objectRenderRadius = 400f;
                break;

            case QualityPreset.VeryHigh:
                lodBias = 1f;
                shadowDistance = 200f;
                treeDistance = 600f;
                treeBillboardDistance = 250f;
                terrainBasemapDistance = 800f;
                terrainDetailDistance = 200f;
                terrainDetailDensity = 0.9f;
                enableWind = true;
                windStrength = 1f;
                windTurbulence = 0.8f;
                cameraFarClip = 1000f;
                enableMSAA = true;
                msaaQuality = 8;
                enableShadows = true;
                objectRenderRadius = 500f;
                break;

            case QualityPreset.AbsoluteBest:
                lodBias = 1.5f;
                shadowDistance = 500f;
                treeDistance = 1000f;
                treeBillboardDistance = 500f;
                terrainBasemapDistance = 1000f;
                terrainDetailDistance = 250f;
                terrainDetailDensity = 1f;
                enableWind = true;
                windStrength = 1.5f;
                windTurbulence = 1f;
                cameraFarClip = 2000f;
                enableMSAA = true;
                msaaQuality = 8;
                enableShadows = true;
                objectRenderRadius = 750f;
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

        // Draw tree distance (green)
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawWireSphere(playerObj.transform.position, treeDistance);

        // Draw billboard distance (yellow)
        Gizmos.color = new Color(1, 1, 0, 0.2f);
        Gizmos.DrawWireSphere(playerObj.transform.position, treeBillboardDistance);

        // Draw object culling radius (orange)
        Gizmos.color = new Color(1, 0.5f, 0, 0.15f);
        Gizmos.DrawWireSphere(playerObj.transform.position, objectRenderRadius);
    }
}