using System;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Spawning;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;
using Utility;

public static class BossShootyOrbPortCheck
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool c, string message) { if (!c) throw new Exception("BossShootyOrbPortCheck: " + message); }
    static T Get<T>(object o, string name) => (T)o.GetType().GetField(name, Private).GetValue(o);
    static void Call(object o, string name) => o.GetType().GetMethod(name, Private).Invoke(o, null);
    static void Near(float a, float b, string text) => Check(Mathf.Abs(a-b)<.001f, text + ": " + a);
    public static void Run()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/BossShootyOrb.prefab"); Check(prefab != null && prefab.GetComponent<BossOrbShooty>() != null, "original orb prefab");
        var playerObj = new GameObject("player fixture"); playerObj.SetActive(false); var player = playerObj.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.playerMovement = playerObj.AddComponent<PlayerMovement>(); var playerBody = playerObj.AddComponent<Rigidbody>(); playerBody.position = new Vector3(10, 3, 5); playerBody.velocity = new Vector3(2, 1, -3);
        var root = new GameObject("boss fixture"); root.SetActive(false); var boss = root.AddComponent<Enemy>(); boss.transform.position = new Vector3(2, 0, 3); var fight = root.AddComponent<FinalFightController>(); fight.boss = boss; fight.orbShooty = prefab;
        typeof(FinalFightController).GetProperty("currentPhase").GetSetMethod(true).Invoke(fight, new object[] { 2 }); MyTime.time = 10;
        BossOrbShooty[] orbs = Array.Empty<BossOrbShooty>();
        try {
            Vector3 predicted = SpawnPositions.PredictPlayerPosition(2f); Check(Vector3.Distance(predicted, new Vector3(14, 5, -1)) < .001f, "rigidbody prediction");
            Call(fight, "SpawnOrbsShooty");
            orbs = Resources.FindObjectsOfTypeAll<BossOrbShooty>().Where(x => x.gameObject.scene.IsValid()).OrderBy(x => Get<float>(x, "moveAtTime")).ToArray();
            Check(orbs.Length == 3, "three instantiated original orbs");
            for(int i=0; i<3; i++) { Near(Get<float>(orbs[i], "speed"), 95f, "phase speed"); Near(Get<float>(orbs[i], "moveAtTime"), 13f + .5f*i, "staggered fire"); Near(Get<float>(orbs[i], "destroyAtTime"), 20f, "orb lifetime"); Near(Get<Vector3>(orbs[i], "offset").magnitude, Mathf.Sqrt(325f), "formation offset"); }
            Check(Physics.GetIgnoreCollision(orbs[0].GetComponent<Collider>(), orbs[1].GetComponent<Collider>()), "orb collision pairs ignored");
            MyRandom.random = new System.Random(0); UnityEngine.Random.InitState(12); playerObj.transform.position = new Vector3(10, 3, 5); Call(orbs[0], "ShootOrb");
            Check(Get<bool>(orbs[0], "isFired"), "fired state"); Near(orbs[0].rb.velocity.magnitude, 95f, "launch speed");
            typeof(FinalFightController).GetField("nextOrbsFollowingTime", Private).SetValue(fight, 100f);
            typeof(FinalFightController).GetField("nextOrbsBleedTime", Private).SetValue(fight, 100f);
            typeof(FinalFightController).GetField("nextOrbsShootyTime", Private).SetValue(fight, 15f);
            typeof(FinalFightController).GetField("goonsDeadAtTime", Private).SetValue(fight, 100f);
            MyTime.time = 15f; Call(fight, "SpecialAttacks");
            Check(Resources.FindObjectsOfTypeAll<BossOrbShooty>().Count(x => x.gameObject.scene.IsValid()) == 3, "no attack at exact deadline");
            MyTime.time = 15.1f; Call(fight, "SpecialAttacks");
            orbs = Resources.FindObjectsOfTypeAll<BossOrbShooty>().Where(x => x.gameObject.scene.IsValid()).ToArray();
            float next = Get<float>(fight, "nextOrbsShootyTime");
            Check(orbs.Length == 6 && next >= 15.1f + 33f * .6f && next <= 15.1f + 33f * 1.4f, "scheduled attack and phase-adjusted interval");
            typeof(FinalFightController).GetProperty("currentPhase").GetSetMethod(true).Invoke(fight, new object[] { 3 });
            MyTime.time = 30f; Call(fight, "SpawnOrbsShooty");
            orbs = Resources.FindObjectsOfTypeAll<BossOrbShooty>().Where(x => x.gameObject.scene.IsValid()).ToArray();
            var thirdPhase = orbs.Where(x => Get<float>(x, "moveAtTime") >= 33f).OrderBy(x => Get<float>(x, "moveAtTime")).ToArray();
            Check(thirdPhase.Length == 3, "phase three capped at three orbs");
            Vector3 secondOffset = Get<Vector3>(thirdPhase[1], "offset");
            Check(Mathf.Abs(secondOffset.x) < .001f && Mathf.Abs(secondOffset.z - 15f) < .001f, "phase-three formation uses four slots");
            Debug.Log("BossShootyOrbPortCheck passed: original prefab spawn, formation, prediction, collision exclusion and launch");
        } finally { foreach(var orb in orbs) if (orb != null) UnityEngine.Object.DestroyImmediate(orb.gameObject); MyPlayer.Instance = null; UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(playerObj); }
    }
}
