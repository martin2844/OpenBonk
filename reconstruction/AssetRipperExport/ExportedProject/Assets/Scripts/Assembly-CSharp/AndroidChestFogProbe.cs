#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Managers;
using Assets.Scripts.UI.InGame.Rewards;
using UnityEngine;

internal sealed class AndroidChestFogProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_chest_fog_probe", false)) return;
        var root = new GameObject("chest fog probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidChestFogProbe>();
    }

    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || MyPlayer.Instance == null || DataManager.Instance == null || UiManager.Instance == null || UiManager.Instance.encounterWindows == null || UiManager.Instance.encounterWindows.HasEncounter())) yield return null;
        var encounter = UiManager.Instance != null ? UiManager.Instance.encounterWindows : null;
        var chest = encounter != null ? encounter.chestWindow as ChestWindowUi : null;
        if (chest == null || chest.chestOpening == null || chest.chestOpening.cam == null) { Debug.LogError("Chest fog probe: camera unavailable"); yield break; }
        var cam = chest.chestOpening.cam;
        var fogController = cam.GetComponent<DisableFogForCamera>();
        if (fogController == null || !fogController.enabled) { Debug.LogError("Chest fog probe: camera fog component unavailable"); yield break; }
        var observer = cam.gameObject.AddComponent<AndroidChestFogObserver>();
        bool priorFog = RenderSettings.fog;
        if (EnemyManager.Instance != null) EnemyManager.Instance.enabledWaves = false;
        RenderSettings.fog = true;
        encounter.AddEncounter(EEncounter.ChestNormal);
        yield return new WaitForSecondsRealtime(1f);
        Debug.Log($"Chest fog probe camera={cam.name} enabled={cam.isActiveAndEnabled} renders={observer.renders} fogDisabledAtPreRender={observer.fogDisabled} globalAfter={RenderSettings.fog}");
        ItemData item = DataManager.Instance.unsortedItems.Find(x => x != null && x.isEnabled);
        if (item == null) { Debug.LogError("Chest fog probe: no item"); yield break; }
        chest.itemData = item; chest.chestOpening.OpenChest(item);
        deadline = Time.realtimeSinceStartup + 12f;
        while (Time.realtimeSinceStartup < deadline && !chest.b_take.gameObject.activeInHierarchy) yield return null;
        bool revealed = chest.b_take.gameObject.activeInHierarchy;
        if (revealed) chest.TakeButton();
        yield return new WaitForSecondsRealtime(.5f);
        bool afterClose = RenderSettings.fog;
        Debug.Log($"Chest fog probe result: renders={observer.renders} fogDisabledAtPreRender={observer.fogDisabled} revealed={revealed} globalAfterClose={afterClose}");
        RenderSettings.fog = priorFog;
        Destroy(observer);
    }
}

internal sealed class AndroidChestFogObserver : MonoBehaviour
{
    public int renders;
    public int fogDisabled;
    void OnPreRender() { renders++; if (!RenderSettings.fog) fogDisabled++; }
}
#endif
