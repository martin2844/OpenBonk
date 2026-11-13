using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class GrassRenderPlayCheck
{
    const string Pending = "GrassRenderPlayCheck.pending";
    static GrassRenderPlayCheck() { EditorApplication.playModeStateChanged += State; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single); SessionState.SetBool(Pending,true); EditorApplication.isPlaying = true; }
    static void State(PlayModeStateChange s) { if (s == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false)) { SessionState.SetBool(Pending,false); new GameObject("grass check").AddComponent<GrassRenderCheckRunner>().Begin(); } }
}
public class GrassRenderCheckRunner : MonoBehaviour
{
    const BindingFlags P = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Check(bool ok,string message) { if (!ok) throw new Exception("GrassRenderPlayCheck: " + message); }
    static object Call(GrassChunkManager g,string name,params object[] args) => typeof(GrassChunkManager).GetMethod(name,P).Invoke(g,args);
    static T Get<T>(GrassChunkManager g,string name) => (T)typeof(GrassChunkManager).GetField(name,P).GetValue(g);
    public void Begin() => StartCoroutine(Guarded());
    IEnumerator Guarded()
    {
        var test = Run();
        while (true)
        {
            bool more;
            try { more = test.MoveNext(); }
            catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); yield break; }
            if (!more) yield break;
            yield return test.Current;
        }
    }
    IEnumerator Run()
    {
        var root = new GameObject("fixture root"); var camObj = new GameObject("fixture camera");
        RenderTexture rt = null; Texture2D texture = null; Material mat = null;
        try
        {
            Check(SystemInfo.supportsInstancing && SystemInfo.supportsComputeShaders,"GPU supports indirect instancing");
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Mesh/GrassQuads.asset"); var source = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/GrassMaterialForest1.mat");
            Check(mesh != null && source != null && source.shader.isSupported,"original grass assets and supported shader"); mat = new Material(source); mat.enableInstancing = true; mat.SetFloat("_WindStrength",0);
            var ground = new GameObject("ground"); ground.transform.SetParent(root.transform); ground.layer = LayerMask.NameToLayer("Ground"); var box = ground.AddComponent<BoxCollider>(); box.size = new Vector3(200,1,200); box.center = new Vector3(0,-.5f,0);
            var go = new GameObject("grass"); go.SetActive(false); go.transform.SetParent(root.transform); go.transform.position = new Vector3(5,0,5);
            var g = go.AddComponent<GrassChunkManager>(); g.grassMesh = mesh; g.grassMaterial = mat; g.grassPerChunk = 100; g.chunkSize = 10; g.renderDistance = 1; g.yThreshold = -1; g.layerMask = 1 << ground.layer; g.testWithoutPlayer = true;
            var camera = camObj.AddComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.magenta; camera.orthographic = true; camera.orthographicSize = 7; camera.transform.position = new Vector3(5,8,-8); camera.transform.LookAt(new Vector3(5,0,5));
            rt = new RenderTexture(256,256,24,RenderTextureFormat.ARGB32); camera.targetTexture = rt; go.SetActive(true); Physics.SyncTransforms(); Call(g,"UpdateGrassQuality",2);
            var chunks = (IDictionary)Get<object>(g,"precomputedGrassPositions"); var data = (IList)Get<object>(g,"allGrassDataList"); var args = Get<uint[]>(g,"args");
            Check(chunks.Count == 9 && data.Count == 900 && args[1] == 900, "generated nine chunks and 900 indirect instances");
            Check(Get<ComputeBuffer>(g,"allGrassBuffer") != null && Get<ComputeBuffer>(g,"argsBuffer") != null,"GPU buffers built");
            yield return null; yield return null; camera.Render();
            RenderTexture.active = rt; texture = new Texture2D(256,256,TextureFormat.RGBA32,false); texture.ReadPixels(new Rect(0,0,256,256),0,0); texture.Apply(); RenderTexture.active = null;
            int visible = 0; foreach (var pixel in texture.GetPixels32()) if (pixel.g > 15 && pixel.g > pixel.r && pixel.g > pixel.b) visible++;
            Check(visible > 20,"rendered green grass pixels, got " + visible); System.IO.File.WriteAllBytes("/home/your-user/megabonk/build/android/grass-render-check.png",texture.EncodeToPNG());
            Get<ComputeBuffer>(g,"allGrassBuffer").Release(); typeof(GrassChunkManager).GetField("allGrassBuffer",P).SetValue(g,null);
            yield return null; yield return null; camera.Render();
            RenderTexture.active = rt; texture.ReadPixels(new Rect(0,0,256,256),0,0); texture.Apply(); RenderTexture.active = null;
            int fallbackVisible = 0; foreach (var pixel in texture.GetPixels32()) if (pixel.g > 15 && pixel.g > pixel.r && pixel.g > pixel.b) fallbackVisible++;
            Check(fallbackVisible > 20 && Get<Material>(g,"fallbackMaterial") != null,"fallback renders grass pixels, got " + fallbackVisible);
            System.IO.File.WriteAllBytes("/home/your-user/megabonk/build/android/grass-fallback-check.png",texture.EncodeToPNG());
            go.transform.position = new Vector3(15,0,5); Call(g,"Update"); Check(chunks.Count == 9 && chunks.Contains(new Vector2Int(2,0)) && !chunks.Contains(new Vector2Int(-1,0)),"moving updates and evicts chunks");
            CurrentSettings.A_SettingUpdated?.Invoke("grass_quality",2,0); Check(Get<int>(g,"currentGrassPerChunk") == 5 && data.Count == 45 && args[1] == 45,"quality event rebuilds instance buffer");
            Debug.Log("GrassRenderPlayCheck passed: original mesh/material, nine chunks, indirect grass pixels=" + visible + ", movement and quality rebuild"); EditorApplication.Exit(0);
        }
        finally { RenderTexture.active = null; if (texture != null) Destroy(texture); if (rt != null) { rt.Release(); Destroy(rt); } if (mat != null) Destroy(mat); Destroy(root); Destroy(camObj); }
    }
}
