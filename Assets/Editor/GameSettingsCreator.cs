using UnityEditor;
using UnityEngine;

/// <summary>
/// One-click creation of the GameSettings asset in Assets/Resources so the
/// game picks it up automatically.
/// </summary>
public static class GameSettingsCreator
{
    [MenuItem("Tools/Raggler/Create Game Settings Asset")]
    public static void CreateAsset()
    {
        var existing = Resources.Load<GameSettings>("GameSettings");
        if (existing != null)
        {
            Selection.activeObject = existing;
            EditorGUIUtility.PingObject(existing);
            Debug.Log("GameSettings asset already exists — selected it for you.");
            return;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");

        var settings = ScriptableObject.CreateInstance<GameSettings>();
        settings.PopulateDefaults();
        AssetDatabase.CreateAsset(settings, "Assets/Resources/GameSettings.asset");
        AssetDatabase.SaveAssets();
        Selection.activeObject = settings;
        EditorGUIUtility.PingObject(settings);
        Debug.Log("Created Assets/Resources/GameSettings.asset — customize cards, levels, background and Endless mode there.");
    }
}
