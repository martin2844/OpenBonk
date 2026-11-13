using UnityEditor;
using UnityEngine;

public static class LegacyVertexLitMaterialAudit
{
    public static void Run()
    {
        int found = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null || material.shader == null || material.shader.name != "Legacy Shaders/VertexLit") continue;
            Debug.Log($"LegacyVertexLitMaterialAudit: path={path}, material={material.name}, instancing={material.enableInstancing}");
            found++;
        }
        Debug.Log($"LegacyVertexLitMaterialAudit complete: {found} matching material assets");
    }
}
