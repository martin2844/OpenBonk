using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class ToonMainPropertiesPortCheck
{
    static void Require(bool condition, string message) { if (!condition) throw new Exception("ToonMainPropertiesPortCheck: " + message); }
    public static void Run()
    {
        string[] paths = {
            "Assets/Shader/MK_Toon_Built-in_Standard_Simple + Outline.shader",
            "Assets/Shader/MK_Toon_Built-in_Standard_Simple.shader",
            "Assets/Shader/MK_Toon_Built-in_Standard_Physically Based + Outline.shader",
            "Assets/Shader/MK_Toon_Built-in_Particles_Simple.shader"
        };
        var texture = new Texture2D(1, 1);
        try {
            foreach (string path in paths) {
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                Require(shader != null, path + " missing");
                int colorIndex = shader.FindPropertyIndex("_AlbedoColor");
                int textureIndex = shader.FindPropertyIndex("_AlbedoMap");
                Require(colorIndex >= 0 && (shader.GetPropertyFlags(colorIndex) & ShaderPropertyFlags.MainColor) != 0, path + " missing MainColor");
                Require(textureIndex >= 0 && (shader.GetPropertyFlags(textureIndex) & ShaderPropertyFlags.MainTexture) != 0, path + " missing MainTexture");
                var material = new Material(shader);
                try {
                    var tint = new Color(.17f,.37f,.57f,.77f);
                    material.color = tint; material.mainTexture = texture;
                    Require(material.GetColor("_AlbedoColor") == tint, path + " material.color does not address albedo");
                    Require(material.GetTexture("_AlbedoMap") == texture, path + " mainTexture does not address albedo");
                    Require(material.color == tint && material.mainTexture == texture, path + " main property reads failed");
                } finally { UnityEngine.Object.DestroyImmediate(material); }
            }
            Debug.Log("ToonMainPropertiesPortCheck passed: four shader main properties and Material API reads/writes");
        } finally { UnityEngine.Object.DestroyImmediate(texture); }
    }
}
