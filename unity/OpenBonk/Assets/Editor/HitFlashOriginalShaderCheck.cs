using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class HitFlashOriginalShaderCheck
{
    public static void Run()
    {
        try {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/DamageFlash.mat");
            if (!source || !source.shader.isSupported || ShaderUtil.ShaderHasError(source.shader)) throw new Exception("real material or shader missing");
            var mat=new Material(source); var texture=new Texture2D(2,1,TextureFormat.RGBA32,false,true); texture.filterMode=FilterMode.Point; texture.wrapMode=TextureWrapMode.Clamp;
            texture.SetPixels(new[]{new Color(1,0,0,.4f),new Color(0,1,0,.8f)}); texture.Apply();
            mat.SetTexture("_MainTex",texture); mat.SetTextureScale("_MainTex",new Vector2(.1f,1)); mat.SetTextureOffset("_MainTex",new Vector2(.9f,0));
            mat.SetColor("_Color",Color.white); mat.SetColor("_FlashColor",new Color(0,0,1,.2f)); mat.SetFloat("_FlashAmount",.5f);
            var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);quad.GetComponent<Renderer>().sharedMaterial=mat;
            var cam=new GameObject("hit flash camera").AddComponent<Camera>();cam.transform.position=new Vector3(0,0,-4);cam.orthographic=true;cam.orthographicSize=1;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.clear;
            var target=new RenderTexture(128,128,24,RenderTextureFormat.ARGB32);cam.targetTexture=target;cam.Render();RenderTexture.active=target;
            var image=new Texture2D(128,128,TextureFormat.RGBA32,false,true);image.ReadPixels(new Rect(0,0,128,128),0,0);image.Apply();
            var left=image.GetPixel(48,64);var right=image.GetPixel(80,64);
            if (!(left.r>.4f && left.g<.05f && left.b>.4f && right.g>.4f && right.r<.05f && right.b>.4f && Math.Abs(left.a-.3f)<.03f && Math.Abs(right.a-.5f)<.03f)) throw new Exception("original UV/alpha mismatch: "+left+" / "+right);
            if (SystemInfo.supportsInstancing) {
                var mesh=quad.GetComponent<MeshFilter>().sharedMesh; quad.SetActive(false); mat.enableInstancing=true;
                mat.SetTexture("_MainTex",Texture2D.whiteTexture);
                var props=new MaterialPropertyBlock();
                props.SetVectorArray("_Color",new[]{new Vector4(1,0,0,1),new Vector4(0,1,0,1)});
                props.SetVectorArray("_FlashColor",new[]{new Vector4(0,0,1,1),new Vector4(1,1,1,1)});
                props.SetFloatArray("_FlashAmount",new[]{0f,1f});
                var matrices=new[]{Matrix4x4.Translate(new Vector3(-.5f,0,0)), Matrix4x4.Translate(new Vector3(.5f,0,0))};
                var commands=new UnityEngine.Rendering.CommandBuffer();commands.DrawMeshInstanced(mesh,0,mat,0,matrices,2,props);
                cam.AddCommandBuffer(UnityEngine.Rendering.CameraEvent.BeforeForwardOpaque,commands);cam.Render();RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,128,128),0,0);image.Apply();var first=image.GetPixel(32,64);var second=image.GetPixel(96,64);
                if (!(first.r>.8f && first.g<.1f && first.b<.1f && second.r>.8f && second.g>.8f && second.b>.8f)) throw new Exception("instance properties: "+first+" / "+second);
                cam.RemoveCommandBuffer(UnityEngine.Rendering.CameraEvent.BeforeForwardOpaque,commands);commands.Release();
            } else throw new Exception("GPU instancing unavailable");
            Debug.Log("HitFlashOriginalShaderCheck passed: real DamageFlash material, untransformed UV, interpolated RGBA and independent instance properties "+left+" / "+right);
            EditorApplication.Exit(0);
        } catch(Exception e) { Debug.LogException(e);EditorApplication.Exit(1); }
    }
}
