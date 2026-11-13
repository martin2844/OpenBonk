#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using UnityEngine;

// Disposable runtime fixture for unreadable multi-submesh MeshRenderer models.
internal sealed class AndroidExternalStaticOutlineProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.splittest" || !intent.Call<bool>("getBooleanExtra", "openbonk_static_outline_probe", false)) return;
        var go = new GameObject("Static external outline probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidExternalStaticOutlineProbe>();
    }
    private IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(10f);
        var temporarySphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var mesh = Instantiate(temporarySphere.GetComponent<MeshFilter>().sharedMesh); Destroy(temporarySphere);
        var triangles = mesh.triangles; int split = triangles.Length / 6 * 3;
        var first = new int[split]; var second = new int[triangles.Length - split];
        System.Array.Copy(triangles, 0, first, 0, first.Length); System.Array.Copy(triangles, split, second, 0, second.Length);
        mesh.subMeshCount = 2; mesh.SetTriangles(first, 0); mesh.SetTriangles(second, 1); mesh.UploadMeshData(true);
        Material template = null;
        foreach (var candidate in Resources.FindObjectsOfTypeAll<Material>())
            if (candidate != null && candidate.shader != null && candidate.shader.isSupported && candidate.renderQueue < 2500 &&
                (candidate.HasProperty("_Color") || candidate.HasProperty("_BaseColor"))) { template = candidate; break; }
        if (template == null) { Debug.LogError("Static outline probe INCONCLUSIVE: no loaded opaque material"); Destroy(mesh); Destroy(gameObject); yield break; }
        var material = new Material(template);
        if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
        var obj = new GameObject("Static test mesh"); obj.layer = 30;
        obj.AddComponent<MeshFilter>().sharedMesh = mesh; var renderer = obj.AddComponent<MeshRenderer>(); renderer.sharedMaterials = new[] { material, material };
        var cameraObject = new GameObject("Static outline test camera"); var camera = cameraObject.AddComponent<Camera>();
        camera.enabled = false; camera.cullingMask = 1 << 30; camera.transform.position = new Vector3(0, 0, -5); camera.orthographic = true; camera.orthographicSize = 1.5f;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
        var target = new RenderTexture(256, 256, 24, RenderTextureFormat.ARGB32); var read = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        camera.targetTexture = target;
        var outline = obj.AddComponent<Outline>(); outline.ConfigureExternalSubmeshes(); outline.OutlineColor = Color.cyan; outline.OutlineWidth = 6; outline.OutlineMode = Outline.Mode.OutlineVisible;
        yield return null;
        var mask = obj.transform.Find("OpenBonkOutlineMask"); var fill = obj.transform.Find("OpenBonkOutlineFill");
        bool proxies = mask != null && fill != null && mask.GetComponent<MeshRenderer>() != null && fill.GetComponent<MeshRenderer>() != null && mask.GetComponent<MeshRenderer>().sharedMaterials.Length == 2 && fill.GetComponent<MeshRenderer>().sharedMaterials.Length == 2;
        camera.Render(); RenderTexture.active = target; read.ReadPixels(new Rect(0, 0, 256, 256), 0, 0); read.Apply();
        int upper = 0, lower = 0;
        var pixels = read.GetPixels32();
        for (int y = 0; y < 256; y++) for (int x = 0; x < 256; x++)
        { var p = pixels[y * 256 + x]; if (p.g > 160 && p.b > 160 && p.r < 80) { if (y >= 128) upper++; else lower++; } }
        outline.enabled = false; yield return null;
        camera.Render(); read.ReadPixels(new Rect(0, 0, 256, 256), 0, 0); read.Apply();
        int cyanDisabled = 0; foreach (var p in read.GetPixels32()) if (p.g > 160 && p.b > 160 && p.r < 80) cyanDisabled++;
        bool hidden = proxies && !mask.gameObject.activeSelf && !fill.gameObject.activeSelf && cyanDisabled == 0;
        outline.enabled = true; yield return null; renderer.enabled = false; yield return null;
        bool followsRenderer = proxies && !mask.GetComponent<Renderer>().enabled && !fill.GetComponent<Renderer>().enabled;
        var late = new GameObject("Initially inactive static mesh"); late.SetActive(false); late.layer = 30;
        late.AddComponent<MeshFilter>().sharedMesh = mesh; late.AddComponent<MeshRenderer>().sharedMaterials = new[] { material, material };
        var lateOutline = late.AddComponent<Outline>(); lateOutline.ConfigureExternalSubmeshes(); late.SetActive(true); yield return null;
        bool activated = late.transform.Find("OpenBonkOutlineMask") != null && late.transform.Find("OpenBonkOutlineFill") != null;
        bool pass = !mesh.isReadable && proxies && upper > 5 && lower > 5 && hidden && followsRenderer && activated;
        Debug.Log($"Static outline probe {(pass ? "PASS" : "FAIL")}: unreadable={!mesh.isReadable} proxies={proxies} cyanUpper={upper} cyanLower={lower} hidden={hidden} sourceDisabled={followsRenderer} inactiveActivated={activated}");
        RenderTexture.active = null; camera.targetTexture = null; Destroy(late); Destroy(obj); Destroy(cameraObject); Destroy(read); Destroy(target); Destroy(material); Destroy(mesh); Destroy(gameObject);
    }
}
#endif
