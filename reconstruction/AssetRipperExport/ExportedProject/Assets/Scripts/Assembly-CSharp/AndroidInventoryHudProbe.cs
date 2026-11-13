#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using UnityEngine;
using UnityEngine.UI;

// Opt-in stress check of the real gameplay inventory HUD on an isolated device.
internal sealed class AndroidInventoryHudProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_hud_stress_probe", false)) return;
        var go = new GameObject("Inventory HUD stress probe");
        DontDestroyOnLoad(go);
        go.AddComponent<AndroidInventoryHudProbe>();
    }

    private IEnumerator Start()
    {
        InventoryHud hud = null;
        MyPlayer player = null;
        float deadline = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < deadline)
        {
            hud = FindFirstObjectByType<InventoryHud>();
            player = MyPlayer.Instance;
            if (hud != null && player != null && player.inventory != null && DataManager.Instance != null) break;
            yield return null;
        }
        if (hud == null || player == null) { Debug.LogError("HUD stress probe: gameplay not found"); yield break; }
        yield return new WaitForSecondsRealtime(2f);
        var inventory = player.inventory;
        foreach (var weapon in DataManager.Instance.GetAllWeapons())
        {
            if (inventory.weaponInventory.weapons.Count >= 12) break;
            if (inventory.weaponInventory.weapons.ContainsKey(weapon.eWeapon)) continue;
            inventory.weaponInventory.AddWeapon(weapon, null);
            if (inventory.weaponInventory.weapons.TryGetValue(weapon.eWeapon, out var added)) added.Disable();
        }
        foreach (var tome in DataManager.Instance.GetAllTomes())
        {
            if (inventory.tomeInventory.tomeLevels.Count >= 8) break;
            inventory.tomeInventory.AddTome(tome, new List<StatModifier>(), ERarity.Common);
        }
        yield return new WaitForSecondsRealtime(2f);
        Canvas.ForceUpdateCanvases();
        var parent = hud.weaponParent.parent;
        var corners = new Vector3[4];
        float bottom = float.PositiveInfinity, top = float.NegativeInfinity;
        int weapons = 0, tomes = 0;
        foreach (RectTransform child in hud.weaponParent)
            if (child.gameObject.activeInHierarchy) { child.GetWorldCorners(corners); bottom = Mathf.Min(bottom, parent.InverseTransformPoint(corners[0]).y); weapons++; }
        foreach (RectTransform child in hud.tomeParent)
            if (child.gameObject.activeInHierarchy) { child.GetWorldCorners(corners); top = Mathf.Max(top, parent.InverseTransformPoint(corners[1]).y); tomes++; }
        Debug.Log($"HUD stress probe: weapons={weapons}, tomes={tomes}, gap={bottom - top:F3}, weaponBottom={bottom:F3}, tomeTop={top:F3}");
    }
}
#endif
