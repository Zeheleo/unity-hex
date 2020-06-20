using UnityEngine;
using UnityEditor;

public class ExampleWindow : ScriptableWizard
{
    [MenuItem("Window/Example")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow<ExampleWindow>("Test");
    }

    private void OnGUI()
    {
       
    }
}
