using System;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

[InitializeOnLoad]
public static class GrassBatchedParityCheck
{
    const string Pending = "GrassBatchedParityCheck.pending";
    static GrassBatchedParityCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("Grass parity fixture").AddComponent<GrassBatchedParityRunner>().Begin(); } }
}

public class GrassBatchedParityRunner : MonoBehaviour
{
    static void Require(bool value, string message) { if (!value) throw new Exception("GrassBatchedParityCheck: " + message); }
    public void Begin() => StartCoroutine(Guarded());
    IEnumerator Guarded()
    {
        var test = Run();
        while (true)
        {
            bool more;
            try { more = test.MoveNext(); }
            catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); yield break; }
            if (!more) yield break;
            yield return test.Current;
        }
    }

    IEnumerator Run()
    {
        yield return null;
        Require(SystemInfo.supportsComputeShaders && SystemInfo.supportsInstancing, "GPU lacks compute or instancing");
        var source = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Mesh/GrassQuads.asset");
        var template = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/GrassMaterialForest1.mat");
        var batchedShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Resources/GrassBatched.shader");
        Require(source != null && source.isReadable && template != null && template.shader.isSupported && batchedShader != null && batchedShader.isSupported, "real mesh and supported shaders");
        var matrices = new[] {
            Matrix4x4.TRS(new Vector3(-2, 0, 1), Quaternion.Euler(0, 25, 0), Vector3.one),
            Matrix4x4.TRS(new Vector3(.5f, .4f, 0), Quaternion.Euler(0, 83, 0), Vector3.one),
            Matrix4x4.TRS(new Vector3(2.5f, -.3f, -1), Quaternion.Euler(0, 143, 0), Vector3.one)
        };
        var vertices = source.vertices; var uv = source.uv; var indices = source.GetTriangles(0);
        var baked = new Vector3[vertices.Length * matrices.Length]; var bakedUv = new Vector2[baked.Length]; var height = new Vector2[baked.Length];
        var bakedIndices = new int[indices.Length * matrices.Length];
        for (int i = 0; i < matrices.Length; i++)
        {
            for (int v = 0; v < vertices.Length; v++) { int k = i * vertices.Length + v; baked[k] = matrices[i].MultiplyPoint3x4(vertices[v]); bakedUv[k] = uv[v]; height[k] = new Vector2(vertices[v].y, 0); }
            for (int t = 0; t < indices.Length; t++) bakedIndices[i * indices.Length + t] = i * vertices.Length + indices[t];
        }
        var batchMesh = new Mesh { name = "Grass parity batch", vertices = baked, uv = bakedUv, uv2 = height, triangles = bakedIndices }; batchMesh.RecalculateBounds();
        var original = new Material(template) { enableInstancing = true };
        var batched = new Material(template) { shader = batchedShader, enableInstancing = false };
        var matrixBuffer = new ComputeBuffer(matrices.Length, 64); matrixBuffer.SetData(matrices); original.SetBuffer("_GrassBuffer", matrixBuffer);
        var args = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);
        args.SetData(new uint[] { source.GetIndexCount(0), (uint)matrices.Length, source.GetIndexStart(0), (uint)source.GetBaseVertex(0), 0 });
        var cameraObject = new GameObject("Grass parity camera"); var camera = cameraObject.AddComponent<Camera>();
        camera.enabled = false; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.magenta; camera.orthographic = true; camera.orthographicSize = 5;
        camera.transform.position = new Vector3(0, 5, -12); camera.transform.LookAt(new Vector3(0, 0, 0)); camera.nearClipPlane = .1f; camera.farClipPlane = 80;
        var target = new RenderTexture(256, 256, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 }; camera.targetTexture = target;
        var pixels = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        bool savedFog = RenderSettings.fog; var savedMode = RenderSettings.fogMode; var savedColor = RenderSettings.fogColor; float savedDensity = RenderSettings.fogDensity;
        bool savedLinearKeyword = Shader.IsKeywordEnabled("FOG_LINEAR"), savedExpKeyword = Shader.IsKeywordEnabled("FOG_EXP"), savedExp2Keyword = Shader.IsKeywordEnabled("FOG_EXP2");
        try
        {
            Color32[] noWind = null;
            foreach (float wind in new[] { 0f, .75f })
            {
                bool fog = false;
                original.SetFloat("_WindStrength", wind); batched.SetFloat("_WindStrength", wind);
                original.SetVector("_PlayerPos", Vector4.zero); batched.SetVector("_PlayerPos", Vector4.zero);
                original.SetFloat("_RenderDistance", 100); batched.SetFloat("_RenderDistance", 100);
                RenderSettings.fog = fog; RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogColor = new Color(.12f, .23f, .33f); RenderSettings.fogDensity = .045f;
                Shader.DisableKeyword("FOG_LINEAR"); Shader.DisableKeyword("FOG_EXP");
                if (fog) Shader.EnableKeyword("FOG_EXP2"); else Shader.DisableKeyword("FOG_EXP2");
                yield return null;
                var drawOriginal = new CommandBuffer { name = "original indirect grass" };
                drawOriginal.DrawMeshInstancedIndirect(source, 0, original, 0, args);
                Color32[] first = Render(camera, target, pixels, drawOriginal); drawOriginal.Release();
                var drawBatched = new CommandBuffer { name = "batched grass" };
                drawBatched.DrawMesh(batchMesh, Matrix4x4.identity, batched, 0, 0);
                Color32[] second = Render(camera, target, pixels, drawBatched); drawBatched.Release();
                int visible = 0, differing = 0, max = 0; long sum = 0;
                for (int i = 0; i < first.Length; i++)
                {
                    if (first[i].g > first[i].r && first[i].g > first[i].b) visible++;
                    int diff = Math.Max(Math.Abs(first[i].r - second[i].r), Math.Max(Math.Abs(first[i].g - second[i].g), Math.Abs(first[i].b - second[i].b)));
                    if (diff > 4) differing++; if (diff > max) max = diff; sum += diff;
                }
                Require(visible > 50, "original grass did not produce visible green pixels");
                Require(differing < first.Length / 100 && sum < first.Length * 2L, $"image mismatch wind={wind} fog={fog} differing={differing} max={max} sum={sum}");
                if (!fog)
                {
                    if (wind == 0f) noWind = first;
                    else Require(ChangedPixels(noWind, first) > 10, "wind did not change the control image");
                }
                Debug.Log($"GrassBatchedParityCheck: wind={wind} fog={fog} visible={visible} differentOver4={differing} max={max} mean={(double)sum / first.Length:F4}");
            }
            Debug.Log("GrassBatchedParityCheck passed: original material/texture, three transforms, alpha, gradient and wind; fog unverified");
            EditorApplication.Exit(0);
        }
        finally
        {
            RenderSettings.fog = savedFog; RenderSettings.fogMode = savedMode; RenderSettings.fogColor = savedColor; RenderSettings.fogDensity = savedDensity;
            if (savedLinearKeyword) Shader.EnableKeyword("FOG_LINEAR"); else Shader.DisableKeyword("FOG_LINEAR");
            if (savedExpKeyword) Shader.EnableKeyword("FOG_EXP"); else Shader.DisableKeyword("FOG_EXP");
            if (savedExp2Keyword) Shader.EnableKeyword("FOG_EXP2"); else Shader.DisableKeyword("FOG_EXP2");
            camera.targetTexture = null; Destroy(cameraObject); target.Release(); Destroy(target); Destroy(pixels); args.Release(); matrixBuffer.Release(); Destroy(original); Destroy(batched); Destroy(batchMesh);
        }
    }

    static int ChangedPixels(Color32[] a, Color32[] b)
    {
        int count = 0;
        for (int i = 0; i < a.Length; i++) if (Math.Abs(a[i].r - b[i].r) + Math.Abs(a[i].g - b[i].g) + Math.Abs(a[i].b - b[i].b) > 6) count++;
        return count;
    }

    static Color32[] Render(Camera camera, RenderTexture target, Texture2D texture, CommandBuffer draw)
    {
        camera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, draw);
        camera.Render();
        camera.RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque, draw);
        var old = RenderTexture.active; RenderTexture.active = target; texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); texture.Apply(); RenderTexture.active = old;
        return texture.GetPixels32();
    }
}
