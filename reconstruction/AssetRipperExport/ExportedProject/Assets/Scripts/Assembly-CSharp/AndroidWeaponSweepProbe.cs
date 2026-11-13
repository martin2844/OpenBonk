#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Reflection;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Attacks;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using Assets.Scripts.Managers;
using UnityEngine;

// Diagnostic only: grant each weapon and observe the regular attack loop.
internal sealed class AndroidWeaponSweepProbe : MonoBehaviour
{
    int shots, hits;
    string current;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_weapon_sweep_probe", false)) return;
        var root = new GameObject("weapon sweep probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidWeaponSweepProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null || DataManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance;
        if (player == null || player.inventory == null || EnemyManager.Instance == null) { Debug.LogError("Weapon sweep probe FAIL: game not ready"); yield break; }
        typeof(MyPlayer).GetField("isInvincible", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, true);
        typeof(MyPlayer).GetField("damageCooldownOverAtTime", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, float.MaxValue);
        Enemy.A_EnemySpawned += OnSpawn; Enemy.A_Damage += OnDamage;
        WeaponAttack.A_SpawnedProjectileSuccessfully += OnShot;
        var inv = player.inventory.weaponInventory;
        foreach (var weapon in inv.weapons.Values) weapon.Disable();
        int tested = 0;
        foreach (var data in DataManager.Instance.GetAllWeapons())
        {
            if (data == null || !data.isEnabled) continue;
            if (!inv.weapons.ContainsKey(data.eWeapon)) inv.AddWeapon(data, null);
            current = data.eWeapon.ToString(); shots = hits = 0;
            inv.ToggleWeapon(data.eWeapon, true);
            Debug.Log("Weapon sweep probe START: weapon=" + current);
            yield return new WaitForSecondsRealtime(7f);
            inv.ToggleWeapon(data.eWeapon, false);
            Debug.Log($"Weapon sweep probe RESULT: weapon={current} projectiles={shots} matchingDamageEvents={hits}");
            tested++;
        }
        Debug.Log("Weapon sweep probe COMPLETE: tested=" + tested);
        Unsubscribe();
    }
    void OnSpawn(Enemy enemy) { if (enemy != null && !enemy.IsBoss()) enemy.SetDiagnosticHealth(1000f); }
    void OnShot(ProjectileBase projectile) { if (projectile != null) shots++; }
    void OnDamage(Enemy enemy, DamageContainer dc) { if (enemy != null && dc != null && dc.damageSource == current) hits++; }
    void Unsubscribe() { Enemy.A_EnemySpawned -= OnSpawn; Enemy.A_Damage -= OnDamage; WeaponAttack.A_SpawnedProjectileSuccessfully -= OnShot; }
    void OnDestroy() => Unsubscribe();
}
#endif
