using System;

namespace Assets.Scripts.Movement
{
	[Flags]
	public enum EMovementState
	{
		Idle = 1,
		Walking = 2,
		Crouching = 4,
		Sliding = 8,
		Airborne = 0x10,
		Wallrunning = 0x20,
		CategoryCrouched = 0xC,
		CategoryFootstepNoise = 0x22
	}
}
