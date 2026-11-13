using UnityEngine;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Menu.Shop;

namespace Assets.Scripts.Player.Movement
{
	public class PlayerMovementValues
	{
		private const float defaultMoveSpeed = 2700f;

		private const float defaultSwimSpeed = 10f;

		public const float defaultMaxSpeed = 10f;

		private const float defaultSlideForce = 200f;

		private const float defaultAirDeceleration = 0.003f;

		private const float defaultExtraGravity = 11f;

		private bool inited;

		private ECharacter currentCharacter;

		private float counterMovement;

		public float moveSpeed { get; private set; }

		public float maxRunSpeed { get; private set; }

		public float airDeceleration { get; private set; }

		public float slideForce { get; private set; } = defaultSlideForce;

		public float extraGravity { get; private set; } = defaultExtraGravity;

		public float swimSpeed { get; private set; } = defaultSwimSpeed;

		private void Init(Rigidbody rb)
		{
			slideForce *= rb.mass;
			inited = true;
		}

		public void CreateMovement(Rigidbody rb, ECharacter character)
		{
			if (!inited) Init(rb);
			else if (currentCharacter == character) return;
			currentCharacter = character;
			moveSpeed = defaultMoveSpeed * rb.mass;
			slideForce = defaultSlideForce * rb.mass;
			maxRunSpeed = defaultMaxSpeed;
			airDeceleration = defaultAirDeceleration;
			counterMovement = GetCounterMovementMultiplier(character);
			swimSpeed = defaultSwimSpeed * rb.mass;
			extraGravity = GetGravity(rb, character);
		}

		private static float GetCounterMovementMultiplier(ECharacter character)
		{
			return character == ECharacter.Calcium ? 0.2f : character == ECharacter.TonyMcZoom ? 0.15f : 1f;
		}

		private static float GetMoveSpeedMultiplier(ECharacter character)
		{
			return 1f;
		}

		public float GetCounterMovementMultiplier(FrictionModifier.EFrictionSurface surface)
		{
			return counterMovement * (surface == FrictionModifier.EFrictionSurface.Ice ? 0.75f : 1f);
		}

		public static float GetSlowdownMultiplier(FrictionModifier.EFrictionSurface surface, ECharacter character)
		{
			if (character == ECharacter.Calcium) return 0.06f;
			if (character == ECharacter.TonyMcZoom) return 0f;
			return surface == FrictionModifier.EFrictionSurface.Ice ? 0.4f : 1f;
		}

		public float GetMoveSpeed(FrictionModifier.EFrictionSurface surface, bool grounded)
		{
			float speedMultiplier = Mathf.Clamp(1f + (PlayerStats.GetStat(EStat.MoveSpeedMultiplier) - 1.5f) * 0.25f, 1f, 4f);
			return moveSpeed * (surface == FrictionModifier.EFrictionSurface.Ice ? 0.4f : 1f) * speedMultiplier;
		}

		public float GetGravity(Rigidbody rb, ECharacter character)
		{
			if (character == ECharacter.Vlad) return 0f;
			return character == ECharacter.Spaceman ? rb.mass * -11f * 0.75f : rb.mass * defaultExtraGravity;
		}

		public float GetMaxSpeed()
		{
			return maxRunSpeed * PlayerStats.GetStat(EStat.MoveSpeedMultiplier);
		}

		public float GetMaxSpeedNoMultiplier()
		{
			return maxRunSpeed;
		}
	}
}
