using System;
using System.Collections.Generic;
using Actors.Enemies;
using Assets.Scripts.Audio.Music;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.UI.InGame.Rewards;
using Assets.Scripts._Data.Hats;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts._Data.ShopItems;
using Assets.Scripts._Data.Tomes;
using UnityEngine;

public class DataManager : MonoBehaviour
{
	public List<ShopItemData> unsortedShopItems;

	public List<WeaponData> unsortedWeapons;

	public List<CharacterData> unsortedCharacterData;

	public List<TomeData> unsortedTomes;

	public List<MapData> maps;

	public List<EnemyData> unsortedEnemies;

	public List<EncounterData> unsortedEncounters;

	public List<MyAchievement> unsortedAchievements;

	public List<ItemData> unsortedItems;

	public List<UnlockableBase> unsortedUnlockables;

	public List<SkinData> unsortedSkins;

	public List<MusicTrack> unsortedMusic;

	public List<HatData> unsortedHats;

	private Dictionary<EWeapon, WeaponData> weapons = new Dictionary<EWeapon, WeaponData>();

	private Dictionary<ECharacter, CharacterData> characterData = new Dictionary<ECharacter, CharacterData>();

	private Dictionary<ETome, TomeData> tomeData = new Dictionary<ETome, TomeData>();

	private Dictionary<EEnemy, EnemyData> enemyData = new Dictionary<EEnemy, EnemyData>();

	private Dictionary<EEncounter, EncounterData> encounterData = new Dictionary<EEncounter, EncounterData>();

	private Dictionary<EItem, ItemData> itemData = new Dictionary<EItem, ItemData>();

	private Dictionary<string, MyAchievement> achievementsData = new Dictionary<string, MyAchievement>();

	private Dictionary<ECharacter, List<SkinData>> skinData = new Dictionary<ECharacter, List<SkinData>>();

	private Dictionary<EHat, HatData> hatData = new Dictionary<EHat, HatData>();
	private bool externalRegistrationReady;

	public static Action A_DataLoaded;

	public static DataManager Instance;

	public Dictionary<EShopItem, ShopItemData> shopItems { get; } = new Dictionary<EShopItem, ShopItemData>();

	public void Load()
	{
		Instance = this;
		foreach (ShopItemData item in unsortedShopItems) shopItems.Add(item.eShopItem, item);
		foreach (WeaponData weapon in unsortedWeapons)
			if (weapon.isEnabled) { weapon.Init(); weapons.Add(weapon.eWeapon, weapon); }
		foreach (CharacterData character in unsortedCharacterData)
			if (character.isEnabled) { characterData.Add(character.eCharacter, character); character.Init(); }
		foreach (MapData map in new List<MapData>(maps)) if (!map.isEnabled) maps.Remove(map);
		foreach (TomeData tome in unsortedTomes) if (tome.isEnabled) tomeData.Add(tome.eTome, tome);
		foreach (EnemyData enemy in unsortedEnemies) enemyData.Add(enemy.enemyName, enemy);
		foreach (EncounterData encounter in unsortedEncounters) encounterData.Add(encounter.encounterType, encounter);
		foreach (MyAchievement achievement in unsortedAchievements)
			if (achievement.isEnabled) achievementsData.Add(achievement.internalName, achievement);
		foreach (ItemData item in unsortedItems)
			if (item.isEnabled) { itemData.Add(item.eItem, item); item.GetDummyItem(); }
		foreach (UnlockableBase unlockable in unsortedUnlockables)
		{
			MyAchievement achievement = unlockable.GetUnlockRequirement();
			if (achievement != null && achievement.isEnabled) achievement.SetUnlockable(unlockable);
		}
		foreach (SkinData skin in unsortedSkins)
		{
			if (!skinData.ContainsKey(skin.character)) skinData.Add(skin.character, new List<SkinData>());
			skinData[skin.character].Add(skin);
		}
		foreach (HatData hat in unsortedHats) if (hat.isEnabled) hatData.Add(hat.eHat, hat);
		externalRegistrationReady = true;
		A_DataLoaded?.Invoke();
	}

	// Runtime content adapter. Loading and registering packages is a separate
	// operation; no external content is enabled merely by adding this method.
	public void RegisterExternalCharacter(string packageId, string characterKey, string weaponKey,
		CharacterData character, WeaponData weapon, SkinData defaultSkin)
	{
		if (!externalRegistrationReady) throw new InvalidOperationException("Game data not loaded");
		if (character == null || weapon == null || defaultSkin == null || !character.isEnabled || !weapon.isEnabled ||
			character.weapon != weapon || character.prefab == null || character.StatCategoryRatios == null ||
			defaultSkin.materials == null || defaultSkin.materials.Length == 0)
			throw new ArgumentException("Incomplete character content");
		var characterId = (ECharacter)OpenBonkContentIds.Character(packageId, characterKey);
		var weaponId = (EWeapon)OpenBonkContentIds.Weapon(packageId, weaponKey);
		if (characterData.ContainsKey(characterId) || weapons.ContainsKey(weaponId) || skinData.ContainsKey(characterId) ||
			unsortedCharacterData.Contains(character) || unsortedWeapons.Contains(weapon) || unsortedSkins.Contains(defaultSkin) ||
			unsortedCharacterData.Exists(c => c != null && c.eCharacter == characterId) ||
			unsortedWeapons.Exists(w => w != null && w.eWeapon == weaponId))
			throw new InvalidOperationException("Content ID collision or object already registered");
		var previousCharacter = character.eCharacter; var previousWeapon = weapon.eWeapon; var previousSkin = defaultSkin.character;
		try
		{
			character.eCharacter = characterId; weapon.eWeapon = weaponId; defaultSkin.character = characterId;
			weapon.Init(); character.Init();
		}
		catch
		{
			character.eCharacter = previousCharacter; weapon.eWeapon = previousWeapon; defaultSkin.character = previousSkin;
			throw;
		}
		weapons.Add(weaponId, weapon); characterData.Add(characterId, character); skinData.Add(characterId, new List<SkinData> { defaultSkin });
		unsortedWeapons.Add(weapon); unsortedCharacterData.Add(character); unsortedSkins.Add(defaultSkin);
	}

	public void RegisterExternalItem(string packageId, string itemKey, OpenBonkItemData item)
	{
		if (!externalRegistrationReady) throw new InvalidOperationException("Game data not loaded");
		if (item == null || !item.isEnabled || item.icon == null || !item.ValidBonuses() ||
			!Enum.IsDefined(typeof(EItemRarity), item.rarity) || item.maxAmount < 0 || item.maxAmountPerRun < 0 ||
			((item.luaStackChanged || item.luaPreAttack || item.luaOnHit || item.luaLevelUp || item.luaEnemyKilled || item.luaPlayerDamaged || item.luaBeforePlayerDamage || item.luaTick) && (string.IsNullOrEmpty(item.luaSource) || item.luaSource.IndexOf('\0') >= 0 || System.Text.Encoding.UTF8.GetByteCount(item.luaSource) > 65536)))
			throw new ArgumentException("Incomplete item content");
		var id = (EItem)OpenBonkContentIds.Item(packageId, itemKey);
		if (itemData.ContainsKey(id) || unsortedItems.Contains(item) || unsortedItems.Exists(i => i != null && i.eItem == id))
			throw new InvalidOperationException("Content ID collision or object already registered");
		item.eItem = id; itemData.Add(id, item); unsortedItems.Add(item);
	}

	public void RegisterExternalWeapon(string packageId, string weaponKey, OpenBonkLuaWeaponData weapon)
	{
		if (!externalRegistrationReady) throw new InvalidOperationException("Game data not loaded");
		if (weapon == null || !weapon.isEnabled || weapon.icon == null || weapon.upgradeData == null || string.IsNullOrEmpty(weapon.luaSource) ||
			weapon.luaSource.IndexOf('\0') >= 0 || System.Text.Encoding.UTF8.GetByteCount(weapon.luaSource) > 65536)
			throw new ArgumentException("Incomplete weapon content");
		var id = (EWeapon)OpenBonkContentIds.Weapon(packageId, weaponKey);
		if (weapons.ContainsKey(id) || unsortedWeapons.Contains(weapon) || unsortedWeapons.Exists(w => w != null && w.eWeapon == id))
			throw new InvalidOperationException("Content ID collision or object already registered");
		var previous = weapon.eWeapon;
		try { weapon.eWeapon = id; weapon.Init(); }
		catch { weapon.eWeapon = previous; throw; }
		weapons.Add(id, weapon); unsortedWeapons.Add(weapon);
	}

	// Registration only; loading a mod package and producing playable stages
	// requires additional integration with map generation and progression.
	public void RegisterExternalMap(string packageId, string mapKey, OpenBonkExternalMapData map)
	{
		if (!externalRegistrationReady) throw new InvalidOperationException("Game data not loaded");
		if (map == null || !map.isEnabled || string.IsNullOrWhiteSpace(map.displayName) ||
			string.IsNullOrWhiteSpace(map.description) || map.icon == null || map.stages == null ||
			map.stages.Length == 0 || map.stages.Length > 3 || map.achievementRequirement != null)
			throw new ArgumentException("Incomplete map content");
		foreach (var stage in map.stages) if (stage == null) throw new ArgumentException("Missing map stage");
		var id = (EMap)OpenBonkContentIds.Map(packageId, mapKey);
		if (maps == null || maps.Contains(map) || maps.Exists(candidate => candidate != null && candidate.eMap == id))
			throw new InvalidOperationException("Content ID collision or object already registered");
		map.eMap = id; map.externalId = packageId + ":" + mapKey; maps.Add(map);
	}

	public bool TryGetItem(EItem item, out ItemData data) => itemData.TryGetValue(item, out data);
	public bool TryGetCharacterData(ECharacter character, out CharacterData data) => characterData.TryGetValue(character, out data);
	public bool TryGetExternalItemDamageSource(string source, out OpenBonkItemData data)
	{
		data = null;
		if (source == null || !source.StartsWith("mod:", StringComparison.Ordinal) || unsortedItems == null) return false;
		string id = source.Substring(4);
		foreach (var candidate in unsortedItems)
			if (candidate is OpenBonkItemData external && string.Equals(external.externalId, id, StringComparison.Ordinal)) { data = external; return true; }
		return false;
	}

	public ShopItemData GetShopItemData(EShopItem item)
	{
		return shopItems[item];
	}

	public WeaponData GetWeapon(EWeapon weapon)
	{
		return weapons[weapon];
	}

	public CharacterData GetCharacterData(ECharacter character)
	{
		return characterData[character];
	}

	public TomeData GetTome(ETome eTome)
	{
		return tomeData[eTome];
	}

	public MapData GetMap(EMap map)
	{
		foreach (MapData candidate in maps) if (candidate.eMap == map) return candidate;
		return null;
	}

	public List<TomeData> GetAllTomes()
	{
		return new List<TomeData>(tomeData.Values);
	}

	public List<WeaponData> GetAllWeapons()
	{
		return new List<WeaponData>(weapons.Values);
	}

	public EnemyData GetEnemyData(EEnemy eEnemy)
	{
		return enemyData.ContainsKey(eEnemy) ? enemyData[eEnemy] : null;
	}

	public EncounterData GetEncounter(EEncounter encounter)
	{
		return encounterData[encounter];
	}

	public MyAchievement GetAchievement(string internalName)
	{
		return achievementsData.ContainsKey(internalName) ? achievementsData[internalName] : null;
	}

	public ItemData GetItem(EItem item)
	{
		return itemData[item];
	}

	public List<UnlockableBase> GetAllPurchasable()
	{
		var result = new List<UnlockableBase>();
		if (unsortedCharacterData != null) result.AddRange(unsortedCharacterData);
		if (unsortedWeapons != null) result.AddRange(unsortedWeapons);
		if (unsortedTomes != null) result.AddRange(unsortedTomes);
		if (unsortedItems != null) result.AddRange(unsortedItems);
		return result;
	}

	private string GetCharactersPath()
	{
		return null;
	}

	private string GetTomePath()
	{
		return null;
	}

	private string GetDataPath()
	{
		return null;
	}

	public List<SkinData> GetSkins(ECharacter character)
	{
		return skinData[character];
	}

	public SkinData GetSkin(ECharacter character, int savedIndex)
	{
		List<SkinData> skins = skinData[character];
		return skins[savedIndex < 0 || savedIndex >= skins.Count ? 0 : savedIndex];
	}

	public HatData GetHat(EHat eHat)
	{
		return eHat == EHat.None ? null : hatData[eHat];
	}
}
