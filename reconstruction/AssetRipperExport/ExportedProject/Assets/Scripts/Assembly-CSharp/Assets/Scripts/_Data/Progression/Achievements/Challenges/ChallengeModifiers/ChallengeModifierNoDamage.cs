using UnityEngine;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;

namespace Assets.Scripts._Data.Progression.Achievements.Challenges.ChallengeModifiers
{
	[CreateAssetMenu(menuName = "Me/Progression/Challenge/ChallengeModifiers/1 HP", order = 1)]
	public class ChallengeModifierNoDamage : ChallengeModifier
	{
		private bool hasBeenCalled;

		private bool hasBeenKilled;

		public override void Init(ChallengeData challengeData)
		{
			PlayerHealth.A_DamagePlayerCalled += OnDamagePlayer;
		}

		public override void Cleanup()
		{
			PlayerHealth.A_DamagePlayerCalled -= OnDamagePlayer;
		}

		private void OnDamagePlayer()
		{
			if (hasBeenCalled) return;
			var effects = MyPlayer.Instance.inventory.statusEffects;
			if (!effects.HasStatusEffect(EStatusEffect.Shield) && !effects.HasStatusEffect(EStatusEffect.TimeFreeze) && !effects.HasStatusEffect(EStatusEffect.Invulnerability))
				hasBeenCalled = true;
		}

		public override void Tick()
		{
			if (hasBeenCalled && !hasBeenKilled) MyPlayer.Instance.inventory.playerHealth.KillPlayer();
		}
	}
}
