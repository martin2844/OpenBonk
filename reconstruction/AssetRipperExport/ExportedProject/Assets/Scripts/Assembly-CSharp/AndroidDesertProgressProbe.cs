#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Managers;
using UnityEngine;

// Disposable diagnostic: start Desert normally, then request two real scene transitions.
internal sealed class AndroidDesertProgressProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_desert_progress_probe", false)) return;
        var root = new GameObject("Desert progress probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidDesertProgressProbe>();
    }
    IEnumerator Start()
    {
        object initialPool = null; PlayerInventory initialInventory = null; MyPlayer previousPlayer = null;
        for (int stage = 0; stage < 3; stage++)
        {
            float deadline = Time.realtimeSinceStartup + 180f;
            while (Time.realtimeSinceStartup < deadline && (MapController.index != stage || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || MyPlayer.Instance == null || (stage > 0 && ReferenceEquals(MyPlayer.Instance, previousPlayer)) || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null || EnemyManager.Instance == null || RunUnlockables.availableItems == null)) yield return null;
            var player = MyPlayer.Instance;
            if (MapController.index != stage || player == null || !player.hasStarted || player.inventory == null || RunUnlockables.availableItems == null || EnemyManager.Instance == null)
            { Debug.LogError($"Desert progress probe INCONCLUSIVE: stage={stage}, index={MapController.index}, player={(player != null)}, pool={(RunUnlockables.availableItems != null)}"); yield break; }
            typeof(MyPlayer).GetField("isInvincible", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(player, true);
            typeof(MyPlayer).GetField("damageCooldownOverAtTime", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(player, float.PositiveInfinity);
            EnemyManager.Instance.enabledWaves = false;
            if (stage == 0) { initialPool = RunUnlockables.availableItems; initialInventory = player.inventory; }
            yield return new WaitForSecondsRealtime(4f);
            var merchants = FindObjectsOfType<InteractableShadyGuy>(); int ready = 0;
            foreach (var merchant in merchants) if (merchant.items != null) ready++;
            var selected = InventoryUtility.GetRandomItemsShadyGuy(EItemRarity.Common);
            bool samePool = ReferenceEquals(initialPool, RunUnlockables.availableItems), sameInventory = ReferenceEquals(initialInventory, player.inventory);
            Debug.Log($"Desert progress probe stage: index={stage}, data={MapController.currentStage?.name}, poolSame={samePool}, inventorySame={sameInventory}, merchants={merchants.Length}, merchantsReady={ready}, directOffers={selected.Count}");
            if (!samePool || !sameInventory || ready != merchants.Length || selected.Count == 0) { Debug.LogError("Desert progress probe FAIL: inventory, pool or merchant setup"); yield break; }
            if (stage < 2) { previousPlayer = player; MapController.LoadNextStage(); }
        }
        Debug.Log("Desert progress probe PASS: first-stage initialization and two stage transitions");
    }
}
#endif
