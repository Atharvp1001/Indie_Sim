#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class EnemyHitboxBulkSetup : EditorWindow
{
    private float hitboxRadius = 0.7f;
    private string hitboxLayer = "EnemyHitbox";

    [MenuItem("Tools/Setup All Enemy Hitboxes")]
    public static void ShowWindow()
    {
        GetWindow<EnemyHitboxBulkSetup>("Enemy Hitbox Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Bulk Enemy Hitbox Setup", EditorStyles.boldLabel);

        hitboxRadius = EditorGUILayout.FloatField("Hitbox Radius:", hitboxRadius);
        hitboxLayer = EditorGUILayout.TextField("Hitbox Layer:", hitboxLayer);

        GUILayout.Space(10);

        if (GUILayout.Button("Add Hitbox to All Enemies in Scene"))
        {
            AddHitboxesToAllEnemies();
        }
    }

    private void AddHitboxesToAllEnemies()
    {
        // Find all GameObjects with "Enemy" in the name or tag
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int count = 0;

        foreach (GameObject obj in allObjects)
        {
            // Check if this is an enemy (adjust condition as needed)
            if (obj.CompareTag("Enemy") || obj.name.Contains("Enemy"))
            {
                // Add the setup script if it doesn't have one
                if (obj.GetComponent<EnemyHitboxSetup>() == null)
                {
                    EnemyHitboxSetup setup = obj.AddComponent<EnemyHitboxSetup>();

                    // Use reflection to set private fields (hacky but works)
                    var radiusField = typeof(EnemyHitboxSetup).GetField("hitboxRadius",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (radiusField != null)
                        radiusField.SetValue(setup, hitboxRadius);

                    var layerField = typeof(EnemyHitboxSetup).GetField("hitboxLayer",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (layerField != null)
                        layerField.SetValue(setup, hitboxLayer);

                    count++;
                }
            }
        }

        Debug.Log($"[EnemyHitboxBulkSetup] Added hitboxes to {count} enemies!");
        EditorUtility.DisplayDialog("Success", $"Added hitboxes to {count} enemies!", "OK");
    }
}
#endif
