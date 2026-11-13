#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Inventory__Items__Pickups.Xp_and_Levels;
using Inventory__Items__Pickups;
using UnityEngine;

// Opt-in controlled XP test of the launcher-imported character's real passive.
internal sealed class AndroidPikachuXpPassiveProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.splittest" || !intent.Call<bool>("getBooleanExtra", "openbonk_pikachu_xp_passive_probe", false)) return;
        var go = new GameObject("Pikachu XP passive probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidPikachuXpPassiveProbe>();
    }
    IEnumerator Start()
    {
        float end = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < end && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance;
        if (player == null || player.inventory == null || player.inventory.playerXp.level != 0 || !(player.inventory.passiveAbility is OpenBonkLevelPassive) || player.character != (ECharacter)OpenBonkContentIds.Character("pikachu", "pikachu")) { Debug.LogError("Pikachu XP passive INCONCLUSIVE: initial player or passive unavailable"); yield break; }
        EnemyManager.Instance.enabledWaves = false;
        foreach (WeaponBase weapon in player.inventory.weaponInventory.weapons.Values) weapon.Disable();
        var inv = player.inventory; inv.playerStats.ForceUpdateStats();
        float baseChance = PlayerStats.GetStat(EStat.PowerupChance), baseDuration = PlayerStats.GetStat(EStat.DurationMultiplier);
        inv.playerXp.AddXp(XpUtility.LevelToXp(1)); inv.playerStats.ForceUpdateStats();
        int level1 = inv.playerXp.level; float chance1 = PlayerStats.GetStat(EStat.PowerupChance), duration1 = PlayerStats.GetStat(EStat.DurationMultiplier);
        inv.playerXp.AddXp(XpUtility.LevelToXp(3) - inv.playerXp.xp); inv.playerStats.ForceUpdateStats();
        int level3 = inv.playerXp.level; float chance3 = PlayerStats.GetStat(EStat.PowerupChance), duration3 = PlayerStats.GetStat(EStat.DurationMultiplier);
        bool pass = level1 == 1 && level3 == 3 && Mathf.Abs(chance1-baseChance-.01f) < .001f && Mathf.Abs(duration1-baseDuration-.01f) < .001f && Mathf.Abs(chance3-baseChance-.03f) < .001f && Mathf.Abs(duration3-baseDuration-.03f) < .001f;
        Debug.Log($"Pikachu XP passive {(pass ? "PASS" : "FAIL")}: base={baseChance:F3}/{baseDuration:F3} level1={level1}:{chance1:F3}/{duration1:F3} level3={level3}:{chance3:F3}/{duration3:F3}");
    }
}
#endif
