using UnityEngine;

[ExecuteAlways]
public class GridVisualizer : MonoBehaviour
{
    [Header("Grid Size")]
    public int width = 10;
    public int height = 10;
    public float cellSize = 1f;

    [Header("Visuals")]
    public Color lineColor = Color.gray;
    public float lineWidth = 0.02f;
    public float yOffset = 0.01f;          // lift lines slightly above plane
    public Material lineMaterial;          // assign a simple unlit material

    [Header("Debug")]
    public bool drawGizmosInSceneView = true;

    void Start()
    {
        // Only build runtime lines in play mode (no need to spam in editor move)
        if (Application.isPlaying)
        {
            BuildRuntimeGridLines();
        }
    }

    void BuildRuntimeGridLines()
    {
        if (lineMaterial == null)
        {
            Debug.LogWarning("GridVisualizer: Please assign a lineMaterial for runtime grid.");
            return;
        }

        // Clear any existing line children
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        float halfWidth = width * cellSize * 0.5f;
        float halfHeight = height * cellSize * 0.5f;
        Vector3 origin = transform.position + Vector3.up * yOffset;

        // Vertical lines
        for (int x = 0; x <= width; x++)
        {
            float worldX = -halfWidth + x * cellSize;
            Vector3 from = origin + new Vector3(worldX, 0f, -halfHeight);
            Vector3 to = origin + new Vector3(worldX, 0f, halfHeight);

            CreateLineRenderer(from, to);
        }

        // Horizontal lines
        for (int z = 0; z <= height; z++)
        {
            float worldZ = -halfHeight + z * cellSize;
            Vector3 from = origin + new Vector3(-halfWidth, 0f, worldZ);
            Vector3 to = origin + new Vector3(halfWidth, 0f, worldZ);

            CreateLineRenderer(from, to);
        }
    }

    void CreateLineRenderer(Vector3 from, Vector3 to)
    {
        GameObject lineObj = new GameObject("GridLine");
        lineObj.transform.SetParent(transform, worldPositionStays: true);

        var lr = lineObj.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        lr.SetPosition(0, from);
        lr.SetPosition(1, to);

        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.material = lineMaterial;
        lr.startColor = lineColor;
        lr.endColor = lineColor;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
    }

    void OnDrawGizmos()
    {
        if (!drawGizmosInSceneView) return;

        Gizmos.color = lineColor;

        float halfWidth = width * cellSize * 0.5f;
        float halfHeight = height * cellSize * 0.5f;
        Vector3 origin = transform.position;

        for (int x = 0; x <= width; x++)
        {
            float worldX = -halfWidth + x * cellSize;
            Vector3 from = origin + new Vector3(worldX, 0f, -halfHeight);
            Vector3 to = origin + new Vector3(worldX, 0f, halfHeight);
            Gizmos.DrawLine(from, to);
        }

        for (int z = 0; z <= height; z++)
        {
            float worldZ = -halfHeight + z * cellSize;
            Vector3 from = origin + new Vector3(-halfWidth, 0f, worldZ);
            Vector3 to = origin + new Vector3(halfWidth, 0f, worldZ);
            Gizmos.DrawLine(from, to);
        }
    }
}
