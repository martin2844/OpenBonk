#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemySpecialAttacks;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Managers;
using Assets.Scripts.Objects.Pooling;
using UnityEngine;

// Disposable opt-in test: force one real Pig attack while holding the player in range.
internal sealed class AndroidPigSlamProbe : MonoBehaviour
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    int hits; float knockback; string damageSource;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install() {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_pig_slam_probe", false)) return;
        var root = new GameObject("Pig slam probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidPigSlamProbe>();
    }
    void OnDamage(PlayerHealth hp, DamageContainer dc, bool shield) { hits++; knockback = dc.knockback; damageSource = dc.damageSource; }
    IEnumerator Start() {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null || DataManager.Instance == null || PoolManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance; var manager = EnemyManager.Instance;
        if (player == null || player.inventory == null || manager == null || DataManager.Instance == null) { Debug.LogError("Pig slam probe INCONCLUSIVE: game unavailable"); yield break; }
        manager.enabledWaves = false;
        foreach (var weapon in player.inventory.weaponInventory.weapons.Values) weapon.Disable();
        var data = DataManager.Instance.GetEnemyData(EEnemy.MinibossPig);
        var pig = data == null ? null : manager.SpawnEnemy(data, player.transform.position + Vector3.back * 8f, -1, true, canBeElite:false);
        if (pig == null) { Debug.LogError("Pig slam probe INCONCLUSIVE: pig spawn failed"); yield break; }
        deadline = Time.realtimeSinceStartup + 30f;
        while (Time.realtimeSinceStartup < deadline && pig != null && pig.IsTeleporting()) yield return null;
        if (pig == null || pig.IsTeleporting()) { Debug.LogError("Pig slam probe INCONCLUSIVE: teleport incomplete"); yield break; }
        var controller = typeof(Enemy).GetField("specialAttackController", Private).GetValue(pig) as SpecialAttackController;
        var attacks = controller == null ? null : typeof(SpecialAttackController).GetField("attacks", Private).GetValue(controller) as HashSet<EnemySpecialAttack>;
        if (attacks == null || attacks.RemoveWhere(a => a.attackName != "SphereSlam") < 1 || attacks.Count != 1) { Debug.LogError("Pig slam probe INCONCLUSIVE: attack config unavailable"); yield break; }
        var body = player.playerMovement.GetRb(); var health = player.inventory.playerHealth;
        typeof(MyPlayer).GetField("isInvincible", Private).SetValue(player, false);
        typeof(MyPlayer).GetField("damageCooldownOverAtTime", Private).SetValue(player, 0f);
        health.hp = health.maxHp; health.shield = 0f;
        pig.transform.rotation = Quaternion.LookRotation(Vector3.forward);
        body.position = pig.GetFeetPosition() + Vector3.forward * 11f + Vector3.up; body.velocity = Vector3.zero; body.constraints = RigidbodyConstraints.FreezeAll; Physics.SyncTransforms();
        pig.enemyMovement.distanceToTarget = 11f;
        typeof(SpecialAttackController).GetField("nextCheckTime", Private).SetValue(controller, 0f);
        int initialHp = health.hp; PlayerHealth.A_TakeDamage += OnDamage;
        controller.Tick();
        if (!PoolManager.Instance.enemyAttacksPools.TryGetValue("SphereSlam", out var pool) || pool.CountActive == 0) { Debug.LogError("Pig slam probe FAIL: attack did not start"); PlayerHealth.A_TakeDamage -= OnDamage; yield break; }
        int warnings = PoolManager.Instance.warningSpherePool.CountActive;
        // Capture the actual warning in the normal gameplay camera before the
        // scheduled attack completes; do not change its renderer or position.
        yield return null;
        yield return new WaitForEndOfFrame();
        var activeWarnings = FindObjectsOfType<CircleWarning>();
        if (activeWarnings.Length > 0 && Camera.main != null)
        {
            var warning = activeWarnings[0];
            var viewport = Camera.main.WorldToViewportPoint(warning.transform.position);
            var capture = ScreenCapture.CaptureScreenshotAsTexture();
            if (capture != null)
            {
                System.IO.File.WriteAllBytes(Application.persistentDataPath + "/pig-slam-warning-gameplay.png", capture.EncodeToPNG());
                Destroy(capture);
                Debug.Log($"Pig slam warning capture: active={activeWarnings.Length}, viewport={viewport}, scale={warning.filler.localScale.x:F2}");
            }
        }
        else Debug.Log($"Pig slam warning capture INCONCLUSIVE: active={activeWarnings.Length}, camera={(Camera.main != null)}");
        deadline = Time.realtimeSinceStartup + 35f;
        while (Time.realtimeSinceStartup < deadline && hits == 0) yield return null;
        int finalHp = health.hp;
        deadline = Time.realtimeSinceStartup + 20f;
        while (Time.realtimeSinceStartup < deadline && pool.CountActive > 0) yield return null;
        bool success = warnings > 0 && hits == 1 && finalHp < initialHp && Mathf.Abs(knockback - 20f) < .01f && damageSource == "Enemy" && pool.CountActive == 0;
        Debug.Log($"Pig slam probe {(success ? "PASS" : "FAIL")}: realPig=true, warningStart={warnings}, hits={hits}, hp={initialHp}->{finalHp}, knockback={knockback}, source={damageSource}, poolActive={pool.CountActive}");
        PlayerHealth.A_TakeDamage -= OnDamage;
    }
    void OnDestroy() { PlayerHealth.A_TakeDamage -= OnDamage; }
}
#endif
