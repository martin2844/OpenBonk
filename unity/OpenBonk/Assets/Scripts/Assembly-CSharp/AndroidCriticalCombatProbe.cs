#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Reflection;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using UnityEngine;

// Disposable run: natural waves and Firestaff hits with controlled crit stat and health.
internal sealed class AndroidCriticalCombatProbe : MonoBehaviour
{
    int normal, critical, incorrect, spawned;
    WeaponBase weapon;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_critical_combat_probe", false)) return;
        var root = new GameObject("critical combat probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidCriticalCombatProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance;
        if (player == null || player.inventory == null || !player.inventory.weaponInventory.weapons.TryGetValue(EWeapon.FireStaff, out weapon)) { Debug.LogError("Critical combat probe FAIL: Firestaff unavailable"); yield break; }
        player.inventory.statInventory.ChangeMovingStat("critical combat probe", new StatModifier { stat = EStat.CritChance, modifyType = EStatModifyType.Flat, modification = .5f });
        typeof(MyPlayer).GetField("isInvincible", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, true);
        typeof(MyPlayer).GetField("damageCooldownOverAtTime", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, float.MaxValue);
        Enemy.A_EnemySpawned += OnSpawn; Enemy.A_Damage += OnDamage;
        deadline = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < deadline && (normal < 2 || critical < 2)) yield return null;
        bool pass = spawned > 0 && normal >= 2 && critical >= 2 && incorrect == 0;
        Debug.Log($"Critical combat probe {(pass ? "PASS" : "INCONCLUSIVE")}: spawned={spawned}, normal={normal}, critical={critical}, mismatchedDamage={incorrect}, chance={WeaponUtility.GetCritChance(weapon):F3}");
        Enemy.A_EnemySpawned -= OnSpawn; Enemy.A_Damage -= OnDamage;
    }
    void OnSpawn(Enemy enemy)
    {
        if (enemy == null || enemy.IsBoss()) return;
        enemy.SetDiagnosticHealth(1000f); spawned++;
    }
    void OnDamage(Enemy enemy, DamageContainer dc)
    {
        if (enemy == null || dc == null || dc.damageSource != "FireStaff" || enemy.IsElite() || enemy.IsBoss()) return;
        float baseline = WeaponUtility.GetDamage(weapon);
        float expected = baseline * (dc.crit ? 2f * WeaponUtility.GetCritDamageMultiplier(weapon) : 1f);
        if (Mathf.Abs(dc.damage - expected) > .02f) incorrect++;
        if (dc.crit) critical++; else normal++;
        Debug.Log($"Critical combat probe hit: crit={dc.crit}, damage={dc.damage:F2}, expected={expected:F2}, enemy={enemy.enemyData.enemyName}");
    }
    void OnDestroy() { Enemy.A_EnemySpawned -= OnSpawn; Enemy.A_Damage -= OnDamage; }
}
#endif
