using System;

namespace Assets.Scripts.Game.Combat.EnemyDebuffs
{
	[Flags]
	public enum EDebuff
	{
		Poison = 1,
		Freeze = 2,
		Burn = 4,
		Stun = 8,
		Echo = 0x10,
		Charm = 0x20,
		Bloodmark = 0x40,
		DebuffsWithCap = 0x2A
	}
}
