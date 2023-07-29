using UnityEditor;
using UnityEngine;

/*
[InitializeOnLoad]
[CustomEditor(typeof(GameManager))]
public class GameManagerEditor : Editor
{
    private SerializedProperty boatControllersProperty;

    static GameManagerEditor()
    {
        GameManager gameManager = FindObjectOfType<GameManager>();
    }
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Show the array property in a custom way
        EditorGUILayout.LabelField("Boat Controllers");
        EditorGUI.indentLevel++;
        int count = boatControllersProperty.arraySize;

        for(int i = 0; i < count; i++)
        {
            SerializedProperty elementProperty = boatControllersProperty.GetArrayElementAtIndex(i);

            EditorGUILayout.PropertyField(elementProperty, GUIContent.none);
        }
        EditorGUI.indentLevel--;

        serializedObject.ApplyModifiedProperties();
    }

    private void OnEnable()
    {
        boatControllersProperty = serializedObject.FindProperty("boatControllers");
    }
}*/
