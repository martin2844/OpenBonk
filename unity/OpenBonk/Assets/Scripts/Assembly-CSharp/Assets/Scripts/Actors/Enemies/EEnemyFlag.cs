using System;

namespace Assets.Scripts.Actors.Enemies
{
	[Flags]
	public enum EEnemyFlag
	{
		None = 0,
		Elite = 1,
		Boss = 2,
		StageBoss = 4,
		Challenge = 8,
		SummonerMiniboss = 0x10,
		FinalBoss = 0x20,
		AnyBoss = 0x36
	}
}
