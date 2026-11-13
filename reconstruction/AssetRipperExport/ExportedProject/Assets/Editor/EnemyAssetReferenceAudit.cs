using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EnemyAssetReferenceAudit
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/ScenesAndroid/MainMenu.unity", OpenSceneMode.Single);
        var manager = Object.FindAnyObjectByType<DataManager>(FindObjectsInactive.Include);
        if (manager == null || manager.unsortedEnemies == null) throw new System.Exception("EnemyAssetReferenceAudit: manager missing");
        int examined = 0, missing = 0, frames = 0;
        foreach (var enemy in manager.unsortedEnemies)
        {
            examined++;
            if (enemy == null || enemy.material == null || enemy.animation == null || enemy.animation.Animations == null || enemy.animation.Animations.Count == 0 || enemy.animation.Animations[0].Meshes == null || enemy.animation.Animations[0].Meshes.Count == 0)
            {
                missing++;
                Debug.LogWarning("Enemy asset audit missing data: " + (enemy != null ? enemy.name : "null"));
                continue;
            }
            var meshes = enemy.animation.Animations[0].Meshes;
            int firstVertices = meshes[0] != null ? meshes[0].vertexCount : 0;
            foreach (var mesh in meshes)
            {
                frames++;
                if (mesh == null) { missing++; Debug.LogWarning("Enemy asset audit missing frame: " + enemy.name); }
                else if (mesh.vertexCount != firstVertices) Debug.LogWarning($"Enemy asset audit vertex mismatch: {enemy.name} first={firstVertices} current={mesh.vertexCount}");
            }
            Debug.Log($"Enemy asset audit: {enemy.enemyName} data={enemy.name} animation={enemy.animation.name} frames={meshes.Count} vertices={firstVertices} material={enemy.material.name} shader={(enemy.material.shader != null ? enemy.material.shader.name : "missing")}");
        }
        Debug.Log($"Enemy asset audit RESULT entries={examined} frames={frames} missing={missing}");
    }
}
