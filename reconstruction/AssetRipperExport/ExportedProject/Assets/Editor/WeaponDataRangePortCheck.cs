using System;
using UnityEditor;
using UnityEngine;

public static class WeaponDataRangePortCheck
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception("WeaponDataRangePortCheck: " + message); }
    public static void Run()
    {
        var data = ScriptableObject.CreateInstance<WeaponData>();
        try
        {
            Check(data.CalculateTotalDistance(5f, 2f) == 9f, "positive speed decreases until zero");
            Check(data.CalculateTotalDistance(0f, 2f) == 0f && data.CalculateTotalDistance(-1f, 2f) == 0f, "nonpositive speed");
            Check(data.CalculateTotalDistance(120f, 20f) == 415f, "subsequent speed is capped at 99");
            int count = 0, nonzero = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:WeaponData"))
            {
                var weapon = AssetDatabase.LoadAssetAtPath<WeaponData>(AssetDatabase.GUIDToAssetPath(guid));
                if (weapon == null) continue;
                Check(weapon.GetSpawnProjectileRange() == weapon.spawnProjectileRange, weapon.name + " range");
                count++; if (weapon.spawnProjectileRange != 0f) nonzero++;
            }
            Check(count > 0 && nonzero > 0, "original weapon assets were loaded");
            Debug.Log($"WeaponDataRangePortCheck passed: distances and {count} weapon assets, {nonzero} nonzero ranges");
        }
        finally { UnityEngine.Object.DestroyImmediate(data); }
    }
}
