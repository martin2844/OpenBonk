#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using UnityEngine;

// Isolated render using the actual Fox ghost material, enabled only by intent.
internal sealed class AndroidToonEmissionProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_toon_emission_probe", false)) return;
        var root = new GameObject("toon emission probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidToonEmissionProbe>();
    }
    IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(8f);
        Material source = null;
        foreach (var mat in Resources.FindObjectsOfTypeAll<Material>()) if (mat.name == "m_fox_ghost") { source = mat; break; }
        if (source == null || !source.shader.isSupported) { Debug.LogError($"Toon emission probe INCONCLUSIVE: material={source}"); yield break; }
        bool sourceKeyword = source.IsKeywordEnabled("_MK_EMISSION") && !source.IsKeywordEnabled("_MK_EMISSION_MAP");
        var material = new Material(source); material.SetFloat("_Outline", 1); material.SetFloat("_Rim", 0); material.SetFloat("_AlphaClipping", 0); material.SetFloat("_RenderFace", 0);
        material.SetColor("_AlbedoColor", Color.black); material.SetColor("_EmissionColor", new Color(.8f, .4f, .2f, 1)); material.SetFloat("_IndirectFade", 1);
        const int layer = 30; var obj = GameObject.CreatePrimitive(PrimitiveType.Quad); obj.layer = layer; obj.transform.position = new Vector3(0,10000,0); obj.transform.localScale = Vector3.one * 2; obj.GetComponent<Renderer>().sharedMaterial = material;
        var camObj = new GameObject("toon emission camera"); camObj.transform.position = new Vector3(0,10000,-4); var cam = camObj.AddComponent<Camera>(); cam.orthographic = true; cam.orthographicSize = 1; cam.aspect = 1; cam.cullingMask = 1 << layer; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black; cam.allowHDR = false; cam.allowMSAA = false;
        var target = new RenderTexture(64,64,24,RenderTextureFormat.ARGB32); var image = new Texture2D(64,64,TextureFormat.RGBA32,false); cam.targetTexture = target; var old = RenderTexture.active;
        Color Read() { cam.Render(); RenderTexture.active = target; image.ReadPixels(new Rect(0,0,64,64),0,0); image.Apply(); return image.GetPixel(32,32); }
        material.DisableKeyword("_MK_EMISSION"); material.DisableKeyword("_MK_EMISSION_MAP"); Color off = Read();
        material.EnableKeyword("_MK_EMISSION"); Color constant = Read();
        material.EnableKeyword("_MK_EMISSION_MAP"); material.SetTexture("_EmissionMap", Texture2D.blackTexture); Color black = Read();
        material.SetTexture("_EmissionMap", Texture2D.whiteTexture); Color white = Read();
        bool ok = sourceKeyword && off.r < .04f && constant.r > .3f && black.r < .04f && Mathf.Abs(white.r - constant.r) < .04f;
        Debug.Log($"Toon emission probe {(ok ? "PASS" : "FAIL")}: sourceKeywords={sourceKeyword} shader={source.shader.name} off={off} constant={constant} black={black} white={white}");
        RenderTexture.active = old; cam.targetTexture = null; target.Release(); Destroy(target); Destroy(image); Destroy(camObj); Destroy(obj); Destroy(material);
        float deadline = Time.realtimeSinceStartup + 120f; CharacterDisplay display = null;
        while (Time.realtimeSinceStartup < deadline) {
            display = FindObjectOfType<CharacterDisplay>();
            if (display != null && display.isActiveAndEnabled && display.playerRenderer != null && display.playerRenderer.renderer != null && DataManager.Instance != null) break;
            yield return null;
        }
        if (display == null || !display.isActiveAndEnabled || display.playerRenderer == null || display.playerRenderer.renderer == null) { Debug.LogError("Toon skin emission probe INCONCLUSIVE: selection unavailable"); yield break; }
        yield return new WaitForSecondsRealtime(1f);
        SkinData ghost = null, normal = null;
        foreach (var skin in DataManager.Instance.unsortedSkins) {
            if (skin == null || skin.character != ECharacter.Fox || skin.materials == null) continue;
            if (skin.skinType == Assets.Scripts._Data.ESkinType.Default) normal = skin;
            foreach (var mat in skin.materials) if (mat != null && mat.name == "m_fox_ghost") ghost = skin;
        }
        if (ghost == null || normal == null) { Debug.LogError($"Toon skin emission probe INCONCLUSIVE: ghost={ghost}, normal={normal}"); yield break; }
        var actor = display.playerRenderer; actor.SetSkin(ghost);
        var active = actor.renderer.sharedMaterials; bool ghostOk = active.Length == ghost.materials.Length;
        for (int i = 0; i < active.Length && i < ghost.materials.Length; i++)
            ghostOk &= active[i].IsKeywordEnabled("_MK_EMISSION") == ghost.materials[i].IsKeywordEnabled("_MK_EMISSION") && active[i].IsKeywordEnabled("_MK_EMISSION_MAP") == ghost.materials[i].IsKeywordEnabled("_MK_EMISSION_MAP") && active[i].GetColor("_EmissionColor") == ghost.materials[i].GetColor("_EmissionColor");
        actor.SetSkin(normal); active = actor.renderer.sharedMaterials; bool restored = active.Length == normal.materials.Length;
        for (int i = 0; i < active.Length && i < normal.materials.Length; i++)
            restored &= active[i].IsKeywordEnabled("_MK_EMISSION") == normal.materials[i].IsKeywordEnabled("_MK_EMISSION") && active[i].IsKeywordEnabled("_MK_EMISSION_MAP") == normal.materials[i].IsKeywordEnabled("_MK_EMISSION_MAP");
        Debug.Log($"Toon skin emission probe {(ghostOk && restored ? "PASS" : "FAIL")}: ghost={ghostOk} restored={restored} shaderLevel={SystemInfo.graphicsShaderLevel}");
    }
}
#endif
