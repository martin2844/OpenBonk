#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using UnityEngine;

// Intent-gated observation for a disposable Roberto run.
internal sealed class AndroidHoarderChestProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_hoarder_chest_probe", false)) return;
        var go = new GameObject("hoarder chest probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidHoarderChestProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || EnemyManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance;
        if (player == null || player.inventory?.passiveAbility is not PassiveAbilityHoarder hoarder) { Debug.LogError("Hoarder chest probe INCONCLUSIVE: Roberto passive unavailable"); yield break; }
        EnemyManager.Instance.enabledWaves = false;
        typeof(MyPlayer).GetField("isInvincible", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, true);
        typeof(MyPlayer).GetField("damageCooldownOverAtTime", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, float.MaxValue);
        foreach (var weapon in player.inventory.weaponInventory.weapons.Values) weapon.Disable();
        var timer = typeof(PassiveAbilityHoarder).GetField("currentTimer", BindingFlags.Instance | BindingFlags.NonPublic);
        var interval = typeof(PassiveAbilityHoarder).GetField("spawnIntervalSeconds", BindingFlags.Instance | BindingFlags.NonPublic);
        float timerBefore = (float)timer.GetValue(hoarder), gameStart = MyTime.time;
        var baseline = new HashSet<OpenChest>(FindObjectsOfType<OpenChest>());
        Debug.Log($"Hoarder chest probe READY: timer={timerBefore:F2} interval={interval.GetValue(hoarder)} baseline={baseline.Count}");
        OpenChest spawned = null; deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && spawned == null) {
            foreach (var chest in FindObjectsOfType<OpenChest>()) if (!baseline.Contains(chest)) { spawned = chest; break; }
            yield return null;
        }
        if (spawned == null) { Debug.LogError($"Hoarder chest probe INCONCLUSIVE: no chest after gameSeconds={MyTime.time-gameStart:F2} timer={timer.GetValue(hoarder)}"); yield break; }
        float elapsed = MyTime.time - gameStart;
        float nextInterval = (float)interval.GetValue(hoarder);
        bool timing = Mathf.Abs(elapsed - timerBefore) < 1.5f && Mathf.Abs(nextInterval - 62.5f) < .01f;
        int opened = 0, awarded = 0; EItem item = default;
        System.Action onOpen = () => opened++;
        System.Action<EItem> onItem = value => { awarded++; item = value; };
        int previousSkip = SaveManager.Instance.config.cfGameSettings.skip_chest_animation;
        SaveManager.Instance.config.cfGameSettings.skip_chest_animation = 1;
        OpenChest.A_Open += onOpen; ItemInventory.A_ItemAdded += onItem;
        var body = player.playerMovement.GetRb(); body.position = spawned.transform.position; body.velocity = Vector3.zero; Physics.SyncTransforms();
        deadline = Time.realtimeSinceStartup + 12f;
        while (Time.realtimeSinceStartup < deadline && opened == 0) yield return null;
        bool collected = opened == 1 && awarded == 1 && !spawned.gameObject.activeSelf;
        Debug.Log($"Hoarder chest probe {(timing && collected ? "PASS" : "FAIL")}: timerBefore={timerBefore:F2} elapsed={elapsed:F2} nextInterval={nextInterval:F2} timing={timing} opened={opened} awarded={awarded} item={item} active={spawned.gameObject.activeSelf}");
        OpenChest.A_Open -= onOpen; ItemInventory.A_ItemAdded -= onItem;
        SaveManager.Instance.config.cfGameSettings.skip_chest_animation = previousSkip;
    }
}
#endif
