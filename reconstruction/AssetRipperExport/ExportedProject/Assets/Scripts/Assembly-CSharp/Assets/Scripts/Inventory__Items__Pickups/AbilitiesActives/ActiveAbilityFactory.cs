using Assets.Scripts.Inventory__Items__Pickups.AbilitiesActives.ImplementationsFuckYou;

namespace Assets.Scripts.Inventory__Items__Pickups.AbilitiesActives
{
	public static class ActiveAbilityFactory
	{
		public static ActiveAbility CreateAbility(EAbiltiyActive ability)
		{
			return ability == EAbiltiyActive.Dash ? new AbilityDash() : null;
		}
	}
}
