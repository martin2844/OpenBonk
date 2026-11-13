#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

// An isolated, explicitly enabled pixel check for the CPU-batched grass path.
internal sealed class AndroidGrassBatchProbe : MonoBehaviour
{
    private static bool qualityProbe;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_grass_batch_probe", false)) return;
        qualityProbe = intent.Call<bool>("getBooleanExtra", "megabonk_grass_quality_probe", false);
        var go = new GameObject("Grass batch pixel probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidGrassBatchProbe>();
    }

    private IEnumerator Start()
    {
        GrassChunkManager grass = null;
        float end = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < end)
        {
            foreach (var candidate in FindObjectsByType<GrassChunkManager>(FindObjectsSortMode.None))
                if (candidate.isActiveAndEnabled && candidate.grassMesh != null && candidate.grassMaterial != null && PlayerCamera.Instance != null) { grass = candidate; break; }
            if (grass != null) break;
            yield return null;
        }
        if (grass == null) { Debug.LogError("Grass batch probe: manager timeout"); yield break; }
        var shader = Resources.Load<Shader>("GrassBatched");
        if (shader == null) { Debug.LogError("Grass batch probe: missing shader"); yield break; }
        var material = new Material(grass.grassMaterial) { shader = shader, enableInstancing = false };
        material.SetFloat("_GrassDebug", 1); material.SetVector("_PlayerPos", new Vector4(0, 1000, 0, 0)); material.SetFloat("_RenderDistance", 100);
        var cameraGo = new GameObject("Grass batch probe camera"); cameraGo.transform.position = new Vector3(0, 1000, 0);
        var camera = cameraGo.AddComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
        camera.cullingMask = 1 << 31; camera.nearClipPlane = .1f; camera.farClipPlane = 30; camera.depth = 100;
        var target = new RenderTexture(256, 256, 24, RenderTextureFormat.ARGB32); camera.targetTexture = target;
        var readback = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        var white = new Texture2D(2, 2); white.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white }); white.Apply();
        var source = grass.grassMesh;
        Debug.Log($"Grass batch probe: mesh={source.name}, readable={source.isReadable}, vertexCount={source.vertexCount}");
        if (!source.isReadable) { Debug.LogError("Grass batch probe: source mesh is not readable in this build"); yield break; }
        var vertices = source.vertices; var uv = source.uv; var triangles = source.triangles;
        var combinedVertices = new Vector3[vertices.Length * 3]; var combinedUv = new Vector2[combinedVertices.Length]; var heights = new Vector2[combinedVertices.Length];
        var combinedTriangles = new int[triangles.Length * 3];
        for (int i = 0; i < 3; i++)
        {
            Matrix4x4 matrix = Matrix4x4.TRS(new Vector3((i - 1) * 2, 999, 5), Quaternion.Euler(0, 30 * i, 0), Vector3.one * 2);
            for (int j = 0; j < vertices.Length; j++) { int k = i * vertices.Length + j; combinedVertices[k] = matrix.MultiplyPoint3x4(vertices[j]); combinedUv[k] = uv[j]; heights[k] = new Vector2(vertices[j].y, 0); }
            for (int j = 0; j < triangles.Length; j++) combinedTriangles[i * triangles.Length + j] = triangles[j] + i * vertices.Length;
        }
        var mesh = new Mesh { name = "Grass batch probe mesh", vertices = combinedVertices, uv = combinedUv, uv2 = heights, triangles = combinedTriangles }; mesh.RecalculateBounds();
        var original = material.mainTexture; bool fog = RenderSettings.fog; RenderSettings.fog = false;
        for (int pass = 0; pass < 2; pass++)
        {
            material.mainTexture = pass == 0 ? original : white;
            for (int frame = 0; frame < 3; frame++)
            {
                Graphics.DrawMesh(mesh, Matrix4x4.identity, material, 31, camera, 0, null, ShadowCastingMode.Off, false);
                yield return new WaitForEndOfFrame();
            }
            var old = RenderTexture.active; RenderTexture.active = target; readback.ReadPixels(new Rect(0, 0, 256, 256), 0, 0); readback.Apply(); RenderTexture.active = old;
            int magenta = 0; foreach (Color32 c in readback.GetPixels32()) if (c.r > 180 && c.b > 180 && c.g < 80) magenta++;
            Debug.Log($"Grass batch probe: texture={(pass == 0 ? "original" : "white")}, magentaPixels={magenta}, vertices={mesh.vertexCount}, shaderSupported={shader.isSupported}");
        }
        RenderSettings.fog = fog; camera.targetTexture = null; Destroy(cameraGo); target.Release(); Destroy(target); Destroy(readback); Destroy(white); Destroy(mesh); Destroy(material);
        if (!qualityProbe) yield break;

        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var type = typeof(GrassChunkManager);
        var setQuality = type.GetMethod("UpdateGrassQuality", flags);
        var rebuildMeshes = type.GetMethod("RebuildFallbackMeshes", flags);
        var dataField = type.GetField("allGrassDataList", flags);
        var meshesField = type.GetField("fallbackMeshes", flags);
        var densityField = type.GetField("currentGrassPerChunk", flags);
        var settings = SaveManager.Instance != null && SaveManager.Instance.config != null ? SaveManager.Instance.config.cfVideoSettings : null;
        int savedQuality = settings != null ? settings.grass_quality : 1;
        foreach (int quality in new[] { 0, 1, 2 })
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();
            setQuality.Invoke(grass, new object[] { quality });
            timer.Stop(); double sampleMilliseconds = timer.Elapsed.TotalMilliseconds;
            timer.Restart(); rebuildMeshes.Invoke(grass, null); timer.Stop(); double meshMilliseconds = timer.Elapsed.TotalMilliseconds;
            yield return null;
            yield return null;
            var records = (ICollection)dataField.GetValue(grass);
            var batches = (IList)meshesField.GetValue(grass);
            int vertexCount = 0; foreach (Mesh batch in batches) if (batch != null) vertexCount += batch.vertexCount;
            bool countsMatch = records.Count * source.vertexCount == vertexCount && (records.Count == 0 || batches.Count > 0);
            Debug.Log($"Grass quality probe {(countsMatch ? "PASS" : "FAIL")}: quality={quality}, perChunk={densityField.GetValue(grass)}, instances={records.Count}, batches={batches.Count}, vertices={vertexCount}, sampleMs={sampleMilliseconds:F1}, meshMs={meshMilliseconds:F1}");
        }
        setQuality.Invoke(grass, new object[] { savedQuality });
        Debug.Log($"Grass quality probe: restored={savedQuality}, settingsNotWritten=true");
    }
}
#endif
