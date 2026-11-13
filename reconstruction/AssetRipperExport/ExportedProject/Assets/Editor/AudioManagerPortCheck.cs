using System;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.Pool;

public static class AudioManagerPortCheck
{
    const BindingFlags P = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Require(bool value, string message) { if (!value) throw new Exception("AudioManagerPortCheck: " + message); }
    static T Get<T>(AudioManager audio, string field) => (T)typeof(AudioManager).GetField(field, P).GetValue(audio);
    static void Set(AudioManager audio, string field, object value) => typeof(AudioManager).GetField(field, P).SetValue(audio, value);
    static void Call(AudioManager audio, string method) => typeof(AudioManager).GetMethod(method, P).Invoke(audio, null);
    public static void Run()
    {
        var root = new GameObject("audio manager test"); root.SetActive(false);
        var audio = root.AddComponent<AudioManager>(); audio.xp = root.AddComponent<AudioSource>(); audio.gold = root.AddComponent<AudioSource>(); audio.silver = root.AddComponent<AudioSource>(); audio.dungeonDoorEnter = root.AddComponent<AudioSource>();
        audio.xp.volume = .4f; audio.gold.volume = .6f;
        audio.uiClick = root.AddComponent<RandomSfx>(); audio.uiClick.s = root.AddComponent<AudioSource>();
        audio.uiSelect = root.AddComponent<RandomSfx>(); audio.uiSelect.s = root.AddComponent<AudioSource>();
        audio.customSfx = root.AddComponent<RandomSfx>(); audio.customSfx.s = root.AddComponent<AudioSource>();
        var click = AudioClip.Create("click", 128, 1, 8000, false); var select = AudioClip.Create("select", 128, 1, 8000, false);
        audio.uiClick.sounds = new[] { click }; audio.uiSelect.sounds = new[] { select };
        var save = root.AddComponent<SaveManager>(); typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save }); save.config = new ConfigSaveFile();
        var game = root.AddComponent<GameManager>(); GameManager.Instance = game; var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
        player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.playerHealth = (PlayerHealth)FormatterServices.GetUninitializedObject(typeof(PlayerHealth)); player.inventory.playerHealth.hp = 100;
        var pickupManager = root.AddComponent<PickupManager>(); PickupManager.Instance = pickupManager;
        typeof(PickupManager).GetField("xpList", P).SetValue(pickupManager, new PickupStackableList(PickupManager.maxXpObjects, EPickup.Xp));
        var pool = root.AddComponent<PoolManager>(); PoolManager.Instance = pool; var pickupObject = new GameObject("xp test"); pickupObject.SetActive(false);
        var pickup = pickupObject.AddComponent<Pickup>(); pickup.ePickup = EPickup.Xp; pickup.Set(Vector3.zero, 3, 0f);
        pool.xpPool = new ObjectPool<GameObject>(() => pickupObject, null, obj => obj.SetActive(false)); pool.xpPool.Get();
        try
        {
            Call(audio, "Awake"); Call(audio, "Start");
            Require(AudioManager.Instance == audio && Mathf.Abs(Get<float>(audio, "baseXpVolume") - .4f) < .0001f, "startup");
            save.config.cfAudioSettings.xp_and_gold = .35f; SaveManager.A_SavesLoaded?.Invoke(); Require(Mathf.Abs(Get<float>(audio, "xpAndGoldVolume") - .35f) < .0001f, "save volume");
            save.config.cfAudioSettings.xp_and_gold = .7f; CurrentSettings.A_SettingUpdated?.Invoke("xp_and_gold", .35f, .7f); Require(Mathf.Abs(Get<float>(audio, "xpAndGoldVolume") - .7f) < .0001f, "setting update");
            audio.PlayXp(); audio.PlayGold(); Require(Get<int>(audio, "xpPerInterval") == 1 && Get<int>(audio, "goldPerInterval") == 1, "pickup playback counters");
            typeof(Pickup).GetMethod("ApplyPickup", P).Invoke(pickup, null);
            Require(Get<int>(audio, "xpPerInterval") == 2 && pool.xpPool.CountInactive == 1 && (int)typeof(PlayerInventory).GetField("pendingXp", P).GetValue(player.inventory) == 3, "actual XP pickup plays sound, awards XP and returns to pool");
            Set(audio, "xpPerInterval", 60); Set(audio, "goldPerInterval", 40); MyTime.time = 0f; Call(audio, "Update");
            Require(Mathf.Abs(Get<float>(audio, "xpVolumeMultiplier") - .85f) < .0001f && Mathf.Abs(Get<float>(audio, "goldVolumeMultiplier") - .925f) < .0001f, "pickup volume throttling");
            Require(Get<int>(audio, "xpPerInterval") == 0 && Get<int>(audio, "goldPerInterval") == 0, "pickup counters reset");
            Set(audio, "nextMenuSelectTime", -1f); Set(audio, "nextMenuEnterTime", -1f); audio.PlayButtonSelect(); audio.PlayButtonEnter(); Call(audio, "LateUpdate");
            Require(audio.uiClick.s.clip == click && audio.uiSelect.s.clip == null, "click wins when selection and enter are queued together");
            audio.PlaySfx(select); Require(audio.customSfx.s.clip == select, "custom clip");
            Call(audio, "OnDestroy"); save.config.cfAudioSettings.xp_and_gold = .1f; SaveManager.A_SavesLoaded?.Invoke(); Require(Mathf.Abs(Get<float>(audio, "xpAndGoldVolume") - .7f) < .0001f, "unsubscribed on destroy");
            Debug.Log("AudioManagerPortCheck passed: settings, pickup throttling, UI priority, custom sound and cleanup");
        }
        finally { Call(audio, "OnDestroy"); UnityEngine.Object.DestroyImmediate(pickupObject); UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(click); UnityEngine.Object.DestroyImmediate(select); AudioManager.Instance = null; MyPlayer.Instance = null; GameManager.Instance = null; PickupManager.Instance = null; PoolManager.Instance = null; typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { null }); }
    }
}
