using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.UI;

public static class FinalPylonPhasePortCheck
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool value, string text) { if (!value) throw new Exception("FinalPylonPhasePortCheck: " + text); }
    static void Call(object o, string name) => o.GetType().GetMethod(name, Private).Invoke(o, null);
    static bool Invulnerable(Enemy e) => (bool)typeof(Enemy).GetField("isInvulnerable", Private).GetValue(e);
    static BossPylon Pylon(GameObject root, int index)
    {
        var obj = new GameObject("pylon " + index); obj.transform.SetParent(root.transform); obj.transform.position = new Vector3(index * 3 + 1, 4, 2);
        var p = obj.AddComponent<BossPylon>(); p.zoneRenderer = new GameObject("zone").AddComponent<MeshRenderer>(); p.zoneRenderer.transform.SetParent(obj.transform);
        p.circleParent = new GameObject("canvas").AddComponent<CanvasGroup>(); p.circleParent.transform.SetParent(obj.transform);
        p.circleProgress = new GameObject("progress", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>(); p.circleProgress.transform.SetParent(obj.transform);
        p.audioStart = obj.AddComponent<AudioSource>(); p.audioLoop = obj.AddComponent<AudioSource>(); p.audioComplete = obj.AddComponent<AudioSource>(); p.audioAbort = obj.AddComponent<AudioSource>();
        p.minimapIcon = new GameObject("icon"); p.minimapIcon.transform.SetParent(obj.transform); p.altarIcon = new GameObject("altar"); p.altarIcon.transform.SetParent(obj.transform);
        p.laser = new GameObject("laser").AddComponent<LineRenderer>(); p.laser.transform.SetParent(obj.transform); p.moveFx = new GameObject("move fx").AddComponent<ParticleSystem>(); p.moveFx.transform.SetParent(obj.transform);
        return p;
    }
    public static void Run()
    {
        var root = new GameObject("phase fixture"); root.SetActive(false);
        var fight = root.AddComponent<FinalFightController>(); fight.audioPylonsSpawn = root.AddComponent<AudioSource>();
        fight.pylons = new[] { Pylon(root, 0), Pylon(root, 1), Pylon(root, 2), Pylon(root, 3) };
        var enemyObj = new GameObject("boss"); var enemy = enemyObj.AddComponent<Enemy>(); enemy.maxHp = 1000; enemy.hp = 750; fight.boss = enemy;
        int started = 0, health = 0; Action onStart = () => started++; Action<Enemy> onHealth = x => { if (x == enemy) health++; };
        FinalFightController.A_PylonsStarted += onStart; Enemy.A_HealthChange += onHealth;
        try {
            Call(fight, "Awake"); Enemy.A_Damage(enemy, new DamageContainer(0f, "fixture"));
            var active = (List<BossPylon>)typeof(FinalFightController).GetField("activePylons", Private).GetValue(fight);
            Check(fight.currentPhase == 1 && active.Count == 3 && new HashSet<BossPylon>(active).Count == 3 && Invulnerable(enemy) && started == 1, "75% threshold selects three unique pylons and shields boss");
            MyTime.time = 1f; Call(fight, "PylonHealing"); Check(enemy.hp == 751f && health == 1, "pylon healing and health change event");
            Call(fight, "PylonHealing"); Check(enemy.hp == 751f && health == 1, "healing interval");
            BossPylon.A_Charged(active[0]); BossPylon.A_Charged(active[0]); Check(active.Count == 1 && Invulnerable(enemy), "shield persists until last pylon");
            BossPylon.A_Charged(active[0]); Check(active.Count == 0 && !Invulnerable(enemy), "last pylon removes shield");
            enemy.hp = 500; Enemy.A_Damage(enemy, new DamageContainer(0f, "fixture")); Check(fight.currentPhase == 2 && started == 2, "50% threshold");
            active = (List<BossPylon>)typeof(FinalFightController).GetField("activePylons", Private).GetValue(fight);
            while (active.Count > 0) BossPylon.A_Charged(active[0]);
            enemy.hp = 250; Enemy.A_Damage(enemy, new DamageContainer(0f, "fixture")); Check(fight.currentPhase == 3 && started == 3, "25% threshold");
            Call(fight, "OnDestroy"); Debug.Log("FinalPylonPhasePortCheck passed: thresholds, distinct pylons, shield, healing and completion");
        } finally {
            FinalFightController.A_PylonsStarted -= onStart; Enemy.A_HealthChange -= onHealth;
            foreach (BossPylon p in fight.pylons) if (p.moveFx != null) UnityEngine.Object.DestroyImmediate(p.moveFx.gameObject);
            UnityEngine.Object.DestroyImmediate(enemyObj); UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
