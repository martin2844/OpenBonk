using System;
using UnityEditor;
using UnityEngine;

public static class ToonEmissionSkinAudit
{
    public static void Run()
    {
        int skins = 0, materials = 0, missing = 0, emissive = 0, masked = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:SkinData")) {
            var skin = AssetDatabase.LoadAssetAtPath<SkinData>(AssetDatabase.GUIDToAssetPath(guid));
            if (skin == null || skin.materials == null) continue;
            skins++;
            foreach (var material in skin.materials) {
                materials++;
                if (material == null || !material.HasProperty("_EmissionColor") || !material.HasProperty("_EmissionMap")) { missing++; Debug.LogError($"ToonEmissionSkinAudit missing properties: skin={skin.name} material={material}"); continue; }
                if (material.IsKeywordEnabled("_MK_EMISSION")) emissive++;
                if (material.IsKeywordEnabled("_MK_EMISSION_MAP")) masked++;
            }
        }
        if (skins == 0 || missing != 0) throw new Exception($"ToonEmissionSkinAudit failed: skins={skins} materials={materials} missing={missing}");
        Debug.Log($"ToonEmissionSkinAudit passed: skins={skins} materials={materials} missing={missing} emission={emissive} emissionMap={masked}");
    }
}
