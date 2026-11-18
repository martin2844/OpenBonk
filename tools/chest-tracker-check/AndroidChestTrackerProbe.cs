// Diagnostic only: mutates a temporary player inventory, gold and chest settings.
// Never include this source in a release or run it against a real profile.
#if UNITY_ANDROID && !UNITY_EDITOR
using System.Collections;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Chests;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using UnityEngine;
public sealed class AndroidChestTrackerProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install() { var o = new GameObject("Chest tracker diagnostic"); DontDestroyOnLoad(o); o.AddComponent<AndroidChestTrackerProbe>(); }
    private void Start() { StartCoroutine(Check()); }
    private IEnumerator Check()
    {
        float deadline = Time.realtimeSinceStartup + 150;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || OpenBonkChestTracker.Total < 1)) yield return null;
        if (MyPlayer.Instance == null || OpenBonkChestTracker.Total < 1) { Debug.LogError("CHEST_PROBE inconclusive: no run or chests"); yield break; }
        yield return new WaitForSeconds(1);
        var all = Object.FindObjectsOfType<InteractableChest>();
        if (all.Length == 0) { Debug.LogError("CHEST_PROBE inconclusive: no active chest"); yield break; }
        if (all.Length < 2) { Debug.LogError("CHEST_PROBE inconclusive: fewer than two chests"); yield break; }
        var chest = all[0]; var animatedChest = all[1]; int total = OpenBonkChestTracker.Total, before = OpenBonkChestTracker.Unopened;
        var playerInv = MyPlayer.Instance.inventory;
        var inv = playerInv.itemInventory;
        inv.AddItem(EItem.Key);
        var key = inv.GetItem(EItem.Key) as ItemKey;
        if (key == null) { Debug.LogError("CHEST_PROBE inconclusive: no key item"); yield break; }
        var chance = typeof(ItemKey).GetField("currentChance", BindingFlags.Instance | BindingFlags.NonPublic);
        chance.SetValue(key, 1f);
        playerInv.ChangeGold(-(int)playerInv.gold);
        animatedChest.chestType = EChest.Normal;
        int beforeFailed = OpenBonkChestTracker.Total - OpenBonkChestTracker.Unopened;
        bool failed = !animatedChest.Interact();
        bool failedUnchanged = OpenBonkChestTracker.Total - OpenBonkChestTracker.Unopened == beforeFailed;
        playerInv.ChangeGold(10000); float gold = playerInv.gold;
        int itemsAdded = 0, openedEvents = 0;
        System.Action<EItem> added = _ => { itemsAdded++; var current = inv.GetItem(EItem.Key) as ItemKey; if (current != null) chance.SetValue(current, 1f); };
        System.Action opened = () => openedEvents++;
        ItemInventory.A_ItemAdded += added; InteractableChest.A_ChestOpened += opened;
        chest.chestType = EChest.Normal;
        SaveManager.Instance.config.cfGameSettings.skip_chest_animation = 1;
        bool openedSuccessfully = false;
        try { openedSuccessfully = chest.Interact(); }
        catch (System.Exception e) { Debug.LogError("CHEST_PROBE interaction exception: " + e); }
        finally { ItemInventory.A_ItemAdded -= added; InteractableChest.A_ChestOpened -= opened; }
        int newTotal = OpenBonkChestTracker.Total, newRemaining = OpenBonkChestTracker.Unopened;
        bool unique = newTotal - newRemaining == total - before + 1;
        bool free = playerInv.gold == gold;
        SaveManager.Instance.config.cfGameSettings.skip_chest_animation = 0;
        int beforeAnimated = OpenBonkChestTracker.Total - OpenBonkChestTracker.Unopened;
        int animationEvents = 0;
        System.Action animatedOpen = () => animationEvents++;
        InteractableChest.A_ChestOpened += animatedOpen;
        bool animationStarted = false;
        try { animationStarted = animatedChest.Interact(); }
        catch (System.Exception e) { Debug.LogError("CHEST_PROBE animation exception: " + e); }
        finally { InteractableChest.A_ChestOpened -= animatedOpen; }
        bool animationCounted = OpenBonkChestTracker.Total - OpenBonkChestTracker.Unopened == beforeAnimated + 1;
        Debug.Log("CHEST_ANIMATION_PROBE " + (failed && failedUnchanged && animationStarted && animationCounted && animationEvents == 1 ? "PASS" : "FAIL") + " insufficientGold=" + failed + " failedUnchanged=" + failedUnchanged + " animationStarted=" + animationStarted + " animationCounted=" + animationCounted + " events=" + animationEvents);

        int chestId = chest.GetInstanceID();
        var timer = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < 10000; i++) { OpenBonkChestTracker.Register(chestId); OpenBonkChestTracker.Opened(chestId); }
        timer.Stop();
        yield return null;
        bool destroyed = chest == null;
        Debug.Log("CHEST_PROBE " + (openedSuccessfully && destroyed && unique && free && itemsAdded == 1 && openedEvents == 1 ? "PASS" : "FAIL") + " opened=" + openedSuccessfully + " destroyed=" + destroyed + " unique=" + unique + " free=" + free + " rewards=" + itemsAdded + " events=" + openedEvents + " beforeTotal=" + total + " beforeRemaining=" + before + " afterTotal=" + newTotal + " afterRemaining=" + newRemaining + " repeated10000ms=" + timer.Elapsed.TotalMilliseconds.ToString("F2"));
    }
}
#endif
