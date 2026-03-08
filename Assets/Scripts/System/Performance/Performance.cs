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

    [Range(0.01f, 1f)]
    public float terrainDetailDensity = 0.5f;
    private float lastTerrainDetailDensity = 0.5f;

    [Header("CTI WIND CONTROL - CRITICAL FOR PERFORMANCE")]
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

        Debug.Log($"[PERF] Found {totalRendererCount} renderers and {terrains.Length} terrains");
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
        debugText.fontSize = 36;
        debugText.color = Color.white;

        RectTransform rect = textGO.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = new Vector2(0.6f, 1);
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

        if (enableObjectCulling && playerTransform != null && allRenderers != null)
            UpdateObjectCulling();

        if (showDebugUI && debugText != null)
            UpdateDebugUI();
    }

    void UpdateCTIWind()
    {
        if (!enableWind)
        {
            // Disable all wind
            Shader.SetGlobalVector(ctiWindPropertyID, Vector4.zero);
            Shader.SetGlobalFloat(ctiTurbulencePropertyID, 0f);
        }
        else
        {
            // Set wind strength - update global shader properties
            // The windVector contains direction in XYZ and strength in W
            windVector = Vector4.zero;
            windVector.w = windStrength; // Wind strength in W channel

            Shader.SetGlobalVector(ctiWindPropertyID, windVector);
            Shader.SetGlobalFloat(ctiTurbulencePropertyID, windTurbulence);
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
        objectRenderRadiusSqr = objectRenderRadius * objectRenderRadius;
    }

    void UpdateDebugUI()
    {
        float fps = 1f / Time.deltaTime;

        string text = $"<color=green><b>PERFORMANCE SETTINGS (LIVE)</b></color>\n\n";
        text += $"<color={(fps > 60 ? "green" : fps > 30 ? "yellow" : "red")}>FPS: {fps:F0}</color>\n";
        text += $"Frame Time: {Time.deltaTime * 1000f:F2}ms\n\n";

        text += $"<b>Current Preset:</b> {qualityPreset}\n\n";

        text += $"<color=orange><b>⚠️ CTI WIND CONTROL (CRITICAL)</b></color>\n";
        text += $"<b>Enable Wind:</b> {(enableWind ? "<color=green>ON" : "<color=red>OFF")}</color>\n";
        text += $"<b>Wind Strength:</b> {windStrength:F2}x\n";
        text += $"<b>Wind Turbulence:</b> {windTurbulence:F2}x\n";
        text += $"<b>Global Wind Vector W:</b> {windVector.w:F3}\n";
        text += $"<b>Global Turbulence:</b> {Shader.GetGlobalFloat(ctiTurbulencePropertyID):F3}\n";
        text += $"\n";

        text += $"<color=orange><b>OTHER SETTINGS</b></color>\n";
        text += $"<b>LOD Bias:</b> {lodBias:F2}\n";
        text += $"<b>Terrain Detail Density:</b> {terrainDetailDensity:F2}\n";
        text += $"<b>Shadows:</b> {(enableShadows ? "<color=green>ON" : "<color=red>OFF")}</color>\n\n";

        if (enableObjectCulling)
        {
            float cullingPercent = totalRendererCount > 0 ? ((float)(totalRendererCount - enabledRendererCount) / totalRendererCount * 100f) : 0f;
            text += $"<b>Objects Culled:</b> {cullingPercent:F1}%\n";
        }

        text += $"\n<i><color=yellow>Toggle Enable Wind to freeze trees!\nSet Strength to 0 for no animation.</color></i>";

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
                enableWind = false;
                windStrength = 0f;
                windTurbulence = 0f;
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
                enableWind = true;
                windStrength = 0.2f;
                windTurbulence = 0.1f;
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
                enableWind = true;
                windStrength = 0.4f;
                windTurbulence = 0.2f;
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
                enableWind = true;
                windStrength = 0.6f;
                windTurbulence = 0.4f;
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
                enableWind = true;
                windStrength = 0.8f;
                windTurbulence = 0.6f;
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
                enableWind = true;
                windStrength = 1f;
                windTurbulence = 0.8f;
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
                enableWind = true;
                windStrength = 1.5f;
                windTurbulence = 1f;
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

        Gizmos.color = new Color(1, 0.5f, 0, 0.15f);
        Gizmos.DrawWireSphere(playerObj.transform.position, objectRenderRadius);
    }
}