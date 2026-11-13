using System;
using System.Collections.Generic;
using UnityEngine;

namespace RetroArsenal
{
	[Serializable]
	public class TargetEffects
	{
		public GameObject hitParticle;

		public GameObject respawnParticle;

		public List<GameObject> deathParticles;

		public AudioClip destroySound;

		public AudioClip respawnSound;
	}
}
