using Assets.Scripts.Game.Combat.EnemySpecialAttacks.Implementations;
using Assets.Scripts.Actors.Player;
using UnityEngine;

public class EnemyGroundSlam : EnemySpecialAttackPrefab
{
	public GameObject hitEffect;

	public AudioSource hitSfx;

	private float finalRadius;

	protected override void Init()
	{
		if (hitEffect != null) hitEffect.SetActive(false);
		if (enemy == null || specialAttack == null) return;
		finalRadius = specialAttack.attackRadius;
		transform.position = enemy.GetFeetPosition() + enemy.transform.forward * finalRadius;
		if (EffectManager.Instance != null)
			circleWarning = EffectManager.Instance.WarningSphere(transform.position, finalRadius, specialAttack.attackChargeTime, SpawnHitEffect);
		if (hitEffect != null) hitEffect.transform.localScale = Vector3.one * finalRadius;
	}

	private void SpawnHitEffect()
	{
		if (hitEffect != null) hitEffect.SetActive(true);
		if (hitSfx != null && hitSfx.gameObject.activeInHierarchy) hitSfx.Play();
		var player = MyPlayer.Instance;
		if (GameManager.Instance != null && player != null && player.inventory != null && player.inventory.playerHealth != null &&
			enemy != null && specialAttack != null && Physics.CheckSphere(transform.position, finalRadius, GameManager.Instance.whatIsPlayer))
		{
			Vector3 direction = (player.transform.position - transform.position).normalized;
			player.inventory.playerHealth.DamagePlayerExternal(specialAttack.GetDamage(enemy), 20f, direction, false, "Enemy");
		}
		Invoke("ReturnToPool", .5f);
	}
}
