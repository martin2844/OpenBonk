using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive;
using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(menuName = "Me/Passive", order = 1)]
public class PassiveData : ScriptableObject
{
	public LocalizedString localizedName;

	public LocalizedString localizedDescription;

	public EPassive ePassive;

	public Texture icon;

	private PassiveAbility dummyPassive;

	public virtual string GetName()
	{
		return localizedName.GetLocalizedString();
	}

	public virtual string GetDescription()
	{
		return dummyPassive.GetDescription(localizedDescription);
	}

	public void Init()
	{
		if (dummyPassive == null) dummyPassive = PassiveAbilityFactory.CreatePassiveAbility(this);
	}
}
