using System;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;

public static class PowerupAppearancePortCheck
{
    static void Require(bool value, string why) { if (!value) throw new Exception("PowerupAppearancePortCheck: " + why); }
    public static void Run()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/GenericPowerup.prefab");
        var instance = UnityEngine.Object.Instantiate(prefab); instance.SetActive(false);
        var powerup = instance.GetComponent<GenericPowerupPrefab>();
        try
        {
            Require(powerup != null && powerup.ps != null && powerup.iconRenderer != null && powerup.minimapRenderer != null, "original prefab wiring");
            var types = new[] { EPickup.Health, EPickup.Nuke, EPickup.Time, EPickup.Shield, EPickup.Rage, EPickup.Haste, EPickup.Stonks, EPickup.Magnet };
            var colors = new[] { "#FF1800", "#000000", "#00FF97", "#00FF41", "#FF42F0", "#00B4FF", "#FFF814", "#FF7F19" };
            var materials = new[] { powerup.hpMaterial, powerup.nukeMaterial, powerup.timeFreezeMaterial, powerup.shieldMaterial, powerup.rageMaterial, powerup.hasteMaterial, powerup.stonksMaterial, powerup.magnetMaterial };
            for (int i = 0; i < types.Length; i++)
            {
                ColorUtility.TryParseHtmlString(colors[i], out Color original);
                Color expected = Color.Lerp(original, Color.white, .2f);
                powerup.Set(types[i]);
                var actual = powerup.ps.main.startColor.color;
                Require(Vector4.Distance(actual, expected) < .001f && Vector4.Distance(MyColorUtility.PickupToColor(types[i]), expected) < .001f, types[i] + " particle color");
                var block = new MaterialPropertyBlock(); powerup.iconRenderer.GetPropertyBlock(block);
                Require(block.GetTexture("_MainTex") == materials[i].mainTexture, types[i] + " icon texture");
                Require(powerup.pickup.ePickup == types[i], types[i] + " pickup type");
            }
            Require(MyColorUtility.PickupToColor(EPickup.Gold) == Color.white, "fallback color");
            Debug.Log("PowerupAppearancePortCheck PASS");
        }
        finally { UnityEngine.Object.DestroyImmediate(instance); }
    }
}
