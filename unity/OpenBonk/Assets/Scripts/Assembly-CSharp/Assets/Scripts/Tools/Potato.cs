using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory__Items__Pickups;

namespace Assets.Scripts.Tools
{
	public static class Potato
	{
		private static float lastCollisionTime;

		private static float noCollisionTimeThreshold;

		public static EPotatoFlags flags;

		public static float totalDamageDone;

		public static float dmgMin1;

		public static float dmgMin2;

		public static float dmgMin5;

		public static float dmgMin10;

		private static float dmgMin1Max;

		private static float dmgMin2Max;

		private static float dmgMin5Max;

		private static int totalKills;

		public static int killsMinute1;

		public static int killsMinute2;

		public static int killsMinute5;

		public static int killsMinute10;

		private static int maxKillsMinute1;

		private static int maxKillsMinute2;

		private static int maxKillsMinute5;

		private static int maxKillsMinute10;

		public static int enemyCollisionCalls;

		public static int playerDamageCalls;

		public static int damageBlocksCount;

		public static int damageTakenCount;

		public static int totalDamageTaken;

		private static int lastKillCount;

		private static int lastGoldCount;

		private static bool isRunning;

		private static float nextCheckTime;

		public static void Init()
		{
		}

		public static void Cleanup()
		{
		}

		private static void OnRunStarted()
		{
		}

		private static void OnRunOver()
		{
		}

		private static void OnRunEnded()
		{
		}

		private static void OnPlayerCollided()
		{
		}

		public static void Update()
		{
		}

		private static void TestInput()
		{
		}

		private static void CheckCollision()
		{
		}

		private static void VerifyKillCount()
		{
		}

		private static void VerifyGold()
		{
		}

		private static void VerifyHp()
		{
		}

		private static void OnEnemyDamaged(Enemy enemy, DamageContainer dc)
		{
		}

		private static void VerifyKillCountPerMinute()
		{
		}

		private static void OnHpTamper()
		{
		}

		private static void OnEnemyCollision()
		{
		}

		private static void OnDamageCalled()
		{
		}

		private static void OnDamageStopped()
		{
		}

		private static void OnDamageTaken(PlayerHealth arg1, DamageContainer arg2, bool arg3)
		{
		}

		private static void MarkPotato(EPotatoFlags flag, string message)
		{
		}
	}
}
