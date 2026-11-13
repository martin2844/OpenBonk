using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemGhost : ItemBase
	{
		public const int maxGhosts = 100;

		private int numGhosts;

		private int numGhostsPerAmount = 6;

		private string damageSource = EItem.Ghost.ToString();

		protected override void OnInitOrAmountChanged()
		{
			numGhosts = amount * numGhostsPerAmount;
		}

		private void OnInteracted(BaseInteractable interactable, bool success)
		{
			if (!success) return;
			for (int i = 0; i < numGhosts; i++) SpawnGhost();
		}

		private void SpawnGhost()
		{
			if (EffectManager.Instance != null && MyPlayer.Instance != null)
				EffectManager.Instance.SpawnGhostProjectile(GetDamage(), GetDuration(), damageSource);
		}

		private float GetDuration()
		{
			return 30f * PlayerStats.GetStat(EStat.DurationMultiplier);
		}

		private float GetDamage()
		{
			return 2f * MyPlayer.Instance.GetBaseDamage();
		}

		public override void Init()
		{
			DetectInteractables.A_Interacted += OnInteracted;
		}

		public override void Cleanup()
		{
			DetectInteractables.A_Interacted -= OnInteracted;
		}

		public override void Tick()
		{
		}

		public override void PreAttack(DamageContainer dc, StatComponents itemAttackModifier)
		{
		}

		public override bool HasPreAttackProc()
		{
			return false;
		}

		public override void ProcOnHitEffects(DamageContainer dc)
		{
		}

		public override bool HasOnHitEffectProc()
		{
			return false;
		}

		public ItemGhost(ItemInventory itemInventoryRef)
			: base(itemInventoryRef)
		{
		}
	}
}
