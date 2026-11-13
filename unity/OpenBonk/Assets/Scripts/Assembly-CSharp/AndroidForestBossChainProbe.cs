#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.SceneManagement;

// Opt-in disposable run: two real altar fights and portal transitions.
internal sealed class AndroidForestBossChainProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_forest_boss_chain_probe", false)) return;
        var root = new GameObject("Forest boss chain probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidForestBossChainProbe>();
    }
    IEnumerator Start()
    {
        PlayerInventory inventory = null; MyPlayer previous = null; int totalHits = 0, rewardItems = 0;
        System.Action<EItem> onItem = item => { rewardItems++; Debug.Log("Forest boss chain item: " + item); };
        ItemInventory.A_ItemAdded += onItem;
        for (int stage=0; stage<3; stage++)
        {
            float until = Time.realtimeSinceStartup + 180f;
            while (Time.realtimeSinceStartup < until && (MapController.index != stage || SceneManager.GetActiveScene().name != "GeneratedMap" || MyPlayer.Instance == null || MyPlayer.Instance == previous || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null || EnemyManager.Instance.summonerController == null)) yield return null;
            var player=MyPlayer.Instance;
            if (player == null || player == previous || !player.hasStarted || MapController.index != stage || player.inventory == null) { Debug.LogError("Forest boss chain INCONCLUSIVE: stage not ready " + stage); yield break; }
            if (stage == 0)
            {
                inventory=player.inventory;
                MapController.runConfig.mapTierIndex=2;
                inventory.statInventory.ChangeMovingStat("forest chain damage",new StatModifier { stat=EStat.DamageMultiplier,modifyType=EStatModifyType.Flat,modification=50000f });
            }
            if (!ReferenceEquals(inventory,player.inventory)) { Debug.LogError("Forest boss chain FAIL: inventory replaced at stage " + stage); yield break; }
            EnemyManager.Instance.enabledWaves=false;
            typeof(MyPlayer).GetField("isInvincible",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(player,true);
            typeof(MyPlayer).GetField("damageCooldownOverAtTime",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(player,MyTime.time+10000f);
            // Consume queued level-up/chest windows using the real selection handlers.
            until=Time.realtimeSinceStartup+45f; float quietSince=Time.realtimeSinceStartup;
            while(Time.realtimeSinceStartup<until)
            {
                var windows=UiManager.Instance != null ? UiManager.Instance.encounterWindows : null;
                if(windows == null) { yield return null; continue; }
                if(!windows.HasEncounter()) { if(Time.realtimeSinceStartup-quietSince>2f) break; }
                else
                {
                    quietSince=Time.realtimeSinceStartup;
                    var chest=windows.chestWindow as ChestWindowUi;
                    var level=windows.levelupScreen as LevelupScreen;
                    if(chest != null && chest.gameObject.activeInHierarchy)
                    {
                        if(chest.b_open.gameObject.activeInHierarchy) chest.OpenButton();
                        else if(chest.b_take.gameObject.activeInHierarchy) chest.TakeButton();
                    }
                    else if(level != null && level.window != null && level.window.activeInHierarchy && level.upgradePicker != null)
                    {
                        foreach(var button in level.upgradePicker.buttons) if(button != null && button.gameObject.activeInHierarchy && button.GetButton().interactable) { button.SelectUpgrade(); break; }
                    }
                }
                yield return null;
            }
            if(UiManager.Instance.encounterWindows.HasEncounter()) { Debug.LogError("Forest boss chain INCONCLUSIVE: reward window did not close stage="+stage); yield break; }
            Debug.Log($"Forest boss chain STAGE: index={stage} data={MapController.currentStage?.name} sameInventory=true rewardItems={rewardItems}");
            if(stage==2) break;
            var altar=FindFirstObjectByType<InteractableBossSpawner>();
            if(altar == null) { Debug.LogError("Forest boss chain FAIL: altar missing stage="+stage); yield break; }
            altar.transform.position=player.transform.position+player.transform.forward*3f; Physics.SyncTransforms();
            yield return new WaitForSecondsRealtime(2f);
            if(!altar.Interact()) { Debug.LogError("Forest boss chain FAIL: altar rejected stage="+stage); yield break; }
            var bosses=(List<Enemy>)typeof(InteractableBossSpawner).GetField("bossEnemies",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(altar);
            if(bosses==null || bosses.Count!=1) { Debug.LogError("Forest boss chain INCONCLUSIVE: boss count stage="+stage); yield break; }
            Enemy boss=bosses[0]; int hits=0;
            void Hit(Enemy enemy,DamageContainer dc) { if(enemy==boss && dc.damageSource=="FireStaff") { hits++; totalHits++; } }
            Enemy.A_Damage+=Hit;
            until=Time.realtimeSinceStartup+120f;
            while(Time.realtimeSinceStartup<until && altar!=null && !altar.portal.activeInHierarchy) yield return null;
            Enemy.A_Damage-=Hit;
            if(altar==null || !altar.portal.activeInHierarchy || hits==0) { Debug.LogError($"Forest boss chain INCONCLUSIVE: stage={stage} hits={hits} portal={(altar!=null && altar.portal.activeInHierarchy)}"); yield break; }
            var portal=altar.portal.GetComponent<InteractablePortal>();
            if(portal==null || !portal.Interact()) { Debug.LogError("Forest boss chain FAIL: portal rejected stage="+stage); yield break; }
            Debug.Log($"Forest boss chain BOSS: stage={stage} hits={hits} portal=true"); previous=player;
        }
        ItemInventory.A_ItemAdded -= onItem;
        Debug.Log($"Forest boss chain PASS: stages=3 bosses=2 firestaffHits={totalHits} rewardItems={rewardItems} sameInventory=true");
    }
}
#endif
