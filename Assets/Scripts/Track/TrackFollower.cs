using UnityEngine;

public class TrackFollower : MonoBehaviour
{
    [Header("Track Reference")]
    public TrackBuilder track;   // assign in inspector

    [Header("Movement")]
    public float moveSpeed = 2f;     // speed when holding W

    [Header("Rotation")]
    public float rotateSpeed = 720f; // deg/sec for smoothing

    [Header("Corners")]
    [Tooltip("Angle in degrees above which a change in direction counts as a 'corner'.")]
    public float cornerAngleThreshold = 5f;

    Transform[] nodes;
    int segmentIndex = 0;            // node[i] → node[i+1]
    float segmentLength;
    float distanceOnSegment = 0f;

    // +1 = along nodes[0]→nodes[1]→..., -1 = backwards
    int travelDirection = 1;

    Quaternion targetRotation;
    bool rotating = false;

    // ---- corner state ----
    bool atCorner = false;
    int cornerNodeIndex = -1;
    int upcomingSegmentIndex = -1;
    int cornerTurnSign = 0;  // -1 = left, +1 = right (relative to player facing)

    // are we on the main spine or a branch path?
    bool IsOnMainTrack => nodes == track.nodes;

    // choices at a junction when coming *from* a branch
    enum JunctionChoice
    {
        None,
        MainForward,
        MainBackward,
        Branch   // generic branch (could be any of the attached / reconnect branches)
    }

    void Start()
    {
        if (track == null || track.nodes == null || track.nodes.Length < 2)
        {
            Debug.LogError("TrackFollower: Track not set or invalid.");
            enabled = false;
            return;
        }

        nodes = track.nodes;
        segmentIndex = 0;
        distanceOnSegment = 0f;

        UpdateSegmentData();
        ApplyPositionOnSegment();
        SetTargetRotation();
        transform.rotation = targetRotation;
        rotating = false;
    }

    void Update()
    {
        HandleFlipInput();       // S to turn around (when not at a corner)
        HandleCornerDecision();  // A/D when *at* a corner
        HandleMoveInput();       // W to move along current path
        RotateToTarget();        // smooth turning
    }

    // --------- S = flip 180° (only when not at a corner) ----------

    void HandleFlipInput()
    {
        if (atCorner) return; // keep corner logic simple for now

        if (Input.GetKeyDown(KeyCode.S))
        {
            travelDirection *= -1;
            SetTargetRotation();
            rotating = true;
        }
    }

    // --------- A / D = choose to turn at a main-track corner ----------

    void HandleCornerDecision()
    {
        if (!atCorner) return;

        bool left = Input.GetKeyDown(KeyCode.A);
        bool right = Input.GetKeyDown(KeyCode.D);

        if (cornerTurnSign == 0)
        {
            atCorner = false;
            return;
        }

        bool correctLeft = left && cornerTurnSign == -1;
        bool correctRight = right && cornerTurnSign == +1;

        if (!(correctLeft || correctRight))
            return; // wrong key or no key yet

        // Commit to the next segment on this path
        segmentIndex = upcomingSegmentIndex;
        UpdateSegmentData();

        distanceOnSegment = (travelDirection > 0) ? 0f : segmentLength;
        ApplyPositionOnSegment();

        SetTargetRotation();
        rotating = true;

        atCorner = false;
        cornerNodeIndex = -1;
        upcomingSegmentIndex = -1;
        cornerTurnSign = 0;
    }

    // --------- W = move in current travelDirection ----------

    void HandleMoveInput()
    {
        // Don't move while waiting for a corner decision
        if (atCorner)
        {
            ApplyPositionOnSegment();
            return;
        }

        // Only move while W is held
        if (!Input.GetKey(KeyCode.W))
        {
            ApplyPositionOnSegment();
            return;
        }

        float move = moveSpeed * Time.deltaTime * travelDirection;
        distanceOnSegment += move;

        if (travelDirection > 0)
        {
            // moving forward along indices: 0→1→2→...
            while (distanceOnSegment >= segmentLength)
            {
                distanceOnSegment -= segmentLength;

                // Node we just reached
                Transform cornerNode = nodes[segmentIndex + 1];

                // 1) First, see if we want to switch onto a BRANCH at this node (main track only)
                // 1) Treat this as a full junction (main + any branches/loops)
                if (IsOnMainTrack)
                {
                    Vector3 incoming = cornerNode.position - nodes[segmentIndex].position;

                    if (ResolveJunctionAtNode(cornerNode, incoming))
                    {
                        // Junction resolver may have put us on main or a branch.
                        ApplyPositionOnSegment();
                        SetTargetRotation();
                        rotating = true;
                        return;
                    }
                }

                // 2) If no branch chosen, handle a CORNER on the current path
                if (segmentIndex < nodes.Length - 2)
                {
                    if (TryEnterCornerForward())
                    {
                        // snapped to corner and waiting for A/D, stop this frame
                        return;
                    }

                    // otherwise, just continue on this path
                    segmentIndex++;
                    UpdateSegmentData();
                }
                else
                {
                    // end of path
                    distanceOnSegment = segmentLength;

                    // If this is a BRANCH, only try to merge if player is actually steering (A/D)
                    if (!IsOnMainTrack)
                    {
                        bool leftHeld = Input.GetKey(KeyCode.A);
                        bool rightHeld = Input.GetKey(KeyCode.D);

                        // no steering input → just stop at the end of the branch
                        if (!leftHeld && !rightHeld)
                            break;

                        Transform branchEnd = nodes[segmentIndex + 1]; // last node of this path
                        Vector3 incoming = branchEnd.position - nodes[segmentIndex].position;

                        // 1) Is there a main-track junction node exactly here?
                        Transform junction = FindJunctionNodeAtPosition(branchEnd.position, 0.05f);
                        if (junction != null)
                        {
                            if (ResolveJunctionAtNode(junction, incoming))
                            {
                                ApplyPositionOnSegment();
                                SetTargetRotation();
                                rotating = true;
                                return;
                            }
                        }

                        // 2) Otherwise, fall back to the "attach mid-segment" helper
                        if (TryAttachBranchEndToMain(branchEnd.position, incoming))
                        {
                            ApplyPositionOnSegment();
                            SetTargetRotation();
                            rotating = true;
                            return;
                        }
                    }

                    // no merge possible → just stop at the end
                    break;
                }
            }
        }
        else
        {
            // moving backward along indices: ...→2→1→0
            while (distanceOnSegment < 0f)
            {
                // we just reached node[segmentIndex]
                if (segmentIndex > 0)
                {
                    // 1) Check for branch when moving "backwards" along main track
                    if (IsOnMainTrack)
                    {
                        Transform cornerNode = nodes[segmentIndex];
                        Vector3 incoming = cornerNode.position - nodes[segmentIndex + 1].position;

                        // snap to junction before switching
                        distanceOnSegment = 0f;

                        if (TrySwitchToBranchAtNode(cornerNode, incoming))
                        {
                            ApplyPositionOnSegment();
                            return;
                        }
                    }

                    // 2) Corner on the current path
                    if (TryEnterCornerBackward())
                    {
                        return; // snapped to corner, waiting for A/D
                    }

                    segmentIndex--;
                    UpdateSegmentData();
                    distanceOnSegment += segmentLength;
                }
                else
                {
                    // we're trying to go past the *start* of this path.
                    if (!IsOnMainTrack)
                    {
                        // arrived at the junction from a branch: treat as a junction
                        Transform junction = nodes[0];

                        Vector3 incoming;
                        if (nodes.Length > 1)
                            incoming = junction.position - nodes[1].position;
                        else
                            incoming = transform.forward;

                        if (ResolveJunctionAtNode(junction, incoming))
                        {
                            return;
                        }
                    }

                    // start of main path: just clamp
                    distanceOnSegment = 0f;
                    break;
                }
            }
        }

        ApplyPositionOnSegment();
        SetTargetRotation();
        rotating = true;
    }

    // ==========================================================
    // BRANCH / LOOP SELECTION (current node on MAIN → branch / loop)
    // ==========================================================

    bool TrySwitchToBranchAtNode(Transform cornerNode, Vector3 incoming)
    {
        bool leftHeld = Input.GetKey(KeyCode.A);
        bool rightHeld = Input.GetKey(KeyCode.D);

        if (!leftHeld && !rightHeld)
            return false; // player isn't asking to turn here

        // use the actual movement direction as the "from" direction
        Vector3 facing = incoming;
        facing.y = 0f;
        if (facing.sqrMagnitude < 0.0001f)
            facing = transform.forward;
        facing.Normalize();

        Transform[] chosenBranch = null;

        // helper: pick this branch array if A/D matches its first direction
        void ConsiderBranch(Transform[] arr)
        {
            if (chosenBranch != null || arr == null || arr.Length == 0) return;

            // arr[0] MUST be the first node *away from* the junction
            Vector3 d = (arr[0].position - cornerNode.position);
            d.y = 0f;
            if (d.sqrMagnitude < 0.0001f) return;   // guard against zero-length

            d.Normalize();
            int s = GetTurnSign(facing, d); // -1 left, +1 right

            if ((leftHeld && s == -1) || (rightHeld && s == +1))
                chosenBranch = arr;
        }

        // 1) Normal branches directly attached to this node
        TrackBranch brHere = cornerNode.GetComponent<TrackBranch>();
        if (brHere != null)
        {
            ConsiderBranch(brHere.branch1Nodes);
            ConsiderBranch(brHere.branch2Nodes);
        }

        // 2) Extra branches whose *end* is at this node (reconnect loops)
        var extra = FindAdditionalBranchesAtNode(cornerNode);
        foreach (var arr in extra)
            ConsiderBranch(arr);

        if (chosenBranch == null)
            return false; // A/D didn't match any branch direction

        // Build a new path: junction node + branch nodes
        Transform[] newNodes = new Transform[chosenBranch.Length + 1];
        newNodes[0] = cornerNode;
        for (int i = 0; i < chosenBranch.Length; i++)
            newNodes[i + 1] = chosenBranch[i];

        nodes = newNodes;
        segmentIndex = 0;
        travelDirection = 1; // moving outward along this branch
        distanceOnSegment = 0f;

        // reset any old corner state
        atCorner = false;
        cornerNodeIndex = -1;
        upcomingSegmentIndex = -1;
        cornerTurnSign = 0;

        UpdateSegmentData();
        SetTargetRotation();
        rotating = true;

        return true;
    }

    // ==========================================================
    // Branch arrays that END at this node (for reconnect / loops)
    // ==========================================================

    System.Collections.Generic.List<Transform[]> FindAdditionalBranchesAtNode(Transform cornerNode)
    {
        var list = new System.Collections.Generic.List<Transform[]>();
        if (track == null) return list;

        var allBranches = track.GetComponentsInChildren<TrackBranch>();
        float eps = 0.05f;
        float epsSqr = eps * eps;

        foreach (var br in allBranches)
        {
            if (br == null) continue;

            void CheckArray(Transform[] arr)
            {
                if (arr == null || arr.Length < 2) return; // need at least 2 nodes

                // skip the "normal" branch attached at this node – handled separately
                if (br.transform == cornerNode) return;

                Transform last = arr[arr.Length - 1];
                if (last == null) return;

                // does this branch END on this node?
                if ((last.position - cornerNode.position).sqrMagnitude <= epsSqr)
                {
                    // Build a path *away from* the corner:
                    // [ second-to-last, ..., first ]
                    int lenInterior = arr.Length - 1;
                    Transform[] path = new Transform[lenInterior];

                    for (int i = 0; i < lenInterior; i++)
                    {
                        // arr[lenInterior-1] is second-to-last
                        path[i] = arr[lenInterior - 1 - i];
                    }

                    // OPTIONAL: if the branch root (br.transform) is ALSO on the main track,
                    // append it so the path truly rejoins the main at the other end.
                    Transform[] main = track.nodes;
                    int rootIndex = System.Array.IndexOf(main, br.transform);
                    if (rootIndex >= 0)
                    {
                        // extend path by one slot, last element = branch root (original junction)
                        Transform[] extended = new Transform[lenInterior + 1];
                        for (int i = 0; i < lenInterior; i++)
                            extended[i] = path[i];
                        extended[lenInterior] = br.transform;

                        path = extended;
                    }

                    // path[0] is now the first node away from the reconnect corner;
                    // path[last] is the original junction node if it sits on the main track.
                    list.Add(path);
                }
            }

            CheckArray(br.branch1Nodes);
            CheckArray(br.branch2Nodes);
        }

        return list;
    }

    // ==========================================================
    // Generic junction resolver (used from branch starts & ends)
    // ==========================================================

    bool ResolveJunctionAtNode(Transform junction, Vector3 incoming)
    {
        if (junction == null) return false;

        // normalise incoming
        incoming.y = 0f;
        if (incoming.sqrMagnitude < 0.0001f)
            incoming = transform.forward;
        incoming.Normalize();

        Transform[] mainNodes = track.nodes;
        int mainIndex = System.Array.IndexOf(mainNodes, junction);

        TrackBranch br = junction.GetComponent<TrackBranch>();

        // ----- collect *all* branch-like paths that start or end here -----
        var branchCandidates = new System.Collections.Generic.List<Transform[]>();

        // direct branches from a TrackBranch on this node
        if (br != null)
        {
            if (br.branch1Nodes != null && br.branch1Nodes.Length > 0)
                branchCandidates.Add(br.branch1Nodes);

            if (br.branch2Nodes != null && br.branch2Nodes.Length > 0)
                branchCandidates.Add(br.branch2Nodes);
        }

        // reconnect / loop branches whose end sits on this node
        var extraPaths = FindAdditionalBranchesAtNode(junction);
        foreach (var arr in extraPaths)
        {
            if (arr != null && arr.Length > 0)
                branchCandidates.Add(arr);
        }

        bool leftHeld = Input.GetKey(KeyCode.A);
        bool rightHeld = Input.GetKey(KeyCode.D);

        // best choices for straight / left / right
        JunctionChoice straightBest = JunctionChoice.None;
        float straightBestAngle = 999f;
        Transform[] straightBranch = null;

        JunctionChoice leftBest = JunctionChoice.None;
        float leftBestAngle = 999f;
        Transform[] leftBranch = null;

        JunctionChoice rightBest = JunctionChoice.None;
        float rightBestAngle = 999f;
        Transform[] rightBranch = null;

        // helper that records best candidates
        void Consider(Vector3 dirWorld, JunctionChoice type, Transform[] branchArr)
        {
            if (type == JunctionChoice.None) return;

            dirWorld.y = 0f;
            if (dirWorld.sqrMagnitude < 0.0001f) return;
            dirWorld.Normalize();

            float angle = Vector3.Angle(incoming, dirWorld); // 0 = straight, ~90 = turn

            // best straight candidate (smallest angle)
            if (angle < straightBestAngle)
            {
                straightBestAngle = angle;
                straightBest = type;
                if (type == JunctionChoice.Branch) straightBranch = branchArr;
            }

            int sign = GetTurnSign(incoming, dirWorld);
            if (sign == -1 && angle < leftBestAngle)
            {
                leftBestAngle = angle;
                leftBest = type;
                if (type == JunctionChoice.Branch) leftBranch = branchArr;
            }
            else if (sign == +1 && angle < rightBestAngle)
            {
                rightBestAngle = angle;
                rightBest = type;
                if (type == JunctionChoice.Branch) rightBranch = branchArr;
            }
        }

        // ----- main-track forward/backward candidates -----

        // main forward (toward node mainIndex+1)
        if (mainIndex >= 0 && mainIndex < mainNodes.Length - 1)
        {
            Vector3 dir = mainNodes[mainIndex + 1].position - junction.position;
            Consider(dir, JunctionChoice.MainForward, null);
        }

        // main backward (toward node mainIndex-1)
        if (mainIndex > 0)
        {
            Vector3 dir = mainNodes[mainIndex - 1].position - junction.position;
            Consider(dir, JunctionChoice.MainBackward, null);
        }

        // ----- all branch candidates (including reconnects / loops) -----
        foreach (var branchArr in branchCandidates)
        {
            if (branchArr == null || branchArr.Length == 0) continue;
            Vector3 dir = branchArr[0].position - junction.position;
            Consider(dir, JunctionChoice.Branch, branchArr);
        }

        // ----- pick final choice -----
        JunctionChoice chosenType = JunctionChoice.None;
        Transform[] chosenBranch = null;

        if (leftHeld && leftBest != JunctionChoice.None)
        {
            chosenType = leftBest;
            if (chosenType == JunctionChoice.Branch) chosenBranch = leftBranch;
        }
        else if (rightHeld && rightBest != JunctionChoice.None)
        {
            chosenType = rightBest;
            if (chosenType == JunctionChoice.Branch) chosenBranch = rightBranch;
        }
        else
        {
            chosenType = straightBest;   // no turn input → go "straight"
            if (chosenType == JunctionChoice.Branch) chosenBranch = straightBranch;
        }

        if (chosenType == JunctionChoice.None)
            return false;

        // ----- Apply chosen lane -----

        if (chosenType == JunctionChoice.MainForward || chosenType == JunctionChoice.MainBackward)
        {
            nodes = mainNodes;

            if (chosenType == JunctionChoice.MainForward)
            {
                segmentIndex = mainIndex;
                travelDirection = 1;
                distanceOnSegment = 0f;
                UpdateSegmentData();
            }
            else // MainBackward
            {
                segmentIndex = Mathf.Max(0, mainIndex - 1);
                travelDirection = -1;
                UpdateSegmentData();
                distanceOnSegment = segmentLength; // start at junction, move "backward"
            }

            transform.position = junction.position;

            // clear corner state
            atCorner = false;
            cornerNodeIndex = -1;
            upcomingSegmentIndex = -1;
            cornerTurnSign = 0;

            SetTargetRotation();
            rotating = true;
            return true;
        }
        else // generic branch
        {
            if (chosenBranch == null || chosenBranch.Length == 0)
                return false;

            Transform[] newNodes = new Transform[chosenBranch.Length + 1];
            newNodes[0] = junction;
            for (int i = 0; i < chosenBranch.Length; i++)
                newNodes[i + 1] = chosenBranch[i];

            nodes = newNodes;
            segmentIndex = 0;
            travelDirection = 1;
            distanceOnSegment = 0f;

            transform.position = junction.position;

            // clear corner state
            atCorner = false;
            cornerNodeIndex = -1;
            upcomingSegmentIndex = -1;
            cornerTurnSign = 0;

            UpdateSegmentData();
            SetTargetRotation();
            rotating = true;
            return true;
        }
    }


    Transform FindJunctionNodeAtPosition(Vector3 pos, float maxDist)
    {
        Transform[] main = track.nodes;
        float maxSqr = maxDist * maxDist;
        int best = -1;
        float bestSqr = maxSqr;

        for (int i = 0; i < main.Length; i++)
        {
            if (main[i] == null) continue;
            float d2 = (main[i].position - pos).sqrMagnitude;
            if (d2 <= bestSqr)
            {
                bestSqr = d2;
                best = i;
            }
        }

        if (best < 0) return null;
        return main[best];
    }

    // ==========================================================
    // Corner detection for the CURRENT path
    // ==========================================================

    bool TryEnterCornerForward()
    {
        if (segmentIndex >= nodes.Length - 2)
            return false;

        Vector3 curDir = (nodes[segmentIndex + 1].position - nodes[segmentIndex].position).normalized;
        Vector3 nextDir = (nodes[segmentIndex + 2].position - nodes[segmentIndex + 1].position).normalized;
        float angle = Vector3.Angle(curDir, nextDir);

        if (angle <= cornerAngleThreshold)
            return false; // basically straight

        atCorner = true;
        cornerNodeIndex = segmentIndex + 1;
        upcomingSegmentIndex = segmentIndex + 1; // segment [segmentIndex+1] connects node[+1]→[+2]

        Vector3 facing = transform.forward;
        cornerTurnSign = GetTurnSign(facing, nextDir);

        transform.position = nodes[cornerNodeIndex].position;
        targetRotation = transform.rotation;
        rotating = false;

        distanceOnSegment = segmentLength; // end of old segment

        return true;
    }

    bool TryEnterCornerBackward()
    {
        if (segmentIndex <= 0)
            return false;

        Vector3 curDir = (nodes[segmentIndex].position - nodes[segmentIndex + 1].position).normalized;
        Vector3 nextDir = (nodes[segmentIndex - 1].position - nodes[segmentIndex].position).normalized;
        float angle = Vector3.Angle(curDir, nextDir);

        if (angle <= cornerAngleThreshold)
            return false;

        atCorner = true;
        cornerNodeIndex = segmentIndex;
        upcomingSegmentIndex = segmentIndex - 1;

        Vector3 facing = transform.forward;
        cornerTurnSign = GetTurnSign(facing, nextDir);

        transform.position = nodes[cornerNodeIndex].position;
        targetRotation = transform.rotation;
        rotating = false;

        distanceOnSegment = 0f; // start of old segment (since we were going backward)

        return true;
    }

    int GetTurnSign(Vector3 fromDir, Vector3 toDir)
    {
        fromDir.y = 0f;
        toDir.y = 0f;
        if (fromDir.sqrMagnitude < 0.0001f || toDir.sqrMagnitude < 0.0001f)
            return 0;

        fromDir.Normalize();
        toDir.Normalize();
        Vector3 cross = Vector3.Cross(fromDir, toDir);

        if (cross.y > 0f) return +1;  // right
        if (cross.y < 0f) return -1;  // left
        return 0;
    }

    // --------- Basic helpers ----------

    void ApplyPositionOnSegment()
    {
        Vector3 start = nodes[segmentIndex].position;
        Vector3 end = nodes[segmentIndex + 1].position;
        Vector3 dir = (end - start).normalized;

        transform.position = start + dir * distanceOnSegment;
    }

    void UpdateSegmentData()
    {
        Vector3 start = nodes[segmentIndex].position;
        Vector3 end = nodes[segmentIndex + 1].position;
        segmentLength = Vector3.Distance(start, end);
        if (segmentLength < 0.001f)
            segmentLength = 0.001f;
    }

    void SetTargetRotation()
    {
        Vector3 start = nodes[segmentIndex].position;
        Vector3 end = nodes[segmentIndex + 1].position;
        Vector3 dir = (end - start).normalized * travelDirection;

        if (dir.sqrMagnitude > 0.0001f)
            targetRotation = Quaternion.LookRotation(dir, Vector3.up);
    }

    void RotateToTarget()
    {
        if (!rotating) return;

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotateSpeed * Time.deltaTime
        );

        if (Quaternion.Angle(transform.rotation, targetRotation) < 0.1f)
        {
            transform.rotation = targetRotation;
            rotating = false;
        }
    }

    // --------- branch-end → main merge at overlapping node (fallback) ----------

    bool TryAttachBranchEndToMain(Vector3 endPos, Vector3 incoming)
    {
        if (track == null || track.nodes == null)
            return false;

        bool leftHeld = Input.GetKey(KeyCode.A);
        bool rightHeld = Input.GetKey(KeyCode.D);

        // If no turn key is held, treat this like any other junction that
        // needs input. (We already tried ResolveJunctionAtNode.)
        if (!leftHeld && !rightHeld)
            return false;

        int mainIndex = FindClosestMainNodeIndex(endPos, 0.05f);
        if (mainIndex < 0)
            return false;

        Transform[] main = track.nodes;

        Vector3 desired = incoming;
        desired.y = 0f;
        if (desired.sqrMagnitude < 0.0001f)
            desired = transform.forward;
        desired.Normalize();

        int bestSeg = -1;      // which segment index on main
        int bestTravel = 1;    // +1 or -1 along that segment
        float bestAngle = 999f;

        void Consider(int segIndex, int travelSign)
        {
            int fromIdx = (travelSign > 0) ? segIndex : segIndex + 1;
            int toIdx = (travelSign > 0) ? segIndex + 1 : segIndex;

            Vector3 dir = main[toIdx].position - main[fromIdx].position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            dir.Normalize();

            float angle = Vector3.Angle(desired, dir);
            int sign = GetTurnSign(desired, dir);

            bool wantThis =
                (leftHeld && sign == -1) ||
                (rightHeld && sign == +1);

            if (!wantThis) return;

            if (angle < bestAngle)
            {
                bestAngle = angle;
                bestSeg = segIndex;
                bestTravel = travelSign;
            }
        }

        // Candidate: mainIndex → mainIndex+1
        if (mainIndex < main.Length - 1)
            Consider(mainIndex, +1);

        // Candidate: mainIndex-1 → mainIndex
        if (mainIndex > 0)
            Consider(mainIndex - 1, -1);

        if (bestSeg < 0)
            return false;

        // Switch onto that main-track segment, facing the chosen direction
        nodes = main;
        segmentIndex = bestSeg;
        travelDirection = bestTravel;
        distanceOnSegment = 0f;

        // clear any stale corner info
        atCorner = false;
        cornerNodeIndex = -1;
        upcomingSegmentIndex = -1;
        cornerTurnSign = 0;

        UpdateSegmentData();

        int snapIdx = (travelDirection > 0) ? segmentIndex : segmentIndex + 1;
        transform.position = main[snapIdx].position;

        SetTargetRotation();
        rotating = true;

        return true;
    }

    int FindClosestMainNodeIndex(Vector3 pos, float maxDist)
    {
        Transform[] main = track.nodes;
        float maxSqr = maxDist * maxDist;
        int best = -1;
        float bestSqr = maxSqr;

        for (int i = 0; i < main.Length; i++)
        {
            if (main[i] == null) continue;
            float d2 = (main[i].position - pos).sqrMagnitude;
            if (d2 <= bestSqr)
            {
                bestSqr = d2;
                best = i;
            }
        }

        return best;
    }
}
