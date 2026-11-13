#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Saves___Serialization.SaveFiles.Configs;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Assets.Scripts.Saves___Serialization.Progression;
using Assets.Scripts._Data.MapsAndStages;
using Newtonsoft.Json;
using UnityEngine;
using Object = UnityEngine.Object;

public static class OpenBonkContentRegistrationCheck
{
    private static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
    private static void Throws<T>(Action action) where T : Exception
    {
        bool rejected = false;
        try { action(); } catch (T) { rejected = true; }
        Require(rejected, "Expected " + typeof(T).Name);
    }
    public static void Run()
    {
        var objects = new List<Object>();
        var previousManager = DataManager.Instance; var previousEvent = DataManager.A_DataLoaded;
        try
        {
            DataManager.A_DataLoaded = null;
            var host = new GameObject("registration fixture"); objects.Add(host); var manager = host.AddComponent<DataManager>();
            foreach (var field in typeof(DataManager).GetFields(BindingFlags.Instance | BindingFlags.Public))
                if (typeof(IList).IsAssignableFrom(field.FieldType) && field.GetValue(manager) == null) field.SetValue(manager, Activator.CreateInstance(field.FieldType));
            var weapon = ScriptableObject.CreateInstance<WeaponData>(); objects.Add(weapon); weapon.isEnabled = true; weapon.eWeapon = EWeapon.FireStaff;
            var character = ScriptableObject.CreateInstance<CharacterData>(); objects.Add(character); character.isEnabled = true; character.eCharacter = ECharacter.Fox; character.weapon = weapon; character.StatCategoryRatios = new List<StatCategoryRatio>();
            var prefab = new GameObject("fixture character"); objects.Add(prefab); character.prefab = prefab;
            var skin = ScriptableObject.CreateInstance<SkinData>(); objects.Add(skin); skin.character = ECharacter.Fox;
            var shader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard"); Require(shader != null, "fixture shader");
            var material = new Material(shader); objects.Add(material); skin.materials = new[] { material };
            manager.unsortedWeapons.Add(weapon); manager.unsortedCharacterData.Add(character); manager.unsortedSkins.Add(skin);
            Throws<InvalidOperationException>(() => manager.RegisterExternalCharacter("pikachu", "hero", "bolt", character, weapon, skin));
            manager.Load();
            var customWeapon = Object.Instantiate(weapon); objects.Add(customWeapon);
            var customCharacter = Object.Instantiate(character); objects.Add(customCharacter); customCharacter.weapon = customWeapon;
            var customSkin = Object.Instantiate(skin); objects.Add(customSkin);
            var charId = (ECharacter)OpenBonkContentIds.Character("pikachu", "hero");
            var weaponId = (EWeapon)OpenBonkContentIds.Weapon("pikachu", "bolt");
            Require((int)charId >= 0x40000000 && (int)weaponId >= 0x40000000, "reserved ID range");
            Require(OpenBonkContentIds.Character("pikachu", "hero") == (int)charId, "stable ID");
            Require(OpenBonkContentIds.Character("other", "hero") != (int)charId, "namespaced ID");
            Throws<ArgumentException>(() => OpenBonkContentIds.Character("Invalid Name", "hero"));
            manager.RegisterExternalCharacter("pikachu", "hero", "bolt", customCharacter, customWeapon, customSkin);
            Require(manager.GetCharacterData(ECharacter.Fox) == character && manager.GetWeapon(EWeapon.FireStaff) == weapon, "original content changed");
            Require(manager.GetCharacterData(charId) == customCharacter && manager.GetWeapon(weaponId) == customWeapon && manager.GetSkin(charId, 0) == customSkin, "registration lookup");
            Require(customCharacter.weapon == customWeapon && customCharacter.eCharacter == charId && customWeapon.eWeapon == weaponId, "linked IDs");
            int before = manager.unsortedCharacterData.Count;
            var duplicateCharacter = Object.Instantiate(character); objects.Add(duplicateCharacter);
            var duplicateWeapon = Object.Instantiate(weapon); objects.Add(duplicateWeapon); duplicateCharacter.weapon = duplicateWeapon;
            var duplicateSkin = Object.Instantiate(skin); objects.Add(duplicateSkin);
            Throws<InvalidOperationException>(() => manager.RegisterExternalCharacter("pikachu", "hero", "bolt", duplicateCharacter, duplicateWeapon, duplicateSkin));
            Require(manager.unsortedCharacterData.Count == before && manager.GetCharacterData(charId) == customCharacter, "duplicate modified registry");
            var prefs = new Preferences { selectedCharacter = charId };
            Require(prefs.GetCharacterSkin(charId) == 0, "default mod skin"); prefs.characterSkins[charId] = 2;
            string json = JsonConvert.SerializeObject(prefs);
            var restored = JsonConvert.DeserializeObject<Preferences>(json);
            Require(restored.selectedCharacter == charId && restored.GetCharacterSkin(charId) == 2 && restored.GetCharacterSkin(ECharacter.Fox) == 0, "preference JSON round trip");
            var progress = new ProgressionSaveFile();
            progress.GetCharacterProgression(ECharacter.Fox).xp = 42; progress.GetCharacterProgression(charId).xp = 234;
            progress.GetCharacterProgression(charId).numRuns = 3; progress.silver = 999;
            var restoredProgress = JsonConvert.DeserializeObject<ProgressionSaveFile>(JsonConvert.SerializeObject(progress));
            restoredProgress.Init();
            Require(restoredProgress.GetCharacterProgression(ECharacter.Fox).xp == 42 && restoredProgress.GetCharacterProgression(charId).xp == 234 &&
                restoredProgress.GetCharacterProgression(charId).numRuns == 3 && restoredProgress.silver == 999, "progression JSON round trip");
            restoredProgress.OnDestroy();
            // Distinct names deliberately found to collide in the 28-bit ID range.
            Require(OpenBonkContentIds.Character("collision", "char-13860") == OpenBonkContentIds.Character("collision", "char-28404"), "collision fixture changed");
            var c1 = Object.Instantiate(character); objects.Add(c1); var w1 = Object.Instantiate(weapon); objects.Add(w1); var s1 = Object.Instantiate(skin); objects.Add(s1); c1.weapon = w1;
            var c2 = Object.Instantiate(character); objects.Add(c2); var w2 = Object.Instantiate(weapon); objects.Add(w2); var s2 = Object.Instantiate(skin); objects.Add(s2); c2.weapon = w2;
            manager.RegisterExternalCharacter("collision", "char-13860", "weapon-one", c1, w1, s1);
            var existing = c1.eCharacter; int beforeCollision = manager.unsortedCharacterData.Count;
            Throws<InvalidOperationException>(() => manager.RegisterExternalCharacter("collision", "char-28404", "weapon-two", c2, w2, s2));
            Require(manager.unsortedCharacterData.Count == beforeCollision && manager.GetCharacterData(existing) == c1 && c2.eCharacter == ECharacter.Fox, "hash collision modified registry");
            var standalone = ScriptableObject.CreateInstance<OpenBonkLuaWeaponData>(); objects.Add(standalone);
            standalone.isEnabled = true; standalone.luaSource = "return {attack=function() return 1 end}";
            var icon = new Texture2D(2, 2); objects.Add(icon); standalone.icon = icon;
            standalone.upgradeData = OpenBonkLuaWeaponData.CreateUpgrades(null); objects.Add(standalone.upgradeData);
            int priorWeapons = manager.GetAllWeapons().Count;
            manager.RegisterExternalWeapon("standalone", "bolt", standalone);
            var standaloneId = (EWeapon)OpenBonkContentIds.Weapon("standalone", "bolt");
            Require(manager.GetWeapon(standaloneId) == standalone && standalone.baseStats != null && manager.GetAllWeapons().Count == priorWeapons + 1, "standalone weapon registration");
            Throws<InvalidOperationException>(() => manager.RegisterExternalWeapon("standalone", "bolt", standalone));
            Require(manager.GetAllWeapons().Count == priorWeapons + 1, "duplicate weapon altered registry");
            var map = ScriptableObject.CreateInstance<OpenBonkExternalMapData>(); objects.Add(map);
            var stage = ScriptableObject.CreateInstance<StageData>(); objects.Add(stage);
            map.isEnabled = true; map.displayName = "Example map"; map.description = "A test map"; map.icon = icon; map.stages = new[] { stage };
            var mapId = (EMap)OpenBonkContentIds.Map("example", "world");
            Require((int)mapId >= 0x40000000 && OpenBonkContentIds.Map("example", "world") == (int)mapId && OpenBonkContentIds.Map("other", "world") != (int)mapId, "map identity");
            manager.RegisterExternalMap("example", "world", map);
            Require(manager.GetMap(mapId) == map && map.GetName() == "Example map" && map.GetDescription() == "A test map" && map.GetInternalName() == "example:world", "map lookup and text");
            int beforeMaps = manager.maps.Count;
            var duplicateMap = ScriptableObject.CreateInstance<OpenBonkExternalMapData>(); objects.Add(duplicateMap);
            duplicateMap.isEnabled = true; duplicateMap.displayName = "Duplicate"; duplicateMap.description = "Test"; duplicateMap.icon = icon; duplicateMap.stages = new[] { stage };
            Throws<InvalidOperationException>(() => manager.RegisterExternalMap("example", "world", duplicateMap));
            Require(manager.maps.Count == beforeMaps && manager.GetMap(mapId) == map, "duplicate map changed registry");
            var incompleteMap = ScriptableObject.CreateInstance<OpenBonkExternalMapData>(); objects.Add(incompleteMap);
            Throws<ArgumentException>(() => manager.RegisterExternalMap("example", "broken", incompleteMap));
            var menuMeta = new MenuMeta { lastSelectedMap = mapId }; menuMeta.SetTier(mapId, 2); menuMeta.SetTierCompletion(mapId, 1);
            var restoredMapMeta = JsonConvert.DeserializeObject<MenuMeta>(JsonConvert.SerializeObject(menuMeta));
            Require(restoredMapMeta.lastSelectedMap == mapId && restoredMapMeta.GetLastSelectedTier(mapId) == 2 && restoredMapMeta.IsTierCompleted(mapId, 1), "map progression JSON round trip");
            Require(OpenBonkContentIds.Map("collision", "map-753") == OpenBonkContentIds.Map("collision", "map-13765"), "map collision fixture changed");
            var collisionMap = ScriptableObject.CreateInstance<OpenBonkExternalMapData>(); objects.Add(collisionMap);
            collisionMap.isEnabled = true; collisionMap.displayName = "Collision"; collisionMap.description = "Test"; collisionMap.icon = icon; collisionMap.stages = new[] { stage };
            manager.RegisterExternalMap("collision", "map-753", collisionMap);
            var collisionMap2 = ScriptableObject.CreateInstance<OpenBonkExternalMapData>(); objects.Add(collisionMap2);
            collisionMap2.isEnabled = true; collisionMap2.displayName = "Collision"; collisionMap2.description = "Test"; collisionMap2.icon = icon; collisionMap2.stages = new[] { stage };
            int priorMapCount = manager.maps.Count;
            Throws<InvalidOperationException>(() => manager.RegisterExternalMap("collision", "map-13765", collisionMap2));
            Require(manager.maps.Count == priorMapCount && manager.GetMap(collisionMap.eMap) == collisionMap && collisionMap2.eMap == EMap.None, "map collision altered registry");
            Debug.Log("OpenBonkContentRegistrationCheck PASS: IDs, original preservation, registration, duplicate and hash collision rejection, preference and progression round trip");
            Debug.Log("OpenBonk map registration PASS: stable identity, lookup, validation, duplicate and collision rejection, map progress JSON round trip");
        }
        finally
        {
            DataManager.A_DataLoaded = previousEvent; DataManager.Instance = previousManager;
            for (int i = objects.Count - 1; i >= 0; i--) if (objects[i] != null) Object.DestroyImmediate(objects[i]);
        }
    }
}
#endif
