using System;
using System.Reflection;
using UnityEngine;

public static class AndroidCameraOffsetCollisionCheck
{
    public static void Run()
    {
#if !UNITY_ANDROID
        throw new Exception("Android build target is required");
#else
        var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            obstacle.layer = 31;
            obstacle.transform.position = new Vector3(0f, 1.9f, -5f);
            obstacle.transform.localScale = new Vector3(.2f, .2f, .2f);
            Physics.SyncTransforms();
            var method = typeof(PlayerCamera).GetMethod("LimitCameraDistance", BindingFlags.Static | BindingFlags.NonPublic);
            if (method == null) throw new Exception("Camera distance method missing");
            const float radius = 1f, castDistance = 8.5f, desiredDistance = 9.5f;
            var ray = new Ray(Vector3.back, Vector3.back);
            int mask = 1 << 31;
            float Limit(Vector3 offset) => (float)method.Invoke(null, new object[] { ray, radius, castDistance, mask, desiredDistance, offset });
            if (Limit(Vector3.zero) != desiredDistance || Limit(Vector3.up) >= desiredDistance)
                throw new Exception("Offset obstacle was not detected independently of the head path");
            obstacle.GetComponent<Collider>().isTrigger = true;
            Physics.SyncTransforms();
            if (Limit(Vector3.up) != desiredDistance) throw new Exception("Trigger blocked the camera");
            obstacle.GetComponent<Collider>().isTrigger = false;
            obstacle.transform.position = new Vector3(0f, -0.9f, -5f);
            Physics.SyncTransforms();
            if (Limit(Vector3.up) >= desiredDistance) throw new Exception("Head-path obstacle no longer blocks the camera");
            Debug.Log("AndroidCameraOffsetCollisionCheck PASS");
        }
        finally { UnityEngine.Object.DestroyImmediate(obstacle); }
#endif
    }
}
