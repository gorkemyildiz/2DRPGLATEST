using UnityEngine;
using UnityEditor;

public class SimpleTest
{
    [MenuItem("Tools/Test Menu Item")]
    private static void TestMenuItem()
    {
        Debug.Log("Test menu item works!");
        EditorUtility.DisplayDialog("Success", "Test menu item works!", "OK");
    }
}
