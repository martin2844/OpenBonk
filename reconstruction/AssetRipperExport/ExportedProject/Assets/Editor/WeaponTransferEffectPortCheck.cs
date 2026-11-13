using System;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;

public static class WeaponTransferEffectPortCheck
{
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Tick(object instance) => instance.GetType().GetMethod("Update", Private).Invoke(instance, null);
    static int Phase(object instance) => (int)instance.GetType().GetField("phase", Private).GetValue(instance);
    public static void Run()
    {
        var root = new GameObject("transfer fixture"); root.SetActive(false);
        var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.height = 2f;
        player.transform.position = new Vector3(1f, 3f, 2f);
        var manager = root.AddComponent<EffectManager>();
        manager.stealItemWui = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/StealWeaponWUI.prefab");
        manager.giveItemWui = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/ReturnWeaponWUI.prefab");
        var weapon = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/MonoBehaviour/WeaponSword.asset");
        var target = new GameObject("boss target"); target.transform.position = new Vector3(11f, 3f, 2f);
        StealWeaponWui steal = null; ReturnWeaponWui give = null;
        try {
            Check(manager.stealItemWui != null && manager.giveItemWui != null && weapon != null, "original assets missing");
            manager.TakeItem(weapon, target.transform, Vector3.up, 2f, 9f, 1.5f);
            manager.GiveItem(weapon);
            steal = UnityEngine.Object.FindObjectOfType<StealWeaponWui>();
            give = UnityEngine.Object.FindObjectOfType<ReturnWeaponWui>();
            Check(steal != null && give != null, "effect instances missing");
            Check(steal.icon.texture == weapon.GetIcon() && give.icon.texture == weapon.GetIcon(), "weapon icons");
            Check(steal.transform.position == player.transform.position && give.transform.localScale == Vector3.zero, "initial position and scale");
            MyTime.deltaTime = 0.75f; Tick(steal); Tick(give);
            Check(Mathf.Abs(steal.transform.position.y - 5f) < 0.001f && Mathf.Abs(give.transform.position.y - 5f) < 0.001f, "rise to player height");
            MyTime.deltaTime = 1.25f; Tick(steal); Tick(give);
            Check(Phase(steal) == 1 && Phase(give) == 1, "hover completion");
            MyTime.deltaTime = 0.5f; Tick(steal); Tick(give);
            Check(Mathf.Abs(steal.transform.position.x - 6f) < 0.001f && Mathf.Abs(steal.transform.position.y - 4.5f) < 0.001f, "theft travels to boss with offset");
            Check(Mathf.Abs(give.transform.position.y - 4f) < 0.001f, "return moves back to player");
            Debug.Log("WeaponTransferEffectPortCheck passed: original prefabs, icons, hover, theft and return movement");
        } finally {
            MyPlayer.Instance = null; MyTime.deltaTime = 0f;
            if (steal != null) UnityEngine.Object.DestroyImmediate(steal.gameObject);
            if (give != null) UnityEngine.Object.DestroyImmediate(give.gameObject);
            UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
