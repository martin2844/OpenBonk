using System;
using System.Reflection;
using Assets.Scripts.Managers;
using UnityEngine;

public static class PlayerFxPortCheck
{
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Call(object instance, string method) => instance.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(instance, null);
    static GameObject Clone(string name)
    {
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>()) if (go.name == name + "(Clone)") return go;
        return null;
    }
    public static void Run()
    {
        var root = new GameObject("fx fixture"); root.SetActive(false);
        var body = new GameObject("fx body"); var rb = body.AddComponent<Rigidbody>(); rb.velocity = new Vector3(4f, -3f, -6f);
        var feet = new GameObject("fx feet"); feet.transform.position = new Vector3(3f, 4f, 5f);
        var jump = new GameObject("fx jump"); jump.AddComponent<ParticleSystem>(); jump.transform.rotation = Quaternion.Euler(0, 40, 0);
        var land = new GameObject("fx land"); land.AddComponent<ParticleSystem>();
        var movement = root.AddComponent<PlayerMovement>(); movement.feet = feet.transform;
        typeof(PlayerMovement).GetField("rb", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(movement, rb);
        var fx = root.AddComponent<PlayerFx>(); fx.playerMovement = movement; fx.jumpFx = jump; fx.landFx = land;
        try
        {
            var pos = new Vector3(7, 8, 9); var rotation = Quaternion.Euler(10, 20, 30);
            var direct = ParticleSpawner.SpawnParticles(jump, pos, rotation);
            Check(direct != jump && direct.transform.position == pos && Quaternion.Angle(direct.transform.rotation, rotation) < .01f, "spawner preserves position and rotation");
            UnityEngine.Object.DestroyImmediate(direct);
            Call(fx, "Awake");
            PlayerMovement.A_Jumped?.Invoke(movement);
            var jumpClone = Clone("fx jump"); Check(jumpClone != null && jumpClone.transform.position == feet.transform.position && Quaternion.Angle(jumpClone.transform.rotation, jump.transform.rotation) < .01f, "jump prefab and transform");
            var jv = jumpClone.GetComponent<ParticleSystem>().velocityOverLifetime;
            Check(Mathf.Abs(jv.x.constant - 2f) < .001f && Mathf.Abs(jv.z.constant + 3f) < .001f, "jump velocity");
            PlayerMovement.A_Landed?.Invoke(7.99f); Check(Clone("fx land") == null, "landing threshold");
            PlayerMovement.A_Landed?.Invoke(8f);
            var landClone = Clone("fx land"); Check(landClone != null && landClone.transform.position == feet.transform.position && Quaternion.Angle(landClone.transform.rotation, Quaternion.LookRotation(Vector3.up)) < .01f, "landing threshold and transform");
            var lv = landClone.GetComponent<ParticleSystem>().velocityOverLifetime;
            Check(Mathf.Abs(lv.x.constant - 5.6f) < .001f && Mathf.Abs(lv.z.constant + 8.4f) < .001f, "landing velocity");
            UnityEngine.Object.DestroyImmediate(jumpClone); UnityEngine.Object.DestroyImmediate(landClone);
            Call(fx, "OnDestroy"); PlayerMovement.A_Jumped?.Invoke(movement); PlayerMovement.A_Landed?.Invoke(9f);
            Check(Clone("fx jump") == null && Clone("fx land") == null, "event cleanup");
            Debug.Log("PlayerFxPortCheck passed: spawn transform, events, velocity, landing threshold and cleanup");
        }
        finally
        {
            Call(fx, "OnDestroy");
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>()) if (go != null && (go.name == "fx jump(Clone)" || go.name == "fx land(Clone)")) UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(body); UnityEngine.Object.DestroyImmediate(feet); UnityEngine.Object.DestroyImmediate(jump); UnityEngine.Object.DestroyImmediate(land);
        }
    }
}
