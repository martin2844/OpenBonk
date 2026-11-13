using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Spawning;
using UnityEngine;

namespace Assets.Scripts.Game.Combat.EnemySpecialAttacks.Implementations
{
	public class EnemySpecialAttackPrefabSingle : EnemySpecialAttackPrefab
	{
		public bool grounded;
		public bool predictive;

		protected override void Init()
		{
			MyPlayer player = MyPlayer.Instance;
			Vector3 position = player.transform.position;
			if (predictive) position += player.playerMovement.GetVelocity() * specialAttack.attackChargeTime;
			if (grounded)
			{
				RaycastHit[] hits = Physics.RaycastAll(position + Vector3.up * 99f, Vector3.down, 999f, GameManager.Instance.whatIsGround);
				if (hits.Length > 0) position = SpawnPositions.FindHitClosestToPlayerY(hits, out _).point;
			}
			transform.position = position;
			CreateWarningSphere(position, SpawnHitEffect);
		}

		private void SpawnHitEffect()
		{
			circleWarning = null;
			GameObject effect = GetEffectPrefab();
			if (effect != null)
			{
				effect.transform.position = transform.position;
				effect.transform.localScale = Vector3.one * specialAttack.attackRadius;
			}
			if (Physics.CheckSphere(transform.position, specialAttack.attackRadius, GameManager.Instance.whatIsPlayer))
			{
				MyPlayer player = MyPlayer.Instance;
				player.inventory.playerHealth.DamagePlayerExternal(specialAttack.GetDamage(enemy), 25f, Vector3.up, false, "Enemy", GetDamageFlags());
				if (eAttack == EEnemyAttack.PoisonSpikes) player.inventory.statusEffects.PoisonPlayer(8f);
			}
			Invoke(nameof(ReturnToPool), 2f);
		}
	}
}
