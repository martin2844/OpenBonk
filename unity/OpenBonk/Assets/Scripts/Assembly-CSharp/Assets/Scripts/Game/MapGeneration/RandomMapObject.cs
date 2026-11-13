using System;
using UnityEngine;

namespace Assets.Scripts.Game.MapGeneration
{
	[Serializable]
	public class RandomMapObject
	{
		public int amount;

		public int maxAmount;

		public float checkRadius;

		public float scaleMin;

		public float scaleMax;

		public float maxSlopeAngle;

		public float upOffset;

		public GameObject[] prefabs;

		public Vector3 randomRotationVector;

		public bool alignWithNormal;

		public int GetAmount()
		{
			return maxAmount == 0 ? amount : global::Utility.MyRandom.random.Next(amount, maxAmount + 1);
		}
	}
}
