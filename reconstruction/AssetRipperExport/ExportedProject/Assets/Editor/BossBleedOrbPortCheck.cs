using System;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;

public static class BossBleedOrbPortCheck
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool condition, string message) { if (!condition) throw new Exception("BossBleedOrbPortCheck: " + message); }
    static T Get<T>(object o, string field) => (T)o.GetType().GetField(field, Private).GetValue(o);
    static void Call(object o, string name) => o.GetType().GetMethod(name, Private).Invoke(o, null);
    static void Near(float value, float expected, string message) => Check(Mathf.Abs(value - expected) < .001f, message + ": " + value);
    public static void Run()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/BossOrbBleed.prefab"); Check(prefab != null && prefab.GetComponent<BossOrbBleed>() != null, "original prefab");
        var root = new GameObject("bleed orb fixture"); root.SetActive(false); var boss = root.AddComponent<Enemy>(); boss.transform.position = new Vector3(4, 2, 1); var fight = root.AddComponent<FinalFightController>(); fight.boss = boss; fight.orbBleed = prefab;
        typeof(FinalFightController).GetProperty("currentPhase").GetSetMethod(true).Invoke(fight, new object[] { 3 }); MyTime.time = 20f;
        BossOrbBleed[] orbs = Array.Empty<BossOrbBleed>();
        try {
            Call(fight, "SpawnOrbsBleed"); orbs = Resources.FindObjectsOfTypeAll<BossOrbBleed>().Where(x => x.gameObject.scene.IsValid()).OrderBy(x => Get<float>(x, "moveAtTime")).ToArray();
            Check(orbs.Length == 3, "phase three creates three orbs");
            for (int i = 0; i < orbs.Length; i++) {
                Near(Get<float>(orbs[i], "acceleration"), 3900f, "phase acceleration"); Near(Get<float>(orbs[i], "moveAtTime"), 23f + .5f * i, "stagger"); Near(Get<float>(orbs[i], "destroyAtTime"), 31f, "lifetime");
                Check(Vector3.Distance(orbs[i].transform.position, boss.transform.position) < .001f, "spawn at boss position");
            }
            Vector3 second = Get<Vector3>(orbs[1], "offset"); Near(second.x, 0f, "four-slot formation x"); Near(second.y, -15f, "formation height"); Near(second.z, 15f, "four-slot formation z");
            Check(Physics.GetIgnoreCollision(orbs[0].GetComponent<Collider>(), orbs[1].GetComponent<Collider>()), "orb collisions ignored");
            Call(orbs[0], "ShootOrb"); Check(Get<bool>(orbs[0], "isFired"), "fired state"); Near(orbs[0].rb.drag, .7f, "flight drag");
            Debug.Log("BossBleedOrbPortCheck passed: original prefab, phase-three formation, acceleration, timers and launch");
        } finally { foreach (var orb in orbs) if (orb != null) UnityEngine.Object.DestroyImmediate(orb.gameObject); UnityEngine.Object.DestroyImmediate(root); }
    }
}
