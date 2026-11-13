using System;
using UnityEngine;

namespace Assets.Scripts._Data.Hats
{
	[Serializable]
	public class HatOrientation
	{
		public ECharacter character;

		public EMeshForHat meshForHat;

		public Vector3 pos;

		public Vector3 rot;

		public Vector3 scale;
	}
}
