using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GridRailMap))]
public class GridRailMapEditor : Editor
{
    SerializedProperty widthProp;
    SerializedProperty heightProp;
    SerializedProperty cellSizeProp;
    SerializedProperty localOriginProp;
    SerializedProperty cellsProp;
    SerializedProperty buildRailsProp;
    SerializedProperty railMatProp;
    SerializedProperty railThicknessProp;
    SerializedProperty railYProp;
    SerializedProperty railLengthFactorProp;

    void OnEnable()
    {
        widthProp = serializedObject.FindProperty("width");
        heightProp = serializedObject.FindProperty("height");
        cellSizeProp = serializedObject.FindProperty("cellSize");
        localOriginProp = serializedObject.FindProperty("localOrigin");
        cellsProp = serializedObject.FindProperty("cells");
        buildRailsProp = serializedObject.FindProperty("buildRailsAtRuntime");
        railMatProp = serializedObject.FindProperty("railMaterial");
        railThicknessProp = serializedObject.FindProperty("railThickness");
        railYProp = serializedObject.FindProperty("railY");
        railLengthFactorProp = serializedObject.FindProperty("railLengthFactor");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Grid Size", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(widthProp);
        EditorGUILayout.PropertyField(heightProp);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Layout", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(cellSizeProp);
        EditorGUILayout.PropertyField(localOriginProp);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Runtime visuals", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(buildRailsProp);
        EditorGUILayout.PropertyField(railMatProp);
        EditorGUILayout.PropertyField(railThicknessProp);
        EditorGUILayout.PropertyField(railYProp);
        EditorGUILayout.PropertyField(railLengthFactorProp);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Cells (edit these booleans to turn rails on/off)", EditorStyles.boldLabel);

        int width = Mathf.Max(1, widthProp.intValue);
        int height = Mathf.Max(1, heightProp.intValue);

        for (int i = 0; i < cellsProp.arraySize; i++)
        {
            SerializedProperty cellProp = cellsProp.GetArrayElementAtIndex(i);
            int x = i % width;
            int y = i / width;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"Cell ({x}, {y})", EditorStyles.boldLabel);

            DrawDirectionWithMirror(cellProp, x, y, width, height, "openNorth", 0, +1, "North");
            DrawDirectionWithMirror(cellProp, x, y, width, height, "openEast", +1, 0, "East");
            DrawDirectionWithMirror(cellProp, x, y, width, height, "openSouth", 0, -1, "South");
            DrawDirectionWithMirror(cellProp, x, y, width, height, "openWest", -1, 0, "West");

            EditorGUILayout.EndVertical();
        }

        serializedObject.ApplyModifiedProperties();
    }

    void DrawDirectionWithMirror(SerializedProperty cellProp,
                                 int x, int y, int width, int height,
                                 string localFieldName,
                                 int dx, int dy,
                                 string label)
    {
        bool isBorder =
            (dy == +1 && y >= height - 1) ||
            (dy == -1 && y <= 0) ||
            (dx == +1 && x >= width - 1) ||
            (dx == -1 && x <= 0);

        var localProp = cellProp.FindPropertyRelative(localFieldName);

        if (isBorder)
        {
            // enforce false at borders
            localProp.boolValue = false;
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.Toggle($"{label} (border)", false);
            EditorGUI.EndDisabledGroup();
            return;
        }

        EditorGUI.BeginChangeCheck();
        bool newVal = EditorGUILayout.Toggle(label, localProp.boolValue);
        if (EditorGUI.EndChangeCheck())
        {
            // set this side
            localProp.boolValue = newVal;

            // mirror to neighbor’s opposite side
            int nx = x + dx;
            int ny = y + dy;
            if (nx >= 0 && nx < width && ny >= 0 && ny < height)
            {
                int neighborIndex = nx + ny * width;
                SerializedProperty neighborCell = cellsProp.GetArrayElementAtIndex(neighborIndex);

                string oppositeField = "";
                if (localFieldName == "openNorth") oppositeField = "openSouth";
                else if (localFieldName == "openSouth") oppositeField = "openNorth";
                else if (localFieldName == "openEast") oppositeField = "openWest";
                else if (localFieldName == "openWest") oppositeField = "openEast";

                if (!string.IsNullOrEmpty(oppositeField))
                {
                    SerializedProperty oppProp = neighborCell.FindPropertyRelative(oppositeField);
                    oppProp.boolValue = newVal;
                }
            }
        }
    }
}
