using Assets.Scripts.Actors.Enemies;

namespace Actors.Enemies
{
	public static class EnemyBehaviour
	{
		public static void DeathBehaviour(Enemy enemy)
		{
			if (enemy.enemyData.enemyName == EEnemy.BoomerSpider && EffectManager.Instance.currentlyExplodingEnemy.Contains(enemy)) return;
			enemy.dissolve.enabled = true;
			enemy.dissolve.StartDissolve();
		}

		public static void FixedUpdate(Enemy enemy)
		{
			if (enemy.enemyData.enemyName == EEnemy.BoomerSpider && enemy.enemyMovement.distanceToTarget < 5f && !enemy.IsDeadOrDyingNextFrame())
			{
				enemy.DiedNextFrame();
				EffectManager.Instance.ExploderEnemy(enemy);
			}
		}
	}
}
