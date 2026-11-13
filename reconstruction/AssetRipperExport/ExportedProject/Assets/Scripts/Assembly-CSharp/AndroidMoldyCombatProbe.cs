#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Reflection;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Managers;
using UnityEngine;

// Forces inventory/health conditions but leaves actual waves and weapon attacks running.
internal sealed class AndroidMoldyCombatProbe : MonoBehaviour
{
    int generated, weaponHits, poisonHits, observedStacks; float poisonDamage;
    Enemy watched;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_moldy_combat_probe", false)) return;
        var root = new GameObject("moldy combat probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidMoldyCombatProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null || EnemyManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance;
        if (player == null || player.inventory == null || EnemyManager.Instance == null) { Debug.LogError("Moldy combat probe FAIL: game unavailable"); yield break; }
        player.inventory.itemInventory.AddItem(EItem.MoldyCheese, 5);
        typeof(MyPlayer).GetField("isInvincible", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, true);
        typeof(MyPlayer).GetField("damageCooldownOverAtTime", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, float.MaxValue);
        Enemy.A_EnemySpawned += OnSpawn; Enemy.A_Damage += OnDamage;
        Debug.Log("Moldy combat probe READY: 5 items, protected player, spawned-enemy health raised");
        deadline = Time.realtimeSinceStartup + 90f;
        while (Time.realtimeSinceStartup < deadline && poisonHits == 0)
        {
            if (watched != null && watched.debuffs.TryGetValue(EDebuff.Poison, out var poison))
                observedStacks = Mathf.Max(observedStacks, poison.GetStacks());
            yield return null;
        }
        // Stop new attacks only after the observed tick, so poison can expire naturally.
        foreach (var weapon in player.inventory.weaponInventory.weapons.Values) weapon.Disable();
        while (Time.realtimeSinceStartup < deadline && watched != null && !watched.IsDead() &&
               (watched.debuffs.ContainsKey(EDebuff.Poison) || watched.debuffsToAdd.ContainsKey(EDebuff.Poison))) yield return null;
        bool survived = watched != null && !watched.IsDead() && watched.gameObject.activeInHierarchy;
        bool expired = survived && !watched.debuffs.ContainsKey(EDebuff.Poison) && !watched.debuffsToAdd.ContainsKey(EDebuff.Poison);
        bool pass = generated > 0 && weaponHits > 0 && observedStacks >= 2 && poisonHits > 0 && poisonDamage > 0f && expired;
        Debug.Log($"Moldy combat probe {(pass ? "PASS" : "INCONCLUSIVE")}: generated={generated} weaponHits={weaponHits} stacks={observedStacks} sameEnemyPoisonHits={poisonHits} poisonDamage={poisonDamage:F2} survived={survived} expired={expired}");
        Enemy.A_EnemySpawned -= OnSpawn; Enemy.A_Damage -= OnDamage;
    }
    void OnSpawn(Enemy enemy)
    {
        if (enemy == null || enemy.IsBoss()) return;
        enemy.SetDiagnosticHealth(200f); generated++;
    }
    void OnDamage(Enemy enemy, DamageContainer dc)
    {
        if (dc == null) return;
        if (dc.damageSource == "FireStaff") { weaponHits++; if (watched == null) watched = enemy; }
        if (dc.damageSource != "Poison" || enemy != watched) return;
        poisonHits++; poisonDamage += dc.damage;
        Debug.Log($"Moldy combat probe poison: enemy={(enemy != null && enemy.enemyData != null ? enemy.enemyData.enemyName.ToString() : "unknown")} damage={dc.damage:F2} source={dc.damageSource}");
    }
    void OnDestroy() { Enemy.A_EnemySpawned -= OnSpawn; Enemy.A_Damage -= OnDamage; }
}
#endif
