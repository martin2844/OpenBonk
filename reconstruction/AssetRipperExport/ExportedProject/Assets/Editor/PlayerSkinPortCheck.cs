using System;
using System.Reflection;
using UnityEngine;

public static class PlayerSkinPortCheck
{
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    public static void Run()
    {
        var root = new GameObject("PlayerSkinCheck");
        var oldManager = AlwaysManager.Instance;
        var shader = Shader.Find("MK/Toon/Built-in/Standard/Simple + Outline");
        Require(shader != null, "bundled toon shader missing");
        var preset = new Material(shader);
        var first = new Material(shader);
        var second = new Material(shader);
        var texture = new Texture2D(2, 2);
        var skin = ScriptableObject.CreateInstance<SkinData>();
        try
        {
            var manager = root.AddComponent<AlwaysManager>();
            AlwaysManager.Instance = manager; manager.playerMaterialPreset = preset;
            var player = root.AddComponent<PlayerRenderer>();
            player.renderer = root.AddComponent<SkinnedMeshRenderer>();
            first.mainTexture = texture; first.color = Color.red;
            first.SetColor("_EmissionColor", Color.green); first.SetFloat("_IndirectFade", 0.4f); first.SetInt("_RenderFace", 1);
            first.SetTexture("_EmissionMap", texture); first.EnableKeyword("_MK_EMISSION"); first.EnableKeyword("_MK_EMISSION_MAP");
            second.color = Color.blue; second.SetInt("_RenderFace", 0); second.SetColor("_EmissionColor", Color.red); second.EnableKeyword("_MK_EMISSION");
            second.SetFloat("_VertexAnimationIntensity", .05f); second.SetVector("_VertexAnimationFrequency", new Vector4(6, 6, 6, 0));
            second.EnableKeyword("_MK_VERTEX_ANIMATION_SINE"); second.EnableKeyword("_MK_VERTEX_ANIMATION_STUTTER");
            skin.materials = new[] { first, second };
            player.SetSkin(skin);
            var field = typeof(PlayerRenderer).GetField("activeMaterials", BindingFlags.Instance | BindingFlags.NonPublic);
            var active = (Material[])field.GetValue(player);
            Require(active.Length == 2 && active[0] != first && active[0] != preset && active[1] != second, "separate runtime materials");
            Require(player.renderer.sharedMaterials.Length == 2, "renderer material count");
            Require(active[0].GetTexture("_AlbedoMap") == texture && active[0].GetTexture("_MainTex") == texture, "skin texture at every shader level");
            Require(active[0].GetColor("_AlbedoColor") == Color.red && active[0].GetColor("_Color") == Color.red, "skin color at every shader level");
            Require(active[0].GetColor("_EmissionColor") == Color.green && active[0].GetTexture("_EmissionMap") == texture && active[0].IsKeywordEnabled("_MK_EMISSION") && active[0].IsKeywordEnabled("_MK_EMISSION_MAP"), "mapped emission copied at every shader level");
            Require(active[1].IsKeywordEnabled("_MK_VERTEX_ANIMATION_SINE") && active[1].IsKeywordEnabled("_MK_VERTEX_ANIMATION_STUTTER") && Mathf.Abs(active[1].GetFloat("_VertexAnimationIntensity") - .05f) < .001f && active[1].GetVector("_VertexAnimationFrequency").x == 6f, "leaf animation copied");
            if (SystemInfo.graphicsShaderLevel >= 45)
            {
                Require(active[0].GetColor("_EmissionColor") == Color.green, "skin emission");
                Require(Mathf.Abs(active[0].GetFloat("_IndirectFade") - 0.4f) < 0.001f, "indirect fade");
                Require(active[1].GetInt("_AlphaClipping") == 1, "transparent skin clipping");
            }
            var saved = active[0];
            skin.materials = new[] { second };
            player.SetSkin(skin);
            active = (Material[])field.GetValue(player);
            Require(active.Length == 1 && active[0] == saved && player.renderer.sharedMaterials.Length == 1, "material reuse on skin switch");
            Require(active[0].GetColor("_EmissionColor") == Color.red && active[0].IsKeywordEnabled("_MK_EMISSION") && !active[0].IsKeywordEnabled("_MK_EMISSION_MAP"), "reuse switches to constant emission");
            Require(active[0].IsKeywordEnabled("_MK_VERTEX_ANIMATION_SINE") && active[0].IsKeywordEnabled("_MK_VERTEX_ANIMATION_STUTTER"), "reuse enables leaf animation");
            second.DisableKeyword("_MK_EMISSION"); player.SetSkin(skin); active = (Material[])field.GetValue(player);
            Require(!active[0].IsKeywordEnabled("_MK_EMISSION") && !active[0].IsKeywordEnabled("_MK_EMISSION_MAP"), "reuse clears emission keywords");
            second.DisableKeyword("_MK_VERTEX_ANIMATION_SINE"); second.DisableKeyword("_MK_VERTEX_ANIMATION_STUTTER"); player.SetSkin(skin);
            Require(!active[0].IsKeywordEnabled("_MK_VERTEX_ANIMATION_SINE") && !active[0].IsKeywordEnabled("_MK_VERTEX_ANIMATION_STUTTER"), "reuse clears animation keywords");
            Debug.Log("PlayerSkinPortCheck passed: material transfer, shader settings, count and reuse");
        }
        finally
        {
            AlwaysManager.Instance = oldManager;
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(preset); UnityEngine.Object.DestroyImmediate(first); UnityEngine.Object.DestroyImmediate(second);
            UnityEngine.Object.DestroyImmediate(texture); UnityEngine.Object.DestroyImmediate(skin);
        }
    }
}
