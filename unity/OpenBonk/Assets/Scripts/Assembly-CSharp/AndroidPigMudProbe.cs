#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemySpecialAttacks;
using Assets.Scripts.Game.Spawning;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using UnityEngine;

// Disposable opt-in: force each original Pig mud attack and place the player in its warning.
internal sealed class AndroidPigMudProbe : MonoBehaviour
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    int hits; float knockback; string source;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install() {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_pig_mud_probe", false)) return;
        var root = new GameObject("Pig mud probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidPigMudProbe>();
    }
    void OnDamage(PlayerHealth hp, DamageContainer dc, bool shield) { hits++; knockback = dc.knockback; source = dc.damageSource; }
    IEnumerator Start() {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null || DataManager.Instance == null || PoolManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance; var manager = EnemyManager.Instance;
        if (player == null || player.inventory == null || manager == null || DataManager.Instance == null) { Debug.LogError("Pig mud probe INCONCLUSIVE: game unavailable"); yield break; }
        manager.enabledWaves = false;
        foreach (var weapon in player.inventory.weaponInventory.weapons.Values) weapon.Disable();
        var data = DataManager.Instance.GetEnemyData(EEnemy.MinibossPig);
        if (data == null) { Debug.LogError("Pig mud probe INCONCLUSIVE: pig data unavailable"); yield break; }
        var body = player.playerMovement.GetRb(); var health = player.inventory.playerHealth;
        for (int index = 0; index < 2; index++) {
            string name = index == 0 ? "PigMudSplash" : "PigMudSplashPredictive";
            var pig = manager.SpawnEnemy(data, body.position + Vector3.back * 12f, -1, true, canBeElite:false);
            if (pig == null) { Debug.LogError($"Pig mud probe INCONCLUSIVE: {name} pig spawn"); yield break; }
            deadline = Time.realtimeSinceStartup + 30f;
            while (Time.realtimeSinceStartup < deadline && pig != null && pig.IsTeleporting()) yield return null;
            if (pig == null || pig.IsTeleporting()) { Debug.LogError($"Pig mud probe INCONCLUSIVE: {name} teleport"); yield break; }
            pig.DisableSpecialAttacks();
            var controller = typeof(Enemy).GetField("specialAttackController", Private).GetValue(pig) as SpecialAttackController;
            var attacks = controller == null ? null : typeof(SpecialAttackController).GetField("attacks", Private).GetValue(controller) as HashSet<EnemySpecialAttack>;
            if (attacks == null) { Debug.LogError("Pig mud probe INCONCLUSIVE: controller missing"); yield break; }
            attacks.RemoveWhere(a => a.attackName != name);
            if (attacks.Count != 1) { Debug.LogError($"Pig mud probe INCONCLUSIVE: {name} missing"); yield break; }
            EnemySpecialAttack config = null; foreach (var a in attacks) config = a;
            // Shipped Pig mud prefabs do not enable grounding. Force this path
            // only in the disposable diagnostic to exercise the ground mask.
            config.attackPrefab.GetComponent<EnemyProjectileMud>().grounded = true;
            typeof(MyPlayer).GetField("isInvincible", Private).SetValue(player, false);
            typeof(MyPlayer).GetField("damageCooldownOverAtTime", Private).SetValue(player, 0f);
            health.hp = health.maxHp; health.shield = 0f; player.inventory.statusEffects.RemoveAllStatusEffects();
            yield return new WaitForFixedUpdate();
            float baseSpeed = player.inventory.playerStats.GetStat(EStat.MoveSpeedMultiplier);
            body.constraints = RigidbodyConstraints.None; body.velocity = index == 0 ? Vector3.zero : Vector3.right * 2f;
            float inputSpeed = body.velocity.x;
            if (index == 1 && Mathf.Abs(inputSpeed - 2f) > .01f) { Debug.LogError($"Pig mud probe INCONCLUSIVE: predictive speed={inputSpeed}"); yield break; }
            Vector3 expected = player.transform.position + (index == 0 ? Vector3.zero : body.velocity * config.attackChargeTime);
            var groundHits = Physics.RaycastAll(expected + Vector3.up * 99f, Vector3.down, 999f, GameManager.Instance.whatIsGround);
            float expectedHeight = groundHits.Length > 0 ? SpawnPositions.FindHitClosestToPlayerY(groundHits, out _).point.y : expected.y;
            pig.enemyMovement.distanceToTarget = 12f;
            typeof(SpecialAttackController).GetField("nextCheckTime", Private).SetValue(controller, 0f);
            hits = 0; PlayerHealth.A_TakeDamage += OnDamage; controller.Tick();
            if (!PoolManager.Instance.enemyAttacksPools.TryGetValue(name, out var pool) || pool.CountActive == 0) { Debug.LogError($"Pig mud probe FAIL: {name} did not start"); PlayerHealth.A_TakeDamage -= OnDamage; yield break; }
            EnemyProjectileMud mud = null; foreach (var item in FindObjectsByType<EnemyProjectileMud>(FindObjectsSortMode.None)) if (item.gameObject.activeInHierarchy && item.predictive == (index == 1)) { mud = item; break; }
            if (mud == null) { Debug.LogError($"Pig mud probe INCONCLUSIVE: {name} object missing"); PlayerHealth.A_TakeDamage -= OnDamage; yield break; }
            float distance = Vector2.Distance(new Vector2(expected.x, expected.z), new Vector2(mud.transform.position.x, mud.transform.position.z));
            float heightError = Mathf.Abs(expectedHeight - mud.transform.position.y);
            body.position = mud.transform.position + Vector3.up; body.velocity = Vector3.zero; body.constraints = RigidbodyConstraints.FreezeAll; Physics.SyncTransforms();
            int initialHp = health.hp; deadline = Time.realtimeSinceStartup + 35f;
            while (Time.realtimeSinceStartup < deadline && hits == 0) yield return null;
            bool slow = player.inventory.statusEffects.HasStatusEffect(EStatusEffect.Slow); int finalHp = health.hp;
            float appliedTime = slow ? player.inventory.statusEffects.statusEffects[EStatusEffect.Slow].addedTime : 0f;
            deadline = Time.realtimeSinceStartup + 5f;
            while (Time.realtimeSinceStartup < deadline && slow && player.inventory.statusEffects.HasStatusEffect(EStatusEffect.Slow) && Mathf.Abs(player.inventory.playerStats.GetStat(EStat.MoveSpeedMultiplier) - baseSpeed * .5f) > .01f) yield return null;
            float slowedSpeed = player.inventory.playerStats.GetStat(EStat.MoveSpeedMultiplier);
            PlayerHealth.A_TakeDamage -= OnDamage;
            deadline = Time.realtimeSinceStartup + 35f;
            while (Time.realtimeSinceStartup < deadline && player.inventory.statusEffects.HasStatusEffect(EStatusEffect.Slow)) yield return null;
            float elapsed = MyTime.time - appliedTime;
            bool expired = !player.inventory.statusEffects.HasStatusEffect(EStatusEffect.Slow);
            deadline = Time.realtimeSinceStartup + 5f;
            while (Time.realtimeSinceStartup < deadline && expired && Mathf.Abs(player.inventory.playerStats.GetStat(EStat.MoveSpeedMultiplier) - baseSpeed) > .01f) yield return null;
            float restoredSpeed = player.inventory.playerStats.GetStat(EStat.MoveSpeedMultiplier);
            deadline = Time.realtimeSinceStartup + 25f;
            while (Time.realtimeSinceStartup < deadline && pool.CountActive > 0) yield return null;
            bool speedOk = Mathf.Abs(slowedSpeed - baseSpeed * .5f) < .01f && Mathf.Abs(restoredSpeed - baseSpeed) < .01f;
            bool ok = groundHits.Length > 0 && distance < .1f && heightError < .1f && hits == 1 && finalHp < initialHp && slow && expired && elapsed >= 3f && elapsed < 4f && speedOk && Mathf.Abs(knockback - 8f) < .01f && source == "Enemy" && pool.CountActive == 0;
            Debug.Log($"Pig mud probe {(ok ? "PASS" : "FAIL")}: attack={name}, inputSpeed={inputSpeed:F2}, targetError={distance:F3}, groundHits={groundHits.Length}, heightError={heightError:F3}, hits={hits}, hp={initialHp}->{finalHp}, slow={slow}, expired={expired}, slowSeconds={elapsed:F2}, speed={baseSpeed:F3}->{slowedSpeed:F3}->{restoredSpeed:F3}, knockback={knockback}, source={source}, poolActive={pool.CountActive}");
            if (!ok) yield break;
            if (pig.gameObject.activeInHierarchy) pig.ReleaseToPool();
        }
    }
    void OnDestroy() { PlayerHealth.A_TakeDamage -= OnDamage; }
}
#endif
