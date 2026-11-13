using System;
using System.Reflection;
using UnityEngine;

public static class DisableFogForCameraPortCheck
{
    public static void Run()
    {
        bool previous = RenderSettings.fog;
        var root = new GameObject("fog camera fixture");
        var component = root.AddComponent<DisableFogForCamera>();
        var pre = typeof(DisableFogForCamera).GetMethod("OnPreRender", BindingFlags.Instance | BindingFlags.NonPublic);
        var post = typeof(DisableFogForCamera).GetMethod("OnPostRender", BindingFlags.Instance | BindingFlags.NonPublic);
        try
        {
            foreach (bool initial in new[] { false, true })
            {
                RenderSettings.fog = initial;
                pre.Invoke(component, null);
                if (RenderSettings.fog) throw new Exception("Fog was not disabled for camera");
                post.Invoke(component, null);
                if (RenderSettings.fog != initial) throw new Exception("Fog state was not restored");
            }
            Debug.Log("DisableFogForCameraPortCheck passed: fog disabled during rendering and both initial states restored");
        }
        finally { RenderSettings.fog = previous; UnityEngine.Object.DestroyImmediate(root); }
    }
}
