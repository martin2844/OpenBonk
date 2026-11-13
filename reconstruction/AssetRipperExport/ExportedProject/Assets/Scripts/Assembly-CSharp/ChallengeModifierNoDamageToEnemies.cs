using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using UnityEngine;

[CreateAssetMenu(menuName = "Me/Progression/Challenge/ChallengeModifiers/No damage to enemies", order = 1)]
public class ChallengeModifierNoDamageToEnemies : ChallengeModifier
{
	public override void Init(ChallengeData challengeData)
	{
		Enemy.A_Damage += OnDamageEnemy;
	}

	public override void Cleanup()
	{
		Enemy.A_Damage -= OnDamageEnemy;
	}

	private void OnDamageEnemy(Enemy arg1, DamageContainer arg2)
	{
		MyPlayer.Instance.inventory.playerHealth.KillPlayer();
	}
}
