#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.IO;
using System.Linq;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using UnityEngine;

// Opt-in Android GPU comparison using the real Forest Moai mesh and materials.
internal sealed class AndroidInteractableOutlineRenderProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_interactable_outline_render_probe", false)) return;
        var go = new GameObject("Interactable outline render probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidInteractableOutlineRenderProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || MapController.currentMap == null || MapController.currentMap.shrines == null)) yield return null;
        var map = MapController.currentMap;
        if (map == null || map.shrines == null) { Debug.LogError("Interactable outline render probe: map unavailable"); yield break; }
        var source = map.shrines.FirstOrDefault(x => x != null && x.GetComponent<InteractableShrineMoai>() != null);
        var originalFilter = source != null ? source.GetComponent<MeshFilter>() : null;
        var originalRenderer = source != null ? source.GetComponent<MeshRenderer>() : null;
        if (originalFilter == null || originalRenderer == null || originalFilter.sharedMesh == null) { Debug.LogError("Interactable outline render probe: original mesh unavailable"); yield break; }
        var obj = new GameObject("isolated Moai mesh"); obj.layer = 31; obj.transform.position = new Vector3(0, 1000, 0);
        obj.AddComponent<MeshFilter>().sharedMesh = originalFilter.sharedMesh;
        var renderer = obj.AddComponent<MeshRenderer>(); renderer.sharedMaterials = originalRenderer.sharedMaterials;
        var bounds = renderer.bounds; float extent = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
        var camObj = new GameObject("isolated outline camera"); var cam = camObj.AddComponent<Camera>(); cam.enabled = false; cam.orthographic = true; cam.orthographicSize = extent * 1.5f; cam.cullingMask = 1 << 31; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black; cam.nearClipPlane = .1f; cam.farClipPlane = extent * 20f; cam.transform.position = bounds.center + new Vector3(0, 0, -extent * 6f); cam.transform.LookAt(bounds.center);
        var rt = new RenderTexture(256, 256, 24, RenderTextureFormat.ARGB32); cam.targetTexture = rt;
        var image = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        Color32[] Render() { cam.Render(); RenderTexture.active = rt; image.ReadPixels(new Rect(0, 0, 256, 256), 0, 0); image.Apply(); return image.GetPixels32(); }
        var before = Render(); File.WriteAllBytes(Path.Combine(Application.persistentDataPath, "moai-outline-before.png"), image.EncodeToPNG());
        var outline = obj.AddComponent<Outline>(); outline.OutlineMode = Outline.Mode.OutlineVisible; outline.OutlineColor = Color.white; outline.OutlineWidth = 5f;
        yield return null; yield return new WaitForEndOfFrame();
        var after = Render(); File.WriteAllBytes(Path.Combine(Application.persistentDataPath, "moai-outline-after.png"), image.EncodeToPNG());
        int changed = 0; for (int i = 0; i < before.Length; i++) if (Mathf.Abs(before[i].r - after[i].r) + Mathf.Abs(before[i].g - after[i].g) + Mathf.Abs(before[i].b - after[i].b) > 24) changed++;
        outline.enabled = false; yield return null; var restored = Render(); int residual = 0; for (int i = 0; i < before.Length; i++) if (Mathf.Abs(before[i].r - restored[i].r) + Mathf.Abs(before[i].g - restored[i].g) + Mathf.Abs(before[i].b - restored[i].b) > 24) residual++;
        Debug.Log($"Interactable outline render probe {(changed > 30 && residual == 0 ? "PASS" : "OBSERVE")}: mesh={originalFilter.sharedMesh.name}, rendererMaterials={originalRenderer.sharedMaterials.Length}, pixelsChanged={changed}, residualAfterDisable={residual}, path={Application.persistentDataPath}");
        RenderTexture.active = null; cam.targetTexture = null; Destroy(rt); Destroy(image); Destroy(obj); Destroy(camObj);
    }
}
#endif
