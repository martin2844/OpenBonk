using System.Collections.Generic;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts._Data.Hats;
using Assets.Scripts.UI.Localization;
using UnityEngine;

[CreateAssetMenu(menuName = "Me/Hat", order = 1)]
public class HatData : UnlockableBase
{
	[Header("Hat Data")]
	public EHat eHat;

	public Texture icon;

	public MyAchievement unlockRequirement;

	public bool useCharacterAltMesh;

	[Header("Hat Render")]
	public Mesh mesh;

	public Material material;

	public List<HatOrientation> orientations;

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
		return LocalizationUtility.GetLocalizedString("Unlockables", "HAT", "Hat");
	}

	public override string GetInternalName()
	{
		return name + "_hat";
	}

	public override int CompareTo(UnlockableBase other)
	{
		int comparison = sortingPriority.CompareTo(other.sortingPriority);
		if (comparison != 0) return comparison;
		comparison = GetPrice().CompareTo(other.GetPrice());
		return comparison != 0 ? comparison : string.Compare(GetName(), other.GetName(), System.StringComparison.Ordinal);
	}
}
