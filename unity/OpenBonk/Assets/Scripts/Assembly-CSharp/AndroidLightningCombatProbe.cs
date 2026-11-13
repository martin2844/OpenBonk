#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using UnityEngine;

// Disposable run: grant Lightning Staff and observe real wave targets and weapon ticks.
internal sealed class AndroidLightningCombatProbe : MonoBehaviour
{
    int primary, chained, badElement;
    readonly HashSet<int> targets = new HashSet<int>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_lightning_combat_probe", false)) return;
        var root = new GameObject("lightning combat probe");
        DontDestroyOnLoad(root);
        root.AddComponent<AndroidLightningCombatProbe>();
    }

    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance;
        var data = DataManager.Instance != null ? DataManager.Instance.GetWeapon(EWeapon.LightningStaff) : null;
        if (player == null || player.inventory == null || data == null) { Debug.LogError("Lightning combat probe FAIL: player or weapon unavailable"); yield break; }
        player.inventory.weaponInventory.ToggleWeapon(EWeapon.FireStaff, false);
        player.inventory.statInventory.ChangeMovingStat("lightning combat probe", new StatModifier { stat = EStat.ProjectileBounces, modifyType = EStatModifyType.Flat, modification = 3f });
        typeof(MyPlayer).GetField("isInvincible", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, true);
        typeof(MyPlayer).GetField("damageCooldownOverAtTime", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, float.MaxValue);
        player.inventory.weaponInventory.AddWeapon(data, null);
        Enemy.A_Damage += OnDamage;
        Debug.Log("Lightning combat probe READY: weapon and bounce stat injected, ordinary waves active");
        deadline = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < deadline && (primary < 2 || chained < 2)) yield return null;
        Debug.Log($"Lightning combat probe {(primary >= 2 && chained >= 2 && badElement == 0 ? "PASS" : "INCONCLUSIVE")}: primary={primary}, chained={chained}, distinctTargets={targets.Count}, badElement={badElement}");
        Enemy.A_Damage -= OnDamage;
    }

    void OnDamage(Enemy enemy, DamageContainer damage)
    {
        if (enemy == null || damage == null || damage.damageSource != EWeapon.LightningStaff.ToString()) return;
        if (damage.procCoefficient == .5f) chained++; else if (damage.procCoefficient == 1f) primary++;
        if (damage.element != Assets.Scripts.Game.Combat.EElement.Lightning) badElement++;
        targets.Add(enemy.GetInstanceID());
        Debug.Log($"Lightning combat probe hit: enemy={enemy.GetInstanceID()}, damage={damage.damage:F2}, proc={damage.procCoefficient:F2}, element={damage.element}");
    }
    void OnDestroy() { Enemy.A_Damage -= OnDamage; }
}
#endif
