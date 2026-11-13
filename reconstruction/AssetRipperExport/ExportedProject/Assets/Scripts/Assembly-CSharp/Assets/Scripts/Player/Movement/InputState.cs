namespace Assets.Scripts.Player.Movement
{
	public struct InputState
	{
		public float moveHorizontal;

		public float moveVertical;

		public float rotationHorizontal;

		public float rotationVertical;

		public bool jumping;

		public bool crouching;

		public bool holdingJump;

		public bool holdingWallrun;

		public InputState(float mH, float mV, float rH, float rV, bool ju, bool cr, bool holdingJump, bool holdingWallrun)
		{
			moveHorizontal = mH;
			moveVertical = mV;
			rotationHorizontal = rH;
			rotationVertical = rV;
			jumping = ju;
			crouching = cr;
			this.holdingJump = holdingJump;
			this.holdingWallrun = holdingWallrun;
		}
	}
}
