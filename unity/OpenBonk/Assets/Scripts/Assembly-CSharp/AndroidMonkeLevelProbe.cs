#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive;
using Assets.Scripts.Menu.Shop;
using UnityEngine;

internal sealed class AndroidMonkeLevelProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_monke_level_probe", false)) return;
        var go = new GameObject("monke level probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidMonkeLevelProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene)) yield return null;
        var player = MyPlayer.Instance;
        if (player == null || player.character != ECharacter.Monke || player.inventory == null || !player.inventory.HasPassive(EPassive.WallClimb))
        { Debug.LogError("Monke level probe INCONCLUSIVE: Monke or passive unavailable"); yield break; }
        var inv = player.inventory;
        int initial = inv.playerXp.level;
        float baseline = inv.playerStats.GetStat(EStat.MaxHealth);
        Debug.Log($"Monke level probe START: level={initial} maxStat={baseline} hp={inv.playerHealth.maxHp}");
        deadline = Time.realtimeSinceStartup + 240f;
        while (Time.realtimeSinceStartup < deadline && inv.playerXp.level == initial && !inv.playerHealth.IsDead()) yield return null;
        if (inv.playerXp.level == initial) { Debug.LogError("Monke level probe INCONCLUSIVE: no natural level-up"); yield break; }
        yield return new WaitForSecondsRealtime(1f);
        int level = inv.playerXp.level;
        bool hasModifier = inv.passiveAbility.statModifiers.ContainsKey(EStat.MaxHealth);
        float actual = inv.playerStats.GetStat(EStat.MaxHealth);
        Debug.Log($"Monke level probe OBSERVED: level={level} maxStat={actual} hp={inv.playerHealth.maxHp} modifier={hasModifier} leveling={LevelupScreen.isLevelingUp}");
    }
}
#endif
