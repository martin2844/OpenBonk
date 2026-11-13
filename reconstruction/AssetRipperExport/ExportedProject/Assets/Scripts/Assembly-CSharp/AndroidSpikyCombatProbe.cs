#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using UnityEngine;

// Intent-gated: force an item grant, but observe ordinary enemy contact and damage.
internal sealed class AndroidSpikyCombatProbe : MonoBehaviour
{
    int hits, thornsHits, hitsAfterRemoval, thornsAfterRemoval;
    bool removed;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_spiky_combat_probe", false)) return;
        var root = new GameObject("Spiky combat probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidSpikyCombatProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance;
        if (player == null || player.inventory == null || EnemyManager.Instance == null) { Debug.LogError("Spiky combat probe FAIL: game unavailable"); yield break; }
        Enemy.A_EnemySpawned += OnSpawn; Enemy.A_Damage += OnEnemyDamage; PlayerHealth.A_TakeDamage += OnPlayerDamage;
        player.inventory.weaponInventory.ToggleWeapon(EWeapon.FireStaff, false);
        player.inventory.itemInventory.AddItem(EItem.SpikyShield);
        Debug.Log("Spiky combat probe READY: shield granted, Firestaff disabled, waves and player damage enabled, new enemy health increased");
        deadline = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < deadline && thornsHits == 0 && player != null && !player.IsDead()) yield return null;
        if (thornsHits == 0 || player == null || player.IsDead()) { Debug.Log("Spiky combat probe INCONCLUSIVE: no live retaliation"); Unsubscribe(); yield break; }
        player.inventory.itemInventory.RemoveItem(EItem.SpikyShield, false);
        yield return new WaitForSecondsRealtime(.5f);
        removed = true;
        Debug.Log($"Spiky combat probe removed: thorns={player.inventory.playerStats.GetRawStat(EStat.Thorns):F2}");
        deadline = Time.realtimeSinceStartup + 45f;
        while (Time.realtimeSinceStartup < deadline && hitsAfterRemoval == 0 && player != null && !player.IsDead()) yield return null;
        yield return null;
        bool pass = hits > 0 && thornsHits > 0 && hitsAfterRemoval > 0 && thornsAfterRemoval == 0 && player != null && Mathf.Abs(player.inventory.playerStats.GetRawStat(EStat.Thorns)) < .001f;
        Debug.Log($"Spiky combat probe {(pass ? "PASS" : "INCONCLUSIVE")}: hitsBefore={hits}, thornsBefore={thornsHits}, hitsAfter={hitsAfterRemoval}, thornsAfter={thornsAfterRemoval}");
        Unsubscribe();
    }
    void OnSpawn(Enemy enemy) { if (enemy != null && !enemy.IsBoss()) enemy.SetDiagnosticHealth(1000f); }
    void OnPlayerDamage(PlayerHealth health, DamageContainer damage, bool shield)
    {
        if (damage == null || damage.enemy == null) return;
        if (removed) hitsAfterRemoval++; else hits++;
        Debug.Log($"Spiky combat probe player hit: removed={removed}, enemy={damage.enemy.GetInstanceID()}, damage={damage.damage:F2}");
    }
    void OnEnemyDamage(Enemy enemy, DamageContainer damage)
    {
        if (enemy == null || damage == null || damage.damageSource != PlayerHealth.thornsDamageSource) return;
        if (removed) thornsAfterRemoval++; else thornsHits++;
        Debug.Log($"Spiky combat probe thorns: removed={removed}, enemy={enemy.GetInstanceID()}, damage={damage.damage:F2}");
    }
    void Unsubscribe() { Enemy.A_EnemySpawned -= OnSpawn; Enemy.A_Damage -= OnEnemyDamage; PlayerHealth.A_TakeDamage -= OnPlayerDamage; }
    void OnDestroy() => Unsubscribe();
}
#endif
