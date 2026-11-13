using UnityEngine;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;

public class KillEnemyTrigger : MonoBehaviour
{
	public float bossPercentage = 0.15f;

	public float knockback;

	public string customDamageSource = "";

	public bool canOneshotRedGhost = true;

	private float chanceToOneshotRedGhosts = 0.33f;

	public bool isBossLamp;

	private void OnTriggerEnter(Collider collider)
	{
		if (EnemyManager.Instance == null || !EnemyManager.Instance.GetEnemy(collider, out Enemy enemy) || enemy == null || !enemy.CanTakeDamage()) return;
		bool redGhost = !canOneshotRedGhost && enemy.enemyData.enemyName == EEnemy.GhostRed;
		if (!enemy.IsBoss() && (!redGhost || Random.Range(0f, 1f) <= chanceToOneshotRedGhosts))
		{
			enemy.Kill(GetDamageSource());
			return;
		}
		var dc = new DamageContainer(0f, GetDamageSource())
		{
			damage = bossPercentage * enemy.maxHp,
			enemy = enemy,
			knockback = knockback,
			direction = (enemy.transform.position - transform.position + Vector3.up).normalized,
			flags = DcFlags.BypassAll
		};
		enemy.DamageExternal(dc);
		if (enemy.IsDead() && isBossLamp) MyAchievements.TryUnlock("a_bobsLantern");
	}

	private string GetDamageSource()
	{
		return customDamageSource == "" ? DamageContainer.unknownDamageSource : customDamageSource;
	}
}
