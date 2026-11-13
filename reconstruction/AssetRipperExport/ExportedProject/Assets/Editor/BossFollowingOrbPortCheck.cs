using System;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;

public static class BossFollowingOrbPortCheck
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool c, string m) { if (!c) throw new Exception("BossFollowingOrbPortCheck: " + m); }
    static T Get<T>(object o, string f) => (T)o.GetType().GetField(f, Private).GetValue(o);
    static void Call(object o, string m) => o.GetType().GetMethod(m, Private).Invoke(o, null);
    static void Near(float a, float b, string m) => Check(Mathf.Abs(a-b) < .002f, m + ": " + a);
    public static void Run()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/BossHomingOrb.prefab"); Check(prefab != null && prefab.GetComponent<BossOrb>() != null, "original prefab");
        var playerObj = new GameObject("player"); playerObj.SetActive(false); MyPlayer.Instance = playerObj.AddComponent<MyPlayer>(); playerObj.transform.position = new Vector3(100, 0, 0);
        var managerObj = new GameObject("manager"); managerObj.SetActive(false); GameManager.Instance = managerObj.AddComponent<GameManager>(); GameManager.Instance.whatIsGroundAndObjects = 0;
        var root = new GameObject("boss fixture"); var boss = root.AddComponent<Enemy>(); boss.collider = root.AddComponent<CapsuleCollider>(); var fight = root.AddComponent<FinalFightController>(); fight.boss = boss; fight.orbFollowing = prefab;
        typeof(FinalFightController).GetProperty("currentPhase").GetSetMethod(true).Invoke(fight, new object[] { 3 }); MyTime.time = 10f;
        BossOrb[] orbs = Array.Empty<BossOrb>();
        try {
            Call(fight, "SpawnOrbsFollowing"); orbs = Resources.FindObjectsOfTypeAll<BossOrb>().Where(x => x.gameObject.scene.IsValid()).OrderBy(x => Get<float>(x, "nextMoveTime")).ToArray(); Check(orbs.Length == 3, "phase three creates three orbs");
            for (int i=0; i<3; i++) { Near(Get<float>(orbs[i], "nextMoveTime"), 12f + .5f*i, "stagger"); Near(Get<float>(orbs[i], "moveInterval"), 1.2f, "phase interval"); Near(Get<float>(orbs[i], "maxMoveDistance"), 60f, "phase max distance"); Check(Get<int>(orbs[i], "iterationsBeforeExplode") == 9, "move count"); Check(Physics.GetIgnoreCollision(orbs[i].collider, boss.collider), "boss collision ignored"); }
            var second = Get<Vector3>(orbs[1], "offset"); Near(second.x, 0f, "four-slot x"); Near(second.y, -20f, "formation height"); Near(second.z, 15f, "four-slot z"); Check(Physics.GetIgnoreCollision(orbs[0].collider, orbs[1].collider), "orb collision ignored");
            var orb = orbs[0]; orb.rb.position = Vector3.zero; Call(orb, "StartMoving"); Near(Get<float>(orb, "moveDist"), 63.5f, "capped distance plus overshoot"); Near(Get<Vector3>(orb, "toPosition").x, 60f, "capped destination"); Near(Get<float>(orb, "nextMoveTime"), 11.2f, "next movement deadline");
            MyTime.fixedDeltaTime = .1f; Call(orb, "UpdateMoving"); Near(orb.rb.velocity.x, 50.8f, "eased movement velocity"); Check(Get<int>(orb, "numMoves") == 1, "move counted");
            Debug.Log("BossFollowingOrbPortCheck passed: original prefab, formation, collision exclusions and capped eased movement");
        } finally { foreach(var orb in orbs) if (orb != null) UnityEngine.Object.DestroyImmediate(orb.gameObject); MyPlayer.Instance = null; GameManager.Instance = null; UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(managerObj); UnityEngine.Object.DestroyImmediate(playerObj); }
    }
}
