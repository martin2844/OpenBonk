#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Reflection;
using Assets.Scripts._Data;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Progression;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using UnityEngine;
using UnityEngine.EventSystems;

// Seeds a single banish charge, then observes the normal level-up UI and external touch input.
internal sealed class AndroidLevelBanishProbe : MonoBehaviour
{
    bool allTools;
    GameObject lastSelection;
    void Update()
    {
        if (!LevelupScreen.isLevelingUp || EventSystem.current == null) return;
        var selected = EventSystem.current.currentSelectedGameObject;
        if (selected == lastSelection) return;
        lastSelection = selected;
        Debug.Log("Level banish navigation focus: " + (selected != null ? selected.name : "null"));
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_level_banish_probe", false)) return;
        var go = new GameObject("level banish probe"); DontDestroyOnLoad(go);
        go.AddComponent<AndroidLevelBanishProbe>().allTools = intent.Call<bool>("getBooleanExtra", "megabonk_all_tools_probe", false);
    }
    static Vector2 Position(GameObject go)
    {
        var rect = go.GetComponent<RectTransform>(); var canvas = go.GetComponentInParent<Canvas>();
        var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        var point = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
        return new Vector2(point.x, Screen.height - point.y);
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene)) yield return null;
        var player = MyPlayer.Instance;
        if (player == null || player.inventory == null) { Debug.LogError("Level banish probe FAIL: player unavailable"); yield break; }
        player.inventory.banishes = 1; player.inventory.banishesUsed = 0;
        if (allTools)
        {
            player.inventory.skips = 1;
            player.inventory.refreshes = 1;
            player.inventory.AddLevel();
            Debug.Log("Level banish probe: seeded all three utility charges and XP");
        }
        Debug.Log(allTools ? "Level banish probe WAIT: all charges and XP injected" : "Level banish probe WAIT: charge injected, normal XP and combat unchanged");
        deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && !LevelupScreen.isLevelingUp && player != null && !player.IsDead()) yield return null;
        var screen = FindObjectOfType<LevelupScreen>();
        if (!LevelupScreen.isLevelingUp || screen == null) { Debug.LogError("Level banish probe INCONCLUSIVE: no level-up"); yield break; }
        yield return new WaitForSecondsRealtime(2f);
        var picker = screen.upgradePicker;
        var field = typeof(UpgradeButton).GetField("upgradable", BindingFlags.NonPublic | BindingFlags.Instance);
        UpgradeButton targetButton = null; IUpgradable target = null;
        foreach (var button in picker.buttons)
        {
            if (button == null || !button.gameObject.activeInHierarchy) continue;
            var candidate = field.GetValue(button) as IUpgradable;
            if (candidate != null && candidate.GetLevel() == 0) { target = candidate; targetButton = button; break; }
        }
        if (targetButton == null || screen.b_banish == null || !screen.b_banish.gameObject.activeInHierarchy) { Debug.LogError("Level banish probe INCONCLUSIVE: no available new offer or banish button"); yield break; }
        var last = picker.buttons[picker.GetNumUpgrades() - 1].GetButton();
        var down = last.navigation.selectOnDown;
        Debug.Log($"Level banish navigation probe: last={last.name} down={(down != null ? down.name : "null")} banish={screen.b_banish.GetButton().name} tabs={(picker.tabsExplicitNavigation != null)}");
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(picker.buttons[0].gameObject);
        var unlockable = target as UnlockableBase; var banishPos = Position(screen.b_banish.gameObject); var offerPos = Position(targetButton.gameObject);
        Debug.Log($"Level banish probe READY: target={unlockable.GetInternalName()} banishX={Mathf.RoundToInt(banishPos.x)} banishY={Mathf.RoundToInt(banishPos.y)} offerX={Mathf.RoundToInt(offerPos.x)} offerY={Mathf.RoundToInt(offerPos.y)} charges={player.inventory.banishes} gold={player.inventory.gold}");
        deadline = Time.realtimeSinceStartup + 60f;
        while (Time.realtimeSinceStartup < deadline && !picker.banishMode) yield return null;
        if (!picker.banishMode) { Debug.LogError("Level banish probe INCONCLUSIVE: banish mode timeout"); yield break; }
        Debug.Log("Level banish probe MODE: active");
        deadline = Time.realtimeSinceStartup + 60f;
        while (Time.realtimeSinceStartup < deadline && (RunUnlockables.banishedUpgradables == null || !RunUnlockables.banishedUpgradables.Contains(unlockable))) yield return null;
        bool banished = RunUnlockables.banishedUpgradables != null && RunUnlockables.banishedUpgradables.Contains(unlockable);
        yield return new WaitForSecondsRealtime(1f);
        bool available = unlockable is WeaponData weapon ? UnlockUtility.GetAvailableWeapons().Contains(weapon) : unlockable is TomeData tome && UnlockUtility.GetAvailableTomes().Contains(tome);
        bool pass = banished && !available && target.GetLevel() == 0 && player.inventory.banishes == 0 && player.inventory.banishesUsed == 1 && !LevelupScreen.isLevelingUp;
        Debug.Log($"Level banish probe {(pass ? "PASS" : "FAIL")}: target={unlockable.GetInternalName()} banished={banished} stillAvailable={available} level={target.GetLevel()} charges={player.inventory.banishes} used={player.inventory.banishesUsed} leveling={LevelupScreen.isLevelingUp}");
    }
}
#endif
