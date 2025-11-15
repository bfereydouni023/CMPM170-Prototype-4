using UnityEngine;

public class GridMover : MonoBehaviour
{
    [Header("Grid Reference")]
    public GridVisualizer grid;          // drag your Plane with GridVisualizer here

    [Header("Movement")]
    public float maxSpeed = 6f;
    public float acceleration = 10f;
    public float deceleration = 12f;
    public float startingSpeed = 2f;
    public float rotateSpeed = 360f;

    float currentSpeed = 0f;

    // 0 = +Z, 1 = +X, 2 = -Z, 3 = -X
    int dirIndex = 0;
    Quaternion targetRotation;
    bool isRotating = false;

    readonly Vector3[] worldDirs = {
        Vector3.forward,
        Vector3.right,
        Vector3.back,
        Vector3.left
    };

    void Start()
    {
        if (grid == null)
        {
            Debug.LogError("GridMover: Please assign a GridVisualizer reference.");
            enabled = false;
            return;
        }

        dirIndex = 0;
        targetRotation = Quaternion.LookRotation(worldDirs[dirIndex], Vector3.up);
        transform.rotation = targetRotation;

        AlignToGrid();
    }

    void Update()
    {
        HandleTurnInput();
        HandleMoveInput();
        ApplyMovement();
        RotateToTarget();
    }

    void HandleTurnInput()
    {
        if (isRotating) return;

        if (Input.GetKeyDown(KeyCode.A))
        {
            dirIndex = (dirIndex + 3) % 4;  // left
            targetRotation = Quaternion.LookRotation(worldDirs[dirIndex], Vector3.up);
            isRotating = true;
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            dirIndex = (dirIndex + 1) % 4;  // right
            targetRotation = Quaternion.LookRotation(worldDirs[dirIndex], Vector3.up);
            isRotating = true;
        }
    }

    void HandleMoveInput()
    {
        bool forwardHeld = Input.GetKey(KeyCode.W);
        bool backwardHeld = Input.GetKey(KeyCode.S);

        float desiredSpeed = 0f;

        if (forwardHeld && !backwardHeld)
        {
            desiredSpeed = maxSpeed;

            if (Mathf.Abs(currentSpeed) < startingSpeed)
                currentSpeed = Mathf.Sign(desiredSpeed) * startingSpeed;
        }
        else if (backwardHeld && !forwardHeld)
        {
            desiredSpeed = -maxSpeed * 0.5f;

            if (Mathf.Abs(currentSpeed) < startingSpeed)
                currentSpeed = Mathf.Sign(desiredSpeed) * startingSpeed;
        }
        else
        {
            desiredSpeed = 0f;
        }

        float rate;
        if (Mathf.Approximately(desiredSpeed, 0f))
            rate = deceleration;
        else if (Mathf.Sign(desiredSpeed) == Mathf.Sign(currentSpeed) || Mathf.Approximately(currentSpeed, 0f))
            rate = acceleration;
        else
            rate = deceleration;

        currentSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed, rate * Time.deltaTime);
    }

    void ApplyMovement()
    {
        if (Mathf.Approximately(currentSpeed, 0f))
            return;

        Vector3 moveDir = worldDirs[dirIndex];
        Vector3 delta = moveDir * currentSpeed * Time.deltaTime;

        Vector3 newPos = transform.position + delta;

        // bounds from the grid
        float halfWidth = grid.width * grid.cellSize * 0.5f;
        float halfHeight = grid.height * grid.cellSize * 0.5f;
        Vector3 origin = grid.transform.position;

        // clamp inside grid
        newPos.x = Mathf.Clamp(newPos.x, origin.x - halfWidth, origin.x + halfWidth);
        newPos.z = Mathf.Clamp(newPos.z, origin.z - halfHeight, origin.z + halfHeight);

        // snap to nearest perpendicular line
        if (dirIndex == 0 || dirIndex == 2)
        {
            // moving along Z → lock X
            newPos.x = SnapToGridLine(
                newPos.x, origin.x, halfWidth, grid.width, grid.cellSize);
        }
        else
        {
            // moving along X → lock Z
            newPos.z = SnapToGridLine(
                newPos.z, origin.z, halfHeight, grid.height, grid.cellSize);
        }

        transform.position = newPos;
    }

    void RotateToTarget()
    {
        if (!isRotating) return;

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotateSpeed * Time.deltaTime
        );

        if (Quaternion.Angle(transform.rotation, targetRotation) < 0.1f)
        {
            transform.rotation = targetRotation;
            isRotating = false;
        }
    }

    void AlignToGrid()
    {
        float halfWidth = grid.width * grid.cellSize * 0.5f;
        float halfHeight = grid.height * grid.cellSize * 0.5f;
        Vector3 origin = grid.transform.position;

        Vector3 pos = transform.position;
        pos.x = SnapToGridLine(pos.x, origin.x, halfWidth, grid.width, grid.cellSize);
        pos.z = SnapToGridLine(pos.z, origin.z, halfHeight, grid.height, grid.cellSize);
        transform.position = pos;
    }

    float SnapToGridLine(float value, float origin, float halfExtent, int cells, float cellSize)
    {
        // value relative to origin
        float rel = value - origin;
        float index = Mathf.Round((rel + halfExtent) / cellSize);
        index = Mathf.Clamp(index, 0, cells);
        return origin - halfExtent + index * cellSize;
    }
}
