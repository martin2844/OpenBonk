using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public static class GrassSamplingPortCheck
{
    const BindingFlags P = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Check(bool ok, string message) { if (!ok) throw new Exception("GrassSamplingPortCheck: " + message); }
    static object Call(GrassChunkManager g, string name, params object[] args) => typeof(GrassChunkManager).GetMethod(name,P).Invoke(g,args);
    static T Get<T>(GrassChunkManager g, string field) => (T)typeof(GrassChunkManager).GetField(field,P).GetValue(g);
    public static void Run()
    {
        var state = new GameObject("grass sample state"); state.SetActive(false); var ground = new GameObject("ground"); ground.layer = LayerMask.NameToLayer("Ground");
        var collider = ground.AddComponent<BoxCollider>(); collider.size = new Vector3(200, 1, 200); collider.center = new Vector3(0,-.5f,0);
        var obstacle = new GameObject("object"); obstacle.layer = LayerMask.NameToLayer("Object"); var box = obstacle.AddComponent<BoxCollider>(); box.size = new Vector3(3,1,3); obstacle.transform.position = new Vector3(4,3,4); box.enabled = false;
        try
        {
            var g = state.AddComponent<GrassChunkManager>(); Check(g.grassPerChunk == 500 && g.chunkSize == 16 && g.renderDistance == 3 && g.yThreshold == 7f,"original defaults");
            g.Set(null,100); g.chunkSize = 10; g.renderDistance = -1; g.yThreshold = -1; g.layerMask = (1 << ground.layer) | (1 << obstacle.layer); Physics.SyncTransforms();
            object[] args = { 4f,4f,0f }; Check((bool)Call(g,"GetTerrainHeight",args) && Mathf.Abs((float)args[2]) < .001f,"flat ground sampled");
            box.enabled = true; Physics.SyncTransforms(); args = new object[] { 4f,4f,0f }; Check(!(bool)Call(g,"GetTerrainHeight",args),"object above ground blocks placement");
            box.enabled = false; g.yThreshold = 1; args = new object[] {4f,4f,0f}; Check(!(bool)Call(g,"GetTerrainHeight",args),"height threshold"); g.yThreshold = -1;
            ground.transform.rotation = Quaternion.Euler(40,0,0); Physics.SyncTransforms(); args = new object[] { 0f,0f,0f }; Check(!(bool)Call(g,"GetTerrainHeight",args),"steep ground rejected");
            ground.transform.rotation = Quaternion.identity; Physics.SyncTransforms();
            g.transform.position = new Vector3(-.1f,0,10.1f); Check((Vector2Int)Call(g,"GetPlayerChunk") == new Vector2Int(-1,1),"negative coordinates floor to chunk");
            Call(g,"UpdateGrassQuality",0); Check(Get<int>(g,"currentGrassPerChunk") == 5,"low density");
            Call(g,"UpdateGrassQuality",1); Check(Get<int>(g,"currentGrassPerChunk") == 35,"medium density");
            Call(g,"UpdateGrassQuality",2); Check(Get<int>(g,"currentGrassPerChunk") == 100,"high density");
            UnityEngine.Random.InitState(3); Call(g,"GenerateGrassForChunk",new Vector2Int(0,0)); var dictionary = (IDictionary)Get<object>(g,"precomputedGrassPositions");
            Check(dictionary.Count == 1 && ((IList)dictionary[new Vector2Int(0,0)]).Count == 100,"chunk samples valid ground");
            Call(g,"UpdateGrassQuality",0); Check(dictionary.Count == 0,"quality update invalidates chunk cache");
            Debug.Log("GrassSamplingPortCheck passed: original density, ground filtering, chunk coordinates and sampled positions");
        }
        finally { UnityEngine.Object.DestroyImmediate(state); UnityEngine.Object.DestroyImmediate(ground); UnityEngine.Object.DestroyImmediate(obstacle); }
    }
}
