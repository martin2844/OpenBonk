#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemyDebuffs.Implementations;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using UnityEngine;

// Controlled items, ordinary weapon attacks and poison bookkeeping.
internal sealed class AndroidPoisonMaskCombatProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_poison_mask_probe", false)) return;
        var root = new GameObject("poison mask probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidPoisonMaskCombatProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null)) yield return null;
        var player=MyPlayer.Instance;
        if (player == null || player.inventory == null || EnemyManager.Instance == null) { Debug.LogError("Poison mask probe FAIL: game not ready"); yield break; }
        typeof(MyPlayer).GetField("isInvincible", BindingFlags.Instance|BindingFlags.NonPublic).SetValue(player,true);
        typeof(MyPlayer).GetField("damageCooldownOverAtTime", BindingFlags.Instance|BindingFlags.NonPublic).SetValue(player,float.MaxValue);
        Enemy.A_EnemySpawned += OnSpawn;
        float baseArmor=PlayerStats.GetStatRaw(EStat.Armor), baseOverheal=PlayerStats.GetStatRaw(EStat.Overheal);
        var inv=player.inventory.itemInventory;
        inv.AddItem(EItem.Gasmask); inv.AddItem(EItem.Snek);
        var mask=inv.GetItem(EItem.Gasmask) as ItemGasmask;
        if (mask == null) { Debug.LogError("Poison mask probe FAIL: mask unavailable"); Unsubscribe(); yield break; }
        var stored=typeof(ItemGasmask).GetField("lastStoredStacks", BindingFlags.Instance|BindingFlags.NonPublic);
        bool applied=false; int counted=0; float armor=0,overheal=0;
        deadline=Time.realtimeSinceStartup+90f;
        while (Time.realtimeSinceStartup<deadline)
        {
            counted=(int)stored.GetValue(mask);
            if(counted>0 && mask.statModifiers.TryGetValue(EStat.Armor,out var a) && mask.statModifiers.TryGetValue(EStat.Overheal,out var o))
            {
                armor=a.GetModifiers().First().modification; overheal=o.GetModifiers().First().modification;
                float expectedArmor=Mathf.Min(counted*.005f,.4f), expectedOverheal=Mathf.Min(counted*.005f,.25f);
                if(Mathf.Abs(armor-expectedArmor)<.0001f && Mathf.Abs(overheal-expectedOverheal)<.0001f &&
                   Mathf.Abs(PlayerStats.GetStatRaw(EStat.Armor)-(baseArmor+armor))<.0001f &&
                   Mathf.Abs(PlayerStats.GetStatRaw(EStat.Overheal)-(baseOverheal+overheal))<.0001f) { applied=true; break; }
            }
            yield return null;
        }
        Debug.Log($"Poison mask probe observed: applied={applied} storedEnemies={counted} livePoisoned={DebuffPoison.numPoisonedEnemies} armorBonus={armor:F3} overhealBonus={overheal:F3}");
        inv.RemoveItem(EItem.Gasmask,false); inv.RemoveItem(EItem.Snek,false);
        bool restored=false; deadline=Time.realtimeSinceStartup+5f;
        while(Time.realtimeSinceStartup<deadline)
        {
            if(Mathf.Abs(PlayerStats.GetStatRaw(EStat.Armor)-baseArmor)<.0001f && Mathf.Abs(PlayerStats.GetStatRaw(EStat.Overheal)-baseOverheal)<.0001f) { restored=true; break; }
            yield return null;
        }
        Debug.Log($"Poison mask probe {(applied && restored ? "PASS" : "INCONCLUSIVE")}: applied={applied} restored={restored} baseArmor={baseArmor:F3} baseOverheal={baseOverheal:F3}");
        Unsubscribe();
    }
    void OnSpawn(Enemy enemy) { if (enemy != null && !enemy.IsBoss()) enemy.SetDiagnosticHealth(1000f); }
    void Unsubscribe() { Enemy.A_EnemySpawned -= OnSpawn; }
    void OnDestroy() => Unsubscribe();
}
#endif
