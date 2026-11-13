#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Reflection;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using UnityEngine;

// Adds one item and observes ordinary enemy contact; does not create or damage enemies.
internal sealed class AndroidElectricPlugCombatProbe : MonoBehaviour
{
    int playerHits, plugHits; float plugDamage; bool effectObserved;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_electric_plug_combat_probe", false)) return;
        var go = new GameObject("electric plug combat probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidElectricPlugCombatProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance;
        if (player == null || player.inventory == null) { Debug.LogError("Electric plug combat probe FAIL: player unavailable"); yield break; }
        player.inventory.itemInventory.AddItem(EItem.ElectricPlug);
        Enemy.A_Damage += OnEnemyDamage; PlayerHealth.A_DamagePlayerCalled += OnPlayerHit;
        Debug.Log("Electric plug combat probe READY: one injected item; waves and weapons unchanged");
        deadline = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < deadline && playerHits == 0 && player != null && !player.IsDead()) yield return null;
        yield return new WaitForSecondsRealtime(1f);
        bool pass = playerHits > 0 && plugHits > 0 && plugDamage > 0f;
        Debug.Log($"Electric plug combat probe {(pass ? "PASS" : "INCONCLUSIVE")}: playerHits={playerHits} plugHits={plugHits} totalDamage={plugDamage:F2} effectObserved={effectObserved} alive={(player != null && !player.IsDead())}");
        Enemy.A_Damage -= OnEnemyDamage; PlayerHealth.A_DamagePlayerCalled -= OnPlayerHit;
    }
    void OnPlayerHit()
    {
        playerHits++;
        var field = typeof(EffectManager).GetField("electricPlugEffect", BindingFlags.Instance | BindingFlags.NonPublic);
        var effect = EffectManager.Instance != null && field != null ? field.GetValue(EffectManager.Instance) as EffectPlayer : null;
        effectObserved |= effect != null && effect.gameObject.activeInHierarchy;
    }
    void OnEnemyDamage(Enemy enemy, DamageContainer damage)
    {
        if (damage == null || damage.damageSource != ItemElectricPlug.damageSource) return;
        plugHits++; plugDamage += damage.damage;
        Debug.Log($"Electric plug combat probe hit: enemy={(enemy != null && enemy.enemyData != null ? enemy.enemyData.enemyName.ToString() : "unknown")} damage={damage.damage:F2} element={damage.element}");
    }
    void OnDestroy() { Enemy.A_Damage -= OnEnemyDamage; PlayerHealth.A_DamagePlayerCalled -= OnPlayerHit; }
}
#endif
