using System.Collections.Generic;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;

namespace Assets.Scripts.Saves___Serialization.Progression.Stats
{
	public class StatTrackingUtility
	{
		private static HashSet<EEnemy> skeletonEnemies = new HashSet<EEnemy> { EEnemy.Skeleton, EEnemy.SkeletonDusty, EEnemy.ArmoredSkeleton, EEnemy.GoldenSkeleton, EEnemy.XpSkeleton, EEnemy.ArmoredSkeletonDusty };

		private static HashSet<EEnemy> wispEnemies = new HashSet<EEnemy> { EEnemy.Wisp };

		private static HashSet<EEnemy> goblinEnemies = new HashSet<EEnemy> { EEnemy.Goblin, EEnemy.GoblinStrong, EEnemy.GoblinTank };

		private static Dictionary<ECharacter, string> keysKillsCharacters = new Dictionary<ECharacter, string>();

		private static Dictionary<EEnemy, string> keysKillsEnemies = new Dictionary<EEnemy, string>();

		private static Dictionary<string, string> keysKillsSources = new Dictionary<string, string>();

		public static bool IsSkeleton(Enemy enemy)
		{
			return skeletonEnemies.Contains(enemy.enemyData.enemyName);
		}

		public static bool IsWisp(Enemy enemy)
		{
			return wispEnemies.Contains(enemy.enemyData.enemyName);
		}

		public static bool IsGoblin(Enemy enemy)
		{
			return goblinEnemies.Contains(enemy.enemyData.enemyName);
		}

		public static string GetKeyKillsCharacter(ECharacter character)
		{
			if (!keysKillsCharacters.TryGetValue(character, out string key)) keysKillsCharacters.Add(character, key = MakeKey(character.ToString()));
			return key;
		}

		public static string GetKeyKillsEnemy(Enemy enemy)
		{
			EEnemy type = enemy.enemyData.enemyName;
			if (!keysKillsEnemies.TryGetValue(type, out string key)) keysKillsEnemies.Add(type, key = MakeKey(type.ToString()));
			return key;
		}

		public static string GetKeyKillsSource(DamageContainer dc)
		{
			string source = dc.damageSource;
			if (!keysKillsSources.TryGetValue(source, out string key)) keysKillsSources.Add(source, key = MakeKey(source));
			return key;
		}

		private static string MakeKey(string value) => char.ToLower(value[0]) + value.Substring(1) + "Kills";
	}
}
