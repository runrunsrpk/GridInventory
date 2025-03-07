using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ItemData))]
public class ItemSOEditor : Editor
{
    private SerializedProperty shapeProperty;
    private int width = 1;
    private int height = 1;

    private void OnEnable()
    {
        ItemData item = (ItemData)target;

        // default shape
        if (item.shape == null)
        {
            item.shape = new bool[1, 1];
            item.shape[0, 0] = true;
        }

        // set width and height
        width = item.shape.GetLength(0);
        height = item.shape.GetLength(1);
    }

    public override void OnInspectorGUI()
    {
        // show default inspector
        DrawDefaultInspector();

        // show shape editor inspector
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Item Shape", EditorStyles.boldLabel);

        ItemData item = (ItemData)target;

        EditorGUILayout.BeginVertical();
        width = EditorGUILayout.IntField("Width", width);
        height = EditorGUILayout.IntField("Height", height);
        if (GUILayout.Button("Resize"))
        {
            ResizeShapeArray(item);
        }
        EditorGUILayout.EndVertical();

        // show checkbox on grid
        if (item.shape != null)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear All"))
            {
                Undo.RecordObject(item, "Clear Shape");
                for (int x = 0; x < item.shape.GetLength(0); x++)
                {
                    for (int y = 0; y < item.shape.GetLength(1); y++)
                    {
                        item.shape[x, y] = false;
                    }
                }
                EditorUtility.SetDirty(item);
            }

            if (GUILayout.Button("Fill All"))
            {
                Undo.RecordObject(item, "Fill Shape");
                for (int x = 0; x < item.shape.GetLength(0); x++)
                {
                    for (int y = 0; y < item.shape.GetLength(1); y++)
                    {
                        item.shape[x, y] = true;
                    }
                }
                EditorUtility.SetDirty(item);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // draw grid
            for (int y = 0; y < item.shape.GetLength(1); y++)
            {
                EditorGUILayout.BeginHorizontal();
                for (int x = 0; x < item.shape.GetLength(0); x++)
                {
                    bool newValue = EditorGUILayout.Toggle(item.shape[x, y], GUILayout.Width(15), GUILayout.Height(15));
                    if (newValue != item.shape[x, y])
                    {
                        Undo.RecordObject(item, "Change Item Shape");
                        item.shape[x, y] = newValue;
                        EditorUtility.SetDirty(item);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }
    }

    private void ResizeShapeArray(ItemData item)
    {
        Undo.RecordObject(item, "Resize Item Shape");

        bool[,] newShape = new bool[width, height];

        int oldWidth = item.shape != null ? item.shape.GetLength(0) : 0;
        int oldHeight = item.shape != null ? item.shape.GetLength(1) : 0;

        for (int x = 0; x < Mathf.Min(oldWidth, width); x++)
        {
            for (int y = 0; y < Mathf.Min(oldHeight, height); y++)
            {
                newShape[x, y] = item.shape[x, y];
            }
        }

        item.shape = newShape;
        EditorUtility.SetDirty(item);
    }
}
