#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Game.Combat.EnemySpecialAttacks;
using Assets.Scripts.Utility;
using UnityEngine;

// Opt-in controlled boss setup. The weapon and enemy combat run normally once the altar is activated.
internal sealed class AndroidBossCombatProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_boss_combat_probe", false)) return;
        var go = new GameObject("Boss combat probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidBossCombatProbe>();
    }
    IEnumerator Start()
    {
        bool contactProbe, inputProbe, attackMonitor, isolateTargetLaser, forceLaserIntersection;
        using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var activity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
        using (var intent = activity.Call<AndroidJavaObject>("getIntent"))
        {
            contactProbe = intent.Call<bool>("getBooleanExtra", "megabonk_boss_contact_probe", false);
            inputProbe = intent.Call<bool>("getBooleanExtra", "megabonk_boss_input_probe", false);
            attackMonitor = intent.Call<bool>("getBooleanExtra", "megabonk_boss_attack_monitor", false);
            isolateTargetLaser = intent.Call<bool>("getBooleanExtra", "megabonk_boss_target_laser_only", false);
            forceLaserIntersection = intent.Call<bool>("getBooleanExtra", "megabonk_boss_laser_intersect", false);
        }
        float deadline = Time.realtimeSinceStartup + 180f;
        InteractableBossSpawner altar = null; TargetOfInterestUi ui = null;
        while (Time.realtimeSinceStartup < deadline)
        {
            altar = FindFirstObjectByType<InteractableBossSpawner>(); ui = FindFirstObjectByType<TargetOfInterestUi>();
            if (altar != null && ui != null && GameManager.Instance != null && GameManager.Instance.IsPlaying() && !GameManager.Instance.cutscene && MyPlayer.Instance != null && MyPlayer.Instance.inventory != null && EnemyManager.Instance != null && EnemyManager.Instance.summonerController != null) break;
            yield return null;
        }
        if (altar == null || ui == null || MyPlayer.Instance == null || EnemyManager.Instance == null) { Debug.LogError("Boss combat probe: required scene state missing"); yield break; }
        var player = MyPlayer.Instance; var manager = EnemyManager.Instance;
        bool waves = manager.enabledWaves; manager.enabledWaves = false;
        if (isolateTargetLaser)
            foreach (var enemy in new List<Enemy>(manager.enemies.Values))
                if (enemy != null && enemy.gameObject.activeInHierarchy) enemy.ReleaseToPool();
        var invincible = typeof(MyPlayer).GetField("isInvincible", BindingFlags.NonPublic | BindingFlags.Instance);
        var until = typeof(MyPlayer).GetField("damageCooldownOverAtTime", BindingFlags.NonPublic | BindingFlags.Instance);
        bool previousInvincible = (bool)invincible.GetValue(player); float previousUntil = (float)until.GetValue(player);
        if (contactProbe) { player.inventory.playerHealth.hp = 10000; player.inventory.playerHealth.maxHp = 10000; invincible.SetValue(player, false); until.SetValue(player, 0f); }
        else { invincible.SetValue(player, true); until.SetValue(player, MyTime.time + 10000f); }
        altar.transform.position = player.transform.position + player.transform.forward * (inputProbe ? 1f : contactProbe ? 10f : 3f);
        Physics.SyncTransforms();
        var bossField = typeof(InteractableBossSpawner).GetField("bossEnemies", BindingFlags.Instance | BindingFlags.NonPublic);
        bool activated;
        if (inputProbe)
        {
            var detector = FindFirstObjectByType<DetectInteractables>();
            var selectedField = typeof(DetectInteractables).GetField("currentInteractable", BindingFlags.Instance | BindingFlags.NonPublic);
            deadline = Time.realtimeSinceStartup + 15f;
            while (Time.realtimeSinceStartup < deadline && (detector == null || selectedField.GetValue(detector) != altar)) yield return null;
            if (detector == null || selectedField.GetValue(detector) != altar) { Debug.LogError("Boss input probe FAIL: altar not selected by detector"); Restore(); yield break; }
            Debug.Log("Boss input probe READY: actual detector selected altar; waiting for interact input");
            deadline = Time.realtimeSinceStartup + 30f;
            while (Time.realtimeSinceStartup < deadline && bossField.GetValue(altar) == null) yield return null;
            activated = bossField.GetValue(altar) != null;
            Debug.Log($"Boss input probe {(activated ? "PASS" : "FAIL")}: boss spawned after external interaction input");
        }
        else activated = altar.Interact();
        var bosses = (List<Enemy>)bossField.GetValue(altar);
        if (!activated || bosses == null || bosses.Count == 0) { Debug.LogError("Boss combat probe: altar failed"); Restore(); yield break; }
        var boss = bosses[0];
        if (isolateTargetLaser)
        {
            var controller = typeof(Enemy).GetField("specialAttackController", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(boss) as SpecialAttackController;
            var attacks = controller == null ? null : typeof(SpecialAttackController).GetField("attacks", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(controller) as HashSet<EnemySpecialAttack>;
            if (attacks == null || attacks.RemoveWhere(a => a.attackName != "LaserTargetAttack") < 1 || attacks.Count != 1)
            { Debug.LogError("Boss target laser probe INCONCLUSIVE: attack configuration unavailable"); Restore(); yield break; }
            foreach (var playerCollider in player.GetComponentsInChildren<Collider>())
                foreach (var bossCollider in boss.GetComponentsInChildren<Collider>())
                    if (playerCollider != null && bossCollider != null) Physics.IgnoreCollision(playerCollider, bossCollider, true);
            typeof(SpecialAttackController).GetField("nextCheckTime", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(controller, 0f);
            Debug.Log("Boss target laser probe: other enemies cleared, only target laser selected, direct boss contact ignored");
        }
        int hitsOnPlayer = 0, hitsOnBoss = 0;
        Enemy.A_Damage += OnBossDamage; PlayerHealth.A_TakeDamage += OnPlayerDamage;
        var field = typeof(TargetOfInterestPrefab).GetField("enemy", BindingFlags.Instance | BindingFlags.NonPublic);
        float maxHp = boss.hp; float minHp = maxHp; TargetOfInterestPrefab row = null;
        var observedAttacks = new HashSet<string>();
        float nextLaserSample = 0f; int laserSamples = 0, laserPlayerRays = 0; float closestLaserAngle = 180f;
        AndroidLaserTriggerObserver laserTriggerObserver = null;
        bool positionedInLaser = false;
        Debug.Log($"Boss combat probe: spawned={boss.enemyData.enemyName}, hp={boss.hp:F2}, pos={boss.transform.position}, player={player.transform.position}");
        deadline = Time.realtimeSinceStartup + 35f;
        while (Time.realtimeSinceStartup < deadline && boss != null && boss.gameObject.activeInHierarchy)
        {
            if (attackMonitor && PoolManager.Instance != null && PoolManager.Instance.enemyAttacksPools != null)
                foreach (var pair in PoolManager.Instance.enemyAttacksPools)
                    if (pair.Value != null && pair.Value.CountActive > 0 && observedAttacks.Add(pair.Key))
                        Debug.Log($"Boss combat probe special attack: name={pair.Key}, active={pair.Value.CountActive}");
            if (isolateTargetLaser && Time.realtimeSinceStartup >= nextLaserSample)
            {
                nextLaserSample = Time.realtimeSinceStartup + .5f;
                var attack = FindFirstObjectByType<EnemySpecialAttackTargetLaser>();
                if (attack != null && attack.laser != null)
                {
                    if (laserTriggerObserver == null) laserTriggerObserver = attack.gameObject.AddComponent<AndroidLaserTriggerObserver>();
                    if (forceLaserIntersection && !positionedInLaser && attack.laser.localScale.z > 10f)
                    {
                        var body = player.playerMovement.GetRb();
                        Vector3 destination = attack.laser.position + attack.laser.forward * (attack.laser.localScale.z * .5f);
                        body.position = destination; body.transform.position = destination;
                        body.velocity = Vector3.zero; body.constraints = RigidbodyConstraints.FreezeAll; Physics.SyncTransforms();
                        positionedInLaser = true;
                        Debug.Log($"Boss target laser probe forced intersection: requested={destination}, player={body.position}, length={attack.laser.localScale.z:F2}");
                    }
                    Vector3 delta = player.transform.position - attack.laser.position;
                    float angle = Vector3.Angle(attack.laser.forward, delta);
                    bool playerRay = Physics.Raycast(attack.laser.position, attack.laser.forward, attack.laser.localScale.z, GameManager.Instance.whatIsPlayer);
                    laserSamples++; if (playerRay) laserPlayerRays++; closestLaserAngle = Mathf.Min(closestLaserAngle, angle);
                    Debug.Log($"Boss target laser sample: angle={angle:F2}, distance={delta.magnitude:F2}, length={attack.laser.localScale.z:F2}, playerRay={playerRay}, player={player.transform.position}, laser={attack.laser.position}");
                }
            }
            row = ui.prefabs.FirstOrDefault(r => r != null && (Enemy)field.GetValue(r) == boss);
            if (boss.hp < minHp)
            {
                minHp = boss.hp;
                Debug.Log($"Boss combat probe: natural hit hp={boss.hp:F2}, hud={(row != null && row.t_hp != null ? row.t_hp.text : "none")}, scale={(row != null && row.hpBar != null ? row.hpBar.transform.localScale.x : -1f):F4}, visible={(row != null && row.gameObject.activeInHierarchy)}");
            }
            yield return new WaitForSecondsRealtime(.2f);
        }
        Debug.Log($"Boss combat probe: finished naturalDamage={maxHp - minHp:F2}, rowFound={row != null}, bossHitEvents={hitsOnBoss}, playerHitEvents={hitsOnPlayer}");
        if (attackMonitor) Debug.Log($"Boss combat probe special attacks observed={string.Join(",", observedAttacks)}");
        if (isolateTargetLaser) Debug.Log($"Boss target laser probe samples={laserSamples}, closestAngle={closestLaserAngle:F2}, playerRays={laserPlayerRays}, playerHits={hitsOnPlayer}, triggerEvents={(laserTriggerObserver != null ? laserTriggerObserver.PlayerTriggers : -1)}");
        Enemy.A_Damage -= OnBossDamage; PlayerHealth.A_TakeDamage -= OnPlayerDamage;
        Restore();
        void OnBossDamage(Enemy damaged, DamageContainer dc) { if (damaged != boss) return; hitsOnBoss++; Debug.Log($"Boss combat probe: damage to boss source={dc.damageSource}, amount={dc.damage:F2}"); }
        void OnPlayerDamage(PlayerHealth health, DamageContainer dc, bool shield) { if (health != player.inventory.playerHealth) return; hitsOnPlayer++; Debug.Log($"Boss combat probe: player hit source={dc.damageSource}, attacker={(dc.enemy != null && dc.enemy.enemyData != null ? dc.enemy.enemyData.enemyName : "none")}, isBoss={(dc.enemy == boss)}, amount={dc.damage:F2}, remaining={health.hp}, shield={shield}"); }
        void Restore() { if (player != null) { invincible.SetValue(player, previousInvincible); until.SetValue(player, previousUntil); } if (manager != null) manager.enabledWaves = waves; }
    }
}

// Attached only by the opt-in observer to the attack's own Rigidbody object.
internal sealed class AndroidLaserTriggerObserver : MonoBehaviour
{
    public int PlayerTriggers { get; private set; }
    void OnTriggerStay(Collider other)
    {
        if (other == null || other.GetComponentInParent<MyPlayer>() == null) return;
        PlayerTriggers++;
        if (PlayerTriggers <= 3) Debug.Log($"Boss target laser trigger: collider={other.name}, layer={other.gameObject.layer}, count={PlayerTriggers}");
    }
}
#endif
