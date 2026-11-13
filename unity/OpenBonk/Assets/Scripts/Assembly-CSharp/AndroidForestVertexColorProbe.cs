#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Assets.Scripts.MapGeneration.ProceduralTiles;
using Assets.Scripts.Managers;
using UnityEngine;

internal sealed class AndroidForestVertexColorProbe : MonoBehaviour
{
    static bool installed;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (installed) return;
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_forest_vertex_color_probe", false)) return;
        installed = true;
        Debug.Log("Forest vertex color probe installed");
        var root = new GameObject("Forest vertex color probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidForestVertexColorProbe>();
    }

    IEnumerator Start()
    {
        Mesh sourceMesh = null; float deadline = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < deadline && (sourceMesh == null || MapController.currentStage == null))
        {
            foreach (var candidate in Resources.FindObjectsOfTypeAll<Mesh>())
                if (candidate.name == "TileWall") { sourceMesh = candidate; break; }
            if (sourceMesh == null || MapController.currentStage == null) yield return null;
        }
        if (sourceMesh == null || MapController.currentStage == null || MapController.currentStage.m_fillMiddleEdge == null) { Debug.LogError("Forest vertex color probe INCONCLUSIVE: wall mesh or stage material unavailable"); yield break; }
        var mesh = Instantiate(sourceMesh);
        var material = new Material(MapController.currentStage.m_fillMiddleEdge);
        var root = new GameObject("isolated Forest wall"); root.transform.position = new Vector3(0, 10000, 0);
        var visual = new GameObject("wall"); visual.layer = 30; visual.transform.SetParent(root.transform, false);
        visual.AddComponent<MeshFilter>().sharedMesh = mesh; visual.AddComponent<MeshRenderer>().sharedMaterial = material;
        var cameraObject = new GameObject("camera"); cameraObject.transform.SetParent(root.transform, false);
        cameraObject.transform.localPosition = new Vector3(0, .5f, -4f);
        var camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 1.5f;
        camera.cullingMask = 1 << 30; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
        var lightObject = new GameObject("light"); lightObject.transform.SetParent(root.transform, false);
        var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional; light.cullingMask = 1 << 30; light.intensity = 1; light.renderMode = LightRenderMode.ForcePixel;
        var target = new RenderTexture(128, 128, 24); var image = new Texture2D(128, 128, TextureFormat.RGBA32, false); camera.targetTexture = target;
        bool oldFog = RenderSettings.fog; var oldAmbient = RenderSettings.ambientLight; var oldMode = RenderSettings.ambientMode; var previous = RenderTexture.active;
        RenderSettings.fog = false; RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = Color.white;
        material.DisableKeyword("_MK_ALBEDO_MAP"); material.SetFloat("_RenderFace", 0); material.SetFloat("_Outline", 0); material.SetFloat("_Specular", 0); material.SetFloat("_Rim", 0);
        material.SetColor("_AlbedoColor", Color.white); material.SetColor("_GoochDarkColor", Color.white); material.SetColor("_GoochBrightColor", Color.white);
        yield return null;
        camera.Render(); RenderTexture.active = target; image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply(); var missing = image.GetPixel(64,64);
        var colors = new Color[mesh.vertexCount]; for (int i = 0; i < colors.Length; i++) colors[i] = Color.white; mesh.colors = colors;
        camera.Render(); image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply(); var white = image.GetPixel(64,64);
        for (int i = 0; i < colors.Length; i++) colors[i] = Color.red; mesh.colors = colors;
        camera.Render(); image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply(); var red = image.GetPixel(64,64);
        bool equal = Mathf.Abs(missing.r-white.r)<.03f && Mathf.Abs(missing.g-white.g)<.03f && Mathf.Abs(missing.b-white.b)<.03f;
        bool visible = white.r>.1f && white.g>.1f && red.r>.1f && red.g<red.r*.2f;
        bool ok = sourceMesh.colors.Length == 0 && equal && visible;
        Debug.Log($"Forest vertex color probe {(ok ? "PASS" : "FAIL")}: mesh={sourceMesh.name}, vertices={sourceMesh.vertexCount}, sourceColors={sourceMesh.colors.Length}, missing={missing}, white={white}, red={red}, shaderLevel={SystemInfo.graphicsShaderLevel}");
        RenderSettings.fog=oldFog; RenderSettings.ambientMode=oldMode; RenderSettings.ambientLight=oldAmbient; RenderTexture.active=previous;
        camera.targetTexture=null; Destroy(image); target.Release(); Destroy(target); Destroy(material); Destroy(mesh); Destroy(root);
    }
}
#endif
