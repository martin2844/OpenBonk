#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using Assets.Scripts.Actors.Player;
using UnityEngine;

// Test-only render observation of the real spawned character material.
internal sealed class AndroidGeorgeLeafProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_george_leaf_probe", false)) return;
        var go = new GameObject("George leaf probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidGeorgeLeafProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.playerRenderer == null || MyPlayer.Instance.playerRenderer.renderer == null)) yield return null;
        var player = MyPlayer.Instance;
        if (player == null || player.playerRenderer == null || player.playerRenderer.renderer == null) { Debug.LogError("George leaf probe INCONCLUSIVE: player not ready"); yield break; }
        Material actual = null;
        foreach (var material in player.playerRenderer.renderer.sharedMaterials)
            if (material != null && material.IsKeywordEnabled("_MK_VERTEX_ANIMATION_SINE") && material.IsKeywordEnabled("_MK_VERTEX_ANIMATION_STUTTER")) { actual = material; break; }
        if (actual == null) { Debug.LogError($"George leaf probe FAIL: no animated material on {player.character}"); yield break; }
        Debug.Log($"George leaf surface: shaderLevel={SystemInfo.graphicsShaderLevel}, alphaClip={actual.GetFloat("_AlphaClipping"):F2}, cutoff={actual.GetFloat("_AlphaCutoff"):F2}, renderFace={actual.GetFloat("_RenderFace"):F0}, outline={actual.GetFloat("_Outline"):F0}");
        var mat = new Material(actual);
        var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere); obj.layer = 31; obj.transform.position = new Vector3(0, 10000, 0);
        obj.GetComponent<Renderer>().sharedMaterial = mat;
        var cameraObject = new GameObject("George leaf render camera"); cameraObject.transform.position = obj.transform.position + new Vector3(0, 0, -5);
        var camera = cameraObject.AddComponent<Camera>(); camera.enabled = false; camera.orthographic = true; camera.orthographicSize = 1.2f;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.blue; camera.cullingMask = 1 << 31;
        var target = new RenderTexture(256, 256, 24, RenderTextureFormat.ARGB32); camera.targetTexture = target;
        var image = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        try {
            Color32[] Render(float value) { mat.SetFloat("_VertexAnimationIntensity", value); camera.Render(); var old = RenderTexture.active; RenderTexture.active = target; image.ReadPixels(new Rect(0,0,256,256),0,0); image.Apply(); RenderTexture.active = old; return image.GetPixels32(); }
            var still = Render(0f); var moving = Render(.25f); var reset = Render(0f);
            int changed = 0, residual = 0;
            for (int i=0; i<still.Length; i++) {
                int d = Math.Abs(still[i].r-moving[i].r)+Math.Abs(still[i].g-moving[i].g)+Math.Abs(still[i].b-moving[i].b);
                int r = Math.Abs(still[i].r-reset[i].r)+Math.Abs(still[i].g-reset[i].g)+Math.Abs(still[i].b-reset[i].b);
                if (d>9) changed++; if (r>9) residual++;
            }
            Debug.Log($"George leaf probe {(changed > 100 && residual == 0 ? "PASS" : "FAIL")}: character={player.character}, material={actual.name}, intensity={actual.GetFloat("_VertexAnimationIntensity"):F2}, changed={changed}, resetDifference={residual}");
            // Isolate the runtime material's face-culling setting with an
            // opaque synthetic texture; disable the outline and animation.
            obj.SetActive(false);
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.layer = 31; quad.transform.position = new Vector3(0,10000,0); quad.GetComponent<Renderer>().sharedMaterial = mat;
            mat.SetTexture("_AlbedoMap", Texture2D.whiteTexture); mat.SetColor("_AlbedoColor", Color.white);
            mat.SetFloat("_VertexAnimationIntensity", 0f); mat.SetFloat("_Outline", 0f);
            int CountVisible() {
                camera.Render(); var old = RenderTexture.active; RenderTexture.active = target;
                image.ReadPixels(new Rect(0,0,256,256),0,0); image.Apply(); RenderTexture.active = old;
                int count = 0; foreach (var pixel in image.GetPixels32()) if (pixel.r > 8 || pixel.g > 8 || pixel.b < 245) count++;
                return count;
            }
            int front = CountVisible(); quad.transform.rotation = Quaternion.Euler(0,180,0); int back = CountVisible();
            Destroy(quad);
            Debug.Log($"George leaf double-sided probe {(front > 100 && back > 100 && actual.GetFloat("_RenderFace") == 0f ? "PASS" : "FAIL")}: front={front}, back={back}, cull={actual.GetFloat("_RenderFace"):F0}");
            SkinData originalSkin = null;
            foreach (var skin in DataManager.Instance.unsortedSkins)
                if (skin != null && skin.character == player.character && skin.skinType == Assets.Scripts._Data.ESkinType.Default) { originalSkin = skin; break; }
            if (originalSkin == null || originalSkin.materials == null || originalSkin.materials.Length < 2 || originalSkin.materials[0].GetInt("_RenderFace") != 2 || originalSkin.materials[1].GetInt("_RenderFace") != 0)
                Debug.LogError("George leaf skin reuse probe INCONCLUSIVE: expected default materials absent");
            else {
                var other = ScriptableObject.CreateInstance<SkinData>();
                other.materials = new Material[originalSkin.materials.Length];
                for (int i=0; i<other.materials.Length; i++) other.materials[i] = originalSkin.materials[0];
                var actor = player.playerRenderer;
                try {
                    actor.SetSkin(other);
                    var changedSkin = actor.renderer.sharedMaterials[1];
                    var preset = AlwaysManager.Instance.playerMaterialPreset;
                    bool resetSurface = changedSkin.GetInt("_RenderFace") == preset.GetInt("_RenderFace") && changedSkin.GetInt("_AlphaClipping") == preset.GetInt("_AlphaClipping") && changedSkin.GetInt("_Outline") == preset.GetInt("_Outline");
                    bool resetAnimation = !changedSkin.IsKeywordEnabled("_MK_VERTEX_ANIMATION_SINE") && !changedSkin.IsKeywordEnabled("_MK_VERTEX_ANIMATION_STUTTER");
                    actor.SetSkin(originalSkin);
                    var restored = actor.renderer.sharedMaterials[1];
                    bool restoredLeaf = restored.GetInt("_RenderFace") == 0 && restored.GetInt("_Outline") == 3 && restored.IsKeywordEnabled("_MK_VERTEX_ANIMATION_SINE");
                    Debug.Log($"George leaf skin reuse probe {(resetSurface && resetAnimation && restoredLeaf ? "PASS" : "FAIL")}: resetSurface={resetSurface}, resetAnimation={resetAnimation}, restoredLeaf={restoredLeaf}");
                } finally { actor.SetSkin(originalSkin); Destroy(other); }
            }
        } catch (Exception e) { Debug.LogError("George leaf probe FAIL: " + e); }
        finally { camera.targetTexture = null; Destroy(obj); Destroy(mat); Destroy(image); Destroy(target); Destroy(cameraObject); }
    }
}
#endif
