using System;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts._Data;
using Assets.Scripts.UI.Localization;
using UnityEngine;

[CreateAssetMenu(menuName = "Me/Skin", order = 1)]
public class SkinData : UnlockableBase, IComparable<SkinData>
{
	public Texture icon;

	public Material[] materials;

	public ECharacter character;

	public MyAchievement unlockRequirement;

	public ESkinType skinType;

	public override string GetName()
	{
		return localizedName.IsEmpty ? "[MISSING LOCALIZATION]" : localizedName.GetLocalizedString() + " " + LocalizationUtility.GetCharacterName(character);
	}

	public override string GetDescription()
	{
		return GetName();
	}

	public override int GetPrice()
	{
		return Mathf.RoundToInt((int)skinType * 6 * 1.75f);
	}

	public override Texture GetIcon()
	{
		return icon;
	}

	public override MyAchievement GetUnlockRequirement()
	{
		return unlockRequirement;
	}

	public override UnlockableBase GetUnlockableRequirement()
	{
		return null;
	}

	public override string GetUnlockableTypeDisplayString()
	{
		return LocalizationUtility.GetLocalizedString("Unlockables", "CHARACTER_COLOR", "Skin");
	}

	public override string GetInternalName()
	{
		return string.Format("{0}{1}", name, character);
	}

	public int CompareTo(SkinData other)
	{
		if (ReferenceEquals(this, other)) return 0;
		if (ReferenceEquals(other, null)) return 1;
		return character != other.character ? character.CompareTo(other.character) : skinType.CompareTo(other.skinType);
	}
}
