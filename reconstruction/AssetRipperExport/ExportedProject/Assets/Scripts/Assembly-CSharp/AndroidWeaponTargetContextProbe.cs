#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using UnityEngine;

// Opt-in: the supplied fixture deals double only when target fields are correct.
internal sealed class AndroidWeaponTargetContextProbe : MonoBehaviour
{
    private string source;
    private int hits;
    private float observed;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.splittest" || !intent.Call<bool>("getBooleanExtra", "openbonk_weapon_target_probe", false)) return;
        var root = new GameObject("Weapon target context probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidWeaponTargetContextProbe>();
    }
    private IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 150f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance; var manager = EnemyManager.Instance;
        if (player == null || player.inventory == null || manager == null) { Debug.LogError("Weapon target probe INCONCLUSIVE: no player"); yield break; }
        WeaponBase weapon = null;
        foreach (var w in player.inventory.weaponInventory.weapons.Values) { if (w.weaponData is OpenBonkLuaWeaponData) weapon = w; w.Disable(); }
        if (weapon == null) { Debug.LogError("Weapon target probe INCONCLUSIVE: no external weapon"); yield break; }
        manager.enabledWaves = false;
        var target = manager.SpawnEnemy(DataManager.Instance.GetEnemyData(EEnemy.Goblin), player.transform.position + Vector3.right * 5f, -1, true, canBeElite:false);
        deadline = Time.realtimeSinceStartup + 20f;
        while (Time.realtimeSinceStartup < deadline && (target == null || !target.CanTakeDamage())) yield return null;
        if (target == null || !target.CanTakeDamage()) { Debug.LogError("Weapon target probe INCONCLUSIVE: target unavailable"); yield break; }
        target.SetDiagnosticHealth(10000f); target.enemyMovement.StopMovement();
        var body = target.GetComponent<Rigidbody>(); if (body != null) body.constraints = RigidbodyConstraints.FreezeAll;
        source = weapon.weaponData.damageSourceName; Enemy.A_Damage += OnDamage; weapon.Enable();
        deadline = Time.realtimeSinceStartup + 20f;
        while (Time.realtimeSinceStartup < deadline && hits < 1) yield return null;
        weapon.Disable(); Enemy.A_Damage -= OnDamage;
        bool pass = hits > 0 && Mathf.Abs(observed - 28f) < .05f;
        Debug.Log($"Weapon target probe {(pass ? "PASS" : "FAIL")}: enemy=Goblin noncritHits={hits} damage={observed:F3} expected=28");
    }
    private void OnDamage(Enemy enemy, DamageContainer damage)
    {
        if (enemy != null && damage != null && damage.damageSource == source && damage.procCoefficient == 1f && !damage.crit) { hits++; observed = damage.damage; }
    }
    private void OnDestroy() { Enemy.A_Damage -= OnDamage; }
}
#endif
