#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using UnityEngine;

// Opt-in isolated-emulator fixture. It forces a death after a real on-hit event,
// then observes the rows created by the real results screen.
internal sealed class AndroidModDamageResultsProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.splittest" || !intent.Call<bool>("getBooleanExtra", "openbonk_mod_results_probe", false)) return;
        var go = new GameObject("Mod results probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidModDamageResultsProbe>();
    }
    private IEnumerator Start()
    {
        float until = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < until && (GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || EnemyManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance; var manager = EnemyManager.Instance;
        var id = (EItem)OpenBonkContentIds.Item("openbonk-item-on-hit", "training-cell");
        if (player == null || player.inventory == null || manager == null || !DataManager.Instance.TryGetItem(id, out var item)) { Debug.LogError("Mod results INCONCLUSIVE: run or item unavailable"); yield break; }
        WeaponBase weapon = null;
        foreach (var candidate in player.inventory.weaponInventory.weapons.Values) { if (weapon == null) weapon = candidate; candidate.Disable(); }
        manager.enabledWaves = false;
        var target = manager.SpawnEnemy(DataManager.Instance.GetEnemyData(EEnemy.Goblin), player.transform.position + Vector3.right * 8f, -1, true, canBeElite: false);
        until = Time.realtimeSinceStartup + 20f;
        while (Time.realtimeSinceStartup < until && target != null && !target.CanTakeDamage()) yield return null;
        if (target == null || !target.CanTakeDamage() || weapon == null) { Debug.LogError("Mod results INCONCLUSIVE: target or weapon unavailable"); yield break; }
        target.enemyMovement.StopMovement(); target.SetDiagnosticHealth(1000f);
        player.inventory.itemInventory.AddItem(id);
        var damage = WeaponUtility.GetDamageContainer(weapon, null, target, Vector3.right, 1f);
        target.DamageFromPlayerWeapon(damage);
        const string sourceId = "mod:openbonk-item-on-hit:training-cell";
        if (RunStats.damageSources == null || !RunStats.damageSources.TryGetValue(sourceId, out var source) || source.damage <= 0f) { Debug.LogError("Mod results FAIL: bonus damage not recorded"); yield break; }
        // Restore the selected weapon before the death UI reads its enabled state.
        weapon.Enable();
        Debug.Log("Mod results ARMED: bonus=" + source.damage + " forcing death");
        until = Time.realtimeSinceStartup + 10f;
        while (Time.realtimeSinceStartup < until && !player.inventory.playerHealth.CanTakeDamage()) yield return null;
        player.inventory.playerHealth.DamagePlayerExternal(100000f, 0f, Vector3.up, true, "Mod results test", DcFlags.BypassAll);
        until = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < until)
        {
            foreach (var panel in Resources.FindObjectsOfTypeAll<GameOverDamageSourcesUi>())
            {
                if (panel == null || !panel.gameObject.scene.IsValid() || !panel.gameObject.activeInHierarchy || panel.damageSourceParent == null) continue;
                foreach (var row in panel.damageSourceParent.GetComponentsInChildren<DamageSourceEntry>(true))
                {
                    if (row.t_sourceName == null || row.t_sourceName.text != item.GetName()) continue;
                    bool pass = row.gameObject.activeInHierarchy && row.icon != null && row.icon.texture == item.GetIcon() && row.t_lvl != null && row.t_lvl.text == "1" && row.t_dmg != null && !string.IsNullOrEmpty(row.t_dmg.text);
                    bool normalWeaponIcon = false;
                    foreach (var slot in Resources.FindObjectsOfTypeAll<InventoryItemPrefabUI>())
                        if (slot != null && slot.gameObject.scene.IsValid() && slot.gameObject.activeInHierarchy && slot.icon != null && slot.icon.texture == weapon.weaponData.GetIcon() && slot.icon.color == Color.white) normalWeaponIcon = true;
                    pass &= normalWeaponIcon;
                    Debug.Log($"Mod results {(pass ? "PASS" : "FAIL")}: name={row.t_sourceName.text} level={row.t_lvl.text} damage={row.t_dmg.text} recorded={source.damage:F2} enabledWeaponIcon={normalWeaponIcon}"); yield break;
                }
            }
            yield return null;
        }
        Debug.LogError("Mod results INCONCLUSIVE: results row not observed");
    }
}
#endif
