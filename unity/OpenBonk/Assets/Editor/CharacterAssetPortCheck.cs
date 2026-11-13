using System;
using UnityEditor;
using UnityEngine;

public static class CharacterAssetPortCheck
{
    public static void Run()
    {
        int count = 0;
        int enabled = 0;
        int passives = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:CharacterData"))
        {
            var character = AssetDatabase.LoadAssetAtPath<CharacterData>(AssetDatabase.GUIDToAssetPath(guid));
            if (character == null) continue;
            count++;
            if (!character.isEnabled) continue;
            enabled++;
            character.Init();
            if (character.categoryRatios == null || character.GetIcon() != character.icon) throw new Exception("Invalid character data: " + character.name);
            if (character.passive != null) passives++;
        }
        if (enabled == 0) throw new Exception("No enabled character assets found");
        Debug.Log($"CharacterAssetPortCheck passed: assets={count}, enabled={enabled}, withPassive={passives}");
    }
}
