#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class OpenBonkMapObjectCheck
{
    public static void Run()
    {
        var forest = AssetDatabase.LoadAssetAtPath<MapData>("Assets/MonoBehaviour/MapForest.asset");
        var desert = AssetDatabase.LoadAssetAtPath<MapData>("Assets/MonoBehaviour/MapDesert.asset");
        var icon = new Texture2D(2, 2); var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            var tile = new OpenBonkProceduralMapRecipe { name = "Object test", description = "Test", tileGridSize = 9, volatility = 1, slopeStrength = 0, flatMapBias = 0, stageDuration = 600 };
            var mesh = new OpenBonkMeshMapRecipe { name = "Object test", description = "Test", noiseScale = 80, heightMultiplier = 8, mapScale = 1, stageDuration = 600 };
            using var forestBuild = OpenBonkProceduralMapBuild.Create(forest, tile, icon);
            using var desertBuild = OpenBonkMeshMapBuild.Create(desert, mesh, icon);
            var placement = new OpenBonkMapObjects.Placement { prefab = prefab, amount = 5, scaleMin = 1, scaleMax = 2, checkRadius = 1, maxSlopeAngle = 30, upOffset = 0 };
            Check(forest, forestBuild.Map, placement); Check(desert, desertBuild.Map, placement);
            Debug.Log("OpenBonk map objects PASS: both templates append independent placements and reject invalid input");
        }
        finally { UnityEngine.Object.DestroyImmediate(icon); UnityEngine.Object.DestroyImmediate(prefab); }
    }
    private static void Check(MapData original, OpenBonkExternalMapData map, OpenBonkMapObjects.Placement placement)
    {
        int[] counts = new int[map.stages.Length];
        for (int i = 0; i < counts.Length; i++) counts[i] = original.stages[i].randomMapObjects?.Length ?? 0;
        OpenBonkMapObjects.AddToStages(map, new[] { placement });
        for (int i = 0; i < counts.Length; i++)
        {
            var stage = map.stages[i]; var entry = stage.randomMapObjects[counts[i]];
            if (stage.randomMapObjects.Length != counts[i] + 1 || (original.stages[i].randomMapObjects?.Length ?? 0) != counts[i] || entry.prefabs[0] != placement.prefab || entry.amount != 5)
                throw new Exception("Map object placement mismatch");
        }
        placement.amount = 0; bool rejected = false;
        try { OpenBonkMapObjects.AddToStages(map, new[] { placement }); } catch (ArgumentException) { rejected = true; }
        placement.amount = 5;
        if (!rejected || map.stages[0].randomMapObjects.Length != counts[0] + 1) throw new Exception("Invalid map object mutated stages");
        placement.stages = new[] { 1, 3 };
        OpenBonkMapObjects.AddToStages(map, new[] { placement });
        for (int i = 0; i < counts.Length; i++)
            if (map.stages[i].randomMapObjects.Length != counts[i] + 1 + (i == 1 ? 0 : 1) || (original.stages[i].randomMapObjects?.Length ?? 0) != counts[i])
                throw new Exception("Map object stage selection mismatch");
        foreach (var stages in new[] { Array.Empty<int>(), new[] { 1, 1 }, new[] { 0 }, new[] { 4 } })
        {
            placement.stages = stages; rejected = false;
            try { OpenBonkMapObjects.AddToStages(map, new[] { placement }); } catch (ArgumentException) { rejected = true; }
            if (!rejected) throw new Exception("Invalid stage selection accepted");
            for (int i = 0; i < counts.Length; i++)
                if (map.stages[i].randomMapObjects.Length != counts[i] + 1 + (i == 1 ? 0 : 1)) throw new Exception("Invalid stage selection mutated stages");
        }
        placement.stages = null;
        Debug.Log("OpenBonk map object stage selection PASS: stages 1 and 3, invalid lists rejected");
    }
    public static void BuildBundle()
    {
        const string dir = "Assets/OpenBonkModAuthoring/MapProbe";
        Directory.CreateDirectory(dir);
        var material = AssetDatabase.LoadAssetAtPath<Material>(dir + "/ProbeMaterial.mat");
        if (material == null) { material = new Material(Shader.Find("Standard")) { color = new Color(1f, 0f, .65f) }; AssetDatabase.CreateAsset(material, dir + "/ProbeMaterial.mat"); }
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "OpenBonk Test Beacon"; cube.transform.localScale = new Vector3(4, 8, 4);
        cube.GetComponent<Renderer>().sharedMaterial = material;
        PrefabUtility.SaveAsPrefabAsset(cube, dir + "/ProbeBeacon.prefab"); UnityEngine.Object.DestroyImmediate(cube);
        AssetDatabase.SaveAssets();
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../../build/android/map-object-bundle")); Directory.CreateDirectory(output);
        var build = new AssetBundleBuild { assetBundleName = "map-props", assetNames = new[] { dir + "/ProbeBeacon.prefab" } };
        if (BuildPipeline.BuildAssetBundles(output, new[] { build }, BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode, BuildTarget.Android) == null)
            throw new Exception("Map object bundle build failed");
        Debug.Log("OpenBonk map object bundle: " + output);
    }
}
#endif
