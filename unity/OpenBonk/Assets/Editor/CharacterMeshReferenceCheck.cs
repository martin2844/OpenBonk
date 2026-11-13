using System;
using UnityEditor;
using UnityEngine;

public static class CharacterMeshReferenceCheck
{
    public static void Run()
    {
        foreach (string name in new[] { "Clank", "Bandit_0" })
        {
            var character = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/" + name + ".asset");
            var android = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/Resources/AndroidCharacters/" + name + ".asset");
            var renderer = character != null && character.prefab != null ? character.prefab.GetComponentInChildren<SkinnedMeshRenderer>(true) : null;
            if (renderer == null || renderer.sharedMesh == null || renderer.sharedMesh.bindposes.Length == 0 ||
                character.meshDefault != renderer.sharedMesh || android == null || android.meshDefault != renderer.sharedMesh)
                throw new Exception("CharacterMeshReferenceCheck failed: " + name);
            Debug.Log($"CharacterMeshReferenceCheck: {name} default={renderer.sharedMesh.name}, bindposes={renderer.sharedMesh.bindposes.Length}");
        }
        Debug.Log("CharacterMeshReferenceCheck PASS: original prefab meshes assigned in both asset locations");
    }

    public static void AuditRoster()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:CharacterData", new[] { "Assets/MonoBehaviour" }))
        {
            var character = AssetDatabase.LoadAssetAtPath<CharacterData>(AssetDatabase.GUIDToAssetPath(guid));
            if (character == null || !character.isEnabled || character.prefab == null) continue;
            var renderer = character.prefab.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (renderer == null) { Debug.LogWarning("Character mesh audit: missing renderer " + character.name); continue; }
            foreach (var entry in new[] { ("default", character.meshDefault), ("low", character.meshLow), ("lowest", character.meshLowest) })
            {
                if (entry.Item2 == null) continue;
                var poses = entry.Item2.bindposes;
                var originalPoses = renderer.sharedMesh != null ? renderer.sharedMesh.bindposes : Array.Empty<Matrix4x4>();
                float delta = 0f;
                if (poses.Length == originalPoses.Length)
                    for (int i = 0; i < poses.Length; i++) for (int j = 0; j < 16; j++)
                        delta = Mathf.Max(delta, Mathf.Abs(poses[i][j] - originalPoses[i][j]));
                else delta = float.PositiveInfinity;
                Debug.Log($"Character mesh audit: {character.name} {entry.Item1}={entry.Item2.name} bindposes={poses.Length} bones={renderer.bones.Length} samePrefab={entry.Item2 == renderer.sharedMesh} maxBindPoseDelta={delta:G9}");
            }
        }
        Debug.Log("Character mesh audit complete");
    }
}
