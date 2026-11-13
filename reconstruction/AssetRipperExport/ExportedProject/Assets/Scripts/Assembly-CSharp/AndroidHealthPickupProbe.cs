#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using UnityEngine;

// Opt-in disposable run, with health/stats adjusted and automatic waves suppressed.
internal sealed class AndroidHealthPickupProbe : MonoBehaviour
{
    int healthEvents;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_health_pickup_probe", false)) return;
        var root = new GameObject("health pickup probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidHealthPickupProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null || PickupManager.Instance == null || !PlayerStats.HasStats())) yield return null;
        var player = MyPlayer.Instance;
        if (player == null || player.inventory == null || EnemyManager.Instance == null || PickupManager.Instance == null) { Debug.LogError("Health pickup probe FAIL: game unavailable"); yield break; }
        EnemyManager.Instance.enabledWaves = false;
        foreach (var weapon in player.inventory.weaponInventory.weapons.Values) weapon.Disable();
        typeof(MyPlayer).GetField("isInvincible", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, true);
        var inventory = player.inventory;
        float regen = PlayerStats.GetStat(EStat.HealthRegen);
        float healing = PlayerStats.GetStat(EStat.HealingMultiplier);
        inventory.statInventory.ChangeMovingStat("health probe regen", new StatModifier { stat = EStat.HealthRegen, modifyType = EStatModifyType.Flat, modification = -regen });
        inventory.statInventory.ChangeMovingStat("health probe healing", new StatModifier { stat = EStat.HealingMultiplier, modifyType = EStatModifyType.Flat, modification = 1f });
        deadline = Time.realtimeSinceStartup + 10f;
        while (Time.realtimeSinceStartup < deadline && (Mathf.Abs(PlayerStats.GetStat(EStat.HealthRegen)) > .001f || Mathf.Abs(PlayerStats.GetStat(EStat.HealingMultiplier) - healing - 1f) > .001f)) yield return null;
        var health = inventory.playerHealth; health.hp = 10;
        int expected = Mathf.Min(health.maxHp, 10 + Mathf.FloorToInt((int)(health.maxHp * .33f * PlayerStats.GetStat(EStat.PowerupBoostMultiplier)) * PlayerStats.GetStat(EStat.HealingMultiplier)));
        Pickup.A_PickupTriggered += OnPickup;
        Pickup pickup = PickupManager.Instance.SpawnPickup(EPickup.Health, player.transform.position, 1, false, 0f);
        if (pickup == null) { Debug.LogError("Health pickup probe FAIL: spawn unavailable"); yield break; }
        Debug.Log($"Health pickup probe READY: maxHp={health.maxHp}, expectedHp={expected}, regen={PlayerStats.GetStat(EStat.HealthRegen):F2}, healingMultiplier={PlayerStats.GetStat(EStat.HealingMultiplier):F2}");
        deadline = Time.realtimeSinceStartup + 45f;
        while (Time.realtimeSinceStartup < deadline && healthEvents == 0) yield return null;
        yield return null;
        bool pass = healthEvents == 1 && health.hp == expected && !pickup.gameObject.activeInHierarchy;
        Debug.Log($"Health pickup probe {(pass ? "PASS" : "INCONCLUSIVE")}: hp={health.hp}, expected={expected}, healthEvents={healthEvents}, pooled={!pickup.gameObject.activeInHierarchy}");
        Pickup.A_PickupTriggered -= OnPickup;
    }
    void OnPickup(Pickup pickup) { if (pickup != null && pickup.ePickup == EPickup.Health) healthEvents++; }
    void OnDestroy() { Pickup.A_PickupTriggered -= OnPickup; }
}
#endif
