#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.ConstantAttacks;
using Assets.Scripts.Managers;
using UnityEngine;

// Opt-in: grant Aegis and observe regular enemy contact and shield retaliation.
internal sealed class AndroidAegisCombatProbe : MonoBehaviour
{
    int uses, regens, regensAfterUse, hits, spawns;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_aegis_combat_probe", false)) return;
        var root = new GameObject("aegis combat probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidAegisCombatProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null || DataManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance;
        var data = DataManager.Instance != null ? DataManager.Instance.GetWeapon(EWeapon.Aegis) : null;
        if (player == null || player.inventory == null || data == null) { Debug.LogError("Aegis combat probe FAIL: player or weapon unavailable"); yield break; }
        Enemy.A_EnemySpawned += OnSpawn; Enemy.A_Damage += OnDamage;
        AegisAttack.A_Used += OnUse; AegisAttack.A_Regen += OnRegen;
        player.inventory.weaponInventory.ToggleWeapon(EWeapon.FireStaff, false);
        player.inventory.weaponInventory.AddWeapon(data, null);
        Debug.Log("Aegis combat probe READY: Aegis granted, Firestaff disabled, waves and player damage enabled, new enemy health increased");
        deadline = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < deadline && (uses == 0 || hits == 0 || regensAfterUse == 0) && player != null && !player.IsDead()) yield return null;
        Debug.Log($"Aegis combat probe {(uses > 0 && hits > 0 && regensAfterUse > 0 ? "PASS" : "INCONCLUSIVE")}: spawns={spawns} uses={uses} regens={regens} regensAfterUse={regensAfterUse} aegisHits={hits} playerDead={(player == null || player.IsDead())}");
        Unsubscribe();
    }
    void OnSpawn(Enemy enemy) { if (enemy != null && !enemy.IsBoss()) { enemy.SetDiagnosticHealth(1000f); spawns++; } }
    void OnDamage(Enemy enemy, DamageContainer dc)
    {
        if (enemy == null || dc == null || dc.damageSource != EWeapon.Aegis.ToString()) return;
        hits++; Debug.Log($"Aegis combat probe hit: enemy={enemy.GetInstanceID()} damage={dc.damage:F2}");
    }
    void OnUse(int remaining) { uses++; Debug.Log($"Aegis combat probe shield used: remaining={remaining}"); }
    void OnRegen(int amount) { regens++; if (uses > 0) regensAfterUse++; Debug.Log($"Aegis combat probe shield regenerated: amount={amount}, afterUse={uses > 0}"); }
    void Unsubscribe() { Enemy.A_EnemySpawned -= OnSpawn; Enemy.A_Damage -= OnDamage; AegisAttack.A_Used -= OnUse; AegisAttack.A_Regen -= OnRegen; }
    void OnDestroy() => Unsubscribe();
}
#endif
