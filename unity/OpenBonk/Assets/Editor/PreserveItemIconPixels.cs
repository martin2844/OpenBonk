using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Content icons are small pixel art. ETC2's block color compression changes their
// visible pixels substantially, even when the alpha channel remains intact.
public static class PreserveItemIconPixels
{
    public static void Apply()
    {
        var paths = new HashSet<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:ItemData"))
        {
            var item = AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guid));
            if (item == null || item.icon == null) continue;
            string path = AssetDatabase.GetAssetPath(item.icon);
            if (string.IsNullOrEmpty(path)) throw new Exception("Item icon has no asset path: " + item.name);
            paths.Add(path);
        }
        if (paths.Count != 87) throw new Exception("Expected 87 distinct item icon textures; found " + paths.Count);
        foreach (string guid in AssetDatabase.FindAssets("t:WeaponData"))
        {
            var data = AssetDatabase.LoadAssetAtPath<WeaponData>(AssetDatabase.GUIDToAssetPath(guid));
            if (data != null && data.icon != null) paths.Add(AssetDatabase.GetAssetPath(data.icon));
        }
        foreach (string guid in AssetDatabase.FindAssets("t:CharacterData"))
        {
            var data = AssetDatabase.LoadAssetAtPath<CharacterData>(AssetDatabase.GUIDToAssetPath(guid));
            if (data != null && data.icon != null) paths.Add(AssetDatabase.GetAssetPath(data.icon));
        }
        foreach (string guid in AssetDatabase.FindAssets("t:TomeData"))
        {
            var data = AssetDatabase.LoadAssetAtPath<TomeData>(AssetDatabase.GUIDToAssetPath(guid));
            if (data != null && data.icon != null) paths.Add(AssetDatabase.GetAssetPath(data.icon));
        }
        if (paths.Count != 164 || paths.Contains("")) throw new Exception("Expected 164 distinct content icon textures; found " + paths.Count);
        var powerupObject = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/GenericPowerup.prefab");
        var powerup = powerupObject == null ? null : powerupObject.GetComponent<GenericPowerupPrefab>();
        if (powerup == null) throw new Exception("Generic powerup prefab is missing");
        var materials = new[] { powerup.hpMaterial, powerup.nukeMaterial, powerup.timeFreezeMaterial,
            powerup.shieldMaterial, powerup.rageMaterial, powerup.hasteMaterial, powerup.stonksMaterial, powerup.magnetMaterial };
        foreach (var material in materials)
        {
            if (material == null || material.mainTexture == null) throw new Exception("Powerup icon material or texture is missing");
            paths.Add(AssetDatabase.GetAssetPath(material.mainTexture));
        }
        if (paths.Count != 172 || paths.Contains("")) throw new Exception("Expected 172 distinct content and powerup textures; found " + paths.Count);
        // Both textures are sampled by the real AuraAttack prefab materials;
        // ETC2 changes their alpha gradients and the shared atlas's colors.
        foreach (string path in new[] { "Assets/Texture2D/aura_line.png", "Assets/Texture2D/atlas03.png" })
        {
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null) throw new Exception("Aura texture is missing: " + path);
            paths.Add(path);
        }
        if (paths.Count != 174) throw new Exception("Expected 174 distinct selected textures; found " + paths.Count);
        // Original textures used by weapon attack prefabs and their referenced
        // projectile/effect materials, including the two flask variants.
        foreach (string name in new[] { "atlas01", "atlas01_custom", "atlas02", "atlas02_fade", "atlas02Custom",
            "atlas03_Custom_psd", "Axe_Default", "beam_dark", "FBX_Arrow_Color", "FBX_Banana_Color",
            "FBX_BluetoothBlade_Color", "FBX_Dice_Color", "FBX_Mine_Color", "FBX_PoisonFlask_Color",
            "FBX_PoisonFlask_Color_1", "FBX_PoisonFlask_Color_2",
            "grain", "ring", "SplashParticle", "ThinCircle" })
        {
            string path = "Assets/Texture2D/" + name + ".png";
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null) throw new Exception("Weapon effect texture is missing: " + path);
            paths.Add(path);
        }
        if (paths.Count != 194) throw new Exception("Expected 194 selected textures; found " + paths.Count);
        // The world Borgar pickup uses a separate 32x32 texture, not the item
        // icon. ETC2 preserved its alpha but visibly changed its pixel colors.
        const string borgarPickup = "Assets/Texture2D/Borgar.png";
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(borgarPickup) == null) throw new Exception("Borgar pickup texture is missing");
        paths.Add(borgarPickup);
        if (paths.Count != 195) throw new Exception("Expected 195 selected textures; found " + paths.Count);
        // Quest pickups and shared powerup particles use these additional
        // material textures; ETC2 changes their visible pixel colors.
        foreach (string name in new[] { "atlas_emojis_PSD", "icon_question", "psd_Encounter", "t_key_gold" })
        {
            string path = "Assets/Texture2D/" + name + ".png";
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null) throw new Exception("Pickup effect texture is missing: " + path);
            paths.Add(path);
        }
        if (paths.Count != 199) throw new Exception("Expected 199 selected textures; found " + paths.Count);
        // Enemy attack prefabs use these textures for slam warnings, spikes,
        // pillars and their color ramp. Android block compression altered their
        // visible colors or alpha compared with the decoded original assets.
        foreach (string name in new[] { "2StepLerp", "FBX_PoisonSpike_Color", "FBX_SandPillar_Color",
            "FBX_StoneSpikes_Color", "psd_projectedCircle", "t_ghostSpike", "t_rootSpike", "t_sand" })
        {
            string path = "Assets/Texture2D/" + name + ".png";
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null) throw new Exception("Enemy attack texture is missing: " + path);
            paths.Add(path);
        }
        if (paths.Count != 207) throw new Exception("Expected 207 selected textures; found " + paths.Count);
        // Direct texture references of materials in the loaded enemy roster.
        // PC decoded pixels match these exports; ETC2 alters colors and two
        // alpha channels. 2StepLerp is already included above.
        foreach (string name in new[] { "B_SpiderBoomer_Color", "B_SpiderBoomer_Emission", "bakedtexture 1",
            "Bush_Default", "Ent1", "Ent2", "Ent3", "FBX_CactusEnemy_Color",
            "FBX_GhostGrave_Emission", "fbx_Ghoul_Color", "FBX_GoblinStrong_Color", "FBX_GolemBoss_Color",
            "FBX_GolemSmall_Color", "FBX_Maoi_Color", "FBX_MegachadGhost_Color", "FBX_Mummy_Color",
            "FBX_MummyTank_Color", "FBX_Pharaoh_Color", "FBX_Pig_Color", "FBX_Scorpion_Color",
            "FBX_Scorpion_Color_0", "FBX_Skeleton 1_Color_Dusty", "FBX_Skeleton 1_Color_Normal",
            "FBX_SkeletonKing_Color", "FBX_Slime_Color_Green", "FBX_Tumblebone_Color", "FBX_WeakGoblin_Color",
            "FBX_Wisp_Color", "Head_Forest", "Monster Bee Glow", "Monster Bee_Color", "PSD_SkeletonNew",
            "t_bandit_default", "t_ghost_black", "t_ghost_purple", "t_ghost_red", "t_ghost_white",
            "t_ghostGrave1", "t_ghostGrave2", "t_ghostGrave3", "t_ghostGrave4", "t_goblin_tank",
            "t_golem_default", "t_hat_frog_blue", "t_hat_frog_green", "t_hat_frog_red", "t_mummyTank",
            "t_pharaoh_1", "t_pharaoh_2", "t_pig", "t_scorpion_small" })
        {
            string path = "Assets/Texture2D/" + name + ".png";
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null) throw new Exception("Enemy material texture is missing: " + path);
            paths.Add(path);
        }
        // Exporters can reorder identically named emission textures. Resolve
        // these two through the materials that use them, not filename suffixes.
        foreach (string name in new[] { "m_ghost", "m_george_body_default" })
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/" + name + ".mat");
            var texture = material == null || !material.HasProperty("_EmissionMap") ? null : material.GetTexture("_EmissionMap");
            if (texture == null) throw new Exception("Enemy emission texture is missing: " + name);
            paths.Add(AssetDatabase.GetAssetPath(texture));
        }
        if (paths.Count != 260) throw new Exception("Expected 260 selected textures; found " + paths.Count);
        // This tiny water/lava normal map loses substantial directional detail
        // to ETC block compression; keep its Android pixels uncompressed.
        const string waterNormal = "Assets/Texture2D/IL3DN_WaterNormal.png";
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(waterNormal) == null) throw new Exception("Water normal texture is missing: " + waterNormal);
        paths.Add(waterNormal);
        // The isolated build backs up PNG importer metadata before this step.
        // Abort before changing anything if an asset uses a different format.
        foreach (string path in paths)
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) || path.IndexOf(".androidlib/", StringComparison.OrdinalIgnoreCase) >= 0 || !path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Selected texture is not a backed-up PNG: " + path);
        int updated = 0;
        foreach (string path in paths)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new Exception("Item icon is not a texture: " + path);
            var settings = importer.GetPlatformTextureSettings("Android");
            if (settings.overridden && settings.format == TextureImporterFormat.RGBA32 && settings.textureCompression == TextureImporterCompression.Uncompressed) continue;
            settings.overridden = true;
            settings.format = TextureImporterFormat.RGBA32;
            settings.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(settings);
            importer.SaveAndReimport();
            updated++;
        }
        Debug.Log($"PreserveItemIconPixels: checked {paths.Count} content icons; updated {updated} Android RGBA32 import overrides");
    }
}
