using UnityEngine;

public class RailMover : MonoBehaviour
{
    [Header("Map")]
    public GridRailMap map;

    [Header("Start")]
    public Vector2Int startNode = new Vector2Int(0, 0);
    public Vector2Int startDirection = Vector2Int.up; // (0,1) = +Z

    [Header("Movement")]
    public float minSpeed = 2f;        // minimum moving speed
    public float maxSpeed = 8f;        // top speed
    public float acceleration = 12f;   // units/sec^2
    public float rotationSpeed = 720f; // deg/sec for visual rotation

    // Graph state
    Vector2Int currentNode;     // node we're logically "at"
    Vector2Int targetNode;      // node we're moving to (when stepping)
    Vector2Int facingDir;       // current facing direction (cardinal)

    bool isStepping = false;    // true = moving from currentNode -> targetNode
    float stepT = 0f;           // 0..1 progress along this step
    float currentSpeed = 0f;    // world units / second along the edge

    void Start()
    {
        if (map == null)
        {
            Debug.LogError("RailMover: Please assign a GridRailMap.");
            enabled = false;
            return;
        }

        currentNode = startNode;
        facingDir = startDirection == Vector2Int.zero ? Vector2Int.up : startDirection;

        // Snap exactly to start node
        transform.position = map.NodeToWorld(currentNode);
        FaceInstant(currentNode + facingDir);

        isStepping = false;
        stepT = 0f;
        currentSpeed = 0f;
    }

    void Update()
    {
        // If we're not moving along a segment, always snap to the current node
        if (!isStepping)
        {
            SnapToCurrentNode();
        }

        HandleTurnInput(); // A/D/S
        HandleMoveInput(); // W & stepping
        RotateVisual();    // smooth model rotation
    }

    // ---------------- INPUT ----------------

    void HandleTurnInput()
    {
        // 1) only turn when we're *not* moving along an edge
        if (isStepping) return;

        Vector2Int newDir = facingDir;
        bool wantTurn = false;

        if (Input.GetKeyDown(KeyCode.A))
        {
            newDir = RotateLeft(facingDir);
            wantTurn = true;
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            newDir = RotateRight(facingDir);
            wantTurn = true;
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            newDir = -facingDir;
            wantTurn = true;
        }

        // 1) Only actually change facing if that direction has a rail from this node.
        if (wantTurn && map.HasEdge(currentNode, newDir))
        {
            facingDir = newDir;
        }
    }

    void HandleMoveInput()
    {
        bool wHeld = Input.GetKey(KeyCode.W);

        if (!isStepping)
        {
            // On a node.
            if (wHeld)
            {
                // If there's a rail ahead, start moving along it.
                TryStartStep();
            }
            else
            {
                // Standing still
                currentSpeed = 0f;
            }
        }
        else
        {
            // Moving along an edge.
            StepAlongEdge(wHeld);
        }
    }

    // ---------------- STEPPING ----------------

    void TryStartStep()
    {
        // Only walk if there is a rail in facingDir
        if (!map.HasEdge(currentNode, facingDir))
            return;

        targetNode = currentNode + facingDir;
        isStepping = true;
        stepT = 0f;

        // ensure we don't crawl; kick up to at least minSpeed
        if (currentSpeed < minSpeed)
            currentSpeed = minSpeed;

        // exact start-of-edge
        transform.position = map.NodeToWorld(currentNode);
    }

    void StepAlongEdge(bool wHeld)
    {
        // 2) If W is released, stop immediately wherever we are.
        if (!wHeld)
        {
            isStepping = false;
            currentSpeed = 0f;
            return;
        }

        Vector3 from = map.NodeToWorld(currentNode);
        Vector3 to = map.NodeToWorld(targetNode);

        float segmentLength = Vector3.Distance(from, to);
        if (segmentLength < 0.0001f)
        {
            // Degenerate, just snap to target node
            CompleteStep();
            return;
        }

        // 3) Accelerate while W is held, clamped between min/max.
        currentSpeed += acceleration * Time.deltaTime;
        currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed);

        // convert world speed into 0..1 stepT increment
        float deltaT = (currentSpeed * Time.deltaTime) / segmentLength;
        stepT += deltaT;
        if (stepT >= 1f)
        {
            stepT = 1f;
        }

        Vector3 pos = Vector3.Lerp(from, to, stepT);
        transform.position = pos;

        // When we reach the target node, decide whether to stop or continue
        if (stepT >= 1f - Mathf.Epsilon)
        {
            CompleteStep();

            // At the node now; if W is still held and there's a rail, auto-start next step
            if (wHeld && map.HasEdge(currentNode, facingDir))
            {
                // don't reset speed; keep whatever we've built up
                TryStartStep();
            }
            else
            {
                // no more forward motion
                currentSpeed = 0f;
            }
        }
    }

    void CompleteStep()
    {
        // We've finished moving from currentNode -> targetNode
        currentNode = targetNode;
        transform.position = map.NodeToWorld(currentNode);

        isStepping = false;
        stepT = 0f;
    }

    void SnapToCurrentNode()
    {
        transform.position = map.NodeToWorld(currentNode);
    }

    // ---------------- VISUAL ROTATION ----------------

    void RotateVisual()
    {
        // Face in the facingDir direction
        Vector3 facingWorld = new Vector3(facingDir.x, 0f, facingDir.y);
        if (facingWorld.sqrMagnitude < 0.01f) return;

        Quaternion targetRot = Quaternion.LookRotation(facingWorld, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRot,
            rotationSpeed * Time.deltaTime
        );
    }

    void FaceInstant(Vector2Int node)
    {
        Vector3 targetPos = map.NodeToWorld(node);
        Vector3 dir = targetPos - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;
        transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
    }

    // ---------------- UTILS ----------------

    Vector2Int RotateLeft(Vector2Int dir)
    {
        if (dir == Vector2Int.up) return Vector2Int.left;
        if (dir == Vector2Int.left) return Vector2Int.down;
        if (dir == Vector2Int.down) return Vector2Int.right;
        if (dir == Vector2Int.right) return Vector2Int.up;
        return dir;
    }

    Vector2Int RotateRight(Vector2Int dir)
    {
        if (dir == Vector2Int.up) return Vector2Int.right;
        if (dir == Vector2Int.right) return Vector2Int.down;
        if (dir == Vector2Int.down) return Vector2Int.left;
        if (dir == Vector2Int.left) return Vector2Int.up;
        return dir;
    }
}
