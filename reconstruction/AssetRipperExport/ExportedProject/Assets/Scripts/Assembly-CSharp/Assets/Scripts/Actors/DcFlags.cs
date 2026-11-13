using System;

namespace Assets.Scripts.Actors
{
	[Flags]
	public enum DcFlags
	{
		None = 0,
		BypassEvade = 1,
		BossDamage = 2,
		BypassAegis = 4,
		FinalBossDamage = 8,
		IgnoreArmor = 0x10,
		BypassAll = 5
	}
}
