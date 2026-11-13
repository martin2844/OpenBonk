using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations;

namespace Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive
{
	public static class PassiveAbilityFactory
	{
		public static PassiveAbility CreatePassiveAbility(PassiveData passive)
		{
			if (passive == null) return new PassiveAbilityMissing();
			if (passive is OpenBonkLevelPassiveData external) return new OpenBonkLevelPassive(external);
			switch (passive.ePassive)
			{
				case EPassive.Bullseye: return new PassiveAbilityBullseye();
				case EPassive.RngBlessing: return new PassiveAbilityRngBlessing();
				case EPassive.SpeedDemon: return new PassiveAbilitySpeedDemon();
				case EPassive.Reinforced: return new PassiveAbilityReinforced();
				case EPassive.Flowstate: return new PassiveAbilityFlowstate();
				case EPassive.CritHappens: return new PassiveAbilityCritHappens();
				case EPassive.Warrior: return new PassiveAbilityWarrior();
				case EPassive.Flex: return new PassiveAbilityFlex();
				case EPassive.WallClimb: return new PassiveAbilityWallClimb();
				case EPassive.Float: return new PassiveAbilityFloating();
				case EPassive.Enduring: return new PassiveAbilityEnduring();
				case EPassive.Plague: return new PassiveAbilityPlague();
				case EPassive.Quantum: return new PassiveAbilityQuantumXp();
				case EPassive.Shadowstep: return new PassiveAbilityShadowstep();
				case EPassive.Gamba: return new PassiveAbilityGamba();
				case EPassive.Vampire: return new PassiveAbilityVampire();
				case EPassive.Curse: return new PassiveAbilityCurse();
				case EPassive.Stonks: return new PassiveAbilityStonks();
				case EPassive.LockIn: return new PassiveAbilityLockIn();
				case EPassive.Zap: return new PassiveAbilityZooma();
				case EPassive.Hoarder: return new PassiveAbilityHoarder();
				default: return new PassiveAbilityMissing();
			}
		}
	}
}
