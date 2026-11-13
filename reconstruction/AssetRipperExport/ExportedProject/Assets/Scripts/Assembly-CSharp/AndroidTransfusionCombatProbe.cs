#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Reflection;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Managers;
using UnityEngine;

// Opt-in disposable run: item and health are controlled, weapons and waves run normally.
internal sealed class AndroidTransfusionCombatProbe : MonoBehaviour
{
    Enemy watched;
    int spawned, weaponHits, markHits, maxStacks;
    float markDamage;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_transfusion_combat_probe", false)) return;
        var root = new GameObject("transfusion combat probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidTransfusionCombatProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null || EnemyManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance;
        if (player == null || player.inventory == null || EnemyManager.Instance == null) { Debug.LogError("Transfusion combat probe FAIL: game unavailable"); yield break; }
        player.inventory.itemInventory.AddItem(EItem.UnstableTransfusion, 4);
        typeof(MyPlayer).GetField("isInvincible", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, true);
        typeof(MyPlayer).GetField("damageCooldownOverAtTime", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, float.MaxValue);
        Enemy.A_EnemySpawned += OnSpawn; Enemy.A_Damage += OnDamage;
        Debug.Log("Transfusion combat probe READY: four items, protected player, enemy health raised");
        deadline = Time.realtimeSinceStartup + 90f;
        while (Time.realtimeSinceStartup < deadline && maxStacks == 0)
        {
            if (watched != null && watched.debuffs.TryGetValue(EDebuff.Bloodmark, out var mark)) maxStacks = Mathf.Max(maxStacks, mark.GetStacks());
            yield return null;
        }
        foreach (var weapon in player.inventory.weaponInventory.weapons.Values) weapon.Disable();
        Debug.Log($"Transfusion combat probe OBSERVED: enemy={(watched != null ? watched.GetInstanceID() : 0)}, hits={weaponHits}, stacks={maxStacks}");
        while (Time.realtimeSinceStartup < deadline && watched != null && !watched.IsDead() && markHits == 0) yield return null;
        bool survived = watched != null && !watched.IsDead() && watched.gameObject.activeInHierarchy;
        bool pass = spawned > 0 && weaponHits > 0 && maxStacks > 0 && markHits > 0 && markDamage > 0f && survived;
        Debug.Log($"Transfusion combat probe {(pass ? "PASS" : "INCONCLUSIVE")}: spawned={spawned} weaponHits={weaponHits} stacks={maxStacks} sameEnemyMarkHits={markHits} markDamage={markDamage:F2} survived={survived}");
        Enemy.A_EnemySpawned -= OnSpawn; Enemy.A_Damage -= OnDamage;
    }
    void OnSpawn(Enemy enemy) { if (enemy == null || enemy.IsBoss()) return; enemy.SetDiagnosticHealth(1000f); spawned++; }
    void OnDamage(Enemy enemy, DamageContainer dc)
    {
        if (enemy == null || dc == null) return;
        if (dc.damageSource == "FireStaff") { weaponHits++; if (watched == null) watched = enemy; }
        if (enemy != watched || dc.damageSource != "Bloodmark") return;
        markHits++; markDamage += dc.damage;
        Debug.Log($"Transfusion combat probe MARK DAMAGE: enemy={enemy.GetInstanceID()} damage={dc.damage:F2} source={dc.damageSource}");
    }
    void OnDestroy() { Enemy.A_EnemySpawned -= OnSpawn; Enemy.A_Damage -= OnDamage; }
}
#endif
