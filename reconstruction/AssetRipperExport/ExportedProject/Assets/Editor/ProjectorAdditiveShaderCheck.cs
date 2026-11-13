using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ProjectorAdditiveShaderCheck
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static Color32[] Render(Camera camera, RenderTexture target, Texture2D image)
    {
        camera.Render(); RenderTexture.active = target;
        image.ReadPixels(new Rect(0, 0, 128, 128), 0, 0); image.Apply();
        RenderTexture.active = null; return image.GetPixels32();
    }
    public static void Run()
    {
        try {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/SphereSlam.prefab");
            Check(prefab, "SphereSlam prefab missing");
            var source = prefab.GetComponentInChildren<Projector>(true);
            Check(source && source.material && source.material.shader.name == "Projector/AdditiveTint", "real projector material missing");
            Check(source.material.GetTexture("_ShadowTex"), "real projector cookie missing");
            Check(!ShaderUtil.ShaderHasError(source.material.shader), "projector shader failed compilation");
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane); ground.layer = 8;
            var groundMaterial = new Material(Shader.Find("Unlit/Color")); groundMaterial.color = new Color(.12f,.12f,.12f,1);
            ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;
            var projector = UnityEngine.Object.Instantiate(source.gameObject).GetComponent<Projector>();
            projector.transform.position = new Vector3(0,1,0); projector.transform.rotation = Quaternion.Euler(90,0,0);
            var camera = new GameObject("projector check camera").AddComponent<Camera>();
            camera.transform.position = new Vector3(0,3,0); camera.transform.rotation = Quaternion.Euler(90,0,0);
            camera.orthographic = true; camera.orthographicSize = 1.1f; camera.clearFlags = CameraClearFlags.SolidColor;
            var target = new RenderTexture(128,128,24,RenderTextureFormat.ARGB32); camera.targetTexture = target;
            var image = new Texture2D(128,128,TextureFormat.RGBA32,false);
            projector.enabled = false; var before = Render(camera,target,image);
            projector.enabled = true; var after = Render(camera,target,image);
            int changed = 0, unchanged = 0, darkened = 0;
            for (int i=0;i<before.Length;i++) {
                int delta = Math.Abs(after[i].r-before[i].r)+Math.Abs(after[i].g-before[i].g)+Math.Abs(after[i].b-before[i].b);
                if (delta > 3) changed++; else unchanged++;
                if (after[i].r+3 < before[i].r || after[i].g+3 < before[i].g || after[i].b+3 < before[i].b) darkened++;
            }
            Check(changed > 50 && unchanged > 1000 && darkened == 0, "expected localized additive projection: changed="+changed+" unchanged="+unchanged+" darkened="+darkened);
            Debug.Log("ProjectorAdditiveShaderCheck passed: actual SphereSlam projector, cookie, layer and GPU blending; changed="+changed+" unchanged="+unchanged);
            EditorApplication.Exit(0);
        } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
