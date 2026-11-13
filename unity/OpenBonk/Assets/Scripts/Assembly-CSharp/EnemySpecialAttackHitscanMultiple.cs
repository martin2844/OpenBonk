using System.Collections;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemySpecialAttacks;
using Assets.Scripts.Game.Combat.EnemySpecialAttacks.Implementations;
using UnityEngine;

public class EnemySpecialAttackHitscanMultiple : EnemySpecialAttackPrefab
{
	public float delayBetweenAttacks;
	public int numToSpawn;
	private int numSpawned;
	public float maxRange;
	public Vector3 attackOffset;
	public float randomPositionRadius;
	private int generation;

	protected override void Init()
	{
		numSpawned = 0;
		generation++;
		StartCoroutine(DoAttack());
	}

	private IEnumerator DoAttack()
	{
		int attackGeneration = generation;
		for (int i = 0; i < numToSpawn; i++)
		{
			MyPlayer player = MyPlayer.Instance;
			Vector3 target = player.transform.position;
			float selection = (float)global::Utility.MyRandom.random.NextDouble();
			if (selection <= 0.4f) target += player.playerMovement.GetVelocity() * specialAttack.attackChargeTime;
			else if (selection <= 0.8f) target += Random.insideUnitSphere * randomPositionRadius;
			Vector3 start = enemy.GetHeadPosition() + attackOffset;
			Vector3 direction = (target - start).normalized;
			if (!CreateWarningHitscan(start, direction, maxRange, () =>
			{
				if (gameObject.activeSelf && generation == attackGeneration) SpawnHitEffect(start, direction);
			})) yield break;
			yield return WaitForSecondsCustom(delayBetweenAttacks);
		}
	}

	private void SpawnHitEffect(Vector3 pos, Vector3 dir)
	{
		GameObject effect = GetEffectPrefab();
		if (effect != null) { effect.transform.position = pos; effect.transform.rotation = Quaternion.LookRotation(dir); }
		if (Physics.SphereCast(pos, specialAttack.attackRadius, dir.normalized, out _, maxRange, GameManager.Instance.whatIsPlayer))
		{
			MyPlayer player = MyPlayer.Instance;
			player.inventory.playerHealth.DamagePlayerExternal(specialAttack.GetDamage(enemy), 5f, dir, false, "Enemy", DcFlags.BypassEvade | DcFlags.BossDamage);
			if (eAttack == EEnemyAttack.PoisonSpikeProjectiles) player.inventory.statusEffects.PoisonPlayer(8f);
		}
		numSpawned++;
		if (numSpawned >= numToSpawn) ReturnToPool();
	}
}
