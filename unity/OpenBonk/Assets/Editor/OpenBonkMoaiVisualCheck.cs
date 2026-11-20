#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Managers;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class OpenBonkMoaiVisualCheck
{
    private const string ShrinePath = "Assets/GameObject/ShrineMaoi.prefab";
    private static readonly MethodInfo Replace = typeof(InteractableShrineMoai).GetMethod("ReplaceVisual", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly MethodInfo StartShrine = typeof(InteractableShrineMoai).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

    public static void Run()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShrinePath);
        var forest = AssetDatabase.LoadAssetAtPath<MapData>("Assets/MonoBehaviour/MapForest.asset");
        Require(prefab != null && forest != null && forest.stages.Length > 1 && Replace != null && StartShrine != null, "native fixtures and replacement hooks");
        byte[] originalFile = File.ReadAllBytes(ShrinePath);
        bool originalDirty = EditorUtility.IsDirty(prefab);
        var nativeRenderer = prefab.GetComponent<MeshRenderer>();
        var nativeMesh = prefab.GetComponent<MeshFilter>().sharedMesh;
        var nativeBounds = nativeMesh.bounds;
        var savedMap = MapController.currentMap;
        var savedStage = MapController.currentStage;
        var map = ScriptableObject.CreateInstance<OpenBonkExternalMapData>();
        var overrides = (IDictionary)typeof(OpenBonkExternalMapData).GetField("moaiVisuals", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(map);
        var sources = new List<GameObject>();
        var meshes = new List<Mesh>();
        var material = new Material(Shader.Find("Unlit/Color"));
        int cases = 0;
        try
        {
            // Offset centers catch fitting code that assumes an origin-centered
            // mesh; both width- and height-constrained shapes must keep +Y up.
            foreach (var size in new[] { new Vector3(1.3f, 3.4f, .9f), new Vector3(4.2f, 1.7f, 2.5f) })
            {
                var source = new GameObject("Moai +Y mock"); sources.Add(source);
                var mesh = Box(new Bounds(new Vector3(.43f, size.y * .5f, -.31f), size)); meshes.Add(mesh);
                source.AddComponent<MeshFilter>().sharedMesh = mesh;
                source.AddComponent<MeshRenderer>().sharedMaterial = material;
                foreach (float yaw in new[] { 0f, 63f, 181f })
                {
                    CheckReplacement(prefab, source, map, overrides, forest, yaw);
                    cases++;
                }
                // SpawnShrines aligns native +Z with the sampled floor normal
                // and rolls around that axis. Exercise all actual city grades,
                // both slope directions and the placement system's 44° limit.
                var normals = new[]
                {
                    new Vector3(-8f, 28f, 0).normalized,
                    new Vector3(0, 30f, 9f).normalized,
                    new Vector3(10f, 32f, 0).normalized,
                    Quaternion.AngleAxis(44f, Vector3.right) * Vector3.up,
                    Quaternion.AngleAxis(-44f, Vector3.forward) * Vector3.up,
                };
                foreach (var normal in normals)
                foreach (float roll in new[] { 19f, 137f })
                {
                    CheckReplacement(prefab, source, map, overrides, forest, roll, normal);
                    cases++;
                }
            }
            string realPath = Environment.GetEnvironmentVariable("OPENBONK_MOAI_VISUAL");
            if (!string.IsNullOrEmpty(realPath))
            {
                var visual = AssetDatabase.LoadAssetAtPath<GameObject>(realPath);
                Require(visual != null, "configured real statue asset: " + realPath);
                CheckReplacement(prefab, visual, map, overrides, forest, 37f);
                cases++;
            }
            Require(nativeRenderer.enabled && nativeMesh == prefab.GetComponent<MeshFilter>().sharedMesh && nativeMesh.bounds == nativeBounds,
                "native prefab renderer and shared mesh unchanged");
            Require(originalFile.SequenceEqual(File.ReadAllBytes(ShrinePath)) && EditorUtility.IsDirty(prefab) == originalDirty,
                "native prefab bytes and dirty state unchanged");
            Debug.Log($"OpenBonk Moai visual PASS: {cases} native-prefab fits including city ramps/44-degree limit, imported +Y aligned to native +Z/floor normal, local base aligned, collider references/bounds and icons preserved, Forest/undeclared stage unchanged, duplicate replacement rejected, source/prefab unchanged");
        }
        finally
        {
            SetMap(savedMap, savedStage);
            foreach (var source in sources) Object.DestroyImmediate(source);
            foreach (var mesh in meshes) Object.DestroyImmediate(mesh);
            Object.DestroyImmediate(material); Object.DestroyImmediate(map);
        }
    }

    private static void CheckReplacement(GameObject prefab, GameObject source, OpenBonkExternalMapData map,
        IDictionary overrides, MapData forest, float yaw, Vector3? floorNormal = null)
    {
        var instance = Object.Instantiate(prefab);
        try
        {
            instance.transform.position = new Vector3(12, 7, -9);
            if (floorNormal.HasValue)
            {
                instance.transform.rotation = Quaternion.LookRotation(floorNormal.Value);
                instance.transform.Rotate(instance.transform.forward, yaw, Space.World);
            }
            else instance.transform.rotation = Quaternion.AngleAxis(yaw, Vector3.up) * prefab.transform.rotation;
            instance.transform.localScale = new Vector3(1.4f, .8f, 1.2f);
            var shrine = instance.GetComponent<InteractableShrineMoai>();
            var original = instance.GetComponent<MeshRenderer>();
            Physics.SyncTransforms();
            var envelope = original.bounds;
            var colliders = instance.GetComponentsInChildren<Collider>(true);
            Require(colliders.Length > 0 && shrine.minimapIcon != null && shrine.alertIcon != null, "native collider and icon fixtures");
            var colliderBounds = colliders.Select(c => c.bounds).ToArray();
            var colliderJson = colliders.Select(c => EditorJsonUtility.ToJson(c)).ToArray();
            var transforms = instance.GetComponentsInChildren<Transform>(true);
            var positions = transforms.Select(t => t.localPosition).ToArray();
            var rotations = transforms.Select(t => t.localRotation).ToArray();
            var scales = transforms.Select(t => t.localScale).ToArray();
            var active = transforms.Select(t => t.gameObject.activeSelf).ToArray();
            var minimap = shrine.minimapIcon; var alert = shrine.alertIcon;
            var icons = new[] { minimap.GetComponent<Renderer>(), alert.GetComponent<Renderer>() };
            var iconEnabled = icons.Select(r => r.enabled).ToArray();
            string sourceJson = EditorJsonUtility.ToJson(source.transform);
            var sourceMesh = source.GetComponent<MeshFilter>().sharedMesh;
            var sourceBounds = sourceMesh.bounds;
            int sourceChildren = source.transform.childCount;

            SetMap(forest, forest.stages[0]);
            StartShrine.Invoke(shrine, null);
            Require(original.enabled && !instance.GetComponentsInChildren<Transform>(true).Any(t => t.name == "ExternalMoaiVisual"), "Forest must keep its original head");
            overrides.Clear(); overrides.Add(forest.stages[1], source);
            SetMap(map, forest.stages[0]);
            StartShrine.Invoke(shrine, null);
            Require(original.enabled && instance.GetComponentsInChildren<Transform>(true).Length == transforms.Length, "undeclared stage must not replace head");
            overrides.Add(forest.stages[0], source);
            string candidates = string.Join(" | ", instance.GetComponentsInChildren<MeshFilter>().Select(filter =>
            {
                var candidateRenderer = filter.GetComponent<MeshRenderer>();
                var worldSize = candidateRenderer != null ? candidateRenderer.bounds.size : Vector3.zero;
                var meshSize = filter.sharedMesh != null ? filter.sharedMesh.bounds.size : Vector3.zero;
                return $"{filter.name}: enabled={candidateRenderer != null && candidateRenderer.enabled}, " +
                    $"worldVolume={worldSize.x*worldSize.y*worldSize.z:F6}, localVolume={meshSize.x*meshSize.y*meshSize.z:F6}, " +
                    $"worldSize={worldSize.ToString("F4")}, localSize={meshSize.ToString("F4")}";
            }));
            StartShrine.Invoke(shrine, null);
            var visual = instance.GetComponentsInChildren<Transform>(true).Single(t => t.name == "ExternalMoaiVisual");
            var renderer = visual.GetComponent<MeshRenderer>();
            Require(renderer != null && renderer.enabled && visual.gameObject.activeInHierarchy && !original.enabled,
                $"external visual shown and native head hidden; sourceSize={sourceBounds.size.ToString("F4")}, " +
                $"normal={(floorNormal.HasValue ? floorNormal.Value.ToString("F5") : "flat")}, roll={yaw}, " +
                $"selectedParent={visual.parent.name}, headEnabled={original.enabled}; candidates: {candidates}");
            Require(Vector3.Dot(visual.up, instance.transform.forward) > .9999f,
                "imported +Y must align with native +Z, including ground-normal placement on ramps");
            if (floorNormal.HasValue)
                Require(Vector3.Dot(visual.up, floorNormal.Value) > .9999f && Vector3.Angle(Vector3.up, visual.up) <= 44.01f,
                    "statue follows the accepted floor normal without extra tilt");
            else Require(Vector3.Dot(visual.up, Vector3.up) > .9999f, "flat-ground statue remains world-up");
            Require(Vector3.Distance(renderer.bounds.min, renderer.bounds.max) > .1f, "nondegenerate visual");
            var tolerance = envelope; tolerance.Expand(.004f);
            Require(tolerance.Contains(renderer.bounds.min) && tolerance.Contains(renderer.bounds.max), "world bounds fit native envelope");
            // On a slope the smallest world-Y AABB corner is not the shrine's
            // local floor. Verify the native +Z base plane in its own frame.
            var sourceBoundsForBase = source.GetComponent<MeshFilter>().sharedMesh.bounds;
            var visualInNative = new Bounds();
            bool firstCorner = true;
            foreach (float x in new[] { sourceBoundsForBase.min.x, sourceBoundsForBase.max.x })
            foreach (float y in new[] { sourceBoundsForBase.min.y, sourceBoundsForBase.max.y })
            foreach (float z in new[] { sourceBoundsForBase.min.z, sourceBoundsForBase.max.z })
            {
                Vector3 corner = instance.transform.InverseTransformPoint(visual.TransformPoint(new Vector3(x,y,z)));
                if (firstCorner) { visualInNative = new Bounds(corner, Vector3.zero); firstCorner = false; }
                else visualInNative.Encapsulate(corner);
            }
            var nativeFloor = instance.GetComponent<MeshFilter>().sharedMesh.bounds;
            Require(Mathf.Abs(visualInNative.min.z-nativeFloor.min.z) < .002f,
                "statue base remains aligned with the native local floor plane");
            if (!floorNormal.HasValue)
                Require(Mathf.Abs(renderer.bounds.min.y-envelope.min.y) < .002f, "flat-ground statue rests at native world floor height");
            Require(visual.gameObject.layer == original.gameObject.layer, "native rendering layer retained");
            Require(visual.GetComponent<MeshFilter>().sharedMesh == sourceMesh, "shared statue mesh retained");
            int children = instance.GetComponentsInChildren<Transform>(true).Length;
            Require(Replace.Invoke(shrine, new object[] { source }) == null && instance.GetComponentsInChildren<Transform>(true).Length == children,
                "second replacement cannot duplicate visual");
            Physics.SyncTransforms();
            var afterColliders = instance.GetComponentsInChildren<Collider>(true);
            Require(colliders.SequenceEqual(afterColliders), "same native collider objects and no added blockers");
            for (int i = 0; i < colliders.Length; i++)
                Require(colliders[i].bounds == colliderBounds[i] && EditorJsonUtility.ToJson(colliders[i]) == colliderJson[i], "native collider bounds/properties unchanged");
            for (int i = 0; i < transforms.Length; i++)
                Require(transforms[i].localPosition == positions[i] && transforms[i].localRotation == rotations[i] &&
                    transforms[i].localScale == scales[i] && transforms[i].gameObject.activeSelf == active[i], "native transforms, scale and activation preserved");
            Require(shrine.minimapIcon == minimap && shrine.alertIcon == alert && !shrine.done, "icons and interaction state preserved");
            for (int i = 0; i < icons.Length; i++) Require(icons[i].enabled == iconEnabled[i], "icon renderer unchanged");
            Require(EditorJsonUtility.ToJson(source.transform) == sourceJson && source.transform.childCount == sourceChildren && sourceMesh.bounds == sourceBounds,
                "source visual unchanged");
        }
        finally { Object.DestroyImmediate(instance); }
    }

    private static void SetMap(MapData map, StageData stage)
    {
        typeof(MapController).GetProperty("currentMap").GetSetMethod(true).Invoke(null, new object[] { map });
        typeof(MapController).GetProperty("currentStage").GetSetMethod(true).Invoke(null, new object[] { stage });
    }

    private static Mesh Box(Bounds bounds)
    {
        var min = bounds.min; var max = bounds.max;
        var mesh = new Mesh { name = "Off-center +Y-up Moai test mesh" };
        mesh.vertices = new[] {
            new Vector3(min.x,min.y,min.z), new Vector3(max.x,min.y,min.z), new Vector3(max.x,max.y,min.z), new Vector3(min.x,max.y,min.z),
            new Vector3(min.x,min.y,max.z), new Vector3(max.x,min.y,max.z), new Vector3(max.x,max.y,max.z), new Vector3(min.x,max.y,max.z) };
        mesh.triangles = new[] { 0,2,1,0,3,2, 4,5,6,4,6,7, 0,1,5,0,5,4, 3,7,6,3,6,2, 0,4,7,0,7,3, 1,2,6,1,6,5 };
        mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
    }

    private static void Require(bool value, string message)
    {
        if (!value) throw new Exception("OpenBonk Moai visual check: " + message);
    }
}
#endif
