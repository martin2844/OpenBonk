#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using UnityEngine;

// Disposable, intent-gated check; grants/removes the item and disables combat.
internal sealed class AndroidSpikyShieldProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_spiky_shield_probe", false)) return;
        var go = new GameObject("spiky shield probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidSpikyShieldProbe>();
    }

    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance;
        if (player == null || player.inventory == null || EnemyManager.Instance == null) { Debug.LogError("Spiky Shield probe FAIL: gameplay unavailable"); yield break; }
        EnemyManager.Instance.enabledWaves = false;
        player.inventory.weaponInventory.ToggleWeapon(EWeapon.FireStaff, false);
        typeof(MyPlayer).GetField("isInvincible", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, true);
        var items = player.inventory.itemInventory;
        var stats = player.inventory.playerStats;
        int initial = items.GetAmount(EItem.SpikyShield);
        float baseArmor = stats.GetRawStat(EStat.Armor), baseThorns = stats.GetRawStat(EStat.Thorns);
        if (initial != 0) { Debug.LogError("Spiky Shield probe FAIL: expected empty disposable inventory"); yield break; }
        items.AddItem(EItem.SpikyShield);
        yield return new WaitForSeconds(1.6f);
        float armor = stats.GetRawStat(EStat.Armor), thorns = stats.GetRawStat(EStat.Thorns);
        items.RemoveItem(EItem.SpikyShield, false);
        yield return new WaitForSecondsRealtime(.3f);
        float finalArmor = stats.GetRawStat(EStat.Armor), finalThorns = stats.GetRawStat(EStat.Thorns);
        bool ok = items.GetAmount(EItem.SpikyShield) == 0 && Mathf.Abs(armor - baseArmor - .1f) < .001f && Mathf.Abs(thorns - baseThorns - armor * 200f) < .001f && Mathf.Abs(finalArmor - baseArmor) < .001f && Mathf.Abs(finalThorns - baseThorns) < .001f;
        Debug.Log($"Spiky Shield probe {(ok ? "PASS" : "FAIL")}: armor={baseArmor:F3}->{armor:F3}->{finalArmor:F3}, thorns={baseThorns:F3}->{thorns:F3}->{finalThorns:F3}, count={items.GetAmount(EItem.SpikyShield)}");
    }
}
#endif
