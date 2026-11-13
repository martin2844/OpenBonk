using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Game.Combat.EnemySpecialAttacks.Implementations;
using Assets.Scripts.Game.Spawning;
using Assets.Scripts.Utility;
using UnityEngine;

public class EnemySpecialAttackCactusProjectile : EnemySpecialAttackPrefab
{
	public bool grounded;
	public bool predictive;
	private float timer;
	private Vector3 impactPos;
	private Vector3 startPos;
	public TrailRenderer trailRenderer;
	private float arcHeight = 20f;

	protected override void Init()
	{
		transform.position = enemy.GetCenterPosition();
		startPos = transform.position;
		timer = 0f;
		trailRenderer.Clear();
		MyPlayer player = MyPlayer.Instance;
		impactPos = player.transform.position;
		if (predictive) impactPos += player.playerMovement.GetVelocity() * specialAttack.attackChargeTime;
		if (grounded)
		{
			RaycastHit[] hits = Physics.RaycastAll(impactPos + Vector3.up * 999f, Vector3.down, 9999f, GameManager.Instance.whatIsGround);
			impactPos = SpawnPositions.FindHitClosestToPlayerY(hits, out _).point;
		}
		CreateWarningSphere(impactPos, SpawnHitEffect);
		transform.position = impactPos;
	}

	private void Update()
	{
		if (timer >= 1f) return;
		timer += MyTime.deltaTime;
		float t = Mathf.Clamp01(timer / specialAttack.attackChargeTime);
		transform.position = Vector3.Lerp(startPos, impactPos, t) + Vector3.up * (4f * t * (1f - t) * arcHeight);
	}

	private void SpawnHitEffect()
	{
		circleWarning = null;
		if (Physics.CheckSphere(transform.position, specialAttack.attackRadius, GameManager.Instance.whatIsPlayer))
		{
			MyPlayer.Instance.inventory.playerHealth.DamagePlayerExternal(specialAttack.GetDamage(enemy), 4f, Vector3.up, false, "Enemy", DcFlags.None, EDamageEffect.None, enemy);
		}
		ReturnToPool();
	}
}
