using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class HexGridGenerator : MonoBehaviour
{
    //==========================================================================
    // SERIALIZED FIELDS - CONFIGURATION
    //==========================================================================

    [Header("Hex Prefab")]
    [SerializeField] private GameObject hexPrefab;

    [Header("Grid Size")]
    [Tooltip(
        "Number of hex rings around the center.\n" +
        "0 = 1 hex\n" +
        "1 = 7 hexes\n" +
        "2 = 19 hexes\n" +
        "3 = 37 hexes\n" +
        "4 = 61 hexes"
    )]
    [Min(0)]
    [SerializeField] private int radius = 3;

    [Header("Negative Radius / Hole")]
    [Tooltip("Enable this to remove hexes from the center of the grid.")]
    [SerializeField] private bool useNegativeRadius = false;

    [Tooltip(
        "Number of inner rings to remove from the center.\n" +
        "Example: Radius 7 + Negative Radius 1 removes the center ring."
    )]
    [Min(0)]
    [SerializeField] private int negativeRadius = 0;

    [Header("Spacing")]
    [Tooltip("Center-to-center distance between neighboring hexes.")]
    [Min(0.01f)]
    [SerializeField] private float spacing = 2f;

    [Header("Color")]
    [Tooltip("Color applied to all generated hexes when using Context Menu generation.")]
    [SerializeField] private Color hexColor = Color.white;

    [Header("Random Color")]
    [Tooltip("If enabled, hitting PLAY will apply one shared random color to the entire grid (excludes white).")]
    [SerializeField] private bool useRandomColor = false;

    [Header("Orientation")]
    [Tooltip(
        "Y-axis rotation applied to each hex so its edges align with neighbors " +
        "instead of its corners. 30 works for most pointy-top hex meshes; " +
        "try 0 if your prefab is already correctly aligned."
    )]
    [SerializeField] private float hexYRotation = 30f;

    [Header("Generation")]
    [SerializeField] private bool generateOnStart = false;

    //==========================================================================
    // CONSTANTS
    //==========================================================================

    private const string GeneratedParentName = "_GeneratedHexes";

    //==========================================================================
    // LIFECYCLE METHODS
    //==========================================================================

    // We use Awake so the color changes BEFORE the HexTile scripts run their Start() methods.
    // This ensures the tiles cache the new random color properly for their blinking mechanics.
    private void Awake()
    {
        if (Application.isPlaying)
        {
            if (generateOnStart)
            {
                GenerateGrid();
            }
            else if (useRandomColor)
            {
                // If the grid was already generated in the editor, just recolor it on play
                RecolorExistingGrid();
            }
        }
    }

    //==========================================================================
    // PUBLIC METHODS - GRID GENERATION
    //==========================================================================
    [ContextMenu("Generate Hex Grid")]
    public void GenerateGrid()
    {
        if (hexPrefab == null)
        {
            Debug.LogWarning($"[{name}] Hex Prefab is not assigned.");
            return;
        }

        // Validate negative radius before generating.
        if (useNegativeRadius)
        {
            if (negativeRadius >= radius)
            {
                Debug.LogError(
                    $"[{name}] Invalid Negative Radius!\n" +
                    $"Radius = {radius}, Negative Radius = {negativeRadius}.\n" +
                    "The Negative Radius must be sufficiently smaller than the main Radius."
                );
                return;
            }

            if (Mathf.Abs(radius - negativeRadius) <= 1)
            {
                Debug.LogError(
                    $"[{name}] Radius and Negative Radius are too close!\n" +
                    $"Radius = {radius}, Negative Radius = {negativeRadius}.\n" +
                    "Increase the main Radius or decrease the Negative Radius."
                );
                return;
            }
        }

        ClearGrid();

        Transform generatedParent = CreateGeneratedParent();

        // 1. Determine the shared grid color
        Color gridColor = hexColor; // Default to the inspector color

        // If we are generating at runtime and Random Color is checked, override with one random color
        if (Application.isPlaying && useRandomColor)
        {
            // Random.ColorHSV ensures vibrant colors and avoids white, grey, or black.
            gridColor = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.6f, 1f);
        }

        int hexCount = 0;
        int removedCount = 0;

        // Generate the complete outer hexagonal structure.
        for (int q = -radius; q <= radius; q++)
        {
            int rMin = Mathf.Max(-radius, -q - radius);
            int rMax = Mathf.Min(radius, -q + radius);

            for (int r = rMin; r <= rMax; r++)
            {
                int hexDistance = Mathf.Max(
                    Mathf.Abs(q),
                    Mathf.Abs(r),
                    Mathf.Abs(q + r)
                );

                if (useNegativeRadius && hexDistance <= negativeRadius)
                {
                    removedCount++;
                    continue;
                }

                float localX = spacing * (q + r * 0.5f);
                float localZ = spacing * (r * Mathf.Sqrt(3f) * 0.5f);

                Vector3 localPosition = new Vector3(localX, 0f, localZ);

                GameObject hex = Instantiate(hexPrefab, generatedParent);
                hex.name = $"Hex_{q}_{r}";
                hex.transform.localPosition = localPosition;
                hex.transform.localRotation = Quaternion.Euler(0f, hexYRotation, 0f);
                hex.transform.localScale = Vector3.one;

                // Apply the single shared color to this specific tile
                ApplyColor(hex, gridColor);

                hexCount++;
            }
        }

        Debug.Log(
            $"[{name}] Generated {hexCount} hexes." +
            (useNegativeRadius ? $" Removed {removedCount} inner hexes." : "")
        );
    }

    [ContextMenu("Clear Hex Grid")]
    public void ClearGrid()
    {
        Transform generatedParent = transform.Find(GeneratedParentName);

        if (generatedParent == null)
            return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            DestroyImmediate(generatedParent.gameObject);
            return;
        }
#endif

        Destroy(generatedParent.gameObject);
    }

    //==========================================================================
    //-------------------PRIVATE METHODS - HELPERS------------------------------
    //==========================================================================

    private void RecolorExistingGrid()
    {
        Transform generatedParent = transform.Find(GeneratedParentName);
        if (generatedParent == null) return;

        // Generate ONE shared random color for the entire pre-existing grid
        Color sharedRandomColor = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.6f, 1f);

        // Apply it to all children
        foreach (Transform child in generatedParent)
        {
            ApplyColor(child.gameObject, sharedRandomColor);
        }
    }

    private Transform CreateGeneratedParent()
    {
        Transform existingParent = transform.Find(GeneratedParentName);
        if (existingParent != null)
        {
            return existingParent;
        }

        GameObject parentObject = new GameObject(GeneratedParentName);
        parentObject.transform.SetParent(transform);
        parentObject.transform.localPosition = Vector3.zero;
        parentObject.transform.localRotation = Quaternion.identity;
        parentObject.transform.localScale = Vector3.one;

        return parentObject.transform;
    }

    private void ApplyColor(GameObject hex, Color colorToApply)
    {
        Renderer[] renderers = hex.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            Material materialInstance = new Material(renderer.sharedMaterial);
            materialInstance.color = colorToApply;
            renderer.material = materialInstance;
        }
    }
}