#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemyDebuffs.Implementations;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Managers;
using UnityEngine;

// Opt-in disposable run: observe the granted glove during ordinary wave combat.
internal sealed class AndroidPoisonGloveCombatProbe : MonoBehaviour
{
    int gloveHits, poisonTicks;
    readonly HashSet<int> targets = new HashSet<int>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_poison_glove_combat_probe", false)) return;
        var root = new GameObject("poison glove combat probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidPoisonGloveCombatProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance;
        if (player == null || player.inventory == null || EnemyManager.Instance == null) { Debug.LogError("Poison glove combat probe FAIL: player or enemy manager unavailable"); yield break; }
        player.inventory.itemInventory.AddItem(EItem.GlovePoison);
        typeof(MyPlayer).GetField("isInvincible", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, true);
        typeof(MyPlayer).GetField("damageCooldownOverAtTime", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, float.MaxValue);
        Enemy.A_EnemySpawned += OnSpawn;
        Enemy.A_Damage += OnDamage;
        Debug.Log("Poison glove combat probe READY: one granted glove, normal waves and attacks, player protected, spawned enemy health increased");
        deadline = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < deadline && (gloveHits == 0 || poisonTicks == 0)) yield return null;
        Debug.Log($"Poison glove combat probe {(gloveHits > 0 && poisonTicks > 0 ? "PASS" : "INCONCLUSIVE")}: gloveHits={gloveHits}, poisonTicks={poisonTicks}, distinctTargets={targets.Count}");
        Enemy.A_EnemySpawned -= OnSpawn; Enemy.A_Damage -= OnDamage;
    }
    void OnSpawn(Enemy enemy) { if (enemy != null && !enemy.IsBoss()) enemy.SetDiagnosticHealth(1000f); }
    void OnDamage(Enemy enemy, DamageContainer damage)
    {
        if (enemy == null || damage == null) return;
        if (damage.damageSource == EItem.GlovePoison.ToString()) gloveHits++;
        else if (damage.damageSource == DebuffPoison.poisonDamageSource) poisonTicks++;
        else return;
        targets.Add(enemy.GetInstanceID());
        Debug.Log($"Poison glove combat probe hit: source={damage.damageSource}, enemy={enemy.GetInstanceID()}, damage={damage.damage:F2}");
    }
    void OnDestroy() { Enemy.A_EnemySpawned -= OnSpawn; Enemy.A_Damage -= OnDamage; }
}
#endif
