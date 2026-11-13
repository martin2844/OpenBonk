#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using UnityEngine;

// Disposable quest test: moves the player and selects generated interactions.
internal sealed class AndroidMonkeQuestProbe : MonoBehaviour
{
    bool readOnly;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity=unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent=activity.Call<AndroidJavaObject>("getIntent");
        bool quest=intent.Call<bool>("getBooleanExtra","megabonk_monke_quest_probe",false);
        bool read=intent.Call<bool>("getBooleanExtra","megabonk_monke_save_probe",false);
        if(!quest && !read) return;
        var go=new GameObject("monke quest probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidMonkeQuestProbe>().readOnly=read;
    }
    IEnumerator Start()
    {
        if(readOnly) {
            float loadDeadline=Time.realtimeSinceStartup+120f;
            while(Time.realtimeSinceStartup<loadDeadline && (!SaveManager.loaded || SaveManager.Instance==null || SaveManager.Instance.progression==null)) yield return null;
            bool loaded=SaveManager.loaded && SaveManager.Instance!=null && SaveManager.Instance.progression!=null;
            bool loadedAchievement=loaded && MyAchievements.IsAchievementDone("a_monke");
            Debug.Log($"Monke save probe {(loadedAchievement?"PASS":"FAIL")}: loaded={loaded} achievement={loadedAchievement}");
            yield break;
        }
        float deadline=Time.realtimeSinceStartup+180f; InteractableCage cage=null; InteractableCageKey key=null;
        while(Time.realtimeSinceStartup<deadline)
        {
            if(GameManager.Instance!=null && GameManager.Instance.IsPlaying() && !GameManager.Instance.cutscene && MyPlayer.Instance!=null && MyPlayer.Instance.hasStarted && EnemyManager.Instance!=null) {
                cage=FindObjectOfType<InteractableCage>();
                foreach(var candidate in FindObjectsOfType<InteractableCageKey>()) if(candidate.eItem==EItem.CageKey) { key=candidate; break; }
            }
            if(cage!=null && key!=null) break;
            yield return null;
        }
        var player=MyPlayer.Instance; var detector=FindObjectOfType<DetectInteractables>();
        if(cage==null || key==null || player==null || detector==null || MyAchievements.IsAchievementDone("a_monke")) { Debug.LogError($"Monke quest probe INCONCLUSIVE: cage={cage}, key={key}, detector={detector}"); yield break; }
        var inv=player.inventory.itemInventory; bool locked=!cage.CanInteract() && inv.GetAmount(EItem.CageKey)==0;
        EnemyManager.Instance.enabledWaves=false; foreach(var weapon in player.inventory.weaponInventory.weapons.Values) weapon.Disable();
        var body=player.playerMovement.GetRb(); var field=typeof(DetectInteractables).GetField("currentInteractable",BindingFlags.Instance|BindingFlags.NonPublic);
        body.position=key.transform.position+Vector3.up*2f; body.velocity=Vector3.zero; Physics.SyncTransforms(); field.SetValue(detector,key); detector.TryInteract();
        bool keyAcquired=inv.GetAmount(EItem.CageKey)==1 && cage.CanInteract();
        body.position=cage.transform.position+Vector3.up*2f; body.velocity=Vector3.zero; Physics.SyncTransforms(); field.SetValue(detector,cage); detector.TryInteract();
        bool unlocked=MyAchievements.IsAchievementDone("a_monke"); var monke=cage.monke!=null?cage.monke.GetComponent<QuestMonke>():null;
        bool opened=unlocked && monke!=null && cage.monke.activeInHierarchy && !cage.collider.enabled && !cage.meshRenderer.enabled && inv.GetAmount(EItem.CageKey)==1;
        bool duplicateRejected=!cage.Interact();
        deadline=Time.realtimeSinceStartup+40f; bool walking=false, moving=false; Vector3 start=monke!=null?monke.transform.position:Vector3.zero;
        var walkingField=typeof(QuestMonke).GetField("isWalking",BindingFlags.Instance|BindingFlags.NonPublic);
        while(monke!=null && monke.gameObject.activeInHierarchy && Time.realtimeSinceStartup<deadline && !moving) {
            walking=(bool)walkingField.GetValue(monke);
            moving=walking && (monke.transform.position-start).sqrMagnitude>1f;
            yield return null;
        }
        bool ok=locked && keyAcquired && opened && duplicateRejected && walking && moving;
        Debug.Log($"Monke quest probe {(ok?"PASS":"FAIL")}: locked={locked} key={keyAcquired} opened={opened} duplicateRejected={duplicateRejected} unlocked={unlocked} walking={walking} moved={moving}");
    }
}
#endif
