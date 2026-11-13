#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using System.Linq;
using Assets.Scripts.Actors.Player;
using Assets.Scripts._Data.Tomes;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using UnityEngine;

// Opt-in controlled check; never runs in ordinary gameplay.
internal sealed class AndroidChaosTomeProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_chaos_tome_probe", false)) return;
        var go = new GameObject("Chaos tome probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidChaosTomeProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || DataManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance;
        if (player == null || player.inventory == null) { Debug.LogError("Chaos tome probe INCONCLUSIVE: no player"); yield break; }
        var inv = player.inventory;
        var tome = DataManager.Instance.GetTome(ETome.Chaos);
        if (tome == null || inv.tomeInventory.HasTome(ETome.Chaos)) { Debug.LogError("Chaos tome probe INCONCLUSIVE: tome unavailable or already owned"); yield break; }
        var before = inv.statInventory.permanentChanges.SelectMany(p => p.Value).ToHashSet();
        int upgrades = 0; Action<ETome,EStat> onUpgrade = (id, stat) => { if (id == ETome.Chaos) upgrades++; };
        TomeInventory.A_TomeUpgrade += onUpgrade;
        inv.tomeInventory.AddTome(tome, tome.GetUpgradeOffer(ERarity.Rare), ERarity.Rare);
        var added = inv.statInventory.permanentChanges.SelectMany(p => p.Value).Where(m => !before.Contains(m)).ToArray();
        EStat changed = added.Length == 1 ? added[0].stat : default;
        float statBefore = inv.playerStats.GetStat(changed);
        yield return null;
        yield return new WaitForFixedUpdate();
        yield return null;
        float statAfter = inv.playerStats.GetStat(changed);
        TomeInventory.A_TomeUpgrade -= onUpgrade;
        bool ok = inv.tomeInventory.GetTomeLevel(ETome.Chaos) == 1 && upgrades == 1 && added.Length == 1 && added[0].modification > 0;
        Debug.Log($"Chaos tome probe {(ok ? "PASS" : "FAIL")}: level={inv.tomeInventory.GetTomeLevel(ETome.Chaos)} upgrades={upgrades} added={added.Length} stat={changed} type={(added.Length == 1 ? added[0].modifyType.ToString() : "none")} amount={(added.Length == 1 ? added[0].modification : 0):F4} playerStatBefore={statBefore:F4} playerStatAfter={statAfter:F4}");
    }
}
#endif
