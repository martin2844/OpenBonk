#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using UnityEngine;

// Opt-in controlled test: normal weapon scheduling against two real enemies.
internal sealed class AndroidPikachuLightningChainProbe : MonoBehaviour
{
    private readonly List<float> primaries = new List<float>();
    private readonly List<float> chains = new List<float>();
    private string source;
    private bool primaryNonCrit;
    private float lastPrimary;
    private int primaryTarget, badElement, badPair;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.splittest" || !intent.Call<bool>("getBooleanExtra", "openbonk_pikachu_chain_probe", false)) return;
        var go = new GameObject("Pikachu chain probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidPikachuLightningChainProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance; var manager = EnemyManager.Instance;
        var id = (EItem)OpenBonkContentIds.Item("openbonk-lightning-test", "training-cell");
        if (player == null || player.inventory == null || manager == null || !DataManager.Instance.TryGetItem(id, out var item) || !(item is OpenBonkItemData)) { Debug.LogError("Pikachu chain INCONCLUSIVE: player or item unavailable"); yield break; }
        WeaponBase weapon = null;
        foreach (var w in player.inventory.weaponInventory.weapons.Values) { if (w.weaponData is OpenBonkLuaWeaponData) weapon = w; w.Disable(); }
        if (weapon == null) { Debug.LogError("Pikachu chain INCONCLUSIVE: external weapon unavailable"); yield break; }
        manager.enabledWaves = false;
        var a = manager.SpawnEnemy(DataManager.Instance.GetEnemyData(EEnemy.Goblin), player.transform.position + Vector3.right * 5f, -1, true, canBeElite: false);
        var b = manager.SpawnEnemy(DataManager.Instance.GetEnemyData(EEnemy.Goblin), player.transform.position + Vector3.right * 8f, -1, true, canBeElite: false);
        deadline = Time.realtimeSinceStartup + 20f;
        while (Time.realtimeSinceStartup < deadline && (a == null || b == null || !a.CanTakeDamage() || !b.CanTakeDamage())) yield return null;
        if (a == null || b == null || !a.CanTakeDamage() || !b.CanTakeDamage()) { Debug.LogError("Pikachu chain INCONCLUSIVE: targets unavailable"); yield break; }
        a.SetDiagnosticHealth(10000f); b.SetDiagnosticHealth(10000f);
        a.enemyMovement.StopMovement(); b.enemyMovement.StopMovement();
        var arb = a.GetComponent<Rigidbody>(); var brb = b.GetComponent<Rigidbody>();
        if (arb != null) arb.constraints = RigidbodyConstraints.FreezeAll;
        if (brb != null) brb.constraints = RigidbodyConstraints.FreezeAll;
        source = weapon.weaponData.damageSourceName; Enemy.A_Damage += OnDamage; weapon.Enable();
        deadline = Time.realtimeSinceStartup + 40f;
        while (Time.realtimeSinceStartup < deadline && (primaries.Count < 2 || chains.Count < 2)) yield return null;
        bool baseOk = primaries.Count >= 2 && chains.Count >= 2 && NearPair(primaries[0], 14f, 21f) && NearPair(primaries[1], 14f, 21f);
        string before = string.Join(",", primaries); primaries.Clear(); chains.Clear(); primaryNonCrit = false;
        player.inventory.itemInventory.AddItem(id);
        deadline = Time.realtimeSinceStartup + 40f;
        while (Time.realtimeSinceStartup < deadline && (primaries.Count < 2 || chains.Count < 2)) yield return null;
        bool boosted = primaries.Count >= 2 && chains.Count >= 2 && NearPair(primaries[0], 28f, 42f) && NearPair(primaries[1], 28f, 42f);
        bool pass = baseOk && boosted && badElement == 0 && badPair == 0;
        Debug.Log($"Pikachu chain {(pass ? "PASS" : "FAIL")}: baseline={before} boosted={string.Join(",", primaries)} chains={string.Join(",", chains)} badElement={badElement} badPair={badPair}");
        Enemy.A_Damage -= OnDamage; weapon.Disable();
    }
    static bool NearPair(float value, float a, float b) => Mathf.Abs(value-a)<.01f || Mathf.Abs(value-b)<.01f;
    void OnDamage(Enemy enemy, DamageContainer dc)
    {
        if (enemy == null || dc == null || dc.damageSource != source) return;
        if (dc.element != EElement.Lightning) badElement++;
        if (Mathf.Abs(dc.procCoefficient - 1f) < .001f)
        { primaryNonCrit = !dc.crit; if (primaryNonCrit) { lastPrimary = dc.damage; primaryTarget = enemy.GetInstanceID(); primaries.Add(dc.damage); } }
        else if (Mathf.Abs(dc.procCoefficient - .5f) < .001f && primaryNonCrit)
        { chains.Add(dc.damage); if (enemy.GetInstanceID() == primaryTarget || Mathf.Abs(dc.damage-lastPrimary*.5f)>.01f) badPair++; primaryNonCrit = false; }
    }
    void OnDestroy() { Enemy.A_Damage -= OnDamage; }
}
#endif
