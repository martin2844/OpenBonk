using System.Collections.Generic;
using Assets.Scripts.Audio.Music;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using UnityEngine;

[CreateAssetMenu(menuName = "Me/Character", order = 1)]
public class CharacterData : UnlockableBase
{
	[Header("Meta")]
	public ECharacter eCharacter;

	public Texture icon;

	[Header("Stats")]
	public List<StatModifier> statModifiers;

	[Header("Other")]
	public GameObject prefab;

	public AudioClip[] audioFootsteps;

	public MusicTrack themeSong;

	[Header("Data")]
	public WeaponData weapon;

	public PassiveData passive;

	public float colliderHeight;

	public float colliderWidth;

	public Dictionary<EStatCategory, float> categoryScores;

	public Dictionary<EStatCategory, float> categoryRatios;

	public List<StatCategoryRatio> StatCategoryRatios;

	[Header("Unlocks")]
	public MyAchievement achievementRequirement;

	public int numQuestsRequiredForVisibilityInCharacterSelection;

	[Header("Renderer")]
	public Mesh meshDefault;

	public Mesh meshLow;

	public Mesh meshLowest;

	public void Init()
	{
		categoryRatios = new Dictionary<EStatCategory, float>();
		foreach (var ratio in StatCategoryRatios) categoryRatios[ratio.category] = ratio.value;
		if (passive != null) passive.Init();
	}

	public override Texture GetIcon()
	{
		return icon;
	}

	public override MyAchievement GetUnlockRequirement()
	{
		return achievementRequirement;
	}

	public bool IsBlackedOutInCharacterSelectionScreen()
	{
		if (achievementRequirement != null && achievementRequirement.IsUnlocked()) return false;
		return Assets.Scripts.Saves___Serialization.Progression.Stats.MyStats.GetStat(Assets.Scripts.Saves___Serialization.Progression.Stats.EMyStat.questsCompleted.ToString()) < numQuestsRequiredForVisibilityInCharacterSelection;
	}

	public override UnlockableBase GetUnlockableRequirement()
	{
		return weapon != null && weapon.GetUnlockRequirement() != null ? weapon : null;
	}

	public override string GetUnlockableTypeDisplayString()
	{
		return Assets.Scripts.UI.Localization.LocalizationUtility.GetLocalizedString("Unlockables", "CHARACTER", "Character");
	}

	public override string GetInternalName()
	{
		return eCharacter.ToString();
	}

	public int GetDisplayRank()
	{
		return SaveManager.Instance.progression.GetCharacterProgression(eCharacter).GetRank();
	}
}
