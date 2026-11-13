#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemyDebuffs.Implementations;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Managers;
using UnityEngine;

// Grants the item, then observes ordinary enemies, attacks, and player damage.
internal sealed class AndroidToxicBarrelCombatProbe : MonoBehaviour
{
    int playerHits, uses, poisonTicks;
    float poisonDamage;
    readonly HashSet<int> poisonedTargets = new HashSet<int>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_toxic_barrel_combat_probe", false)) return;
        var root = new GameObject("toxic barrel combat probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidToxicBarrelCombatProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance;
        if (player == null || player.inventory == null) { Debug.LogError("Toxic barrel combat probe FAIL: player unavailable"); yield break; }
        player.inventory.itemInventory.AddItem(EItem.ToxicBarrel);
        PlayerHealth.A_TakeDamage += OnPlayerDamage;
        ItemToxicBarrel.A_OnUse += OnUse;
        Enemy.A_Damage += OnEnemyDamage;
        Debug.Log("Toxic barrel combat probe READY: one granted item; normal waves and combat");
        deadline = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < deadline && poisonTicks < 2 && player != null && !player.IsDead()) yield return null;
        Debug.Log($"Toxic barrel combat probe {(playerHits > 0 && uses > 0 && poisonTicks > 0 ? "PASS" : "INCONCLUSIVE")}: playerHits={playerHits}, uses={uses}, poisonTicks={poisonTicks}, poisonDamage={poisonDamage:F2}, distinctTargets={poisonedTargets.Count}");
        Unsubscribe();
    }
    void OnPlayerDamage(PlayerHealth health, DamageContainer damage, bool shieldDamage) { playerHits++; }
    void OnUse(float radius) { uses++; Debug.Log($"Toxic barrel combat probe use: radius={radius:F2}, playerHits={playerHits}"); }
    void OnEnemyDamage(Enemy enemy, DamageContainer damage)
    {
        if (enemy == null || damage == null || damage.damageSource != DebuffPoison.poisonDamageSource) return;
        poisonTicks++; poisonDamage += damage.damage; poisonedTargets.Add(enemy.GetInstanceID());
        Debug.Log($"Toxic barrel combat probe poison: enemy={enemy.GetInstanceID()}, damage={damage.damage:F2}");
    }
    void Unsubscribe() { PlayerHealth.A_TakeDamage -= OnPlayerDamage; ItemToxicBarrel.A_OnUse -= OnUse; Enemy.A_Damage -= OnEnemyDamage; }
    void OnDestroy() => Unsubscribe();
}
#endif
