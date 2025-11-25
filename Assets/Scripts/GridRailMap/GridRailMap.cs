using System;
using System.Collections.Generic;
using UnityEngine;

public class GridRailMap : MonoBehaviour
{
    [Header("Grid Size")]
    public int width = 8;
    public int height = 8;

    [Header("Layout")]
    public float cellSize = 1f;
    [Tooltip("Local offset from this object to the bottom-left of the grid")]
    public Vector3 localOrigin = Vector3.zero;  // bottom-left in *local* space

    [Header("Cells (edit these booleans to turn rails on/off)")]
    public List<RailCell> cells = new List<RailCell>();

    [Header("Runtime visuals (Game view)")]
    public bool buildRailsAtRuntime = true;
    public Material railMaterial;
    public float railThickness = 0.1f;
    public float railY = 0.02f;   // height above ground

    [Tooltip("Multiplier applied to the visual length of each rail segment (before parent scaling).")]
    [Range(0.001f, 1f)]
    public float railLengthFactor = 1f;

    [Serializable]
    public class RailCell
    {
        public bool openNorth = true; // (0, +1)
        public bool openEast = true; // (+1, 0)
        public bool openSouth = true; // (0, -1)
        public bool openWest = true; // (-1, 0)
    }

    void OnValidate()
    {
        int needed = Mathf.Max(0, width * height);
        while (cells.Count < needed) cells.Add(new RailCell());
        while (cells.Count > needed) cells.RemoveAt(cells.Count - 1);

        // just clamp borders, no mesh building here
        SyncEdges();

        // safety clamp in case inspector range is bypassed somehow
        railLengthFactor = Mathf.Clamp(railLengthFactor, 0.001f, 1f);
    }

    void SyncEdges()
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                RailCell c = GetCell(x, y);

                // Bounds: if on border, no edge out of grid
                if (y == height - 1) c.openNorth = false;
                if (x == width - 1) c.openEast = false;
                if (y == 0) c.openSouth = false;
                if (x == 0) c.openWest = false;
            }
        }
    }

    Color GetColorForDir(Vector2Int dir)
    {
        if (dir == Vector2Int.up) return Color.blue;    // North
        if (dir == Vector2Int.right) return new Color(1f, 0.5f, 0f); // East = orange
        if (dir == Vector2Int.down) return Color.red;     // South
        if (dir == Vector2Int.left) return Color.green;   // West

        return Color.white;
    }

    public bool InBounds(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < width &&
               cell.y >= 0 && cell.y < height;
    }

    RailCell GetCell(int x, int y)
    {
        x = Mathf.Clamp(x, 0, width - 1);
        y = Mathf.Clamp(y, 0, height - 1);
        int index = x + y * width;
        return cells[index];
    }

    // Logical rail: exists only if *both* cells say it's open.
    public bool HasEdge(Vector2Int from, Vector2Int dir)
    {
        if (!InBounds(from)) return false;
        Vector2Int to = from + dir;
        if (!InBounds(to)) return false;

        RailCell cFrom = GetCell(from.x, from.y);
        RailCell cTo = GetCell(to.x, to.y);

        if (dir == Vector2Int.up)
            return cFrom.openNorth && cTo.openSouth;
        if (dir == Vector2Int.right)
            return cFrom.openEast && cTo.openWest;
        if (dir == Vector2Int.down)
            return cFrom.openSouth && cTo.openNorth;
        if (dir == Vector2Int.left)
            return cFrom.openWest && cTo.openEast;

        return false;
    }

    public Vector3 NodeToWorld(Vector2Int cell)
    {
        // bottom-left of grid in world space, based on this object
        Vector3 worldOrigin = transform.TransformPoint(localOrigin);
        return worldOrigin + new Vector3(cell.x * cellSize, 0f, cell.y * cellSize);
    }

    // ---------- SCENE VIEW GIZMOS (yellow lines only) ----------

    void OnDrawGizmos()
    {
        if (width <= 0 || height <= 0) return;

        // Draw nodes
        Gizmos.color = Color.gray;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3 p = NodeToWorld(new Vector2Int(x, y));
                p.y += 0.03f;
                Gizmos.DrawSphere(p, cellSize * 0.05f);
            }
        }

        if (cells == null || cells.Count != width * height) return;

        // Draw rails
        Gizmos.color = Color.yellow;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                Vector3 from = NodeToWorld(cell);
                from.y += 0.04f;

                if (HasEdge(cell, Vector2Int.up))
                {
                    Vector3 to = NodeToWorld(cell + Vector2Int.up);
                    to.y += 0.04f;
                    Gizmos.DrawLine(from, to);
                }

                if (HasEdge(cell, Vector2Int.right))
                {
                    Vector3 to = NodeToWorld(cell + Vector2Int.right);
                    to.y += 0.04f;
                    Gizmos.DrawLine(from, to);
                }
            }
        }
    }

    // ---------- RUNTIME RAILS (actual cubes for Game view) ----------
    // (NOTE: you only call BuildRailsRuntime yourself; nothing runs in Start.)

    void BuildRailsRuntime()
    {
        Transform railsParent = new GameObject("RailsRuntime").transform;
        railsParent.SetParent(transform, false);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int cellPos = new Vector2Int(x, y);

                // Draw an arm in each direction, BUT ONLY if the logical edge exists
                if (HasEdge(cellPos, Vector2Int.up))
                    CreateRailArm(cellPos, Vector2Int.up, railsParent);

                if (HasEdge(cellPos, Vector2Int.right))
                    CreateRailArm(cellPos, Vector2Int.right, railsParent);

                if (HasEdge(cellPos, Vector2Int.down))
                    CreateRailArm(cellPos, Vector2Int.down, railsParent);

                if (HasEdge(cellPos, Vector2Int.left))
                    CreateRailArm(cellPos, Vector2Int.left, railsParent);
            }
        }
    }
    void CreateRailArm(Vector2Int fromCell, Vector2Int dir, Transform parent)
    {
        Vector3 from = NodeToWorld(fromCell);
        from.y += railY;

        Vector3 to = NodeToWorld(fromCell + dir);
        to.y += railY;

        Vector3 fullDir = to - from;
        float baseLength = fullDir.magnitude;
        if (baseLength <= 0.0001f) return;

        Vector3 dirNorm = fullDir / baseLength;

        // Max arm is half the cell distance (so we never cross the midpoint)
        float maxArmLength = baseLength * 0.5f;
        float armLength = maxArmLength * railLengthFactor;   // railLengthFactor = 0..1

        // Center is half the arm length away from the node
        Vector3 mid = from + dirNorm * (armLength * 8f);

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = $"Rail {fromCell} dir {dir}";
        go.transform.SetParent(parent, false);

        go.transform.position = mid;
        go.transform.rotation = Quaternion.LookRotation(dirNorm, Vector3.up);
        go.transform.localScale = new Vector3(railThickness, railThickness, armLength);

        var rend = go.GetComponent<Renderer>();
        if (rend != null)
        {
            if (railMaterial != null)
                rend.material = new Material(railMaterial);

            rend.material.color = GetColorForDir(dir);
        }

        var col = go.GetComponent<Collider>();
        if (col) col.enabled = false;
    }




#if UNITY_EDITOR
    // ---------- EDITOR CONTEXT MENU: redraw rails ----------

    [ContextMenu("Redraw Active Grid Lines")]
    void RedrawActiveGridLines()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("Redraw Active Grid Lines: run this in Edit Mode, not Play Mode.");
            return;
        }

        // Remove any existing RailsRuntime children
        var toDelete = new List<GameObject>();
        foreach (Transform child in transform)
        {
            if (child.name == "RailsRuntime")
                toDelete.Add(child.gameObject);
        }
        foreach (var go in toDelete)
        {
            UnityEditor.Undo.DestroyObjectImmediate(go);
        }

        BuildRailsRuntimeEditor();
    }

    void BuildRailsRuntimeEditor()
    {
        if (cells == null || cells.Count != width * height) return;

        Transform railsParent = new GameObject("RailsRuntime").transform;
        UnityEditor.Undo.RegisterCreatedObjectUndo(railsParent.gameObject, "Create RailsRuntime");
        railsParent.SetParent(transform, false);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int cellPos = new Vector2Int(x, y);

                if (HasEdge(cellPos, Vector2Int.up))
                    CreateRailArmEditor(cellPos, Vector2Int.up, railsParent);

                if (HasEdge(cellPos, Vector2Int.right))
                    CreateRailArmEditor(cellPos, Vector2Int.right, railsParent);

                if (HasEdge(cellPos, Vector2Int.down))
                    CreateRailArmEditor(cellPos, Vector2Int.down, railsParent);

                if (HasEdge(cellPos, Vector2Int.left))
                    CreateRailArmEditor(cellPos, Vector2Int.left, railsParent);
            }
        }
    }


    void CreateRailArmEditor(Vector2Int fromCell, Vector2Int dir, Transform parent)
    {
        Vector3 from = NodeToWorld(fromCell);
        from.y += railY;

        Vector3 to = NodeToWorld(fromCell + dir);
        to.y += railY;

        Vector3 fullDir = to - from;
        float baseLength = fullDir.magnitude;
        if (baseLength <= 0.0001f) return;

        Vector3 dirNorm = fullDir / baseLength;

        float maxArmLength = baseLength * 0.5f;
        float armLength = maxArmLength * railLengthFactor;

        Vector3 mid = from + dirNorm * (armLength * 8f);

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = $"Rail {fromCell} dir {dir}";
        UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Create Rail Segment");

        go.transform.SetParent(parent, false);
        go.transform.position = mid;
        go.transform.rotation = Quaternion.LookRotation(dirNorm, Vector3.up);
        go.transform.localScale = new Vector3(railThickness, railThickness, armLength);

        var rend = go.GetComponent<Renderer>();
        if (rend != null)
        {
            if (railMaterial != null)
                rend.sharedMaterial = new Material(railMaterial);

            rend.sharedMaterial.color = GetColorForDir(dir);
        }

        var col = go.GetComponent<Collider>();
        if (col) UnityEditor.Undo.DestroyObjectImmediate(col);
    }

#endif
}

