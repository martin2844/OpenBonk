#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Inventory__Items__Pickups.Xp_and_Levels;
using UnityEngine;

// Opt-in only: controlled XP pickup on a disposable run.
internal sealed class AndroidClankPassiveProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity=unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent=activity.Call<AndroidJavaObject>("getIntent");
        if(!intent.Call<bool>("getBooleanExtra","megabonk_clank_passive_probe",false)) return;
        var go=new GameObject("Clank passive probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidClankPassiveProbe>();
    }
    IEnumerator Start()
    {
        float deadline=Time.realtimeSinceStartup+180f;
        while(Time.realtimeSinceStartup<deadline && (MyPlayer.Instance==null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || MyPlayer.Instance.inventory==null || GameManager.Instance==null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance==null || PickupManager.Instance==null)) yield return null;
        var player=MyPlayer.Instance;
        if(player==null || player.character!=ECharacter.Cl4nk || player.inventory==null || !player.inventory.HasPassive(EPassive.CritHappens) || EnemyManager.Instance==null || PickupManager.Instance==null) { Debug.LogError("Clank passive probe INCONCLUSIVE: player or passive unavailable"); yield break; }
        var inv=player.inventory; var xp=inv.playerXp;
        if(xp.level!=0) { Debug.LogError("Clank passive probe INCONCLUSIVE: player already leveled"); yield break; }
        EnemyManager.Instance.enabledWaves=false;
        foreach(var weapon in inv.weaponInventory.weapons.Values) weapon.Disable();
        typeof(MyPlayer).GetField("isInvincible",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(player,true);
        float baseline=inv.playerStats.GetStat(EStat.CritChance);
        xp.xp=6; typeof(PlayerXp).GetField("leftOverXp",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(xp,0f);
        var pickup=PickupManager.Instance.SpawnPickup(EPickup.Xp,player.transform.position,1,false,0f);
        deadline=Time.realtimeSinceStartup+40f;
        while(Time.realtimeSinceStartup<deadline && xp.level==0) yield return null;
        if(xp.level==0) { Debug.LogError("Clank passive probe INCONCLUSIVE: pickup did not trigger level-up"); yield break; }
        yield return new WaitForSecondsRealtime(2f);
        bool has=inv.passiveAbility.statModifiers.TryGetValue(EStat.CritChance,out var container);
        var values=has ? container.GetModifiers().ToArray() : new Assets.Scripts.Inventory__Items__Pickups.Stats.StatModifier[0];
        float bonus=values.Length==1?values[0].modification:float.NaN;
        float final=inv.playerStats.GetStat(EStat.CritChance);
        bool passive=values.Length==1 && values[0].modifyType==EStatModifyType.Flat && Mathf.Abs(bonus-xp.level*.01f)<.0001f;
        bool stat=Mathf.Abs(final-baseline-bonus)<.0001f;
        bool collected=pickup!=null && !pickup.gameObject.activeInHierarchy;
        Debug.Log($"Clank passive probe {(passive&&stat&&collected ? "PASS" : "FAIL")}: xpLevel={xp.level} collected={collected} modifier={bonus:F4} baseline={baseline:F4} actual={final:F4} passiveMatch={passive} statMatch={stat} leveling={LevelupScreen.isLevelingUp}");
    }
}
#endif
