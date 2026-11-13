#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using UnityEngine;

// Opt-in diagnostic: controlled sequential inventory operations, not item effect coverage.
internal sealed class AndroidItemLifecycleSweepProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_item_lifecycle_sweep", false)) return;
        var root = new GameObject("item lifecycle sweep probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidItemLifecycleSweepProbe>();
    }
    IEnumerator Start()
    {
        bool timed;
        using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var activity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
        using (var intent = activity.Call<AndroidJavaObject>("getIntent"))
            timed = intent.Call<bool>("getBooleanExtra", "megabonk_item_lifecycle_timed", false);
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null || DataManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance;
        if (player == null || player.inventory == null || DataManager.Instance == null) { Debug.LogError("Item lifecycle sweep FAIL: game not ready"); yield break; }
        EnemyManager.Instance.enabledWaves = false;
        player.inventory.weaponInventory.ToggleWeapon(EWeapon.FireStaff, false);
        typeof(MyPlayer).GetField("isInvincible", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, true);
        typeof(MyPlayer).GetField("damageCooldownOverAtTime", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, float.MaxValue);
        var inventory = player.inventory.itemInventory;
        int tested = 0, failed = 0, statDifferences = 0;
        foreach (var data in DataManager.Instance.unsortedItems)
        {
            if (data == null || !data.isEnabled) continue;
            var item = data.eItem;
            int baseline = inventory.GetAmount(item);
            var statBaseline = new Dictionary<EStat, float>(player.inventory.playerStats.rawStats);
            Debug.Log($"Item lifecycle sweep START: item={item} baseline={baseline}");
            bool ok = true;
            try { inventory.AddItem(item); ok = inventory.GetAmount(item) == baseline + 1; }
            catch (Exception e) { ok = false; Debug.LogError($"Item lifecycle sweep exception: item={item} step=add1 {e}"); }
            yield return new WaitForSecondsRealtime(.15f);
            try { inventory.AddItem(item); ok &= inventory.GetAmount(item) == baseline + 2; }
            catch (Exception e) { ok = false; Debug.LogError($"Item lifecycle sweep exception: item={item} step=add2 {e}"); }
            // Timed mode gives periodic stat effects a chance to activate before removal.
            if (timed) yield return new WaitForSeconds(2.2f);
            else yield return new WaitForSecondsRealtime(.15f);
            try { inventory.RemoveItem(item, false); ok &= inventory.GetAmount(item) == baseline + 1; }
            catch (Exception e) { ok = false; Debug.LogError($"Item lifecycle sweep exception: item={item} step=remove1 {e}"); }
            yield return new WaitForSecondsRealtime(.15f);
            try { inventory.RemoveItem(item, false); ok &= inventory.GetAmount(item) == baseline; }
            catch (Exception e) { ok = false; Debug.LogError($"Item lifecycle sweep exception: item={item} step=remove2 {e}"); }
            yield return new WaitForSecondsRealtime(.15f);
            foreach (var pair in statBaseline)
            {
                float current = player.inventory.playerStats.GetRawStat(pair.Key);
                if (Mathf.Abs(current - pair.Value) > .0001f)
                {
                    statDifferences++;
                    Debug.Log($"Item lifecycle sweep STAT DIFFERENCE: item={item} stat={pair.Key} before={pair.Value:F5} after={current:F5}");
                }
            }
            tested++; if (!ok) failed++;
            Debug.Log($"Item lifecycle sweep RESULT: item={item} ok={ok} final={inventory.GetAmount(item)} baseline={baseline}");
        }
        Debug.Log($"Item lifecycle sweep COMPLETE: tested={tested} failed={failed} statDifferences={statDifferences}");
    }
}
#endif
