#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Creates local Resources copies from the user's exported game assets.</summary>
public static class OpenBonkPrepareCharacterResources
{
    private static readonly string[] Names = {
        "Amog", "Athena", "Bandit_0", "Birdo", "Bush_0", "Calcium", "Chadwell", "Clank",
        "Dicehead", "Fox", "Knight", "Megachad", "Monke", "Ninja", "Noelle", "Ogre",
        "Roberto", "Robinette", "Spaceman", "TonyMcZoom", "Vlad"
    };
    private const string DirectoryPath = "Assets/Resources/AndroidCharacters";
    private const string Fallback = "Assets/Resources/AndroidMegachad.prefab";

    public static void Run()
    {
        bool createdFolder = false;
        bool createdFallback = false;
        string backupRoot = null;
        bool sourceChanged = false;
        try
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            if (!File.Exists(Path.Combine(root, ".openbonk-workspace.json")))
                throw new InvalidOperationException("This step requires an isolated OpenBonk workspace.");
            backupRoot = Environment.GetEnvironmentVariable("OPENBONK_CHARACTER_BACKUP");
            if (string.IsNullOrEmpty(backupRoot) || !Path.IsPathRooted(backupRoot) || Directory.Exists(backupRoot) || File.Exists(backupRoot))
                throw new InvalidOperationException("A new absolute backup directory is required.");
            if (!AssetDatabase.IsValidFolder("Assets/Resources") || AssetDatabase.IsValidFolder(DirectoryPath) ||
                Directory.Exists(DirectoryPath) || File.Exists(DirectoryPath + ".meta") || File.Exists(Fallback) || File.Exists(Fallback + ".meta"))
                throw new InvalidOperationException("Resources destination is missing or already exists.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/B_Megachad.prefab") == null)
                throw new InvalidDataException("Missing Megachad fallback prefab.");
            var ids = new HashSet<ECharacter>();
            foreach (string name in Names)
            {
                string path = "Assets/MonoBehaviour/" + name + ".asset";
                var character = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
                if (character == null) throw new InvalidDataException("Missing character data: " + path);
                if (character.prefab == null || character.weapon == null || character.passive == null || character.icon == null || !ids.Add(character.eCharacter))
                    throw new InvalidDataException("Incomplete or duplicate character data: " + path +
                        " prefab=" + (character.prefab != null) + " weapon=" + (character.weapon != null) +
                        " passive=" + (character.passive != null) + " icon=" + (character.icon != null));
            }
            // The working build corrects these two default meshes to the ones
            // actually used by their animated prefabs. Preserve exported data.
            foreach (string name in new[] { "Bandit_0", "Clank" })
            {
                var character = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/" + name + ".asset");
                var renderer = character.prefab.GetComponentInChildren<SkinnedMeshRenderer>(true);
                if (renderer == null || renderer.sharedMesh == null || renderer.sharedMesh.bindposes.Length == 0)
                    throw new InvalidDataException("Missing animated prefab mesh for: " + name);
            }
            Directory.CreateDirectory(backupRoot);
            foreach (string name in new[] { "Bandit_0", "Clank" })
            {
                string asset = "Assets/MonoBehaviour/" + name + ".asset";
                File.Copy(asset, Path.Combine(backupRoot, name + ".asset"), false);
                File.Copy(asset + ".meta", Path.Combine(backupRoot, name + ".asset.meta"), false);
            }
            sourceChanged = true;
            foreach (string name in new[] { "Bandit_0", "Clank" })
            {
                var character = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/" + name + ".asset");
                character.meshDefault = character.prefab.GetComponentInChildren<SkinnedMeshRenderer>(true).sharedMesh;
                EditorUtility.SetDirty(character);
            }
            AssetDatabase.SaveAssets();
            if (string.IsNullOrEmpty(AssetDatabase.CreateFolder("Assets/Resources", "AndroidCharacters")))
                throw new IOException("Could not create the character Resources folder.");
            createdFolder = true;
            foreach (string name in Names)
                if (!AssetDatabase.CopyAsset("Assets/MonoBehaviour/" + name + ".asset", DirectoryPath + "/" + name + ".asset"))
                    throw new IOException("Could not copy character data: " + name);
            if (!AssetDatabase.CopyAsset("Assets/GameObject/B_Megachad.prefab", Fallback))
                throw new IOException("Could not copy the fallback prefab.");
            createdFallback = true;
            AssetDatabase.SaveAssets();
            foreach (string name in Names)
            {
                var original = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/MonoBehaviour/" + name + ".asset");
                var copy = AssetDatabase.LoadAssetAtPath<CharacterData>(DirectoryPath + "/" + name + ".asset");
                if (copy == null || copy.eCharacter != original.eCharacter || copy.prefab != original.prefab ||
                    copy.weapon != original.weapon || copy.passive != original.passive || copy.icon != original.icon)
                    throw new InvalidDataException("Copied character references did not match: " + name);
            }
            Debug.Log("Created 21 local character Resources and the fallback prefab.");
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            if (createdFallback) AssetDatabase.DeleteAsset(Fallback);
            if (createdFolder) AssetDatabase.DeleteAsset(DirectoryPath);
            if (sourceChanged)
            {
                foreach (string name in new[] { "Bandit_0", "Clank" })
                {
                    string asset = "Assets/MonoBehaviour/" + name + ".asset";
                    File.Copy(Path.Combine(backupRoot, name + ".asset"), asset, true);
                    AssetDatabase.ImportAsset(asset, ImportAssetOptions.ForceUpdate);
                }
            }
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }
}
#endif
