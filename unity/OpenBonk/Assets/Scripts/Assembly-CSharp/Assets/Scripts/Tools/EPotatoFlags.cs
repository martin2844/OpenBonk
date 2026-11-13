using System;

namespace Assets.Scripts.Tools
{
	[Flags]
	public enum EPotatoFlags
	{
		None = 0,
		Collision = 1,
		Kills = 2,
		Gold = 4,
		Hp = 8,
		Damage = 0x10,
		KillsPerMinute = 0x20,
		HpTamper = 0x40
	}
}
