#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.IO;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using UnityEngine;

// Explicitly enabled only in a disposable test run; does not change the pools.
internal sealed class AndroidItemAvailabilityProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_item_availability_probe", false)) return;
        var go = new GameObject("Item availability probe");
        DontDestroyOnLoad(go);
        go.AddComponent<AndroidItemAvailabilityProbe>();
    }

    private IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline &&
               (DataManager.Instance == null || DataManager.Instance.unsortedItems == null ||
                SaveManager.Instance == null || SaveManager.Instance.progression == null ||
                MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || RunUnlockables.availableItems == null)) yield return null;
        if (DataManager.Instance == null || SaveManager.Instance == null || SaveManager.Instance.progression == null ||
            MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || RunUnlockables.availableItems == null)
        {
            Debug.LogError("Item availability probe: game not ready");
            yield break;
        }
        string path = Path.Combine(Application.persistentDataPath, "item-availability.tsv");
        int count = 0;
        using (var writer = new StreamWriter(path, false))
        {
            foreach (var item in DataManager.Instance.unsortedItems)
            {
                if (item == null) { Debug.LogError("Item availability probe: null item"); continue; }
                writer.WriteLine($"{(int)item.eItem}\t{(MyAchievements.IsAvailable(item) ? 1 : 0)}");
                count++;
            }
        }
        Debug.Log($"Item availability probe: completed count={count} path={path}");
        string poolPath = Path.Combine(Application.persistentDataPath, "item-pools.tsv");
        int poolCount = 0;
        using (var writer = new StreamWriter(poolPath, false))
        {
            foreach (var pool in RunUnlockables.availableItems)
            {
                if (pool.Value == null) { Debug.LogError($"Item pool probe: null pool {(int)pool.Key}"); continue; }
                for (int i = 0; i < pool.Value.Count; i++)
                {
                    var item = pool.Value[i];
                    if (item == null) { Debug.LogError($"Item pool probe: null item in {(int)pool.Key}"); continue; }
                    writer.WriteLine($"{(int)pool.Key}\t{i}\t{(int)item.eItem}");
                    poolCount++;
                }
            }
        }
        Debug.Log($"Item pool probe: completed count={poolCount} path={poolPath}");
        string unlockPath = Path.Combine(Application.persistentDataPath, "unlock-availability.tsv");
        using (var writer = new StreamWriter(unlockPath, false))
        {
            foreach (var item in DataManager.Instance.unsortedWeapons)
                writer.WriteLine($"weapon\t{(int)item.eWeapon}\t{(MyAchievements.IsAvailable(item) ? 1 : 0)}");
            foreach (var item in DataManager.Instance.unsortedCharacterData)
                writer.WriteLine($"character\t{(int)item.eCharacter}\t{(MyAchievements.IsAvailable(item) ? 1 : 0)}");
            foreach (var item in DataManager.Instance.unsortedTomes)
                writer.WriteLine($"tome\t{(int)item.eTome}\t{(MyAchievements.IsAvailable(item) ? 1 : 0)}");
        }
        Debug.Log($"Unlock availability probe: completed path={unlockPath}");
    }
}
#endif
