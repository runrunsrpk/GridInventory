using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ItemData))]
public class ItemSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // show default inspector
        DrawDefaultInspector();

        // show shape editor inspector
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Item Shape", EditorStyles.boldLabel);

        ItemData item = (ItemData)target;

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
}
