using TMPro;
using UnityEditor;
using UnityEngine;

namespace SuikodenLike.EditorTools
{
    /// <summary>
    /// The one-click entry point: bakes placeholder art, creates the sample
    /// content assets, then builds and saves a playable scene.
    /// Menu: SuikodenLike > Build Sample Game
    /// </summary>
    public static class SuikodenLikeMenu
    {
        [MenuItem("SuikodenLike/Build Sample Game", false, 0)]
        public static void BuildSampleGame()
        {
            // TextMeshPro needs its runtime resources imported once per project;
            // without them every label in the scene would come out blank.
            if (TMP_Settings.defaultFontAsset == null)
            {
                EditorUtility.DisplayDialog(
                    "Import TextMeshPro first",
                    "This project's UI uses TextMeshPro, and its essential resources " +
                    "haven't been imported yet.\n\n" +
                    "Go to  Window > TextMeshPro > Import TMP Essential Resources,\n" +
                    "then run  SuikodenLike > Build Sample Game  again.",
                    "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Build the sample game?",
                    "This creates placeholder art, content assets and a scene at\n" +
                    SampleSceneBuilder.ScenePath + "\n\n" +
                    "Any scene currently open will be closed — save it first.",
                    "Build it", "Cancel"))
                return;

            try
            {
                EditorUtility.DisplayProgressBar("Building sample game", "Baking placeholder art...", 0.15f);
                var art = PlaceholderArt.BakeAll();

                EditorUtility.DisplayProgressBar("Building sample game", "Creating content assets...", 0.5f);
                var content = SampleContentBuilder.Build(art);

                EditorUtility.DisplayProgressBar("Building sample game", "Assembling the scene...", 0.8f);
                SampleSceneBuilder.Build(art, content);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            Debug.Log("[SuikodenLike] Sample game built. Open " + SampleSceneBuilder.ScenePath +
                      " and press Play.\nWASD/arrows to move, E to talk and open chests, " +
                      "I for the bag. Walk into the tall grass on the right for a fight.");

            EditorUtility.DisplayDialog(
                "Done",
                "The sample scene is open and saved.\n\nPress Play, then:\n" +
                "  WASD / arrows  — walk\n" +
                "  E  — talk to people, open chests\n" +
                "  I  — open the bag\n\n" +
                "Walk into the tall grass on the right side of the map to trigger a battle.",
                "Play time");
        }

        [MenuItem("SuikodenLike/Rebake Placeholder Art Only", false, 20)]
        public static void RebakeArt()
        {
            PlaceholderArt.BakeAll();
            AssetDatabase.Refresh();
            Debug.Log("[SuikodenLike] Placeholder art rebaked into " + PlaceholderArt.ArtFolder + ".");
        }
    }
}
